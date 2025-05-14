using System.Text.Json.Serialization;

namespace Telepathic.Models;

public class ProjectTask
{
	[JsonIgnore]
	public int ID { get; set; }

	[JsonPropertyName("title")]
	public string Title { get; set; } = string.Empty;

	public bool IsCompleted { get; set; }

	[JsonConverter(typeof(NullableDateTimeConverter))]
	public DateTime? DueDate { get; set; }

	public int Priority { get; set; }

	public string PriorityReasoning { get; set; } = string.Empty;

	[JsonIgnore]
	public bool IsPriority { get; set; }

	[JsonIgnore]
	public int ProjectID { get; set; }

	[JsonIgnore]
	public bool IsRecommendation { get; set; }

	public AssistType AssistType { get; set; } = AssistType.None;

	public string AssistData { get; set; } = string.Empty;
}