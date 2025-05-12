using Microsoft.Extensions.AI.MultimodalConversation;

namespace Telepathic.Services;

/// <summary>
/// Interface for a service that provides location-related capabilities
/// </summary>
public interface ILocationMcpService
{
    /// <summary>
    /// Initializes the MCP and registers it with the chat client
    /// </summary>
    /// <param name="client">The chat client to register with</param>
    void InitializeMcp(IChatClient client);
    
    /// <summary>
    /// Checks if the user is nearby a specified point of interest
    /// </summary>
    /// <param name="pointOfInterest">The point of interest to check (e.g., "coffee shop", "Target", "grocery store")</param>
    /// <param name="distanceThresholdMeters">The distance threshold in meters (default: 100)</param>
    /// <returns>True if the user is within the threshold distance of the point of interest</returns>
    Task<bool> IsNearbyAsync(string pointOfInterest, double distanceThresholdMeters = 100);
    
    /// <summary>
    /// Sets the user's current location
    /// </summary>
    /// <param name="latitude">The user's latitude</param>
    /// <param name="longitude">The user's longitude</param>
    void SetCurrentLocation(double latitude, double longitude);
    
    /// <summary>
    /// Gets the user's current location
    /// </summary>
    /// <returns>A tuple containing the latitude and longitude</returns>
    (double Latitude, double Longitude) GetCurrentLocation();
}