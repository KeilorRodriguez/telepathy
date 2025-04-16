using System.ComponentModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Telepathic.Pages.Controls;

public partial class ActivityIndicatorView : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(ActivityIndicatorView), string.Empty);
    public static readonly BindableProperty DetailProperty = BindableProperty.Create(
        nameof(Detail), typeof(string), typeof(ActivityIndicatorView), string.Empty);
    public static readonly BindableProperty IsRunningProperty = BindableProperty.Create(
        nameof(IsRunning), typeof(bool), typeof(ActivityIndicatorView), false, propertyChanged: OnIsRunningChanged);
    public static readonly BindableProperty TitleStyleProperty = BindableProperty.Create(
        nameof(TitleStyle), typeof(Style), typeof(ActivityIndicatorView), null);
    public static readonly BindableProperty DetailStyleProperty = BindableProperty.Create(
        nameof(DetailStyle), typeof(Style), typeof(ActivityIndicatorView), null);
    public static readonly BindableProperty TitleTextColorProperty = BindableProperty.Create(
        nameof(TitleTextColor), typeof(Color), typeof(ActivityIndicatorView), Colors.White);
    public static readonly BindableProperty DetailTextColorProperty = BindableProperty.Create(
        nameof(DetailTextColor), typeof(Color), typeof(ActivityIndicatorView), Colors.Gray);
    
    // New color properties for shimmer effect
    public static readonly BindableProperty ShimmerStartColorProperty = BindableProperty.Create(
        nameof(ShimmerStartColor), typeof(Color), typeof(ActivityIndicatorView), Colors.DeepSkyBlue);
    public static readonly BindableProperty ShimmerEndColorProperty = BindableProperty.Create(
        nameof(ShimmerEndColor), typeof(Color), typeof(ActivityIndicatorView), Colors.HotPink);

    public ActivityIndicatorView()
    {
        InitializeComponent();
        
        // Set initial colors
        UpdateShimmerColors();
        
        // Set initial visibility of detail
        UpdateDetailVisibility();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    public string Detail
    {
        get => (string)GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }
    public bool IsRunning
    {
        get => (bool)GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }
    public Style TitleStyle
    {
        get => (Style)GetValue(TitleStyleProperty);
        set => SetValue(TitleStyleProperty, value);
    }
    public Style DetailStyle
    {
        get => (Style)GetValue(DetailStyleProperty);
        set => SetValue(DetailStyleProperty, value);
    }
    public Color TitleTextColor
    {
        get => (Color)GetValue(TitleTextColorProperty);
        set => SetValue(TitleTextColorProperty, value);
    }
    public Color DetailTextColor
    {
        get => (Color)GetValue(DetailTextColorProperty);
        set => SetValue(DetailTextColorProperty, value);
    }
    public Color ShimmerStartColor
    {
        get => (Color)GetValue(ShimmerStartColorProperty);
        set => SetValue(ShimmerStartColorProperty, value);
    }
    public Color ShimmerEndColor
    {
        get => (Color)GetValue(ShimmerEndColorProperty);
        set => SetValue(ShimmerEndColorProperty, value);
    }

    private static void OnIsRunningChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ActivityIndicatorView view)
        {
            // AnimatedGradientText is always running its animation,
            // we just need to control visibility
            view.IsVisible = (bool)newValue;
        }
    }
    
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        
        if (TitleText == null)
            return;
            
        if (propertyName == TitleTextColorProperty.PropertyName ||
            propertyName == ShimmerStartColorProperty.PropertyName ||
            propertyName == ShimmerEndColorProperty.PropertyName)
        {
            UpdateShimmerColors();
        }
        else if (propertyName == DetailProperty.PropertyName)
        {
            UpdateDetailVisibility();
        }
        else if (propertyName == DetailTextColorProperty.PropertyName && DetailLabel != null)
        {
            DetailLabel.TextColor = DetailTextColor;
        }
        else if (propertyName == DetailStyleProperty.PropertyName && DetailLabel != null)
        {
            DetailLabel.Style = DetailStyle;
        }
    }
    
    private void UpdateShimmerColors()
    {
        if (TitleText == null)
            return;
            
        TitleText.BaseFontColor = TitleTextColor;
        TitleText.GradientStartColor = ShimmerStartColor;
        TitleText.GradientEndColor = ShimmerEndColor;
    }
    
    private void UpdateDetailVisibility()
    {
        if (DetailLabel == null)
            return;
            
        DetailLabel.IsVisible = !string.IsNullOrEmpty(Detail);
        DetailLabel.Text = Detail;
    }
}
