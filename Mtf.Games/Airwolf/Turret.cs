using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;

namespace Mtf.Games.Airwolf;

/// <summary>
/// A ground enemy that travels with the terrain and shoots at the helicopter.
/// </summary>
public sealed class Turret(float x, float y, float size) : AirwolfEnemy(x, y, size, size, 100)
{
    public Cooldown FireCooldown { get; } = new(45);

    public override void Draw(ICanvas canvas)
    {
        var baseTop = Y + Height / 2f;
        canvas.DrawBox(X, baseTop, Width, Height / 2f);
        canvas.DrawSegment(CenterX, baseTop, X, Y);
    }
}
