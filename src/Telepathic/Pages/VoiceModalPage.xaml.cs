using System;
namespace Telepathic.Pages;

public partial class VoiceModalPage : ContentPage
{
    public VoiceModalPage(VoiceModalPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }
}