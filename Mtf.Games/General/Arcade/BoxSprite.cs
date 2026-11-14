using Mtf.Games.Interfaces;
using System.Drawing;

namespace Mtf.Games.General.Arcade;

/// <summary>
/// A sprite drawn as a single rectangle: paddles, bricks, bullets.
/// </summary>
public class BoxSprite : Sprite
{
    public BoxSprite(float x, float y, float width, float height) : base(x, y, width, height)
    {
    }

    public bool Fill { get; set; } = true;

    /// <summary>
    /// Color of the box. Black when not set.
    /// </summary>
    public Color? Color { get; set; }

    public override void Draw(ICanvas canvas)
    {
        canvas.DrawBox(X, Y, Width, Height, Fill, Color);
    }
}
