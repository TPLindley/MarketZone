using Microsoft.Maui.Graphics;

namespace mzWheeler.Controls;

/// <summary>
/// Circular compass gauge with a fixed north-pointing arrow and rotating compass ring
/// </summary>
public class CompassGauge : IDrawable
{
    public double Heading { get; set; }
    public Color ArcColor { get; set; } = Colors.Blue;
    public Color BackgroundArcColor { get; set; } = Colors.Gray;
    public float ArcThickness { get; set; } = 12;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var centerX = dirtyRect.Center.X;
        var centerY = dirtyRect.Center.Y;
        var radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2 - 30; // Leave room for labels

        // Draw background circle
        canvas.StrokeColor = BackgroundArcColor;
        canvas.StrokeSize = ArcThickness;
        canvas.DrawCircle(centerX, centerY, radius);

        // Draw rotating compass ring with cardinal directions
        DrawCompassRing(canvas, centerX, centerY, radius);

        // Draw fixed north-pointing arrow in center
        DrawNorthArrow(canvas, centerX, centerY, radius * 0.6f);

        // Draw heading text on arc at the top
        DrawHeadingText(canvas, centerX, centerY, radius);
    }

    private void DrawCompassRing(ICanvas canvas, float centerX, float centerY, float radius)
    {
        // Cardinal directions to draw on the ring
        var cardinals = new[] 
        { 
            (0, "N"), 
            (45, "NE"), 
            (90, "E"), 
            (135, "SE"), 
            (180, "S"), 
            (225, "SW"), 
            (270, "W"), 
            (315, "NW") 
        };

        canvas.FontSize = 14;

        foreach (var (heading, label) in cardinals)
        {
            // Calculate rotation: subtract current Heading so the ring rotates
            // North (0°) should be at top (270° screen) when Heading = 0
            var screenAngle = (270 + heading - Heading) % 360;
            var radians = screenAngle * Math.PI / 180.0;

            var labelRadius = radius + 15;
            var x = centerX + (float)(labelRadius * Math.Cos(radians));
            var y = centerY + (float)(labelRadius * Math.Sin(radians));

            // Highlight the direction we're currently heading toward
            var currentDirection = GetClosestCardinal(Heading);
            if (label == currentDirection)
            {
                canvas.FontColor = ArcColor;
                canvas.FontSize = 16;
            }
            else
            {
                canvas.FontColor = Colors.Gray;
                canvas.FontSize = 12;
            }

            canvas.DrawString(label, x - 10, y - 6, 20, 12, HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        // Draw tick marks every 30 degrees
        canvas.StrokeColor = Colors.Gray;
        canvas.StrokeSize = 2;

        for (int deg = 0; deg < 360; deg += 30)
        {
            var screenAngle = (270 + deg - Heading) % 360;
            var radians = screenAngle * Math.PI / 180.0;

            var innerRadius = radius - 5;
            var outerRadius = radius + 5;

            var x1 = centerX + (float)(innerRadius * Math.Cos(radians));
            var y1 = centerY + (float)(innerRadius * Math.Sin(radians));
            var x2 = centerX + (float)(outerRadius * Math.Cos(radians));
            var y2 = centerY + (float)(outerRadius * Math.Sin(radians));

            canvas.DrawLine(x1, y1, x2, y2);
        }
    }

    private void DrawNorthArrow(ICanvas canvas, float centerX, float centerY, float length)
    {
        // Draw a fixed arrow pointing straight up (North)
        var arrowPath = new PathF();

        // Arrow tip at top
        arrowPath.MoveTo(centerX, centerY - length);

        // Left side of arrow
        arrowPath.LineTo(centerX - 8, centerY - length + 20);
        arrowPath.LineTo(centerX - 4, centerY - length + 20);
        arrowPath.LineTo(centerX - 4, centerY + length * 0.5f);
        arrowPath.LineTo(centerX + 4, centerY + length * 0.5f);
        arrowPath.LineTo(centerX + 4, centerY - length + 20);
        arrowPath.LineTo(centerX + 8, centerY - length + 20);

        // Close to tip
        arrowPath.Close();

        canvas.FillColor = ArcColor;
        canvas.FillPath(arrowPath);

        // Draw outline
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 1;
        canvas.DrawPath(arrowPath);
    }

    private void DrawHeadingText(ICanvas canvas, float centerX, float centerY, float radius)
    {
        // Draw heading value at the top of the gauge
        var headingText = $"{Heading:F0}°";
        canvas.FontColor = ArcColor;
        canvas.FontSize = 16;
        canvas.DrawString(headingText, centerX - 20, centerY - radius - 25, 40, 20, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private string GetClosestCardinal(double heading)
    {
        var cardinals = new[] 
        { 
            (0, "N"), 
            (45, "NE"), 
            (90, "E"), 
            (135, "SE"), 
            (180, "S"), 
            (225, "SW"), 
            (270, "W"), 
            (315, "NW") 
        };

        var normalized = heading % 360;
        if (normalized < 0) normalized += 360;

        var closest = cardinals[0];
        var minDiff = Math.Min(Math.Abs(normalized - 0), Math.Abs(normalized - 360));

        foreach (var (deg, label) in cardinals)
        {
            var diff = Math.Abs(normalized - deg);
            if (diff < minDiff)
            {
                minDiff = diff;
                closest = (deg, label);
            }
        }

        return closest.Item2;
    }
}
