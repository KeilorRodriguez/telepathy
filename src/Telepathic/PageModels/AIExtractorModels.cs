using System.Text.Json.Serialization;
using Telepathic.Models;

namespace Telepathic.PageModels;

/// <summary>
/// Result of extracting projects and tasks from transcript
/// Format specifically designed for OpenAI schema generation
/// </summary>
[JsonSerializable(typeof(AIExtractionResult))]
public class AIExtractionResult
{
    /// <summary>
    /// Projects mentioned in the transcript with their tasks
    /// </summary>
    public List<Project> Projects { get; set; } = new();
}