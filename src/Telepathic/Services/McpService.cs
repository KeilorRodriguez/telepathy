using Microsoft.Extensions.Logging;
using Telepathic.Tools;

namespace Telepathic.Services;

/// <summary>
/// Interface for the MCP service that manages Model Context Protocol integration
/// </summary>
public interface IMcpService
{   
    /// <summary>
    /// Gets the LocationTools instance
    /// </summary>
    LocationTools LocationTools { get; }
}

/// <summary>
/// Service that manages Model Context Protocol (MCP) integration
/// </summary>
public class McpService : IMcpService
{
    private readonly ILogger<McpService> _logger;
    private readonly LocationTools _locationTools;
    
    public McpService(ILogger<McpService> logger, LocationTools locationTools)
    {
        _logger = logger;
        _locationTools = locationTools;
        
        _logger.LogInformation("MCP Service created");
    }
    
    public LocationTools LocationTools => _locationTools;
}