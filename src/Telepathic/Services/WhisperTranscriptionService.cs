using Microsoft.Extensions.AI;
using OpenAI;

namespace Telepathic.Services;

public class WhisperTranscriptionService : ITranscriptionService
{
    readonly IChatClient _chat;  // reuse IChatClient via OpenAIClient.AsAudioClient()
    
    public WhisperTranscriptionService(IChatClient chat) => _chat = chat;
    
    public async Task<string> TranscribeAsync(string path, CancellationToken ct)
    {
        var openAiApiKey = Preferences.Default.Get("openai_api_key", string.Empty);
        var client = new OpenAIClient(openAiApiKey);

        try
        {
            await using var stream = File.OpenRead(path);
            var result = await client.GetAudioClient("whisper-1").TranscribeAudioAsync(stream, "file.wav", cancellationToken: ct);
                    

            return result.Value.Text.Trim();
        }
        catch (Exception ex)
        {
            // Will add better error handling in Phase 5
            throw new Exception($"Failed to transcribe audio: {ex.Message}", ex);
        }
    }
}
