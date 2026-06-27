using Microsoft.Maui.Graphics;

namespace mzWheeler.Controls;

/// <summary>
/// 3D-like sphere that visualizes pitch, roll, and yaw
/// </summary>
public class TiltSphere : IDrawable
{
    public double Pitch { get; set; }
    public double Roll { get; set; }
    public double Yaw { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var centerX = dirtyRect.Center.X;
        var centerY = dirtyRect.Center.Y;
        var radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2 - 10;

        // Draw outer sphere (background)
        canvas.FillColor = Color.FromArgb("#1a1a1a");
        canvas.FillCircle(centerX, centerY, radius);

        // Draw horizon line representing pitch and roll
        // Pitch rotates around X-axis, Roll rotates around Y-axis
        var pitchRad = Pitch * Math.PI / 180.0;
        var rollRad = Roll * Math.PI / 180.0;

        // Calculate horizon effect based on pitch
        var horizonOffset = (float)(Math.Sin(pitchRad) * radius * 0.6);

        // Draw sky (upper) and ground (lower) hemispheres
        var skyPath = new PathF();
        var groundPath = new PathF();

        const int segments = 60;
        for (int i = 0; i <= segments; i++)
        {
            var t = (float)i / segments;
            var angle = t * 360f;
            var radians = angle * Math.PI / 180.0;

            // Apply roll rotation to the circle points
            var x = (float)(Math.Cos(radians) * radius);
            var y = (float)(Math.Sin(radians) * radius);

            // Rotate by roll
            var xRot = (float)(x * Math.Cos(rollRad) - y * Math.Sin(rollRad));
            var yRot = (float)(x * Math.Sin(rollRad) + y * Math.Cos(rollRad));

            var finalX = centerX + xRot;
            var finalY = centerY + yRot + horizonOffset;

            if (i == 0)
            {
                skyPath.MoveTo(centerX, centerY);
                groundPath.MoveTo(centerX, centerY);
            }

            // Split sky and ground at the midpoint
            if (yRot + horizonOffset < 0)
            {
                skyPath.LineTo(finalX, finalY);
            }
            else
            {
                groundPath.LineTo(finalX, finalY);
            }
        }

        // Draw sky (blue gradient)
        canvas.FillColor = Color.FromArgb("#2196F3");
        canvas.FillCircle(centerX, centerY, radius * 0.95f);

        // Draw ground (brown/earth)
        var groundGradient = new RadialGradientPaint
        {
            StartColor = Color.FromArgb("#8B4513"),
            EndColor = Color.FromArgb("#654321")
        };
        canvas.SetFillPaint(groundGradient, new RectF(centerX - radius, centerY - radius + horizonOffset, radius * 2, radius * 2));
        canvas.FillEllipse(centerX - radius, centerY - radius + horizonOffset, radius * 2, radius * 2);

        // Draw horizon line with roll
        var horizonPath = new PathF();
        var horizonLeftX = centerX - (float)(radius * Math.Cos(rollRad));
        var horizonLeftY = centerY + horizonOffset - (float)(radius * Math.Sin(rollRad));
        var horizonRightX = centerX + (float)(radius * Math.Cos(rollRad));
        var horizonRightY = centerY + horizonOffset + (float)(radius * Math.Sin(rollRad));

        horizonPath.MoveTo(horizonLeftX, horizonLeftY);
        horizonPath.LineTo(horizonRightX, horizonRightY);

        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 3;
        canvas.DrawPath(horizonPath);

        // Draw center crosshair
        canvas.StrokeColor = Colors.Yellow;
        canvas.StrokeSize = 2;
        canvas.DrawLine(centerX - 15, centerY, centerX + 15, centerY);
        canvas.DrawLine(centerX, centerY - 15, centerX, centerY + 15);
        canvas.DrawCircle(centerX, centerY, 20);

        // Draw yaw indicator (compass ring)
        var yawRad = (Yaw - 90) * Math.PI / 180.0; // -90 to make 0° point up
        var compassRadius = radius + 15;

        // Draw compass cardinal directions
        canvas.StrokeColor = Color.FromArgb("#888");
        canvas.StrokeSize = 1;

        var cardinals = new[] { ("N", 0), ("E", 90), ("S", 180), ("W", 270) };
        foreach (var (label, angle) in cardinals)
        {
            var cardRad = (angle - 90) * Math.PI / 180.0;
            var tickX = centerX + (float)(compassRadius * Math.Cos(cardRad));
            var tickY = centerY + (float)(compassRadius * Math.Sin(cardRad));

            canvas.FontSize = 12;
            canvas.FontColor = Color.FromArgb("#888");
            canvas.DrawString(label, tickX - 6, tickY - 6, 12, 12, HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        // Draw yaw pointer
        var pointerX = centerX + (float)(compassRadius * Math.Cos(yawRad));
        var pointerY = centerY + (float)(compassRadius * Math.Sin(yawRad));

        canvas.StrokeColor = Colors.Red;
        canvas.StrokeSize = 3;
        canvas.DrawLine(centerX, centerY, pointerX, pointerY);

        // Draw pointer tip
        canvas.FillColor = Colors.Red;
        canvas.FillCircle(pointerX, pointerY, 5);

        // Draw outer border
        canvas.StrokeColor = Color.FromArgb("#333");
        canvas.StrokeSize = 2;
        canvas.DrawCircle(centerX, centerY, radius);
    }
}
