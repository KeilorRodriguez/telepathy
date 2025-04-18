using System.Text.Json.Serialization;

namespace Telepathic.Models;

public class ProjectTask
{
	public int ID { get; set; }
	public string Title { get; set; } = string.Empty;
	public bool IsCompleted { get; set; }
	public DateTime? DueDate { get; set; }
	public int Priority { get; set; }

	[JsonIgnore]
	public int ProjectID { get; set; }
	
	[JsonIgnore]
	public bool IsRecommendation { get; set; }
}