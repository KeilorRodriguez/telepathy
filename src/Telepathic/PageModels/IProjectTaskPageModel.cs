using CommunityToolkit.Mvvm.Input;
using Telepathic.Models;

namespace Telepathic.PageModels;

public interface IProjectTaskPageModel
{
	IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
	IAsyncRelayCommand<ProjectTask> AcceptRecommendationCommand { get; }
	IAsyncRelayCommand<ProjectTask> RejectRecommendationCommand { get; }
	bool IsBusy { get; }
}