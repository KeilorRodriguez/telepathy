using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Telepathic.Models;
using Telepathic.Services;

namespace Telepathic.PageModels;

public enum PhotoPhase { Analyzing, Reviewing }

public partial class PhotoPageModel : ObservableObject, IProjectTaskPageModel, IQueryAttributable
{
    private readonly ProjectRepository _projectRepository;
    private readonly TaskRepository _taskRepository;
    private readonly IChatClientService _chatClientService;
    private readonly ModalErrorHandler _errorHandler;
    private readonly ILogger<PhotoPageModel> _logger;
    private readonly Stopwatch _stopwatch = new();

    private FileResult? _fileResult;
    
    [ObservableProperty] private string _imageSource;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private PhotoPhase _phase = PhotoPhase.Analyzing;
    
    // Status indicator properties
    [ObservableProperty] private bool _isAnalyzingContext = true;
    [ObservableProperty] private string _analysisStatusTitle = "Processing Photo";
    [ObservableProperty] private string _analysisStatusDetail = "Preparing to analyze your image...";
    
    // Extracted projects and tasks
    [ObservableProperty] private List<Project> _projects = new();

    public IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand => throw new NotImplementedException();

    public PhotoPageModel(
        ProjectRepository projectRepository,
        TaskRepository taskRepository,
        IChatClientService chatClientService,
        ModalErrorHandler errorHandler,
        ILogger<PhotoPageModel> logger)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _chatClientService = chatClientService;
        _errorHandler = errorHandler;
        _logger = logger;
    }
    
    [RelayCommand]
    private async Task PageAppearing()
    {
        if (_fileResult != null)
        {
            // Load the image from the file result
            // ImageSource = _fileResult.FullPath;
            if (_fileResult != null)
            {
                // save the file into local storage
                ImageSource = Path.Combine(FileSystem.CacheDirectory, _fileResult.FileName);

                using Stream sourceStream = await _fileResult.OpenReadAsync();
                using FileStream localFileStream = File.OpenWrite(ImageSource);

                await sourceStream.CopyToAsync(localFileStream);
            }
            _fileResult = null; // Clear the file result to avoid reloading
        
            await AnalyzeImageAsync();
        }
        else
        {
            _errorHandler.HandleError(new Exception("No image was provided to analyze"));
            await GoBackAsync();
        }
    }
    
    private async Task AnalyzeImageAsync()
    {
        try
        {
            IsBusy = true;
            IsAnalyzingContext = true;
            AnalysisStatusTitle = "Processing Photo";
            AnalysisStatusDetail = "Detecting text and visual content...";
            
            _stopwatch.Restart();
            
            // Analyze the image using AI
            await ExtractTasksFromImageAsync();
            
            _stopwatch.Stop();
            _logger.LogInformation("Photo analysis completed in {AnalysisDuration}ms", 
                _stopwatch.ElapsedMilliseconds);
            
            // Set phase to reviewing to show the results
            Phase = PhotoPhase.Reviewing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing image");
            _errorHandler.HandleError(ex);
        }
        finally
        {
            IsAnalyzingContext = false;
            IsBusy = false;
        }
    }
    
    private async Task ExtractTasksFromImageAsync()
    {
        if (!_chatClientService.IsInitialized)
        {
            _logger.LogError("ChatClient is not initialized");
            AnalysisStatusDetail = "Error: AI services not initialized";
            throw new InvalidOperationException("Chat client not initialized");
        }
        
        try
        {
            AnalysisStatusDetail = "Extracting tasks from image content...";
            
            // Build the prompt for the AI model
            var prompt = new System.Text.StringBuilder();
            prompt.AppendLine("# Image Analysis Task");
            prompt.AppendLine("Analyze the image for task lists, to-do items, notes, or any content that could be organized into projects and tasks.");
            prompt.AppendLine();
            prompt.AppendLine("## Instructions:");
            prompt.AppendLine("1. Identify any projects and tasks (to-do items) visible in the image");
            prompt.AppendLine("2. Format handwritten text, screenshots, or photos of physical notes into structured data");
            prompt.AppendLine("3. Group related tasks into projects when appropriate");
            // prompt.AppendLine("4. If the image contains a calendar, schedule, or dates, include those as due dates");
            prompt.AppendLine();
            prompt.AppendLine("If no projects/tasks are found, return an empty projects array.");
            
            // Call the AI service with the image
            var client = _chatClientService.GetClient();
            if (client == null)
            {
                throw new InvalidOperationException("Could not get chat client");
            }

            byte[] imageBytes = File.ReadAllBytes(ImageSource);
            
            var msg = new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant,
            [
                new TextContent(prompt.ToString()),
                new DataContent(imageBytes, mediaType: "image/png")
            ]);
            
            var apiResponse = await client.GetResponseAsync<ProjectsJson>(msg);
            
            if (apiResponse?.Result?.Projects != null)
            {
                // Transform the API response into our model
                Projects = apiResponse.Result.Projects
                    .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                    .ToList();
                
                // For projects that don't have a name, add a default name
                for (int i = 0; i < Projects.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(Projects[i].Name))
                    {
                        Projects[i].Name = $"Project {i + 1}";
                    }
                }
            }
            
            if (Projects.Count > 0)
            {
                AnalysisStatusDetail = $"Successfully extracted {Projects.Count} projects and {Projects.Sum(p => p.Tasks.Count)} tasks!";
            }
            else
            {
                AnalysisStatusDetail = "No tasks found in the image. Try again with a clearer image.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting tasks from image");
            AnalysisStatusDetail = "Error extracting tasks: " + ex.Message;
            throw;
        }
    }

    [RelayCommand]
    private Task TaskCompleted(ProjectTask task)
    {
        task.IsCompleted = !task.IsCompleted;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task AcceptRecommendation(ProjectTask task)
    {
        // Mark as not a recommendation anymore
        task.IsRecommendation = false;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task RejectRecommendation(ProjectTask task)
    {
        // Find and remove the task from its project
        foreach (var project in Projects)
        {
            if (project.Tasks.Contains(task))
            {
                project.Tasks.Remove(task);
                break;
            }
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void DeleteProject(Project? project)
    {
        if (project != null && Projects.Contains(project))
        {
            Projects.Remove(project);
        }
    }

    [RelayCommand]
    private void DeleteTask(ProjectTask? task)
    {
        if (task == null) return;
        
        foreach (var project in Projects)
        {
            if (project.Tasks.Contains(task))
            {
                project.Tasks.Remove(task);
                break;
            }
        }
    }

    [RelayCommand]
    private async Task ReanalyzeAsync()
    {
        Phase = PhotoPhase.Analyzing;
        Projects.Clear();
        await AnalyzeImageAsync();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            AnalysisStatusTitle = "Saving Tasks";
            AnalysisStatusDetail = "Adding your tasks to the database...";
            
            int savedProjects = 0;
            int savedTasks = 0;
            
            // Save each project and its tasks
            foreach (var project in Projects.Where(p => p.Tasks.Any()))
            {
                // Save project
                await _projectRepository.SaveItemAsync(project);
                savedProjects++;
                
                // Save tasks
                foreach (var task in project.Tasks)
                {
                    task.ProjectID = project.ID;
                    await _taskRepository.SaveItemAsync(task);
                    savedTasks++;
                }
            }
            
            // Show completion message
            await AppShell.DisplayToastAsync($"Saved {savedProjects} projects with {savedTasks} tasks!");
            
            // Return to main page
            await GoBackAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving tasks from photo");
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
        await Shell.Current.GoToAsync("..");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _fileResult = query["FileResult"] as FileResult;
    }
}
