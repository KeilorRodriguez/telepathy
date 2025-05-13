using Telepathic.Models;

namespace Telepathic.Models;

public class PriorityTaskResult
{
	[System.Text.Json.Serialization.JsonPropertyName("priorityTasks")]
	public List<ProjectTask>? PriorityTasks { get; set; }

	[System.Text.Json.Serialization.JsonPropertyName("personalizedGreeting")]
	public string? PersonalizedGreeting { get; set; }
}