using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Font = Microsoft.Maui.Graphics.Font;

namespace Telepathic.Pages.Controls
{
    public class AnimatedGradientText : GraphicsView
    {
        // Bindable property so you can set Text in XAML or code:
        public static readonly BindableProperty TextProperty =
            BindableProperty.Create(
                nameof(Text),
                typeof(string),
                typeof(AnimatedGradientText),
                defaultValue: "Thinking...",
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

        // New color properties for ultra customization!
        public static readonly BindableProperty BaseFontColorProperty =
            BindableProperty.Create(
                nameof(BaseFontColor),
                typeof(Color),
                typeof(AnimatedGradientText),
                defaultValue: Colors.White,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (AnimatedGradientText)bindable;
                    ctrl.Invalidate();
                }
            );

        public static readonly BindableProperty GradientStartColorProperty =
            BindableProperty.Create(
                nameof(GradientStartColor),
                typeof(Color),
                typeof(AnimatedGradientText),
                defaultValue: Colors.DeepSkyBlue,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (AnimatedGradientText)bindable;
                    ctrl.Invalidate();
                }
            );

        public static readonly BindableProperty GradientEndColorProperty =
            BindableProperty.Create(
                nameof(GradientEndColor),
                typeof(Color),
                typeof(AnimatedGradientText),
                defaultValue: Colors.HotPink,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (AnimatedGradientText)bindable;
                    ctrl.Invalidate();
                }
            );

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

        // Tracks how far along our "shimmer wave" is (0=none, 1=full width):
        private float _progress;

        private readonly TextDrawable _drawable;

        public AnimatedGradientText()
        {
            // Hook up our drawable
            _drawable = new TextDrawable(this);
            Drawable = _drawable;
            
            // Set some defaults
            HeightRequest = 40;
            WidthRequest = 250;
            HorizontalOptions = LayoutOptions.Start;

            // Animate forever:
            StartAnimation();
        }

        private void StartAnimation()
        {
            // Animate _progress from 0→1 every 1.5s, then repeat
            new Animation(
                callback: val =>
                {
                    _progress = (float)val;
                    Invalidate(); // Forces a redraw
                },
                start: 0,
                end: 1,
                easing: Easing.Linear
            )
            .Commit(
                owner: this,
                name: "ShimmerAnimation",
                rate: 16,        // ~60fps
                length: 1500,    // 1.5 seconds per cycle
                finished: (_, __) => StartAnimation()  // repeat forever
            );
        }

        // We'll expose the progress to the drawable
        private class TextDrawable : IDrawable
        {
            private readonly AnimatedGradientText _parent;

            public TextDrawable(AnimatedGradientText parent)
            {
                _parent = parent;
            }

            public void Draw(ICanvas canvas, RectF dirtyRect)
            {
                // Show debug background if requested - this helps us see if control is there
                if (_parent.ShowDebugBackground)
                {
                    canvas.FillColor = Colors.Red.WithAlpha(0.3f);
                    canvas.FillRectangle(0, 0, dirtyRect.Width, dirtyRect.Height);

                    // Draw frame
                    canvas.StrokeColor = Colors.Yellow;
                    canvas.StrokeSize = 1;
                    canvas.DrawRectangle(0, 0, dirtyRect.Width, dirtyRect.Height);
                }

                // Get text or use default
                var text = _parent.Text ?? "Thinking...";
                if (string.IsNullOrEmpty(text))
                    return;

                // Explicitly set font properties - this is crucial!
                float fontSz = 24f; // IMPORTANT: Explicit size helps with visibility
                canvas.Font = Font.DefaultBold;
                canvas.FontSize = fontSz;
                canvas.FontColor = _parent.BaseFontColor; // Now using our customizable base color!

                // Draw text at absolute position - avoid relying on measured text size
                float x = 0;
                float y = (dirtyRect.Height - fontSz) / 2; // Vertical center-ish

                // Draw the base text
                canvas.DrawString(text, x, y, dirtyRect.Width, dirtyRect.Height, HorizontalAlignment.Left, VerticalAlignment.Top);

                // Setup shimmer wave - use fixed width instead of measuring
                float textWidth = dirtyRect.Width - 20f; // Approximate width
                float clipWidth = textWidth * 0.4f; // Fixed width for shimmer effect (30% of text width)

                // Calculate shimmer position based on progress
                float shimmerPosition = -textWidth + (_parent._progress * textWidth * 2); // Move from left to right

                // Apply clip for shimmer region
                canvas.SaveState();
                canvas.ClipRectangle(x + shimmerPosition - (clipWidth / 2), y, clipWidth, fontSz * 1.2f);

                // Create gradient for shimmer effect - with moving gradient relative to shimmer position
                // var gradient = new RadialGradientPaint
                // {
                //     Center = new PointF(x + shimmerPosition, y + (fontSz / 2)),
                //     Radius = clipWidth,
                //     StartColor = _parent.GradientStartColor,
                //     EndColor = _parent.GradientEndColor
                // };

                var gradient = new LinearGradientPaint
                {
                    StartPoint = new PointF(x + shimmerPosition - clipWidth, y),
                    EndPoint = new PointF(x + shimmerPosition + clipWidth, y),
                    GradientStops = new[]
                    {
                        new PaintGradientStop(0.0f, _parent.GradientStartColor.WithAlpha(0.0f)),
                        new PaintGradientStop(0.2f, _parent.GradientStartColor),
                        new PaintGradientStop(0.5f, _parent.GradientEndColor),
                        new PaintGradientStop(0.8f, _parent.GradientStartColor),
                        new PaintGradientStop(1.0f, _parent.GradientStartColor.WithAlpha(0.0f))
                    }
                };

                // gradient.BlendStartAndEndColors(_parent.GradientStartColor, _parent.GradientEndColor, 0.5f);

                // Apply gradient paint
                canvas.SetFillPaint(gradient, dirtyRect);
                // canvas.FontColor = Colors.Transparent;

                // Draw the shimmer text with same position but gradient fill
                canvas.DrawString(text, x, y, dirtyRect.Width, dirtyRect.Height, HorizontalAlignment.Left, VerticalAlignment.Top);

                // Always restore canvas state
                canvas.RestoreState();
            }
        }
    }
}
