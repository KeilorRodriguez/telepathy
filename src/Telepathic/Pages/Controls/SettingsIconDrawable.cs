using Microsoft.Maui.Graphics;

namespace Telepathic.Pages.Controls
{
    // Welcome, space adventurer! This draws a classic gear for your settings icon.
    public class SettingsIconDrawable : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float cx = dirtyRect.Center.X;
            float cy = dirtyRect.Center.Y;
            float r = Math.Min(dirtyRect.Width, dirtyRect.Height) * 0.35f;
            float toothLength = r * 0.35f;
            int teeth = 8;
            float angleStep = 360f / teeth;

            // Draw gear teeth
            for (int i = 0; i < teeth; i++)
            {
                float angle = i * angleStep;
                float rad = (float)(Math.PI * angle / 180.0);
                float x1 = cx + (float)Math.Cos(rad) * r;
                float y1 = cy + (float)Math.Sin(rad) * r;
                float x2 = cx + (float)Math.Cos(rad) * (r + toothLength);
                float y2 = cy + (float)Math.Sin(rad) * (r + toothLength);
                canvas.StrokeColor = Colors.Gray;
                canvas.StrokeSize = 3;
                canvas.DrawLine(x1, y1, x2, y2);
            }

            // Draw gear body
            canvas.StrokeColor = Colors.Gray;
            canvas.StrokeSize = 3;
            canvas.FillColor = Colors.White;
            canvas.FillCircle(cx, cy, r);
            canvas.DrawCircle(cx, cy, r);

            // Draw center hole
            canvas.FillColor = Colors.Gray;
            canvas.FillCircle(cx, cy, r * 0.35f);
        }
    }
}
