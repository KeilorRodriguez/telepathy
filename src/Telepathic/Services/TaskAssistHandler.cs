using Microsoft.Extensions.Logging;
using Plugin.Maui.CalendarStore;
using Telepathic.Data;
using Telepathic.Models;
using Telepathic.Services;
using Microsoft.Extensions.AI;
using Microsoft.Maui.ApplicationModel;

namespace Telepathic.Services;

/// <summary>
/// Service for handling task assist actions based on AssistType
/// </summary>
public class TaskAssistHandler
{
    private readonly ICalendarStore _calendarStore;
    private readonly IChatClientService _chatClientService;
    private readonly ModalErrorHandler _errorHandler;
    private readonly ILogger _logger;

    public TaskAssistHandler(
        ICalendarStore calendarStore,
        IChatClientService chatClientService,
        ModalErrorHandler errorHandler,
        ILogger<TaskAssistHandler> logger)
    {
        _calendarStore = calendarStore;
        _chatClientService = chatClientService;
        _errorHandler = errorHandler;
        _logger = logger;
    }

    /// <summary>
    /// Handle assistance for a task based on its AssistType
    /// </summary>
    /// <param name="task">The task to assist with</param>
    /// <param name="isLocationEnabled">Whether location services are enabled</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task HandleAssistAsync(ProjectTask task, bool isLocationEnabled = false)
    {
        if (task == null || task.AssistType == AssistType.None)
            return;

        try
        {
            switch (task.AssistType)
            {
                case AssistType.Calendar:
                    await HandleCalendarAssistAsync(task);
                    break;
                case AssistType.Maps:
                    await OpenMapsAsync(task.AssistData, task.Title, isLocationEnabled);
                    break;
                case AssistType.Phone:
                    PhoneDialer.Default.Open(task.AssistData);
                    break;
                case AssistType.Email:
                    var message = new EmailMessage
                    {
                        Subject = task.Title,
                        To = new List<string> { task.AssistData }
                    };
                    await Email.Default.ComposeAsync(message);
                    break;
                case AssistType.AI:
                    await HandleAIAssistAsync(task);
                    break;
                case AssistType.Browser:
                    await HandleBrowserAssistAsync(task);
                    break;
            }
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
        }
    }

    private async Task HandleCalendarAssistAsync(ProjectTask task)
    {
        try
        {
            // Get all connected calendars
            var calendars = await _calendarStore.GetCalendars();

            // Check if any calendar is connected and available
            if (calendars == null || !calendars.Any())
            {
                await AppShell.DisplayToastAsync("No calendars available. Please connect a calendar in settings.");
                return;
            }

            // Default to using the first available calendar
            var calendarId = calendars.First().Id;

            // Create a simple event with the task title
            string eventId = await _calendarStore.CreateEvent(
                calendarId,
                task.Title,
                task.AssistData, // Use AssistData as description
                string.Empty, // No location
                DateTimeOffset.Now.AddHours(1), // Start time (1 hour from now)
                DateTimeOffset.Now.AddHours(2), // End time (2 hours from now)
                false // Not an all-day event
            );

            if (!string.IsNullOrEmpty(eventId))
            {
                await AppShell.DisplayToastAsync("Calendar event created successfully!");
            }
            else
            {
                await AppShell.DisplayToastAsync("Failed to create calendar event.");
            }
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(new Exception("Error creating calendar event", ex));
            await AppShell.DisplayToastAsync("Error creating calendar event. See logs for details.");
        }
    }

    private async Task HandleAIAssistAsync(ProjectTask task)
    {
        var promptText = $"Assist me with: {task.Title} {task.AssistData}";
        var client = _chatClientService.GetClient();
        var chatResponse = await client.GetResponseAsync<string>(promptText);
        var aiText = chatResponse.Result ?? string.Empty;
        await Shell.Current.DisplayAlert("AI Assist", aiText, "OK");
    }

    private async Task HandleBrowserAssistAsync(ProjectTask task)
    {
        try
        {
            // First, we need to determine the URL based on the task's data
            string url = await GenerateBrowserUrlAsync(task);

            // Try to launch the browser with the URL
            if (!string.IsNullOrEmpty(url))
            {
                await Launcher.Default.OpenAsync(url);
            }
            else
            {
                await AppShell.DisplayToastAsync("Could not generate a valid URL for this task");
            }
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(new Exception("Error opening browser", ex));
            await AppShell.DisplayToastAsync("Error opening browser. See logs for details.");
        }
    }

