## Phase 1 – Voice Modal & Audio Capture

1. **Add Route & Register Modal**  
   In **`MauiProgram.cs`**, register your new page/VM pair:
   ```csharp
   // after existing AddTransientWithShellRoute calls:
   builder.Services
     .AddTransientWithShellRoute<Pages.VoiceModalPage, PageModels.VoiceModalPageModel>("voice");
   ```

2. **Create `VoiceModalPage`**  
   - **File:** `Pages/VoiceModalPage.xaml` + `.xaml.cs`  
   - Namespace: `Telepathic.Pages`  
   - Minimal XAML skeleton:
     ```xml
     <!-- Pages/VoiceModalPage.xaml -->
     <ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                  xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
                  x:Class="Telepathic.Pages.VoiceModalPage"
                  Title="Dictate Your Day">
       <ContentPage.BindingContext>
         <pageModels:VoiceModalPageModel />
       </ContentPage.BindingContext>
       <Grid Padding="20">
         <!-- Recording Phase -->
         <StackLayout x:Name="RecordingView"
                      IsVisible="{Binding Phase, Converter={StaticResource PhaseToRecordingBool}}">
           <Label Text="Tap to Dictate Your Day" 
                  FontSize="Large" 
                  HorizontalOptions="Center" />
           <Button Text="{Binding RecordButtonText}"
                   Command="{Binding ToggleRecordingCommand}"
                   HorizontalOptions="Center" />
           <ActivityIndicator IsRunning="{Binding IsBusy}" />
         </StackLayout>
         <!-- Review Phase -->
         <ScrollView x:Name="ReviewView"
                     IsVisible="{Binding Phase, Converter={StaticResource PhaseToReviewBool}}">
           <!-- Projects & Tasks list + Save button (see Phase 4) -->
         </ScrollView>
       </Grid>
     </ContentPage>
     ```
   - **Code‑behind:** in `VoiceModalPage.xaml.cs` just `InitializeComponent();`

3. **Define & Register `IAudioService`**  
   You may already have a similar service—if not, add:
   ```csharp
   // Shared interface
   namespace Telepathic.Services;
   public interface IAudioService
   {
     Task StartRecordingAsync(CancellationToken ct);
     Task StopRecordingAsync();
     string RecordedFilePath { get; }
   }
   // Concrete impl (using Plugin.Maui.Audio)
   namespace Telepathic.Services;
   public class AudioService : IAudioService { /* …as in plan…*/ }
   ```
   Then in **`MauiProgram.cs`**:
   ```csharp
   builder.Services.AddSingleton<IAudioService, AudioService>();
   ```

4. **Implement `VoiceModalPageModel` (Recording)**  
   ```csharp
   // PageModels/VoiceModalPageModel.cs
   namespace Telepathic.PageModels;
   public partial class VoiceModalPageModel : ObservableObject
   {
     readonly IAudioService _audio;
     readonly ITranscriptionService _transcriber;
     readonly IChatClient _chat;

     [ObservableProperty] bool isRecording;
     [ObservableProperty] bool isBusy;
     [ObservableProperty] VoicePhase phase = VoicePhase.Recording;
     [ObservableProperty] string recordButtonText = "🎤 Record";

     public VoiceModalPageModel(
       IAudioService audio,
       ITranscriptionService transcriber,
       IChatClient chat)
     {
       _audio = audio;
       _transcriber = transcriber;
       _chat = chat;
       ToggleRecordingCommand = new AsyncRelayCommand(ToggleRecordingAsync);
     }

     public IAsyncRelayCommand ToggleRecordingCommand { get; }

     private async Task ToggleRecordingAsync()
     {
       if (!IsRecording)
       {
         await _audio.StartRecordingAsync(CancellationToken.None);
         IsRecording = true;
         RecordButtonText = "⏹ Stop";
       }
       else
       {
         await _audio.StopRecordingAsync();
         IsRecording = false;
         RecordButtonText = "🎤 Record";
         Phase = VoicePhase.Transcribing;
         await TranscribeAsync();
       }
     }
   }

   public enum VoicePhase { Recording, Transcribing, Reviewing }
   ```

### Validation #1  
- ✔️ Calling `Shell.Current.GoToAsync("voice")` brings up the modal.  
- ✔️ Inside the modal you can start/stop recording, and `AudioService.RecordedFilePath` points to a valid file (play back to confirm).

---

