using Mtf.Drawing.Geometry;
using Mtf.Games.Interfaces;

namespace Mtf.Games.General.Arcade;

/// <summary>
/// A sprite drawn as a circle. Collisions use its bounding box.
/// </summary>
public class BallSprite : Sprite
{
    public BallSprite(float radius) : base(0f, 0f, radius * 2f, radius * 2f)
    {
    }

    public float Radius => Width / 2f;

    public bool Fill { get; set; } = true;

    public CircleF Circle => new(CenterX, CenterY, Radius);

    public override void Draw(ICanvas canvas)
    {
        canvas.DrawDisc(CenterX, CenterY, Radius, Fill);
    }
}
