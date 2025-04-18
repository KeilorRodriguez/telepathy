using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using Plugin.Maui.CalendarStore;
using System.Collections.ObjectModel;
using Telepathic.Models;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Extensions.AI;

namespace Telepathic.PageModels;

public partial class MainPageModel : ObservableObject, IProjectTaskPageModel
{
	private bool _isNavigatedTo;
	private bool _dataLoaded;
	private readonly ProjectRepository _projectRepository;
	private readonly TaskRepository _taskRepository;
	private readonly CategoryRepository _categoryRepository;
	private readonly ModalErrorHandler _errorHandler;
	private readonly SeedDataService _seedDataService;
	private readonly ICalendarStore _calendarStore;
	private readonly IChatClient? _chatClient;
	private CancellationTokenSource? _cancelTokenSource;
	private bool _isCheckingLocation;

	[ObservableProperty]
	private List<CategoryChartData> _todoCategoryData = [];

	[ObservableProperty]
	private List<Brush> _todoCategoryColors = [];

	[ObservableProperty]
	private List<ProjectTask> _tasks = [];
	
	[ObservableProperty]
	private List<ProjectTask> _priorityTasks = [];

	[ObservableProperty]
	private List<Project> _projects = [];

	[ObservableProperty]
	bool _isBusy;

	[ObservableProperty]
	bool _isRefreshing;
	
	[ObservableProperty]
	bool _isAnalyzingContext;
	
	[ObservableProperty]
	string _analysisStatusTitle = "Scanning your task universe";
	
	[ObservableProperty]
	string _analysisStatusDetail = "Gathering your location, time, and calendar events to determine which tasks require your immediate attention...";
	
	[ObservableProperty]
	bool _hasPriorityTasks;

	[ObservableProperty]
	private string _today = DateTime.Now.ToString("dddd, MMM d");

	[ObservableProperty]
	private bool _isSettingsSheetOpen;

	[ObservableProperty]
	private string _openAIApiKey = Preferences.Default.Get("openai_api_key", string.Empty);

	[ObservableProperty]
	private bool _isTelepathyEnabled = Preferences.Default.Get("telepathy_enabled", false);

	[ObservableProperty]
	private string _calendarButtonText = Preferences.Default.Get("calendar_connected", false) ? "Disconnect" : "Connect";

	[ObservableProperty]
	private string _aboutMeText = Preferences.Default.Get("about_me_text", string.Empty);

	[ObservableProperty]
	private ObservableCollection<CalendarInfo> _userCalendars = new();

	[ObservableProperty]
	private bool _isLoadingCalendars;

	[ObservableProperty]
	private bool _hasLoadedCalendars;

	[ObservableProperty]
	private bool _isLocationEnabled = Preferences.Default.Get("location_enabled", false);

	[ObservableProperty]
	private string _currentLocation = "Location not available";

	[ObservableProperty]
	private bool _isGettingLocation;

	public bool HasCompletedTasks
		=> Tasks?.Any(t => t.IsCompleted) ?? false;

	public MainPageModel(SeedDataService seedDataService, ProjectRepository projectRepository,
		TaskRepository taskRepository, CategoryRepository categoryRepository, ModalErrorHandler errorHandler,
		ICalendarStore calendarStore, IChatClient? chatClient = null)
	{
		_projectRepository = projectRepository;
		_taskRepository = taskRepository;
		_categoryRepository = categoryRepository;
		_errorHandler = errorHandler;
		_seedDataService = seedDataService;
		_calendarStore = calendarStore;
		_chatClient = chatClient;
		
		// Load saved calendar choices
		LoadSavedCalendars();

		// Initialize location if enabled
		if (IsLocationEnabled)
		{
			_ = GetCurrentLocationAsync();
		}
	}
		/// <summary>
	/// Retrieves calendar events for the connected calendars for today
	/// </summary>
	private async Task<List<CalendarEvent>> GetCalendarEventsAsync()
	{
		var results = new List<CalendarEvent>();
		
		try
		{
			// Only proceed if we have selected calendars
			if (!UserCalendars.Any(c => c.IsSelected))
				return results;
				
			// Get events for today
			var today = DateTime.Today;
			var tomorrow = today.AddDays(1);
			
			// Get all calendars first
			var calendars = await _calendarStore.GetCalendars();
			
			// Filter to only selected calendars by ID
			var selectedCalendarIds = UserCalendars.Where(c => c.IsSelected).Select(c => c.Id).ToHashSet();
			var selectedCalendars = calendars.Where(c => selectedCalendarIds.Contains(c.Id)).ToList();
			
			foreach (var calendar in selectedCalendars)
			{
				try
				{
					// Get events from the calendar for today
					var events = await _calendarStore.GetEvents(calendar.Id, today, tomorrow);
					results.AddRange(events);
				}
				catch (Exception ex)
				{
					// Log but continue with other calendars
					System.Diagnostics.Debug.WriteLine($"Error getting events for calendar {calendar.Name}: {ex.Message}");
				}
			}
		}
		catch (Exception ex)
		{
			_errorHandler.HandleError(ex);
		}
		
		return results;
	}

