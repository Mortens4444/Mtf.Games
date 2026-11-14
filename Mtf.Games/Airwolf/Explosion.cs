using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;

namespace Mtf.Games.Airwolf;

/// <summary>
/// A short lived blast: a flash, an expanding ring and flying sparks.
/// </summary>
public sealed class Explosion : Sprite
{
    private const int SparkCount = 8;

    private readonly int duration;

    public Explosion(float centerX, float centerY, float radius, int duration = 10)
        : base(centerX - radius, centerY - radius, radius * 2f, radius * 2f)
    {
        this.duration = Math.Max(1, duration);
    }

    /// <summary>
    /// Number of game moments since the blast started.
    /// </summary>
    public int Age { get; private set; }

    public override void Update()
    {
        base.Update();
        Age++;
        if (Age >= duration)
        {
            Kill();
        }
    }

    public override void Draw(ICanvas canvas)
    {
        var progress = (float)Age / duration;
        var radius = MathF.Max(1f, Width / 2f * (0.3f + 0.7f * progress));

        // The fireball and the flash in it last only at the beginning, then the ring and the sparks fly apart.
        if (progress < 0.7f)
        {
            canvas.DrawDisc(CenterX, CenterY, radius * 0.8f, true, AirwolfColors.Fire);
        }

        if (progress < 0.4f)
        {
            canvas.DrawDisc(CenterX, CenterY, radius * 0.5f, true, AirwolfColors.Flash);
        }

        canvas.DrawDisc(CenterX, CenterY, radius, false, AirwolfColors.Spark);
        for (var i = 0; i < SparkCount; i++)
        {
            var angle = i * MathF.PI * 2f / SparkCount + 0.4f;
            var cos = MathF.Cos(angle);
            var sin = MathF.Sin(angle);
            canvas.DrawSegment(
                CenterX + cos * radius * 0.5f,
                CenterY + sin * radius * 0.5f,
                CenterX + cos * radius * 1.25f,
                CenterY + sin * radius * 1.25f,
                AirwolfColors.Fire,
                MathF.Max(1f, Width * 0.06f));
        }
    }
}
