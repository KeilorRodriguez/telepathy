using Microsoft.Maui.Graphics;
using Syncfusion.Maui.Toolkit.TextInputLayout;
using System.ComponentModel;

namespace Telepathic.Pages.Controls
{
    /// <summary>
    /// GradientTextInputLayout extends SfTextInputLayout to support gradient borders using Brush property
    /// </summary>
    public class GradientTextInputLayout : SfTextInputLayout
    {
        /// <summary>
        /// Bindable property for StrokeBrush
        /// </summary>
        public static readonly BindableProperty StrokeBrushProperty = BindableProperty.Create(
            nameof(StrokeBrush),
            typeof(Brush),
            typeof(GradientTextInputLayout),
            defaultValue: null,
            propertyChanged: OnStrokeBrushPropertyChanged);

        /// <summary>
        /// Gets or sets the brush used for the stroke/outline of the input layout
        /// </summary>
        public Brush StrokeBrush
        {
            get => (Brush)GetValue(StrokeBrushProperty);
            set => SetValue(StrokeBrushProperty, value);
        }

        private static void OnStrokeBrushPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is GradientTextInputLayout layout)
            {
                // Use Invalidate instead of InvalidateDrawable
                layout.InvalidateMeasure();//.Invalidate();
            }
        }

        /// <summary>
        /// Custom override of OnDraw to implement Brush-based stroke rendering
        /// </summary>
        protected override void OnDraw(ICanvas canvas, RectF dirtyRect)
        {
            if (StrokeBrush != null)
            {
                // Save the current stroke before drawing
                Color originalStroke = this.Stroke;

                // If StrokeBrush is provided, set Stroke to transparent for base draw operations
                // (we'll draw our own border with the brush)
                this.SetValue(StrokeProperty, Colors.Transparent);
                
                // Call the base draw method which will set up various rectangles and state
                base.OnDraw(canvas, dirtyRect);
                
                // Restore the stroke color
                this.SetValue(StrokeProperty, originalStroke);

                // Draw our custom gradient border on top
                DrawCustomBorder(canvas, dirtyRect);
            }
            else
            {
                // Use default drawing if no StrokeBrush is specified
                base.OnDraw(canvas, dirtyRect);
            }
        }

        /// <summary>
        /// Draws a custom border using the StrokeBrush
        /// </summary>
        private void DrawCustomBorder(ICanvas canvas, RectF dirtyRect)
        {
            bool isOutlined = ContainerType == ContainerType.Outlined;
            bool hasRoundCorners = OutlineCornerRadius > 0;
            
            canvas.SaveState();
            
            // Handle RTL layouts - checking TextAlignment instead of using IsRTL
            // if (HorizontalTextAlignment == TextAlignment.End)
            // {
            //     canvas.Translate((float)Width, 0);
            //     canvas.Scale(-1, 1);
            // }

            // Set the stroke size based on focus state - use IsFocused instead of IsLayoutFocused
            float strokeSize = (float)(IsFocused ? FocusedStrokeThickness : UnfocusedStrokeThickness);
            canvas.StrokeSize = strokeSize;
            
            // For outlined container type
            if (isOutlined)
            {
                if (hasRoundCorners)
                {
                    // Draw the rounded outline with the brush
                    if (strokeSize > 0)
                    {
                        // Get the rectangle dimensions from the control
                        RectF outlineRect = GetOutlineRectF();
                        
                        // Draw the outline with our line-segment drawing approach
                        DrawGradientRoundedRectangle(canvas, outlineRect, strokeSize, (float)OutlineCornerRadius, dirtyRect);
                    }
                }
                else
                {
                    // Draw a regular rectangle with the brush
                    RectF outlineRect = GetOutlineRectF();
                    DrawGradientRectangleStroke(canvas, outlineRect, strokeSize, dirtyRect);
                }
            }
            else // For filled or none container types
            {
                // Draw a line at the bottom for filled or none types
                PointF start, end;
                GetBaseLinePoints(out start, out end);
                
                // Draw the gradient line
                DrawGradientLine(canvas, start, end, strokeSize, dirtyRect);
            }
            
            canvas.RestoreState();
        }
        
        /// <summary>
        /// Draws a gradient rounded rectangle outline
        /// </summary>
        private void DrawGradientRoundedRectangle(ICanvas canvas, RectF rect, float strokeWidth, float cornerRadius, RectF dirtyRect)
        {
            // Set up the canvas
            canvas.StrokeColor = Colors.Transparent;
            canvas.FillColor = Colors.Transparent;
            
            // The key to drawing gradient outlines is to draw separate line segments
            // and apply the gradient paint to each one
            float x = rect.X;
            float y = rect.Y;
            float right = x + rect.Width;
            float bottom = y + rect.Height;
            float diameter = cornerRadius * 2;
            
            // Make sure we don't have corners bigger than the rectangle
            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;
            
            // Draw top line (between the rounded corners)
            RectF topLine = new RectF(
                x + cornerRadius, 
                y,
                rect.Width - diameter, 
                strokeWidth
            );
            canvas.SetFillPaint(StrokeBrush, topLine);
            canvas.FillRectangle(topLine);
            
            // Draw right line (between the rounded corners)
            RectF rightLine = new RectF(
                right - strokeWidth,
                y + cornerRadius,
                strokeWidth,
                rect.Height - diameter
            );
            canvas.SetFillPaint(StrokeBrush, rightLine);
            canvas.FillRectangle(rightLine);
            
            // Draw bottom line (between the rounded corners)
            RectF bottomLine = new RectF(
                x + cornerRadius,
                bottom - strokeWidth,
                rect.Width - diameter,
                strokeWidth
            );
            canvas.SetFillPaint(StrokeBrush, bottomLine);
            canvas.FillRectangle(bottomLine);
            
            // Draw left line (between the rounded corners)
            RectF leftLine = new RectF(
                x,
                y + cornerRadius,
                strokeWidth,
                rect.Height - diameter
            );
            canvas.SetFillPaint(StrokeBrush, leftLine);
            canvas.FillRectangle(leftLine);
            
            // Now draw the corner arcs as strokes
            // We have to use individual line segments since we can't directly
            // apply gradient to strokes in MAUI Graphics
            
            // Top-left corner
            DrawCornerArc(canvas, new PointF(x + cornerRadius, y + cornerRadius), 
                           cornerRadius, strokeWidth, StrokeBrush, 180, 270);
                           
            // Top-right corner
            DrawCornerArc(canvas, new PointF(right - cornerRadius, y + cornerRadius), 
                           cornerRadius, strokeWidth, StrokeBrush, 270, 360);
                           
            // Bottom-right corner
            DrawCornerArc(canvas, new PointF(right - cornerRadius, bottom - cornerRadius), 
                           cornerRadius, strokeWidth, StrokeBrush, 0, 90);
                           
            // Bottom-left corner
            DrawCornerArc(canvas, new PointF(x + cornerRadius, bottom - cornerRadius), 
                           cornerRadius, strokeWidth, StrokeBrush, 90, 180);
        }
        
        /// <summary>
        /// Helper method to draw a corner arc with gradient
        /// </summary>
        private void DrawCornerArc(ICanvas canvas, PointF center, float radius, 
                                  float strokeWidth, Brush brush, float startAngle, float endAngle)
        {
            // To draw a gradient arc, we'll approximate it with line segments
            int segments = 8; // Number of line segments to use for the arc
            float angleIncrement = (endAngle - startAngle) / segments;
            
            for (int i = 0; i < segments; i++)
            {
                float angle1 = (startAngle + i * angleIncrement) * (float)Math.PI / 180;
                float angle2 = (startAngle + (i + 1) * angleIncrement) * (float)Math.PI / 180;
                
                float x1 = center.X + (radius - strokeWidth/2) * (float)Math.Cos(angle1);
                float y1 = center.Y + (radius - strokeWidth/2) * (float)Math.Sin(angle1);
                float x2 = center.X + (radius - strokeWidth/2) * (float)Math.Cos(angle2);
                float y2 = center.Y + (radius - strokeWidth/2) * (float)Math.Sin(angle2);
                
                // Create a small rectangle for this segment
                float minX = Math.Min(x1, x2);
                float minY = Math.Min(y1, y2);
                float width = Math.Abs(x2 - x1) + strokeWidth;
                float height = Math.Abs(y2 - y1) + strokeWidth;
                
                // Adjust for small segments
                if (width < strokeWidth) width = strokeWidth;
                if (height < strokeWidth) height = strokeWidth;
                
                RectF segmentRect = new RectF(minX, minY, width, height);
                canvas.SetFillPaint(brush, segmentRect);
                
                // Draw line as a thin rectangle
                PathF path = new PathF();
                path.MoveTo(x1, y1);
                path.LineTo(x2, y2);
                
                // Create a stroked version of the path by drawing a filled rectangle
                float angle = (float)Math.Atan2(y2 - y1, x2 - x1);
                float dx = (float)Math.Sin(angle) * (strokeWidth / 2);
                float dy = (float)Math.Cos(angle) * (strokeWidth / 2);
                
                PathF strokePath = new PathF();
                strokePath.MoveTo(x1 - dx, y1 + dy);
                strokePath.LineTo(x2 - dx, y2 + dy);
                strokePath.LineTo(x2 + dx, y2 - dy);
                strokePath.LineTo(x1 + dx, y1 - dy);
                strokePath.Close();
                
                // Fill the stroke path with our gradient
                canvas.FillPath(strokePath);
            }
        }
        
        /// <summary>
        /// Draws a gradient rectangle stroke
        /// </summary>
        private void DrawGradientRectangleStroke(ICanvas canvas, RectF rect, float strokeWidth, RectF dirtyRect)
        {
            // Draw the outer rectangle
            canvas.StrokeColor = Colors.Transparent;
            canvas.FillColor = Colors.Transparent;
            canvas.SetFillPaint(StrokeBrush, dirtyRect);
            
            // Draw top line
            canvas.FillRectangle(rect.X, rect.Y, rect.Width, strokeWidth);
            
            // Draw right line
            canvas.FillRectangle(rect.X + rect.Width - strokeWidth, rect.Y, strokeWidth, rect.Height);
            
            // Draw bottom line
            canvas.FillRectangle(rect.X, rect.Y + rect.Height - strokeWidth, rect.Width, strokeWidth);
            
            // Draw left line
            canvas.FillRectangle(rect.X, rect.Y, strokeWidth, rect.Height);
        }
        
        /// <summary>
        /// Draws a gradient line between two points
        /// </summary>
        private void DrawGradientLine(ICanvas canvas, PointF start, PointF end, float strokeWidth, RectF dirtyRect)
        {
            // Save state
            canvas.SaveState();
            
            // Calculate the line rectangle
            RectF lineRect = new RectF(
                start.X,
                start.Y - (strokeWidth / 2),
                end.X - start.X,
                strokeWidth
            );
            
            // Fill with gradient
            canvas.FillColor = Colors.Transparent;
            canvas.SetFillPaint(StrokeBrush, lineRect);
            canvas.FillRectangle(lineRect);
            
            canvas.RestoreState();
        }

        /// <summary>
        /// Helper method to access the outline rectangle from the base class
        /// </summary>
        private RectF GetOutlineRectF()
        {
            // Simplified rectangle calculation - we'll use standard padding and calculations
            // instead of accessing internal properties
            
            float x, y, width, height;
            float padding = 12f; // Standard padding
            
            x = padding;
            y = padding;
            width = (float)(Width - (padding * 2));
            height = (float)(Height - (padding * 2));
            
            // Adjust for container type
            if (ContainerType == ContainerType.Outlined)
            {
                // Give space for the hint text at the top
                float hintHeight = 20f; // Approximation for hint text height 
                y += hintHeight / 2;
                height -= hintHeight;
            }
            
            // Adjust for helper/error text at the bottom
            float helperTextPadding = 0;
            if (!string.IsNullOrEmpty(HelperText) || !string.IsNullOrEmpty(ErrorText))
            {
                helperTextPadding = 24f; // Approximation for helper/error text height
            }
            
            height -= helperTextPadding;
            
            return new RectF(x, y, width, height);
        }

        /// <summary>
        /// Helper method to calculate the baseline points for filled or none container types
        /// </summary>
        private void GetBaseLinePoints(out PointF start, out PointF end)
        {
            float padding = 12f; // Standard padding
            float helperTextPadding = 0;
            
            // Adjust for helper/error text at the bottom
            if (!string.IsNullOrEmpty(HelperText) || !string.IsNullOrEmpty(ErrorText))
            {
                helperTextPadding = 24f; // Approximation for helper/error text height
            }
            
            float y = (float)(Height - padding - helperTextPadding);
            
            start = new PointF(padding, y);
            end = new PointF((float)(Width - padding), y);
        }
    }
}