using Microsoft.Extensions.AI.MultimodalConversation;
using Microsoft.Extensions.Logging;

namespace Telepathic.Services;

/// <summary>
/// Implementation of the location MCP service that provides nearby location checking
/// </summary>
public class LocationMcpService : ILocationMcpService
{
    private readonly ILogger<LocationMcpService> _logger;
    private double _currentLatitude;
    private double _currentLongitude;
    private bool _hasLocation;
    
    public LocationMcpService(ILogger<LocationMcpService> logger)
    {
        _logger = logger;
    }
    
    public void InitializeMcp(IChatClient client)
    {
        if (client == null)
        {
            _logger.LogWarning("Cannot initialize Location MCP: chat client is null");
            return;
        }
        
        // Register the IsNearby function with the chat client
        client.RegisterFunction("IsNearby", IsNearbyFunction);
        _logger.LogInformation("Location MCP initialized successfully");
    }
    
    private Task<bool> IsNearbyFunction(MultimodalConversationContext context, string pointOfInterest, double distanceThresholdMeters = 100)
    {
        return IsNearbyAsync(pointOfInterest, distanceThresholdMeters);
    }
    
    public async Task<bool> IsNearbyAsync(string pointOfInterest, double distanceThresholdMeters = 100)
    {
        if (!_hasLocation)
        {
            _logger.LogWarning("Cannot check if nearby: no current location set");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(pointOfInterest))
        {
            _logger.LogWarning("Cannot check if nearby: point of interest is empty");
            return false;
        }
        
        try
        {
            // Get the point of interest coordinates
            var coordinates = await GetPointOfInterestCoordinatesAsync(pointOfInterest);
            if (!coordinates.HasValue)
            {
                _logger.LogWarning("Could not find coordinates for point of interest: {PointOfInterest}", pointOfInterest);
                return false;
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
            
            return isNearby;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if nearby point of interest: {PointOfInterest}", pointOfInterest);
            return false;
        }
    }
    
    public void SetCurrentLocation(double latitude, double longitude)
    {
        _currentLatitude = latitude;
        _currentLongitude = longitude;
        _hasLocation = true;
        _logger.LogInformation("Current location updated to: {Latitude}, {Longitude}", latitude, longitude);
    }
    
    public (double Latitude, double Longitude) GetCurrentLocation()
    {
        return (_currentLatitude, _currentLongitude);
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
        // In a real implementation, this would use a geocoding service or Places API
        // For this implementation, we'll simulate by returning coordinates near the user's location
        
        try
        {
            // Simulate a nearby point (within 50-150 meters)
            Random random = new Random();
            double distance = random.Next(50, 150); // random distance between 50 and 150 meters
            double bearing = random.Next(0, 360); // random direction in degrees
            
            // Calculate new coordinates based on distance and bearing
            double lat2, lon2;
            
            // Convert to radians
            double latRad = DegreesToRadians(_currentLatitude);
            double lonRad = DegreesToRadians(_currentLongitude);
            double bearingRad = DegreesToRadians(bearing);
            double distanceRadians = distance / 6371000.0; // Earth's radius in meters
            
            // Calculate new position
            lat2 = Math.Asin(Math.Sin(latRad) * Math.Cos(distanceRadians) +
                             Math.Cos(latRad) * Math.Sin(distanceRadians) * Math.Cos(bearingRad));
            
            lon2 = lonRad + Math.Atan2(
                Math.Sin(bearingRad) * Math.Sin(distanceRadians) * Math.Cos(latRad),
                Math.Cos(distanceRadians) - Math.Sin(latRad) * Math.Sin(lat2));
            
            // Convert back to degrees
            double newLat = RadiansToDegrees(lat2);
            double newLon = RadiansToDegrees(lon2);
            
            _logger.LogInformation("Simulated coordinates for '{PointOfInterest}': {Latitude}, {Longitude}", 
                pointOfInterest, newLat, newLon);
            
            return (newLat, newLon);
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