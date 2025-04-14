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
}