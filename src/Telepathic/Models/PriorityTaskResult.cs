using System.Text.Json.Serialization;
using Telepathic.Models;

namespace Telepathic.Models;

public class PriorityTaskResult
{
	[JsonPropertyName("tasks")]
	public List<ProjectTask>? PriorityTasks { get; set; }

	[JsonPropertyName("personalized_greeting")]
	public string? PersonalizedGreeting { get; set; }
}