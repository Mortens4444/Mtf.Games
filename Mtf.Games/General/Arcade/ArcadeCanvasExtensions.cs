using Mtf.Drawing;
using Mtf.Drawing.Geometry;
using Mtf.Drawing.Render;
using Mtf.Games.Interfaces;
using System.Drawing;

namespace Mtf.Games.General.Arcade;

/// <summary>
/// Drawing helpers that never hand an off-screen coordinate to the canvas.
/// Hosts with a small display (for example the EV3 brick) convert coordinates to bytes,
/// so a negative or too large value would throw there.
/// Every method returns false when nothing was drawn.
/// </summary>
public static class ArcadeCanvasExtensions
{
    /// <summary>
    /// Draws a rectangle clipped to the screen.
    /// </summary>
    public static bool DrawBox(this ICanvas canvas, float x, float y, float width, float height, bool fill = true, Color? color = null)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        if (width <= 0f || height <= 0f)
        {
            return false;
        }

        var left = MathF.Round(x);
        var top = MathF.Round(y);
        var right = left + MathF.Max(1f, MathF.Round(width)) - 1f;
        var bottom = top + MathF.Max(1f, MathF.Round(height)) - 1f;

        left = MathF.Max(left, 0f);
        top = MathF.Max(top, 0f);
        right = MathF.Min(right, canvas.ScreenWidth - 1);
        bottom = MathF.Min(bottom, canvas.ScreenHeight - 1);
        if (right < left || bottom < top)
        {
            return false;
        }

        var rectangle = new RectanglePrimitive
        {
            Rect = new RectF(left, top, right - left + 1f, bottom - top + 1f),
            Fill = fill
        };
        canvas.Draw(rectangle, color ?? Color.Black);
        return true;
    }

    /// <summary>
    /// Draws a circle when its center is on the screen.
    /// </summary>
    public static bool DrawDisc(this ICanvas canvas, float centerX, float centerY, float radius, bool fill = true, Color? color = null)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        if (radius <= 0f)
        {
            return false;
        }

        var x = MathF.Round(centerX);
        var y = MathF.Round(centerY);
        if (x < 0f || y < 0f || x > canvas.ScreenWidth - 1 || y > canvas.ScreenHeight - 1)
        {
            return false;
        }

        var circle = new CirclePrimitive
        {
            Circle = new CircleF(x, y, MathF.Max(1f, MathF.Round(radius))),
            Fill = fill
        };
        canvas.Draw(circle, color ?? Color.Black);
        return true;
    }

    /// <summary>
    /// Draws a line segment clipped to the screen. A host may ignore the thickness, it is one pixel when not set.
    /// </summary>
    public static bool DrawSegment(this ICanvas canvas, float x1, float y1, float x2, float y2, Color? color = null, float thickness = 0f)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        float maxX = canvas.ScreenWidth - 1;
        float maxY = canvas.ScreenHeight - 1;
        if (maxX < 0f || maxY < 0f)
        {
            return false;
        }

        var dx = x2 - x1;
        var dy = y2 - y1;
        var start = 0f;
        var end = 1f;

        // Liang-Barsky clipping against the screen rectangle.
        if (!Clip(-dx, x1, ref start, ref end) || !Clip(dx, maxX - x1, ref start, ref end) ||
            !Clip(-dy, y1, ref start, ref end) || !Clip(dy, maxY - y1, ref start, ref end))
        {
            return false;
        }

        var a = new PointF(Snap(x1 + start * dx, maxX), Snap(y1 + start * dy, maxY));
        var b = new PointF(Snap(x1 + end * dx, maxX), Snap(y1 + end * dy, maxY));
        var line = new LinePrimitive
        {
            Line = new LineF(a, b),
            Thickness = thickness
        };
        canvas.Draw(line, color ?? Color.Black);
        return true;
    }

    /// <summary>
    /// Draws a text when its position is on the screen.
    /// </summary>
    public static bool DrawLabel(this ICanvas canvas, float x, float y, string text, FontType fontType = FontType.Normal, Color? color = null)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        if (String.IsNullOrEmpty(text))
        {
            return false;
        }

        var left = MathF.Round(x);
        var top = MathF.Round(y);
        if (left < 0f || top < 0f || left > canvas.ScreenWidth - 1 || top > canvas.ScreenHeight - 1)
        {
            return false;
        }

        canvas.Draw(new TextPrimitive(left, top, text, fontType), color ?? Color.Black);
        return true;
    }

    private static bool Clip(float p, float q, ref float start, ref float end)
    {
        if (p == 0f)
        {
            return q >= 0f;
        }

        var ratio = q / p;
        if (p < 0f)
        {
            if (ratio > end)
            {
                return false;
            }
            if (ratio > start)
            {
                start = ratio;
            }
        }
        else
        {
            if (ratio < start)
            {
                return false;
            }
            if (ratio < end)
            {
                end = ratio;
            }
        }

        return true;
    }

    private static float Snap(float value, float max)
    {
        return MathF.Max(0f, MathF.Min(max, MathF.Round(value)));
    }
}
