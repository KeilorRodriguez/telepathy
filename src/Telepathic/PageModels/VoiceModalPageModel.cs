using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using Telepathic.Services;

namespace Telepathic.PageModels;

public enum VoicePhase { Recording, Transcribing, Reviewing }

public partial class VoiceModalPageModel : ObservableObject
{
    readonly IAudioManager _audioManager;
    readonly IAudioService _audio;

    IAudioSource _audioSource = null;

    IAudioRecorder _recorder;
    readonly ITranscriptionService _transcriber;
    readonly ModalErrorHandler _errorHandler;
    readonly IChatClient _chat;
    readonly ILogger<VoiceModalPageModel> _logger;

    [ObservableProperty] bool isRecording;
    [ObservableProperty] bool isBusy;
    [ObservableProperty] VoicePhase phase = VoicePhase.Recording;
    [ObservableProperty] string recordButtonText = "🎤 Record";
    [ObservableProperty] string transcript = string.Empty;

    // Extracted projects and tasks
    [ObservableProperty] ObservableCollection<ProjectVm> projects = new();
    [ObservableProperty] ObservableCollection<TaskVm> standaloneTasks = new();

    // Priority options for pickers
    public ObservableCollection<int?> PriorityOptions { get; } = new() { null, 1, 2, 3, 4, 5 };

    private readonly ProjectRepository _projectRepository;
    private readonly TaskRepository _taskRepository;

    // Stopwatch for measuring performance
    private Stopwatch _stopwatch = new();

    public VoiceModalPageModel(
        IAudioManager audioManager,
        ITranscriptionService transcriber,
        ModalErrorHandler errorHandler,
        IChatClient chat,
        ProjectRepository projectRepository,
        TaskRepository taskRepository,
        ILogger<VoiceModalPageModel> logger)
    {
        // _audio = audio;
        _audioManager = audioManager;
        _transcriber = transcriber;
        _errorHandler = errorHandler;
        _chat = chat;
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _logger = logger;

        ToggleRecordingCommand = new AsyncRelayCommand(ToggleRecordingAsync);
        DeleteProjectCommand = new RelayCommand<ProjectVm>(DeleteProject);
        DeleteTaskCommand = new RelayCommand<TaskVm>(DeleteTask);
        DeleteStandaloneTaskCommand = new RelayCommand<TaskVm>(DeleteStandaloneTask);
        ReRecordCommand = new AsyncRelayCommand(ReRecordAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);

        _logger.LogInformation("Voice Modal Page Model initialized");
    }

