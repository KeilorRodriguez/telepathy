using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System.Threading.Tasks;
using System.Collections.Generic;
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
    
    /// <summary>
    /// Gets the MCP client that can interact with the MCP server
    /// </summary>
    McpClient GetMcpClient();
    
    /// <summary>
    /// Gets available MCP tools that can be used with chat clients
    /// </summary>
    Task<IList<object>> GetAvailableToolsAsync();
}

/// <summary>
/// Service that manages Model Context Protocol (MCP) integration
/// </summary>
public class McpService : IMcpService
{
    private readonly ILogger<McpService> _logger;
    private readonly LocationTools _locationTools;
    private McpClient? _mcpClient;
    private IList<object>? _cachedTools;
    
    public McpService(ILogger<McpService> logger, LocationTools locationTools)
    {
        _logger = logger;
        _locationTools = locationTools;
        
        _logger.LogInformation("MCP Service created");
    }
    
    public LocationTools LocationTools => _locationTools;
    
    public McpClient GetMcpClient()
    {
        if (_mcpClient == null)
        {
            _mcpClient = new McpClient();
            _logger.LogInformation("Created new MCP client");
        }
        
        return _mcpClient;
    }
    
    public async Task<IList<object>> GetAvailableToolsAsync()
    {
        if (_cachedTools != null)
        {
            return _cachedTools;
        }
        
        try
        {
            var client = GetMcpClient();
            var tools = await client.ListToolsAsync();
            
            _logger.LogInformation("Retrieved {Count} MCP tools", tools.Count);
            _cachedTools = tools;
            
            return tools;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving MCP tools");
            return new List<object>();
        }
    }
}