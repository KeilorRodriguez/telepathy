using Telepathic.Models;
using Telepathic.PageModels;

namespace Telepathic.Pages;

public partial class MainPage : ContentPage
{
	public MainPage(MainPageModel model)
	{
		InitializeComponent();
		BindingContext = model;
	}
	
	private void CalendarCheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
	{
		if (sender is CheckBox checkBox && checkBox.BindingContext is CalendarInfo calendar && BindingContext is MainPageModel viewModel)
		{
			viewModel.ToggleCalendarSelectionCommand.Execute(calendar);
		}
	}
}