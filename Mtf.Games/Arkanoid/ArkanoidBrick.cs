using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;

namespace Mtf.Games.Arkanoid;

/// <summary>
/// A brick that needs one or more hits. It is drawn filled while it needs more than one hit.
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
        canvas.DrawBox(X, Y, Width - 1f, Height - 1f, Hits > 1);
    }
}
