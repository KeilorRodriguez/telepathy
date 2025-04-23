// filepath: /Users/davidortinau/work/dotnet-buildai/src/Telepathic/Pages/Controls/SkiaAnimatedGradientTextSK.cs
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System;

namespace Telepathic.Pages.Controls;

/// <summary>
/// ChatGPT‑style animated "thinking" header powered by SkiaSharp rendering.
/// </summary>
public sealed class SkiaAnimatedGradientTextSK : SKCanvasView
{
    /*───────────────────────────  Bindables  ───────────────────────────*/

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string),
            typeof(SkiaAnimatedGradientTextSK), "Thinking…",
            propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (SkiaAnimatedGradientTextSK)bindable;
                    ctrl.InvalidateSurface(); // Redraw whenever text changes
                }
            );

    public static readonly BindableProperty IsRunningProperty =
        BindableProperty.Create(nameof(IsRunning), typeof(bool),
            typeof(SkiaAnimatedGradientTextSK), false,
            propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (SkiaAnimatedGradientTextSK)bindable;
                    bool isRunning = (bool)newValue;
                    
                    // Start or stop the animation based on IsRunning
                    if (isRunning)
                    {
                        ctrl.StartAnimation();
                    }
                    else
                    {
                        ctrl.StopAnimation();
                    }
                    
                    ctrl.InvalidateSurface(); // Redraw when running state changes
                }
            );

    public static readonly BindableProperty BaseFontColorProperty =
        BindableProperty.Create(nameof(BaseFontColor), typeof(Color),
            typeof(SkiaAnimatedGradientTextSK), Color.FromArgb("#8c8c8c"),
            propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (SkiaAnimatedGradientTextSK)bindable;
                    ctrl.InvalidateSurface(); // Redraw whenever color changes
                }
            );

    public static readonly BindableProperty ShowDebugBackgroundProperty =
            BindableProperty.Create(
                nameof(ShowDebugBackground),
                typeof(bool),
                typeof(SkiaAnimatedGradientTextSK),
                defaultValue: false,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (SkiaAnimatedGradientTextSK)bindable;
                    ctrl.InvalidateSurface(); // Redraw when debug setting changes
                }
            );

    public static readonly BindableProperty GradientStartColorProperty =
        BindableProperty.Create(nameof(GradientStartColor), typeof(Color),
            typeof(SkiaAnimatedGradientTextSK), Color.FromArgb("#8c8c8c"),
            propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (SkiaAnimatedGradientTextSK)bindable;
                    ctrl.InvalidateSurface();
                }
            );

    public static readonly BindableProperty GradientEndColorProperty =
        BindableProperty.Create(nameof(GradientEndColor), typeof(Color),
            typeof(SkiaAnimatedGradientTextSK), Color.FromArgb("#8c8c8c"),
            propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (SkiaAnimatedGradientTextSK)bindable;
                    ctrl.InvalidateSurface();
                }
            );

    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(nameof(FontSize), typeof(float),
            typeof(SkiaAnimatedGradientTextSK), 20f,
            propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (SkiaAnimatedGradientTextSK)bindable;
                    ctrl.InvalidateSurface(); // Redraw whenever text changes
                }
            );

    /*───────────────────────────  Public API  ──────────────────────────*/
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsRunning
    {
        get => (bool)GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    public bool ShowDebugBackground
    {
        get => (bool)GetValue(ShowDebugBackgroundProperty);
        set => SetValue(ShowDebugBackgroundProperty, value);
    }

    public Color BaseFontColor
    {
        get => (Color)GetValue(BaseFontColorProperty);
        set => SetValue(BaseFontColorProperty, value);
    }

    public Color GradientStartColor
    {
        get => (Color)GetValue(GradientStartColorProperty);
        set => SetValue(GradientStartColorProperty, value);
    }

    public Color GradientEndColor
    {
        get => (Color)GetValue(GradientEndColorProperty);
        set => SetValue(GradientEndColorProperty, value);
    }

    public float FontSize
    {
        get => (float)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /*───────────────────────────  Internals  ───────────────────────────*/
    private float _progress; // 0 → 1
    private string _animationName = "shimmer";
    private bool _isAnimationRunning = false;

    public SkiaAnimatedGradientTextSK()
    {
        HeightRequest = 32;
        HorizontalOptions = LayoutOptions.Start;
        
        // Register the paint surface handler
        PaintSurface += OnPaintSurface;
        
        // Animation will be started when IsRunning is set to true, not in the constructor
    }

    public void StartAnimation()
    {
        // Only start the animation if it's not already running
        if (_isAnimationRunning)
            return;

        _isAnimationRunning = true;
        
        // Stop any existing animation first
        this.AbortAnimation(_animationName);
        
        // Create a repeating animation that runs at 30 FPS (rate = 30)
        new Animation(v =>
            {
                _progress = (float)v;
                InvalidateSurface(); // Force redraw with new progress value
            },
            0, 1, Easing.Linear)
        .Commit(this, _animationName, 30, 1500, Easing.Linear, (v, c) => 
        {
            // Only restart if IsRunning is still true
            if (IsRunning)
            {
                // Immediately restart the animation to make it continuous
                StartAnimation();
            }
            else
            {
                _isAnimationRunning = false;
            }
        }, () => IsRunning); // Only run when IsRunning is true
    }

    public void StopAnimation()
    {
        if (_isAnimationRunning)
        {
            this.AbortAnimation(_animationName);
            _isAnimationRunning = false;
            _progress = 0; // Reset progress
            InvalidateSurface(); // Redraw without animation
        }
    }
    
    // Converting Maui Color to SKColor
    private static SKColor ToSKColor(Color color)
    {
        return new SKColor(
            (byte)(color.Red * 255),
            (byte)(color.Green * 255),
            (byte)(color.Blue * 255),
            (byte)(color.Alpha * 255));
    }
    
    // Convert to transparent SKColor
    private static SKColor WithAlpha(Color color, float alpha)
    {
        return new SKColor(
            (byte)(color.Red * 255),
            (byte)(color.Green * 255),
            (byte)(color.Blue * 255),
            (byte)(alpha * 255));
    }

    /*─────────────────────────  SkiaSharp Drawing  ─────────────────────*/
    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        SKSurface surface = e.Surface;
        SKCanvas canvas = surface.Canvas;
        canvas.Clear();
        
        string text = Text ?? "";
        if (string.IsNullOrWhiteSpace(text)) return;
        
        // Debug - draw a rectangle to show the control bounds are working
        if (ShowDebugBackground)
        {
            using var debugBorderPaint = new SKPaint
            {
                Color = SKColors.Red,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };
            canvas.DrawRect(0, 0, e.Info.Width, e.Info.Height, debugBorderPaint);
        }
        
        // Create font for text
        using var skTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        using var skFont = new SKFont(skTypeface, FontSize);
        
        // Create paint for text
        using var textPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = ToSKColor(BaseFontColor)
        };
        
        // Measure text
        var textWidth = skFont.MeasureText(text);
        
        // Calculate correct position for text drawing
        float x = 0;
        // For vertical centering, we need the text height which is roughly equal to font size
        float textHeight = FontSize; 
        float y = (e.Info.Height + textHeight) / 2f; // Center vertically
        
        // Debug background
        if (ShowDebugBackground)
        {
            using var debugPaint = new SKPaint
            {
                Color = new SKColor(255, 0, 0, 76), // Red with 0.3 alpha
                Style = SKPaintStyle.Fill
            };
            canvas.DrawRect(0, 0, e.Info.Width, e.Info.Height, debugPaint);
            
            // Frame
            using var framePaint = new SKPaint
            {
                Color = SKColors.Yellow,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1
            };
            canvas.DrawRect(0, 0, e.Info.Width, e.Info.Height, framePaint);
        }
        
        // Always draw the text with standard paint when not animating,
        // or as the base layer when animating
        canvas.DrawText(text, x, y, SKTextAlign.Left, skFont, textPaint);
        
        // Only draw shimmer effect if IsRunning is true AND the animation is actually running
        if (IsRunning && _isAnimationRunning)
        {
            // Setup shimmer
            float bandWidth = textWidth * 2f;
            float offset = (_progress * bandWidth) - bandWidth;
            
            // Create gradient for shimmer effect
            using var gradientPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            
            // Create shader with gradient
            using var shader = SKShader.CreateLinearGradient(
                new SKPoint(x + offset, 0),
                new SKPoint(x + offset + bandWidth, 0),
                new[]
                {
                    SKColors.Transparent,
                    WithAlpha(GradientStartColor, 0.55f),
                    WithAlpha(GradientEndColor, 0.85f),
                    WithAlpha(GradientStartColor, 0.55f),
                    SKColors.Transparent
                },
                new float[] { 0.0f, 0.35f, 0.5f, 0.65f, 1.0f },
                SKShaderTileMode.Clamp);
                
            gradientPaint.Shader = shader;
            
            // Apply clipping to the shimmer effect area
            canvas.Save();
            // Get proper text bounds including descenders
            var textBounds = new SKRect();
            skFont.MeasureText(text, out textBounds);
            
            // Create a clip rect that accounts for the full text height including descenders
            // The bounds from MeasureText are relative to baseline, so we need to adjust y position
            // Make the clipping area slightly larger to ensure all descenders are covered
            canvas.ClipRect(new SKRect(
                x,                         // Left
                y + textBounds.Top - 2,    // Top (adding the negative top offset from baseline with extra padding)
                x + textWidth,             // Right
                y + textBounds.Bottom + 2  // Bottom (adding the positive bottom offset from baseline with extra padding)
            ));
            
            // Draw shimmer text
            canvas.DrawText(text, x, y, SKTextAlign.Left, skFont, gradientPaint);
            canvas.Restore();
        }
    }

    // Unsubscribe from events when the control is unloaded
    protected override void OnParentSet()
    {
        base.OnParentSet();
        
        if (Parent == null)
        {
            // Control is being removed from the visual tree
            PaintSurface -= OnPaintSurface;
            StopAnimation(); // Ensure animation is stopped when control is unloaded
        }
    }
}