	private async Task LoadData()
	{
		try
		{
			IsBusy = true;

			Projects = await _projectRepository.ListAsync();

			var chartData = new List<CategoryChartData>();
			var chartColors = new List<Brush>();

			var categories = await _categoryRepository.ListAsync();
			foreach (var category in categories)
			{
				chartColors.Add(category.ColorBrush);

				var ps = Projects.Where(p => p.CategoryID == category.ID).ToList();
				int tasksCount = ps.SelectMany(p => p.Tasks).Count();

				chartData.Add(new(category.Title, tasksCount));
			}

			TodoCategoryData = chartData;
			TodoCategoryColors = chartColors;

			Tasks = await _taskRepository.ListAsync();
		}
		finally
		{
			IsBusy = false;
			OnPropertyChanged(nameof(HasCompletedTasks));
		}
	}

	private async Task InitData(SeedDataService seedDataService)
	{
		bool isSeeded = Preferences.Default.ContainsKey("is_seeded");

		if (!isSeeded)
		{
			await seedDataService.LoadSeedDataAsync();
		}

		Preferences.Default.Set("is_seeded", true);
		await Refresh();
	}

	[RelayCommand]
	private async Task Refresh()
	{
		try
		{
			IsRefreshing = true;
			await LoadData();
		}
		catch (Exception e)
		{
			_errorHandler.HandleError(e);
		}
		finally
		{
			IsRefreshing = false;
		}
	}

	[RelayCommand]
	private void NavigatedTo() =>
		_isNavigatedTo = true;

	[RelayCommand]
	private void NavigatedFrom() =>
		_isNavigatedTo = false;
	[RelayCommand]
	private async Task Appearing()
	{
		if (!_dataLoaded)
		{
			await InitData(_seedDataService);
			_dataLoaded = true;
			await Refresh();
		}
		// This means we are being navigated to
		else if (!_isNavigatedTo)
		{
			await Refresh();
		}
		
		// Analyze tasks based on context (location, time, calendar)
		await AnalyzeAndPrioritizeTasks();
	}

	[RelayCommand]
	private Task Completed(ProjectTask task)
	{
		OnPropertyChanged(nameof(HasCompletedTasks));
		return _taskRepository.SaveItemAsync(task);
	}

	[RelayCommand]
	private Task AddTask()
		=> Shell.Current.GoToAsync($"task");

	[RelayCommand]
	private Task NavigateToProject(Project project)
		=> Shell.Current.GoToAsync($"project?id={project.ID}");

	[RelayCommand]
	private Task NavigateToTask(ProjectTask task)
		=> Shell.Current.GoToAsync($"task?id={task.ID}");

	[RelayCommand]
	private async Task CleanTasks()
	{
		var completedTasks = Tasks.Where(t => t.IsCompleted).ToList();
		foreach (var task in completedTasks)
		{
			await _taskRepository.DeleteItemAsync(task);
			Tasks.Remove(task);
		}

		OnPropertyChanged(nameof(HasCompletedTasks));
		Tasks = new(Tasks);
		await AppShell.DisplayToastAsync("All cleaned up!");
	}

	partial void OnIsTelepathyEnabledChanged(bool value)
	{
		Preferences.Default.Set("telepathy_enabled", value);
	}

	partial void OnOpenAIApiKeyChanged(string value)
	{
		Preferences.Default.Set("openai_api_key", value);
	}
	
	partial void OnAboutMeTextChanged(string value)
	{
		Preferences.Default.Set("about_me_text", value);
	}

	[RelayCommand]
	private void ShowSettings()
	{
		IsSettingsSheetOpen = true;
	}
	[RelayCommand]
	private async Task SaveApiKey()
	{
		Preferences.Default.Set("openai_api_key", OpenAIApiKey);
		await AppShell.DisplayToastAsync("API Key saved!");
	}
	
	private void LoadSavedCalendars()
	{
		var savedCalendarsJson = Preferences.Default.Get("saved_calendars", string.Empty);
		if (!string.IsNullOrEmpty(savedCalendarsJson))
		{
			try
			{
				var savedCalendars = System.Text.Json.JsonSerializer.Deserialize<List<CalendarInfo>>(savedCalendarsJson);
				if (savedCalendars != null)
				{
					UserCalendars = new ObservableCollection<CalendarInfo>(savedCalendars);
					HasLoadedCalendars = true;
				}
			}			catch
			{
				// Silent fail, will reload calendars
			}
		}
	}
	
