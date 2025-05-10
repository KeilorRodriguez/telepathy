using CommunityToolkit.Mvvm.Input;
using Telepathic.Models;

namespace Telepathic.PageModels;

public interface IProjectTaskPageModel
{
	IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
	IAsyncRelayCommand<ProjectTask> AcceptRecommendationCommand { get; }
	IAsyncRelayCommand<ProjectTask> RejectRecommendationCommand { get; }

	// Assist command to perform quick actions like calendar, maps, email or AI
	IAsyncRelayCommand<ProjectTask> AssistCommand { get; }
	bool IsBusy { get; }
}