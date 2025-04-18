using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using Plugin.Maui.CalendarStore;
using System.Collections.ObjectModel;
using Telepathic.Models;
using Microsoft.Maui.Devices.Sensors;

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
	private CancellationTokenSource? _cancelTokenSource;
	private bool _isCheckingLocation;

	[ObservableProperty]
	private List<CategoryChartData> _todoCategoryData = [];

	[ObservableProperty]
	private List<Brush> _todoCategoryColors = [];

	[ObservableProperty]
	private List<ProjectTask> _tasks = [];

	[ObservableProperty]
	private List<Project> _projects = [];

	[ObservableProperty]
	bool _isBusy;

	[ObservableProperty]
	bool _isRefreshing;

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
		ICalendarStore calendarStore)
	{
		_projectRepository = projectRepository;
		_taskRepository = taskRepository;
		_categoryRepository = categoryRepository;
		_errorHandler = errorHandler;
		_seedDataService = seedDataService;
		_calendarStore = calendarStore;
		
		// Load saved calendar choices
		LoadSavedCalendars();

		// Initialize location if enabled
		if (IsLocationEnabled)
		{
			_ = GetCurrentLocationAsync();
		}
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
}