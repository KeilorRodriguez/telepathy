using System.ComponentModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Toolkit;

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
        nameof(TitleTextColor), typeof(Color), typeof(ActivityIndicatorView), GetAppThemeColor("OnBackground", Colors.White));
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
        
        // Subscribe to app theme changes
        Application.Current.RequestedThemeChanged += OnAppThemeChanged;
    }
    
    private void OnAppThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        // Update colors when theme changes
        UpdateShimmerColors();
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
            bool isRunning = (bool)newValue;
            // Restore original visibility behavior - only show when running
            view.IsVisible = isRunning;
            
            // Update the IsRunning property on the SkiaAnimatedGradientTextSK control
            if (view.TitleText != null)
            {
                // Set the IsRunning property and explicitly start the animation if needed
                view.TitleText.IsRunning = isRunning;
                
                if (isRunning)
                {
                    // Explicitly call StartAnimation to ensure it begins
                    view.TitleText.StartAnimation();
                }
                else
                {
                    // Explicitly stop animation when not running
                    view.TitleText.StopAnimation();
                }
                
                // Force a redraw
                view.TitleText.InvalidateSurface();
            }
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
            
        // If we're using the default value, update it when theme changes
        if (TitleTextColor == null || TitleTextColor == Colors.White)
        {
            SetValue(TitleTextColorProperty, GetAppThemeColor("OnBackground", Colors.White));
        }
            
        // Make sure BaseFontColor is always visible by ensuring it has sufficient opacity
        var color = TitleTextColor;
        // Ensure alpha is at least 0.7 (roughly 178 in byte value) for visibility
        if (color.Alpha < 0.7)
        {
            color = color.WithAlpha(0.7f);
        }
        
        TitleText.BaseFontColor = color;
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
    
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        
        if (Handler == null)
        {
            // Unsubscribe from theme change events when control is detached
            if (Application.Current != null)
            {
                Application.Current.RequestedThemeChanged -= OnAppThemeChanged;
            }
        }
    }
    
    private static Color GetAppThemeColor(string resourceKey, Color defaultColor)
    {
        if (Application.Current?.Resources.TryGetValue(resourceKey, out var resourceValue) == true && 
            resourceValue is Microsoft.Maui.Controls.Toolkit.AppThemeColor appThemeColor)
        {
            return Application.Current.RequestedTheme == AppTheme.Dark 
                ? appThemeColor.Dark 
                : appThemeColor.Light;
        }
        
        return defaultColor;
    }
}