    public IAsyncRelayCommand ToggleRecordingCommand { get; }
    public IRelayCommand<ProjectVm> DeleteProjectCommand { get; }
    public IRelayCommand<TaskVm> DeleteTaskCommand { get; }
    public IRelayCommand<TaskVm> DeleteStandaloneTaskCommand { get; }
    public IAsyncRelayCommand ReRecordCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }

    private async Task ToggleRecordingAsync()
    {
        if (!IsRecording)
        {
            try
            {
                // Check for microphone permissions first
                _logger.LogInformation("Checking microphone permissions");
                var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    _logger.LogInformation("Requesting microphone permissions");
                    status = await Permissions.RequestAsync<Permissions.Microphone>();
                    if (status != PermissionStatus.Granted)
                    {
                        _logger.LogWarning("Microphone permission denied");
                        // Permission denied - offer fallback
                        bool navigateToManual = await Shell.Current.DisplayAlert(
                            "Microphone Access Denied",
                            "Voice recording requires microphone access. Would you like to enter tasks manually instead?",
                            "Enter Manually", "Cancel");

                        if (navigateToManual)
                        {
                            _logger.LogInformation("User chose manual task entry after permission denial");
                            // Navigate to manual task entry
                            await Shell.Current.GoToAsync("task");
                            await Shell.Current.Navigation.PopModalAsync(); // Close this modal
                        }
                        return;
                    }
                }

                _logger.LogInformation("Starting voice recording");
                _stopwatch.Restart();
                _recorder = _audioManager.CreateRecorder();
                await _recorder.StartAsync();
                // await _audio.StartRecordingAsync(CancellationToken.None);
                IsRecording = true;
                RecordButtonText = "⏹ Stop";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting recording");
                _errorHandler.HandleError(ex);
            }
        }
        else
        {
            try
            {
                _audioSource = await _recorder.StopAsync();
                IsRecording = false;
                RecordButtonText = "🎤 Record";

                // Log recording duration
                _stopwatch.Stop();
                _logger.LogInformation("Voice recording completed in {RecordingDuration}ms", _stopwatch.ElapsedMilliseconds);

                Phase = VoicePhase.Transcribing;

                // Now we'll actually transcribe the audio!
                await TranscribeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping recording");
                _errorHandler.HandleError(ex);
                IsRecording = false;
                RecordButtonText = "🎤 Record";
            }
        }
    }

    private async Task TranscribeAsync()
    {
        try
        {
            IsBusy = true;
            
            // Create a temporary file path to save our recording
            string audioFilePath = Path.Combine(FileSystem.CacheDirectory, $"recording_{DateTime.Now:yyyyMMddHHmmss}.wav");
            
            _logger.LogInformation("Saving audio to temporary file at {FilePath}", audioFilePath);
            
            // Save the audio source to a file
            if (_audioSource != null)
            {
                await using (var fileStream = File.Create(audioFilePath))
                {
                    var audioStream = _audioSource.GetAudioStream();
                    await audioStream.CopyToAsync(fileStream);
                }
                
                _logger.LogInformation("Audio successfully saved to file");
            }
            else
            {
                _logger.LogError("Audio source is null - no recording available");
                throw new InvalidOperationException("No recording is available to transcribe");
            }
            
            // Verify the file exists
            if (!File.Exists(audioFilePath))
            {
                _logger.LogError("Recorded audio file not found at {FilePath}", audioFilePath);
                throw new FileNotFoundException("Recorded audio file not found");
            }

            // Transcribe the audio using Whisper
            _logger.LogInformation("Starting audio transcription");
            _stopwatch.Restart();
            Transcript = await _transcriber.TranscribeAsync(audioFilePath, CancellationToken.None);
            _stopwatch.Stop();
            _logger.LogInformation("Audio transcription completed in {TranscriptionDuration}ms, length: {TranscriptLength}",
                _stopwatch.ElapsedMilliseconds, Transcript?.Length ?? 0);

            // Extract projects and tasks from the transcript
            await ExtractTasksAsync();

            // After successful transcription and extraction, move to review phase
            Phase = VoicePhase.Reviewing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed");
            _errorHandler.HandleError(ex);
            // Return to recording phase if transcription fails
            Phase = VoicePhase.Recording;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Extract projects and tasks from the transcript using AI
    /// </summary>
    private async Task ExtractTasksAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Transcript) || _chat == null)
            {
                _logger.LogWarning("Cannot extract tasks: transcript is empty or chat client is null");
                return;
            }

            // Clear previous extraction results
            Projects.Clear();
            StandaloneTasks.Clear();

            _logger.LogInformation("Starting task extraction from transcript");
            _stopwatch.Restart();

            // Create a prompt that will extract projects and tasks from the transcript
            var prompt = $@"
Extract projects and tasks from this voice memo transcript. 
Analyze the text to identify:
1. Projects with their associated tasks
2. Standalone tasks not associated with any project
3. Any mentioned due dates in YYYY-MM-DD format
4. Any mentioned priority levels (1-5, where 5 is highest)

Return the data in this format:
{{
  ""projects"": [
    {{
      ""name"": ""Project Name"",
      ""tasks"": [
        {{
          ""title"": ""Task description"",
          ""dueDate"": ""YYYY-MM-DD"",
          ""priority"": 3
        }}
      ]
    }}
  ],
  ""standaloneTasks"": [
    {{
      ""title"": ""Task description"",
      ""dueDate"": null,
      ""priority"": null
    }}
  ]
}}

