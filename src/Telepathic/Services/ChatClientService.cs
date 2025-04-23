using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace Telepathic.Services;

/// <summary>
/// Interface for a service that manages chat client creation and updates
/// </summary>
public interface IChatClientService
{
    /// <summary>
    /// Gets the current chat client instance
    /// </summary>
    /// <returns>The current IChatClient instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when the chat client has not been initialized</exception>
    IChatClient GetClient();
    
    /// <summary>
    /// Updates the chat client with a new API key
    /// </summary>
    /// <param name="apiKey">The OpenAI API key</param>
    /// <param name="model">The model to use (defaults to gpt-4o-mini)</param>
    void UpdateClient(string apiKey, string model = "gpt-4o-mini");
    
    /// <summary>
    /// Checks if the client is initialized and ready to use
    /// </summary>
    bool IsInitialized { get; }
}

/// <summary>
/// Service that manages the chat client and allows updating it at runtime
/// </summary>
public class ChatClientService : IChatClientService
{
    private IChatClient? _chatClient;
    private readonly ILogger _logger;

    public ChatClientService(ILogger<ChatClientService> logger)
    {
        _logger = logger;
        
        // Try to initialize from preferences if available
        var apiKey = Preferences.Default.Get("openai_api_key", string.Empty);
        if (!string.IsNullOrEmpty(apiKey))
        {
            UpdateClient(apiKey);
        }
    }

    public IChatClient GetClient()
    {
        return _chatClient ?? throw new InvalidOperationException("Chat client has not been initialized. Please provide an API key first.");
    }

    public bool IsInitialized => _chatClient != null;

    public void UpdateClient(string apiKey, string model = "gpt-4o-mini")
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Attempted to update chat client with empty API key");
            _chatClient = null;
            return;
        }

        try
        {
            var openAIClient = new OpenAIClient(apiKey);
            _chatClient = openAIClient.GetChatClient(model: model).AsIChatClient();
            
            // Add logging wrapper
            _chatClient = new LoggingChatClient(_chatClient, _logger);
            
            _logger.LogInformation("Chat client successfully initialized with model: {Model}", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update chat client");
            _chatClient = null;
            throw;
        }
    }
}
