using ModelContextProtocol;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Telepathic.Tools;

public sealed class LocationTools
{
    private readonly ILogger<LocationTools> _logger;
    private double _currentLatitude;
    private double _currentLongitude;
    private bool _hasLocation;
    private string? _googlePlacesApiKey;
    private static readonly System.Net.Http.HttpClient _httpClient = new();

    public LocationTools(ILogger<LocationTools> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Sets the Google Places API key to use for geocoding.
    /// </summary>
    public void SetGooglePlacesApiKey(string? apiKey)
    {
        _googlePlacesApiKey = apiKey;
    }

    /// <summary>
    /// Sets the user's current location
    /// </summary>
    public void SetCurrentLocation(double latitude, double longitude)
    {
        _currentLatitude = latitude;
        _currentLongitude = longitude;
        _hasLocation = true;
        _logger.LogInformation("Current location updated to: {Latitude}, {Longitude}", latitude, longitude);
    }

    /// <summary>
    /// Gets the user's current location
    /// </summary>
    public (double Latitude, double Longitude) GetCurrentLocation()
    {
        return (_currentLatitude, _currentLongitude);
    }

    /// <summary>
    /// Checks if the user is nearby a specified point of interest
    /// </summary>
    /// <param name="pointOfInterest">The point of interest to check (e.g., "coffee shop", "Target", "grocery store")</param>
    /// <param name="distanceThresholdMeters">The distance threshold in meters (default: 100)</param>
    /// <returns>True if the user is within the threshold distance of the point of interest</returns>
    [Description("Checks if the user is near a location or business type")]
    public async Task<string> IsNearby(
        [Description("Type of location or business (e.g., coffee shop, Target, grocery store)")] string pointOfInterest,
        [Description("Distance threshold in meters (default: 100)")] double distanceThresholdMeters = 100)
    {
        if (!_hasLocation)
        {
            _logger.LogWarning("Cannot check if nearby: no current location set");
            return "false - No current location is set";
        }

        if (string.IsNullOrWhiteSpace(pointOfInterest))
        {
            _logger.LogWarning("Cannot check if nearby: point of interest is empty");
            return "false - Point of interest cannot be empty";
        }

        try
        {
            // Get the point of interest coordinates
            var coordinates = await GetPointOfInterestCoordinatesAsync(pointOfInterest);
            if (!coordinates.HasValue)
            {
                _logger.LogWarning("Could not find coordinates for point of interest: {PointOfInterest}", pointOfInterest);
                return $"false - Could not find coordinates for {pointOfInterest}";
            }

            // Calculate distance using Haversine formula
            double distance = CalculateDistance(
                _currentLatitude,
                _currentLongitude,
                coordinates.Value.Latitude,
                coordinates.Value.Longitude);

            bool isNearby = distance <= distanceThresholdMeters;
            _logger.LogInformation(
                "Checking if near {PointOfInterest}: distance is {Distance:F2}m, threshold is {Threshold}m, result: {IsNearby}",
                pointOfInterest, distance, distanceThresholdMeters, isNearby);

            return isNearby
                ? $"true - You are {distance:F2}m away from {pointOfInterest}"
                : $"false - You are {distance:F2}m away from {pointOfInterest} (threshold: {distanceThresholdMeters}m)";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if nearby point of interest: {PointOfInterest}", pointOfInterest);
            return $"false - Error calculating distance to {pointOfInterest}: {ex.Message}";
        }
    }

    /// <summary>
    /// Calculates the distance between two points using the Haversine formula
    /// </summary>
    /// <returns>Distance in meters</returns>
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Earth's radius in meters
        const double earthRadiusMeters = 6371000;

        // Convert degrees to radians
        double lat1Rad = DegreesToRadians(lat1);
        double lon1Rad = DegreesToRadians(lon1);
        double lat2Rad = DegreesToRadians(lat2);
        double lon2Rad = DegreesToRadians(lon2);

        // Calculate differences
        double dLat = lat2Rad - lat1Rad;
        double dLon = lon2Rad - lon1Rad;

        // Haversine formula
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double distance = earthRadiusMeters * c;

        return distance;
    }

    private double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    /// <summary>
    /// Gets the coordinates for a point of interest
    /// </summary>
    /// <param name="pointOfInterest">The name or type of the point of interest</param>
    /// <returns>The coordinates or null if not found</returns>
    private async Task<(double Latitude, double Longitude)?> GetPointOfInterestCoordinatesAsync(string pointOfInterest)
    {
        if (string.IsNullOrWhiteSpace(_googlePlacesApiKey))
        {
            _logger.LogError("Google Places API key is not set.");
            return null;
        }
        if (!_hasLocation)
        {
            _logger.LogError("Current location is not set.");
            return null;
        }
        try
        {
            // Google Places Nearby Search API
            string url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={_currentLatitude},{_currentLongitude}&radius=1000&keyword={Uri.EscapeDataString(pointOfInterest)}&key={_googlePlacesApiKey}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Google Places API request failed: {StatusCode}", response.StatusCode);
                return null;
            }
            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var results = doc.RootElement.GetProperty("results");
            if (results.GetArrayLength() == 0)
            {
                _logger.LogWarning("No places found for '{PointOfInterest}'", pointOfInterest);
                return null;
            }
            var location = results[0].GetProperty("geometry").GetProperty("location");
            double lat = location.GetProperty("lat").GetDouble();
            double lng = location.GetProperty("lng").GetDouble();
            _logger.LogInformation("Found coordinates for '{PointOfInterest}': {Latitude}, {Longitude}", pointOfInterest, lat, lng);
            return (lat, lng);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting coordinates for point of interest: {PointOfInterest}", pointOfInterest);
            return null;
        }
    }

    private double RadiansToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }
}