## Phase 2 – Client‑Side Whisper Transcription

1. **Define & Register `ITranscriptionService`**  
   ```csharp
   // Services/ITranscriptionService.cs
   namespace Telepathic.Services;
   public interface ITranscriptionService
   {
     Task<string> TranscribeAsync(string audioFilePath, CancellationToken ct);
   }
   // Services/WhisperTranscriptionService.cs
   public class WhisperTranscriptionService : ITranscriptionService
   {
     readonly IChatClient _chat;  // reuse IChatClient via OpenAIClient.ASChatClient()

     public WhisperTranscriptionService(IChatClient chat) => _chat = chat;

     public async Task<string> TranscribeAsync(string path, CancellationToken ct)
     {
       await using var stream = File.OpenRead(path);
       var result = await _chat
         .AsAudioClient()                                // extension to get audio APIs
         .Transcriptions.CreateTranscriptionAsync(
           stream, model: "whisper-1", ct);
       return result.Text.Trim();
     }
   }
   ```
   Then in **`MauiProgram.cs`**:
   ```csharp
   builder.Services.AddSingleton<ITranscriptionService, WhisperTranscriptionService>();
   ```

2. **Hook into VM**  
   ```csharp
   // inside VoiceModalPageModel, add:
   [ObservableProperty] string transcript;

   private async Task TranscribeAsync()
   {
     IsBusy = true;
     Transcript = await _transcriber.TranscribeAsync(_audio.RecordedFilePath, CancellationToken.None);
     IsBusy = false;
     Phase = VoicePhase.Reviewing;
   }
   ```
   *(Bind a hidden `Label` to `Transcript` if you need to debug.)*

### Validation #2  
- ✔️ After stopping recording, you see **Phase=Reviewing** and `Transcript` matches your voice.  
- ✔️ Any errors (bad file, network issues) show an alert via your existing `ModalErrorHandler`.

---

## Phase 3 – GPT Task Extraction

1. **Extend `VoiceModalPageModel`**  
   ```csharp
   [ObservableProperty] ObservableCollection<ProjectVm> projects;
   [ObservableProperty] ObservableCollection<TaskVm> standaloneTasks;

   private async Task TranscribeAsync()  // after setting Phase=Reviewing
   {
     // … transcription code …
     await ExtractTasksAsync();
   }

   private async Task ExtractTasksAsync()
   {
     IsBusy = true;
     var prompt = $@"
   You are an AI that extracts actionable to-do items from a transcript.
   Return JSON: {{ ""projects"": [ {{ ""name"": string, ""tasks"": [string] }} ], ""tasks"": [string] }}
   Transcript:
   {Transcript}";
     var extraction = await _chat.GetResponseAsync<ExtractionResult>(prompt);
     Projects = new ObservableCollection<ProjectVm>(
       extraction.Projects.Select(p => new ProjectVm(p.Name, p.Tasks)));
     StandaloneTasks = new ObservableCollection<TaskVm>(
       extraction.Tasks.Select(t => new TaskVm(t)));
     IsBusy = false;
   }
   ```

2. **Define `ExtractionResult`, `ProjectVm`, `TaskVm`**  
   ```csharp
   // PageModels/ExtractionResult.cs
   public class ExtractionResult
   {
     [JsonPropertyName("projects")]
     public List<ProjectDto> Projects { get; set; }
     [JsonPropertyName("tasks")]
     public List<string> Tasks { get; set; }
   }
   public class ProjectDto { public string Name { get; set; } public List<string> Tasks { get; set; } }
   // PageModels/ProjectVm.cs
   public record ProjectVm(string Name, List<string> Tasks);
   // PageModels/TaskVm.cs
   public record TaskVm(string Description);
   ```

3. **Unit Tests**  
   - Add tests in your existing test project (if any) asserting that given a small transcript, `ExtractTasksAsync` yields correct `ExtractionResult`.

### Validation #3  
- ✔️ On device: after transcription, the modal’s `Projects` and `StandaloneTasks` collections are populated correctly.

---

## Phase 4 – Review & Edit in the Modal

