using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;

namespace Mtf.Games.Airwolf;

/// <summary>
/// A flying enemy that comes from the right on a wavy path.
/// </summary>
public sealed class Drone(float x, float centerLine, float width, float height, float swing) : AirwolfEnemy(x, centerLine, width, height, 50)
{
    private readonly float waveCenter = centerLine;
    private readonly float waveHeight = swing;
    private int age;

    public override void Update()
    {
        age++;
        X += VelocityX;
        Y = waveCenter + MathF.Sin(age * 0.15f) * waveHeight;
    }

    public override void Draw(ICanvas canvas)
    {
        canvas.DrawBox(X, Y, Width, Height, false);
    }
}
