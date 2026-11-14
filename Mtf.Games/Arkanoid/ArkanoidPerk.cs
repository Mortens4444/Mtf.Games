using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;
using System.Drawing;

namespace Mtf.Games.Arkanoid;

/// <summary>
/// What a caught perk does.
/// </summary>
public enum ArkanoidPerkKind
{
    /// <summary>
    /// The paddle gets twice as wide for a while.
    /// </summary>
    WidePaddle,

    /// <summary>
    /// The ball moves slower for a while.
    /// </summary>
    SlowBall,

    /// <summary>
    /// The ball sticks to the paddle for a while, and is launched again with the center or the up button.
    /// </summary>
    StickyPaddle,

    /// <summary>
    /// One more life.
    /// </summary>
    ExtraLife
}

/// <summary>
/// A bonus that falls from a destroyed brick. The paddle has to catch it.
/// Every kind has its own mark inside the box, so the kinds can be told apart without colors too.
/// </summary>
public sealed class ArkanoidPerk(float x, float y, float width, float height, ArkanoidPerkKind kind) : Sprite(x, y, width, height)
{
    public ArkanoidPerkKind Kind { get; } = kind;

    public override void Draw(ICanvas canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        canvas.DrawBox(X, Y, Width, Height, true, ColorOf(Kind));
        canvas.DrawBox(X, Y, Width, Height, false);

        switch (Kind)
        {
            case ArkanoidPerkKind.WidePaddle:
                canvas.DrawSegment(X + Width * 0.2f, CenterY, X + Width * 0.8f, CenterY);
                canvas.DrawSegment(X + Width * 0.2f, Y + Height * 0.25f, X + Width * 0.2f, Y + Height * 0.75f);
                canvas.DrawSegment(X + Width * 0.8f, Y + Height * 0.25f, X + Width * 0.8f, Y + Height * 0.75f);
                break;
            case ArkanoidPerkKind.SlowBall:
                canvas.DrawDisc(CenterX, CenterY, MathF.Max(1f, Height * 0.25f));
                break;
            case ArkanoidPerkKind.StickyPaddle:
                canvas.DrawBox(X + Width * 0.15f, CenterY, Width * 0.7f, Height * 0.45f);
                break;
            default:
                canvas.DrawSegment(CenterX, Y + Height * 0.2f, CenterX, Y + Height * 0.8f);
                canvas.DrawSegment(X + Width * 0.25f, CenterY, X + Width * 0.75f, CenterY);
                break;
        }
    }

    private static Color ColorOf(ArkanoidPerkKind kind)
    {
        return kind switch
        {
            ArkanoidPerkKind.WidePaddle => Color.Gold,
            ArkanoidPerkKind.SlowBall => Color.LightSkyBlue,
            ArkanoidPerkKind.StickyPaddle => Color.Plum,
            _ => Color.HotPink
        };
    }
}
