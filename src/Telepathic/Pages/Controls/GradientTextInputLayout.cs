using Microsoft.Maui.Graphics;
using Syncfusion.Maui.Toolkit.TextInputLayout;
using System.ComponentModel;
using System.Diagnostics;

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

                // Draw our custom gradient border AFTER the base drawing
                // This ensures the content is drawn first and our border is on top
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
            
            // Set the stroke size based on focus state - use IsFocused instead of IsLayoutFocused
            float strokeSize = (float)(Content.IsFocused ? FocusedStrokeThickness : UnfocusedStrokeThickness);
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
            
            // Only show hint gap when the hint is actually floating above the input
            bool shouldShowHintGap = ContainerType == ContainerType.Outlined && 
                                    !string.IsNullOrEmpty(Hint) &&
                                    ShowHint &&
                                    (Content.IsFocused || 
                                     IsHintAlwaysFloated || 
                                     (Content is Entry entry && !string.IsNullOrEmpty(entry.Text)));
            
            if (shouldShowHintGap)
            {
                // Calculate hint text width with better precision
                float hintWidth = Hint.Length * 9f; // Improved width calculation
                float gapStart = x + 12;
                float gapWidth = hintWidth + 24; // Add extra padding around text
                
                // Draw top line with a gap for the hint text
                // First part of top line (before gap)
                RectF topLine1 = new RectF(
                    x + cornerRadius, 
                    y,
                    gapStart - x - cornerRadius,
                    strokeWidth
                );
                if (topLine1.Width > 0)
                {
                    canvas.SetFillPaint(StrokeBrush, topLine1);
                    canvas.FillRectangle(topLine1);
                }
                
                // Second part of top line (after gap)
                RectF topLine2 = new RectF(
                    gapStart + gapWidth, 
                    y,
                    right - cornerRadius - (gapStart + gapWidth),
                    strokeWidth
                );
                if (topLine2.Width > 0)
                {
                    canvas.SetFillPaint(StrokeBrush, topLine2);
                    canvas.FillRectangle(topLine2);
                }
            }
            else 
            {
                // Draw complete top line (between the rounded corners)
                RectF topLine = new RectF(
                    x + cornerRadius, 
                    y,
                    rect.Width - diameter, 
                    strokeWidth
                );
                canvas.SetFillPaint(StrokeBrush, topLine);
                canvas.FillRectangle(topLine);
            }
            
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
            
            // Only show hint gap when the hint is actually floating above the input
            bool shouldShowHintGap = ContainerType == ContainerType.Outlined && 
                                    !string.IsNullOrEmpty(Hint) &&
                                    ShowHint &&
                                    (Content.IsFocused || 
                                     IsHintAlwaysFloated || 
                                     (Content is Entry entry && !string.IsNullOrEmpty(entry.Text)));
            
            if (shouldShowHintGap)
            {
                // Calculate hint text width with better precision
                float hintWidth = Hint.Length * 9f; // Improved width calculation
                float gapStart = rect.X + 8;
                float gapWidth = hintWidth + 16; // Add extra padding around text
                
                // First part of top line (before gap)
                canvas.FillRectangle(rect.X, rect.Y, gapStart - rect.X, strokeWidth);
                
                // Second part of top line (after gap)
                float secondPartWidth = rect.Width - (gapStart - rect.X) - gapWidth;
                if (secondPartWidth > 0)
                {
                    canvas.FillRectangle(gapStart + gapWidth, rect.Y, secondPartWidth, strokeWidth);
                }
            }
            else
            {
                // Draw full top line
                canvas.FillRectangle(rect.X, rect.Y, rect.Width, strokeWidth);
            }
            
            // Draw right, bottom and left sides
            canvas.FillRectangle(rect.X + rect.Width - strokeWidth, rect.Y, strokeWidth, rect.Height);
            canvas.FillRectangle(rect.X, rect.Y + rect.Height - strokeWidth, rect.Width, strokeWidth);
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
            // Use the same constants as SfTextInputLayout
            const float outlineBorderPadding = 8f; // THE MAGIC NUMBER FROM THE ANCIENT SCROLLS!
            
            // Calculate dimensions that match the standard SfTextInputLayout
            float x, y, width, height;
            
            // Calculate leading/trailing view adjustments
            float leadingWidth = 0;
            if (ShowLeadingView && LeadingView != null)
            {
                // Add space for leading view
                if (LeadingViewPosition == ViewPosition.Outside)
                {
                    leadingWidth = (float)LeadingView.Width + 12; // 12 is the standard padding
                }
            }
            
            float trailingWidth = 0;
            if (ShowTrailingView && TrailingView != null)
            {
                // Add space for trailing view
                if (TrailingViewPosition == ViewPosition.Outside)
                {
                    trailingWidth = (float)TrailingView.Width + 12; // 12 is the standard padding
                }
            }
            
            // Calculate the outline rectangle differently based on container type
            if (ContainerType == ContainerType.Outlined)
            {
                // For outlined containers
                x = leadingWidth + 2;
                y = outlineBorderPadding + 2;
                width = (float)Width - leadingWidth - trailingWidth - 4;
                
                // IMPROVED HEIGHT CALCULATION:
                height = (float)Height;

                // Calculate space needed for floating hint (when present)
                if (!string.IsNullOrEmpty(Hint))
                {
                    // Estimate floating hint height based on typical font sizes and spacing
                    // Font size is typically 12px with ~4px padding
                    float hintHeight = 16f;
                    height -= hintHeight;
                }
                
                // Calculate space for helper/error text (when present)
                if (!string.IsNullOrEmpty(HelperText) || !string.IsNullOrEmpty(ErrorText))
                {
                    // Helper/error text is typically 12px with ~4-8px padding
                    float textHeight = 16f;
                    height -= textHeight;
                }
                
                // Apply the outline border padding (top and bottom)
                height -= (outlineBorderPadding * 2);
            }
            else
            {
                // For filled/none containers
                x = leadingWidth + 2;
                y = 0;
                width = (float)Width - leadingWidth - trailingWidth - 4;
                height = (float)Height;
                
                // Adjust for helper/error text
                if (!string.IsNullOrEmpty(HelperText) || !string.IsNullOrEmpty(ErrorText))
                {
                    height -= 20f; // Height for helper/error text area
                }
                
                // Adjust for filled container type (no outline, just bottom line)
                if (ContainerType == ContainerType.Filled)
                {
                    height -= 4f; // Small adjustment for baseline position
                }
            }
            
            return new RectF(x, y, width, height);
        }

        /// <summary>
        /// Helper method to calculate the baseline points for filled or none container types
        /// </summary>
        private void GetBaseLinePoints(out PointF start, out PointF end)
        {
            // For filled or none container types, only draw the bottom line
            float leadingWidth = GetLeftViewPadding();
            float trailingWidth = GetRightViewPadding();
            
            float y = (float)Height - 24f; // Position baseline slightly higher to match standard
            
            // Adjust for helper/error text
            if (!string.IsNullOrEmpty(HelperText) || !string.IsNullOrEmpty(ErrorText))
            {
                y -= 20f; // Standard height for helper/error text
            }
            
            start = new PointF(leadingWidth, y);
            end = new PointF((float)Width - trailingWidth, y);
        }
        
        /// <summary>
        /// Helper method to get padding for views on left side
        /// </summary>
        private float GetLeftViewPadding()
        {
            float padding = 4f; // Standard padding
            
            if (ShowLeadingView && LeadingView != null && LeadingViewPosition == ViewPosition.Outside)
            {
                padding += (float)LeadingView.Width + 8f;
            }
            
            return padding;
        }

        /// <summary>
        /// Helper method to get padding for views on right side
        /// </summary>
        private float GetRightViewPadding()
        {
            float padding = 4f; // Standard padding
            
            if (ShowTrailingView && TrailingView != null && TrailingViewPosition == ViewPosition.Outside)
            {
                padding += (float)TrailingView.Width + 8f;
            }
            
            return padding;
        }
    }
}