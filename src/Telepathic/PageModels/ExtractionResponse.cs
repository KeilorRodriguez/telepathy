namespace Telepathic.PageModels;

/// <summary>
/// Response from AI extraction of projects and tasks
/// </summary>
public class ExtractionResponse
{
    /// <summary>
    /// Projects mentioned in the transcript with their tasks
    /// </summary>
    public List<ProjectVm> Projects { get; set; } = new();
    
    /// <summary>
    /// Standalone tasks not associated with any project
    /// </summary>
    public List<TaskVm> StandaloneTasks { get; set; } = new();
}
