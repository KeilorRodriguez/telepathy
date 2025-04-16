// filepath: /Users/davidortinau/work/dotnet-buildai/src/Telepathic/Models/CalendarInfo.cs
namespace Telepathic.Models;

public class CalendarInfo
{
	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public bool IsSelected { get; set; }

	public CalendarInfo(string id, string name, bool isSelected = false)
	{
		Id = id;
		Name = name;
		IsSelected = isSelected;
	}
}