1. **Expand `VoiceModalPage.xaml`** to show the review lists when `Phase=Reviewing`:
   ```xml
   <!-- inside ReviewView ScrollView -->
   <VerticalStackLayout Spacing="12">
     <!-- Projects -->
     <CollectionView ItemsSource="{Binding Projects}">
       <CollectionView.ItemTemplate>
         <DataTemplate>
           <Frame Padding="10" CornerRadius="8">
             <VerticalStackLayout>
               <Entry Text="{Binding Name}" />
               <CollectionView ItemsSource="{Binding Tasks}">
                 <CollectionView.ItemTemplate>
                   <DataTemplate>
                     <Grid ColumnDefinitions="*,Auto">
                       <Entry Text="{Binding .}" Grid.Column="0" />
                       <Button Text="🗑" 
                               Command="{Binding Source={RelativeSource AncestorType={x:Type pageModels:VoiceModalPageModel}}, 
                                                 Path=DeleteTaskCommand}" 
                               CommandParameter="{Binding .}" 
                               Grid.Column="1" />
                     </Grid>
                   </DataTemplate>
                 </CollectionView.ItemTemplate>
               </CollectionView>
             </VerticalStackLayout>
           </Frame>
         </DataTemplate>
       </CollectionView.ItemTemplate>
     </CollectionView>

     <!-- Standalone Tasks -->
     <CollectionView ItemsSource="{Binding StandaloneTasks}">
       <!-- similar template as above -->
     </CollectionView>

     <Button Text="Save" Command="{Binding SaveCommand}" />
   </VerticalStackLayout>
   ```

2. **Add Commands in VM**  
   ```csharp
   public IRelayCommand<string> DeleteTaskCommand { get; }
   public IRelayCommand SaveCommand { get; }

   public VoiceModalPageModel(...)
   {
     // …
     DeleteTaskCommand = new RelayCommand<string>(OnDeleteTask);
     SaveCommand = new AsyncRelayCommand(OnSaveAsync);
   }

   void OnDeleteTask(string description)
   {
     var task = StandaloneTasks.FirstOrDefault(t => t.Description == description);
     if (task != null) StandaloneTasks.Remove(task);
     // Also search within each ProjectVm.Tasks if needed
   }

   async Task OnSaveAsync()
   {
     // Map back to your Models and use existing ProjectRepository/TaskRepository
     await ProjectRepository.AddOrUpdateVoiceProjectsAsync(
       Projects.Select(p => new Project { Title = p.Name, /* … */}).ToList(),
       StandaloneTasks.Select(t => new TaskModel { Title = t.Description }).ToList());
     await Shell.Current.GoToAsync("..");
   }
   ```

### Validation #4  
- ✔️ Review view shows editable entries for each project & task.  
- ✔️ Deleting items updates the UI.  
- ✔️ “Save” persists data (verify in `ProjectListPageModel` / repository) and closes the modal.

---

## Phase 5 – Error Handling & Edge Cases

1. **Transcription / Extraction Errors**  
   - Wrap calls in `try/catch` and route exceptions to your **`ModalErrorHandler`** service to show a popup with Retry/Cancel.

2. **No Tasks Detected**  
   - If `ExtractionResult.Projects` and `.Tasks` are both empty, show a friendly message with options to “Re‑Record” or “Cancel.”

3. **Permissions & Silences**  
   - If mic permission is denied, display an alert and a “Enter Manually” fallback (navigating to `TaskDetailPage`).

4. **Telemetry**  
   - Inject `ILogger<VoiceModalPageModel>` and log durations for recording, transcription, extraction, and any failures.

### Validation #5  
- ✔️ Deny mic permission → fallback prompt appears.  
- ✔️ Simulate API failures → user sees retry UI.  
- ✔️ Empty dictation → “No tasks found” message shows.

---

## Phase 6 – Final QA & Deployment

1. **Cross‑Platform Testing**  
   - Verify on **Android, iOS, macOS, Windows** that the modal opens, records, transcribes, extracts, reviews, and saves exactly as expected.

2. **Performance & Cost**  
   - Use your `ILogger` telemetry to measure average API times.  
   - Check that transcription + extraction latency stays under ~5 s for a 30 s dictation.

3. **Beta Roll‑out**  
   - Deploy to internal testers or TestFlight. Gather feedback on accuracy and UX.

4. **Documentation**  
   - Update `README.md` with “Voice Dictation” instructions.  
   - Document any new preferences (e.g. you may store a toggle in `Preferences.Default` to enable/disable voice).

5. **Release**  
   - Merge `voice-feature` branch into `main`, tag, and ship.  

### Validation #6  
- ✔️ All automated tests and manual checks pass.  
- ✔️ Beta feedback is positive and no major bugs remain.