using CommunityToolkit.Mvvm.Input;
using Telepathic.Models;

namespace Telepathic.PageModels;

public interface IProjectTaskPageModel
{
	IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
	bool IsBusy { get; }
}