	private void SaveSelectedCalendars()
	{
		var calendarsJson = System.Text.Json.JsonSerializer.Serialize(UserCalendars);
		Preferences.Default.Set("saved_calendars", calendarsJson);
	}

	[RelayCommand]
	private async Task LoadCalendars()
	{
		if (IsLoadingCalendars)
			return;
			
		try
		{
			IsLoadingCalendars = true;
			
			var calendars = await _calendarStore.GetCalendars();
			var tempCalendars = new List<CalendarInfo>();
			
			// If we already have saved calendars, preserve selections
			var existingCalendars = UserCalendars.ToDictionary(c => c.Id, c => c);
			
			foreach (var calendar in calendars)
			{
				bool isSelected = existingCalendars.ContainsKey(calendar.Id) && existingCalendars[calendar.Id].IsSelected;
				tempCalendars.Add(new CalendarInfo(calendar.Id, calendar.Name, isSelected));
			}
			
			UserCalendars = new ObservableCollection<CalendarInfo>(tempCalendars);
			SaveSelectedCalendars();
			
			HasLoadedCalendars = true;
			CalendarButtonText = UserCalendars.Any(c => c.IsSelected) ? "Manage Calendars" : "Connect";
			Preferences.Default.Set("calendar_connected", UserCalendars.Any(c => c.IsSelected));
		}
		catch (Exception ex)
		{
			_errorHandler.HandleError(ex);
		}
		finally
		{
			IsLoadingCalendars = false;
		}
	}
	
	[RelayCommand]
	private async Task ToggleCalendarSelection(CalendarInfo calendar)
	{
		SaveSelectedCalendars();
		
		CalendarButtonText = UserCalendars.Any(c => c.IsSelected) ? "Manage Calendars" : "Connect";
		Preferences.Default.Set("calendar_connected", UserCalendars.Any(c => c.IsSelected));
		await AppShell.DisplayToastAsync($"Calendar '{calendar.Name}' {(calendar.IsSelected ? "connected" : "disconnected")}!");
	}

	[RelayCommand]
	private async Task ToggleCalendar()
	{
		await LoadCalendars();
		// if (!HasLoadedCalendars)
		// {
		// 	await LoadCalendars();
		// }
		// else
		// {
		// 	// We already have calendars, so this is just to show the calendar section
		// }
	}
	
	[RelayCommand]
	private async Task DisconnectAllCalendars()
	{
		foreach (var calendar in UserCalendars)
		{
			calendar.IsSelected = false;
		}
		
		SaveSelectedCalendars();
		CalendarButtonText = "Connect";
		Preferences.Default.Set("calendar_connected", false);
		await AppShell.DisplayToastAsync("All calendars disconnected!");
	}

	partial void OnIsLocationEnabledChanged(bool value)
	{
		Preferences.Default.Set("location_enabled", value);

		if (value)
		{
			_ = GetCurrentLocationAsync();
		}
		else
		{
			CurrentLocation = "Location not available";
			if (_cancelTokenSource != null && !_cancelTokenSource.IsCancellationRequested)
				_cancelTokenSource.Cancel();
		}
	}

	[RelayCommand]
	public async Task RefreshLocationAsync()
	{
		if (IsLocationEnabled)
		{
			await GetCurrentLocationAsync();
		}
	}

	public async Task GetCurrentLocationAsync()
	{
		try
		{
			IsGettingLocation = true;
			_cancelTokenSource = new CancellationTokenSource();

			var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
			var location = await Geolocation.GetLocationAsync(request, _cancelTokenSource.Token);

			if (location != null)
			{
				CurrentLocation = $"Lat: {location.Latitude:F4}, Long: {location.Longitude:F4}";
			}
			else
			{
				CurrentLocation = "Location unavailable";
			}
		}
		catch (FeatureNotSupportedException)
		{
			CurrentLocation = "Location not supported on this device";
		}
		catch (PermissionException)
		{
			CurrentLocation = "Location permission not granted";
		}
		catch (Exception ex)
		{
			CurrentLocation = $"Error: {ex.Message}";
		}
		finally
		{
			IsGettingLocation = false;
		}
	}
	
