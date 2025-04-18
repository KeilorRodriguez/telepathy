namespace Telepathic.PageModels;

/// <summary>
/// Result of extracting projects and tasks from transcript
/// </summary>
public class ExtractionResult
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

/// <summary>
/// View model for a project mentioned in transcript
/// </summary>
public class ProjectVm
{
    /// <summary>
    /// Name of the project
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Tasks associated with this project
    /// </summary>
    public List<TaskVm> Tasks { get; set; } = new();
}

/// <summary>
/// View model for a task mentioned in transcript
/// </summary>
public class TaskVm
{
    /// <summary>
    /// Title of the task
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional due date for the task
    /// </summary>
    public DateTime? DueDate { get; set; }
    
    /// <summary>
    /// Optional priority (1-5)
    /// </summary>
    public int? Priority { get; set; }
}
