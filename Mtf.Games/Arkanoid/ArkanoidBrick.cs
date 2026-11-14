using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;

namespace Mtf.Games.Arkanoid;

/// <summary>
/// A brick that needs one or more hits. Without a color it is drawn filled while it needs more than one hit.
/// With a color it is a colored box with a black frame, and a black core while it needs more than one hit.
/// The core keeps the two kinds of bricks apart on a display that has no colors.
/// </summary>
public sealed class ArkanoidBrick(float x, float y, float width, float height, int hits) : BoxSprite(x, y, width, height)
{
    public int Hits { get; private set; } = hits;

    /// <summary>
    /// Registers a hit and returns true when the brick was destroyed by it.
    /// </summary>
    public bool Hit()
    {
        Hits--;
        if (Hits > 0)
        {
            return false;
        }

        Kill();
        return true;
    }

    public override void Draw(ICanvas canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        var width = Width - 1f;
        var height = Height - 1f;
        if (Color is not { } fill)
        {
            canvas.DrawBox(X, Y, width, height, Hits > 1);
            return;
        }

        canvas.DrawBox(X, Y, width, height, true, fill);
        canvas.DrawBox(X, Y, width, height, false);
        if (Hits > 1)
        {
            var inset = MathF.Max(1f, MathF.Min(width, height) * 0.25f);
            canvas.DrawBox(X + inset, Y + inset, width - inset * 2f, height - inset * 2f);
        }
    }
}