	/// <summary>
	/// Analyzes tasks based on calendar events, location, time of day, and personal preferences
	/// to identify priority tasks that should be highlighted to the user.
	/// </summary>
	private async Task AnalyzeAndPrioritizeTasks()
	{
		// Reset priority tasks
		PriorityTasks = [];
		HasPriorityTasks = false;
		
		// Only proceed if telepathy is enabled and we have an API client
		if (!IsTelepathyEnabled || _chatClient == null || string.IsNullOrWhiteSpace(OpenAIApiKey))
		{
			IsAnalyzingContext = false;
			return;
		}
		
		try
		{
			IsAnalyzingContext = true;
			AnalysisStatusTitle = "Scanning your task universe";
			AnalysisStatusDetail = "Gathering your location, time, and calendar events to determine which tasks require your immediate attention...";
			
			// Get calendar events
			var events = await GetCalendarEventsAsync();
			AnalysisStatusDetail = "Analyzing calendar events and tasks...";
			
			// Create a context description for the AI
			var sb = new System.Text.StringBuilder();
			
			// Add basic context information
			sb.AppendLine("CONTEXT INFORMATION:");
			
			// Current time
			var now = DateTime.Now;
			sb.AppendLine($"Current Date and Time: {now}");
			sb.AppendLine($"Day of Week: {now.DayOfWeek}");
			sb.AppendLine($"Time of Day: {GetTimeOfDayDescription(now)}");
			
			// Location
			if (IsLocationEnabled)
			{
				sb.AppendLine($"Current Location: {CurrentLocation}");
			}
			
			// Calendar events
			sb.AppendLine($"Calendar Events for Today ({events.Count}):");
			if (events.Any())
			{
				foreach (var evt in events.OrderBy(e => e.StartDate))
				{
					sb.AppendLine($"- {evt.Title} ({evt.StartDate:t} - {evt.EndDate:t})");
				}
			}
			else
			{
				sb.AppendLine("- No calendar events for today");
			}
			
			// About me
			if (!string.IsNullOrWhiteSpace(AboutMeText))
			{
				sb.AppendLine("\nABOUT ME:");
				sb.AppendLine(AboutMeText);
			}
					// Add only incomplete tasks - no need to process completed ones!
			sb.AppendLine("\nACTIVE TASKS:");
			foreach (var task in Tasks.Where(t => !t.IsCompleted))
			{
				var projectName = "";
				var project = Projects.FirstOrDefault(p => p.ID == task.ProjectID);
				if (project != null)
				{
					projectName = project.Name;
				}
				
				sb.AppendLine($"- Task '{task.Title}', Project: '{projectName}', Due: {(task.DueDate.HasValue ? task.DueDate.Value.ToString("d") : "No due date")}, Priority: {task.Priority}");
			}
			
			// Instructions for the AI
			sb.AppendLine("\nINSTRUCTIONS:");
			sb.AppendLine("Based on all the context above, identify which tasks should be prioritized right now. Consider:");
			sb.AppendLine("1. Tasks that are due soon or today");
			sb.AppendLine("2. Tasks that relate to upcoming calendar events");
			sb.AppendLine("3. Tasks that might be relevant to my current location");
			sb.AppendLine("4. Tasks that align with my personal preferences in the 'About Me' section");
			sb.AppendLine("5. Only include uncompleted tasks");
			sb.AppendLine("\nRETURN FORMAT:");
			sb.AppendLine("Return a JSON object with a single property 'priorityTaskIds' that contains an array of task IDs (as integers) that should be prioritized. Example:");
			sb.AppendLine("{ \"priorityTaskIds\": [1, 2, 3] }");
			
			AnalysisStatusDetail = "Applying cosmic intelligence to your tasks...";
					// Send to AI for analysis using the same pattern as in ProjectDetailPageModel
			if (_chatClient != null)
			{
				try 
				{
					var apiResponse = await _chatClient.GetResponseAsync<PriorityTaskResult>(sb.ToString());
					if (apiResponse?.Result?.PriorityTaskIds != null)
					{
						// Get the priority tasks
						var priorityIds = new HashSet<int>(apiResponse.Result.PriorityTaskIds);
						PriorityTasks = Tasks.Where(t => priorityIds.Contains(t.ID) && !t.IsCompleted).ToList();
						HasPriorityTasks = PriorityTasks.Any();
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Error calling AI for task prioritization: {ex.Message}");
				}
			}
		}
		catch (Exception ex)
		{
			_errorHandler.HandleError(ex);
		}
		finally
		{
			IsAnalyzingContext = false;
		}
	}
	
	private string GetTimeOfDayDescription(DateTime time)
	{
		var hour = time.Hour;
		if (hour >= 5 && hour < 12)
			return "Morning";
		else if (hour >= 12 && hour < 17)
			return "Afternoon";
		else if (hour >= 17 && hour < 21)
			return "Evening";
		else
			return "Night";
	}
	
	private class PriorityTaskResult
	{
		[System.Text.Json.Serialization.JsonPropertyName("priorityTaskIds")]
		public List<int>? PriorityTaskIds { get; set; }
	}
}