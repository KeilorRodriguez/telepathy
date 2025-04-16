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
		// This handles when a user checks a recommendation checkbox
	private void RecommendationCheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
	{
		var checkbox = (CheckBox)sender;
		
		if (checkbox.BindingContext is not ProjectTask task)
			return;
			
		// Update the IsAccepted property based on checkbox state
		task.IsAccepted = e.Value;
		
		// Only execute the command when accepted (checked)
		if (e.Value)
		{
			// When checked, execute the accept recommendation command
			AcceptRecommendationCommand?.Execute(task);
		}
	}
	
}