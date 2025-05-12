using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Telepathic.Tools;

namespace Telepathic.Services;

/// <summary>
/// Interface for the MCP service that manages Model Context Protocol integration
/// </summary>
public interface IMcpService
{
    /// <summary>
    /// Initialize the MCP server
    /// </summary>
    void Initialize();
    
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
    private readonly McpServer _mcpServer;
    
    public McpService(ILogger<McpService> logger, LocationTools locationTools)
    {
        _logger = logger;
        _locationTools = locationTools;
        
        // Create MCP server
        _mcpServer = new McpServerBuilder()
            .WithTools(_locationTools)
            .Build();
            
        _logger.LogInformation("MCP Service created");
    }
    
    public void Initialize()
    {
        try
        {
            // Start the MCP server
            // In a real application, you would connect this to an appropriate transport
            // such as a network connection, websocket, or standard I/O
            _logger.LogInformation("MCP server initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MCP server");
        }
    }
    
    public LocationTools LocationTools => _locationTools;
}