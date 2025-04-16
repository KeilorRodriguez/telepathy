using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Font = Microsoft.Maui.Graphics.Font;

namespace Telepathic.Pages.Controls;

/// <summary>
/// ChatGPT‑style animated “thinking” header.
/// </summary>
public sealed class AnimatedGradientText : GraphicsView
{
    /*───────────────────────────  Bindables  ───────────────────────────*/

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string),
            typeof(AnimatedGradientText), "Thinking…",
            propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (AnimatedGradientText)bindable;
                    ctrl.Invalidate(); // Redraw whenever text changes
                }
            );

    public static readonly BindableProperty BaseFontColorProperty =
        BindableProperty.Create(nameof(BaseFontColor), typeof(Color),
            typeof(AnimatedGradientText), Color.FromArgb("#8c8c8c"),
            propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (AnimatedGradientText)bindable;
                    ctrl.Invalidate(); // Redraw whenever text changes
                }
            );

    public static readonly BindableProperty ShowDebugBackgroundProperty =
            BindableProperty.Create(
                nameof(ShowDebugBackground),
                typeof(bool),
                typeof(AnimatedGradientText),
                defaultValue: false,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (AnimatedGradientText)bindable;
                    ctrl.Invalidate(); // Redraw when debug setting changes
                }
            );


    public static readonly BindableProperty GradientStartColorProperty =
        BindableProperty.Create(nameof(GradientStartColor), typeof(Color),
            typeof(AnimatedGradientText), Color.FromArgb("#8c8c8c"));

    public static readonly BindableProperty GradientEndColorProperty =
        BindableProperty.Create(nameof(GradientEndColor), typeof(Color),
            typeof(AnimatedGradientText), Color.FromArgb("#8c8c8c"));

    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(nameof(FontSize), typeof(float),
            typeof(AnimatedGradientText), 20f,
            propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (AnimatedGradientText)bindable;
                    ctrl.Invalidate(); // Redraw whenever text changes
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
    private float _progress;           // 0 → 1
    private readonly IDrawable _drawer;

    public AnimatedGradientText()
    {
        _drawer = new ShimmerDrawable(this);
        Drawable = _drawer;

        HeightRequest = 32;
        HorizontalOptions = LayoutOptions.Start;

        StartAnimation();
    }

    private void StartAnimation()
    {
        new Animation(v =>
            {
                _progress = (float)v;
                Invalidate();
            },
            0, 1, Easing.Linear)
        .Commit(this, "shimmer", 16, 1250, finished: (_, __) => StartAnimation());
    }

    /*─────────────────────────  Drawable  ──────────────────────────────*/
    private sealed class ShimmerDrawable : IDrawable
    {
        private readonly AnimatedGradientText _owner;
        private static readonly Color Transparent = Colors.Transparent;

        public ShimmerDrawable(AnimatedGradientText owner) => _owner = owner;

        public void Draw(ICanvas canvas, RectF dirty)
        {
            if (_owner.ShowDebugBackground)
                {
                    canvas.FillColor = Colors.Red.WithAlpha(0.3f);
                    canvas.FillRectangle(0, 0, dirty.Width, dirty.Height);

                    // Draw frame
                    canvas.StrokeColor = Colors.Yellow;
                    canvas.StrokeSize = 1;
                    canvas.DrawRectangle(0, 0, dirty.Width, dirty.Height);
                }

            string text = _owner.Text ?? "";
            if (string.IsNullOrWhiteSpace(text)) return;

            /*─ 1. Measure ─*/
            canvas.Font = Font.DefaultBold;
            canvas.FontSize = _owner.FontSize;
            SizeF txtSize = canvas.GetStringSize(text, Font.DefaultBold, _owner.FontSize);

            float x = 0;
            float y = (dirty.Height - txtSize.Height) / 2f;

            /*─ 2. Base (light gray) ─*/
            canvas.FontColor = _owner.BaseFontColor;//.WithAlpha(0.5f);
            canvas.DrawString(text, x, y, dirty.Width, dirty.Height, HorizontalAlignment.Left, VerticalAlignment.Top);

            /*─ 3. Shimmer overlay ─*/
            canvas.SaveState();
            canvas.ClipRectangle(x, y, txtSize.Width, txtSize.Height);

            // 200 %‑width band like CSS `background-size:200% 100%`
            float bandWidth = txtSize.Width * 2f;
            float offset    = ( _owner._progress * bandWidth ) - bandWidth;

            var grad = new LinearGradientPaint
            {
                StartPoint = new PointF(x + offset, y),
                EndPoint   = new PointF(x + offset + bandWidth, y),
                GradientStops = new[]
                {
                    new PaintGradientStop(0.00f, Transparent),
                    new PaintGradientStop(0.35f, _owner.GradientStartColor.WithAlpha(0.55f)),
                    new PaintGradientStop(0.50f, _owner.GradientEndColor.WithAlpha(0.85f)),
                    new PaintGradientStop(0.65f, _owner.GradientStartColor.WithAlpha(0.55f)),
                    new PaintGradientStop(1.00f, Transparent)
                }
            };

            canvas.SetFillPaint(grad, dirty);
            canvas.DrawString(text, x, y, dirty.Width, dirty.Height, HorizontalAlignment.Left, VerticalAlignment.Top);   // same position, gradient fill
            canvas.RestoreState();
        }
    }
}
