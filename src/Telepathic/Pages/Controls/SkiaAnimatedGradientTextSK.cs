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

    public SkiaAnimatedGradientTextSK()
    {
        HeightRequest = 32;
        HorizontalOptions = LayoutOptions.Start;
        
        // Register the paint surface handler
        PaintSurface += OnPaintSurface;
        
        StartAnimation();
    }

    private void StartAnimation()
    {
        new Animation(v =>
            {
                _progress = (float)v;
                InvalidateSurface();
            },
            0, 1, Easing.Linear)
        .Commit(this, "shimmer", 16, 1250, finished: (_, __) => StartAnimation());
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
        
        // Create paint for text
        using var textPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = ToSKColor(BaseFontColor),
            TextSize = FontSize,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        
        // Measure text
        SKRect textBounds = new SKRect();
        textPaint.MeasureText(text, ref textBounds);
        
        // Calculate position
        float x = 0;
        float y = (e.Info.Height + textBounds.Height) / 2f; // Center vertically
        
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
        
        // Draw base text
        canvas.DrawText(text, x, y, textPaint);
        
        // Setup shimmer
        float bandWidth = textBounds.Width * 2f;
        float offset = (_progress * bandWidth) - bandWidth;
        
        // Create gradient for shimmer effect
        using var gradientPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            TextSize = FontSize,
            Typeface = textPaint.Typeface
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
        canvas.ClipRect(new SKRect(x, y - textBounds.Height, x + textBounds.Width, y));
        
        // Draw shimmer text
        canvas.DrawText(text, x, y, gradientPaint);
        canvas.Restore();
    }

    // Unsubscribe from events when the control is unloaded
    protected override void OnParentSet()
    {
        base.OnParentSet();
        
        if (Parent == null)
        {
            // Control is being removed from the visual tree
            PaintSurface -= OnPaintSurface;
            this.AbortAnimation("shimmer");
        }
    }
}
