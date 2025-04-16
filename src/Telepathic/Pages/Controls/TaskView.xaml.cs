using System.Windows.Input;
using Telepathic.Models;

namespace Telepathic.Pages.Controls;

public partial class TaskView
{
	public TaskView()
	{
		InitializeComponent();
	}

	public static readonly BindableProperty TaskCompletedCommandProperty = BindableProperty.Create(
		nameof(TaskCompletedCommand),
		typeof(ICommand),
		typeof(TaskView),
		null);

	public static readonly BindableProperty AcceptRecommendationCommandProperty = BindableProperty.Create(
		nameof(AcceptRecommendationCommand),
		typeof(ICommand),
		typeof(TaskView),
		null);

	public static readonly BindableProperty RejectRecommendationCommandProperty = BindableProperty.Create(
		nameof(RejectRecommendationCommand),
		typeof(ICommand),
		typeof(TaskView),
		null);

	public ICommand TaskCompletedCommand
	{
		get => (ICommand)GetValue(TaskCompletedCommandProperty);
		set => SetValue(TaskCompletedCommandProperty, value);
	}
	
	public ICommand AcceptRecommendationCommand
	{
		get => (ICommand)GetValue(AcceptRecommendationCommandProperty);
		set => SetValue(AcceptRecommendationCommandProperty, value);
	}

	public ICommand RejectRecommendationCommand
	{
		get => (ICommand)GetValue(RejectRecommendationCommandProperty);
		set => SetValue(RejectRecommendationCommandProperty, value);
	}
	private void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
	{
		var checkbox = (CheckBox)sender;
		
		if (checkbox.BindingContext is not ProjectTask task)
			return;
		
		if (task.IsCompleted == e.Value)
			return;

		task.IsCompleted = e.Value;
		TaskCompletedCommand?.Execute(task);
	}
	
}