Here's the transcript: {Transcript}";

            // Get response from the AI service
            var response = await _chat.GetResponseAsync<ExtractionResponse>(prompt);

            _stopwatch.Stop();
            _logger.LogInformation("Task extraction completed in {ExtractionDuration}ms", _stopwatch.ElapsedMilliseconds);

            if (response?.Result != null)
            {
                // Add all projects and tasks to observable collections
                foreach (var project in response.Result.Projects)
                {
                    Projects.Add(project);
                }

                foreach (var task in response.Result.StandaloneTasks)
                {
                    StandaloneTasks.Add(task);
                }

                _logger.LogInformation("Extracted {ProjectCount} projects and {TaskCount} standalone tasks",
                    Projects.Count, StandaloneTasks.Count);

                // Check if no projects or tasks were detected
                if (Projects.Count == 0 && StandaloneTasks.Count == 0)
                {
                    _logger.LogWarning("No projects or tasks detected in transcript");
                    await Shell.Current.DisplayAlert(
                        "No Tasks Detected",
                        "No projects or tasks were detected in your voice memo. Would you like to try again?",
                        "OK");

                    // Return to recording phase
                    await ReRecordAsync();
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task extraction failed");
            _errorHandler.HandleError(ex);
        }
    }

    /// <summary>
    /// Delete a project and its tasks from the list
    /// </summary>
    private void DeleteProject(ProjectVm? project)
    {
        if (project != null)
        {
            Projects.Remove(project);
            _logger.LogInformation("Deleted project: {ProjectName}", project.Name);
        }
    }

    /// <summary>
    /// Delete a task from its project
    /// </summary>
    private void DeleteTask(TaskVm? task)
    {
        if (task == null) return;

        foreach (var project in Projects)
        {
            if (project.Tasks.Contains(task))
            {
                project.Tasks.Remove(task);
                _logger.LogInformation("Deleted task: {TaskTitle} from project: {ProjectName}",
                    task.Title, project.Name);
                break;
            }
        }
    }

    /// <summary>
    /// Delete a standalone task
    /// </summary>
    private void DeleteStandaloneTask(TaskVm? task)
    {
        if (task != null)
        {
            StandaloneTasks.Remove(task);
            _logger.LogInformation("Deleted standalone task: {TaskTitle}", task.Title);
        }
    }

    /// <summary>
    /// Start the recording process over
    /// </summary>
    private async Task ReRecordAsync()
    {
        _logger.LogInformation("Re-starting recording process");

        // Reset everything back to initial state
        Phase = VoicePhase.Recording;
        Transcript = string.Empty;
        Projects.Clear();
        StandaloneTasks.Clear();

        // Wait a moment to ensure UI updates
        await Task.Delay(100);
    }

    /// <summary>
    /// Save all projects and tasks
    /// </summary>
    private async Task SaveAsync()
    {
        try
        {
            _logger.LogInformation("Starting save operation for voice memo");
            _stopwatch.Restart();
            IsBusy = true;

            int projectCount = 0;
            int taskCount = 0;

            // Save each project and its tasks
            foreach (var projectVm in Projects)
            {
                // Create a new project
                var project = new Models.Project
                {
                    Name = projectVm.Name,
                    Description = $"Created from voice memo: {DateTime.Now:g}"
                };

                // Save the project to get its ID
                await _projectRepository.SaveItemAsync(project);
                projectCount++;

                // Save each task associated with this project
                foreach (var taskVm in projectVm.Tasks)
                {
                    var task = new Models.ProjectTask
                    {
                        Title = taskVm.Title,
                        ProjectID = project.ID,
                        DueDate = taskVm.DueDate,
                        Priority = taskVm.Priority ?? 0
                    };

                    await _taskRepository.SaveItemAsync(task);
                    taskCount++;
                }
            }

            // Save standalone tasks
            foreach (var taskVm in StandaloneTasks)
            {
                var task = new Models.ProjectTask
                {
                    Title = taskVm.Title,
                    DueDate = taskVm.DueDate,
                    Priority = taskVm.Priority ?? 0
                };

                await _taskRepository.SaveItemAsync(task);
                taskCount++;
            }

            _stopwatch.Stop();
            _logger.LogInformation("Voice memo saved successfully: {ProjectCount} projects and {TaskCount} tasks in {SaveDuration}ms",
                projectCount, taskCount, _stopwatch.ElapsedMilliseconds);

            // Close the modal
            await Shell.Current.Navigation.PopModalAsync();

            // Notify the user that everything was saved
            await Shell.Current.DisplayAlert("Success", "Your voice memo has been saved as projects and tasks.", "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving voice memo data");
            _errorHandler.HandleError(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        _logger.LogInformation("Navigating back from voice modal");
        await Shell.Current.GoToAsync("..");
    }
}