    private async Task<string> GenerateBrowserUrlAsync(ProjectTask task)
    {
        // Default to a search if we can't determine a specific URL
        string url = string.Empty;
        
        try
        {
            // Check if the AI service is available to help determine the URL
            if (_chatClientService.IsInitialized)
            {
                var prompt = "Given this task information, generate a relevant URL that should be opened in a web browser.\n" +
                             $"Task Title: \"{task.Title}\"\n" +
                             $"Additional Data: \"{task.AssistData}\"\n\n" +
                             "Rules:\n" +
                             "1. If this looks like a search query, format it as a Bing search URL.\n" +
                             "2. If it's a reference to a specific website, provide the direct URL with https://.\n" +
                             "3. If it's a general task like \"find dentists\", make it a search for \"dentists near me\".\n" +
                             "4. Format a proper URL for opening in a browser.\n" +
                             "5. Always return just the URL with no other text or explanation.\n\n" +
                             "Examples:\n" +
                             "- For \"check the weather\", return \"https://www.bing.com/search?q=weather+forecast+near+me\"\n" +
                             "- For \"go to amazon\", return \"https://www.amazon.com\"\n" +
                             "- For \"find pizza restaurants\", return \"https://www.bing.com/search?q=pizza+restaurants+near+me\"";

                var client = _chatClientService.GetClient();
                var response = await client.GetResponseAsync<string>(prompt);
                
                if (response?.Result != null)
                {
                    url = response.Result.Trim();
                    
                    // Ensure URL has a protocol
                    if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                    {
                        url = "https://" + url;
                    }
                    
                    _logger.LogInformation("Generated URL for browser task: {Url}", url);
                }
            }
            else
            {
                // Fallback if AI isn't available - create a basic search URL
                string searchTerm = string.IsNullOrEmpty(task.AssistData) ? task.Title : task.AssistData;
                searchTerm = Uri.EscapeDataString(searchTerm);
                url = $"https://www.bing.com/search?q={searchTerm}";
                _logger.LogInformation("AI not available, using basic search URL: {Url}", url);
            }
            
            return url;
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
            
            // Emergency fallback - basic Bing search with the task title
            string searchTerm = Uri.EscapeDataString(task.Title);
            return $"https://www.bing.com/search?q={searchTerm}";
        }
    }

    private async Task OpenMapsAsync(string assistData, string taskTitle, bool isLocationEnabled)
    {
        try
        {
            // Check if AssistData contains lat/long coordinates
            if (!string.IsNullOrWhiteSpace(assistData) && assistData.Contains(","))
            {
                // Try to parse coordinates format "lat, long"
                var parts = assistData.Split(',');
                if (parts.Length == 2 &&
                    double.TryParse(parts[0].Trim(), out double lat) &&
                    double.TryParse(parts[1].Trim(), out double lng))
                {
                    // We have valid coordinates - set course for this sector!
                    var location = new Location(lat, lng);
                    var options = new MapLaunchOptions { Name = taskTitle };
                    await Map.Default.OpenAsync(location, options);
                }
                else
                {
                    var placemark = new Placemark
                    {
                        CountryName = "United States",
                        AdminArea = "",
                        Thoroughfare = assistData,
                        Locality = ""
                    };
                    // Not valid coordinates - treat as address/place name
                    await Map.Default.OpenAsync(placemark);
                }
            }
            else if (!string.IsNullOrWhiteSpace(assistData))
            {
                // Use as place name or address
                var placemark = new Placemark
                {
                    CountryName = "United States",
                    AdminArea = "",
                    Thoroughfare = assistData,
                    Locality = ""
                };
                // Not valid coordinates - treat as address/place name
                await Map.Default.OpenAsync(placemark);
            }
            else if (isLocationEnabled)
            {
                // No location specified - use current cosmic coordinates
                var currentLocation = await Geolocation.GetLastKnownLocationAsync();
                if (currentLocation != null)
                {
                    var location = new Location(currentLocation.Latitude, currentLocation.Longitude);
                    var options = new MapLaunchOptions { Name = "Current Location" };
                    await Map.Default.OpenAsync(location, options);
                }
                else
                {
                    await AppShell.DisplayToastAsync("Could not determine your location coordinates. Enable location services or specify a location.");
                }
            }
            else
            {
                await AppShell.DisplayToastAsync("No location specified and location services are disabled.");
            }
        }
        catch (Exception ex)
        {
            await AppShell.DisplayToastAsync($"Failed to navigate to location: {ex.Message}");
        }
    }
}
