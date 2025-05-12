using System.Text.Json.Serialization;

namespace Telepathic.Models;

public enum AssistType { None, Calendar, Maps, Phone, Email, AI, Browser }

public class ProjectTask
{
	[JsonPropertyName("id")]
	public int ID { get; set; }

	[JsonPropertyName("title")]
	public string Title { get; set; } = string.Empty;

	[JsonPropertyName("isCompleted")]
	public bool IsCompleted { get; set; }

	[JsonConverter(typeof(NullableDateTimeConverter))]
	[JsonPropertyName("dueDate")]
	public DateTime? DueDate { get; set; }

	[JsonPropertyName("priority")]
	public int Priority { get; set; }

	[JsonPropertyName("priorityReasoning")]
	public string PriorityReasoning { get; set; } = string.Empty;

	[JsonIgnore]
	public bool IsPriority { get; set; }

	[JsonIgnore]
	public int ProjectID { get; set; }

	[JsonIgnore]
	public bool IsRecommendation { get; set; }

	[JsonPropertyName("assistType")]
	public AssistType AssistType { get; set; } = AssistType.None;

	[JsonPropertyName("assistData")]
	public string AssistData { get; set; } = string.Empty;
}