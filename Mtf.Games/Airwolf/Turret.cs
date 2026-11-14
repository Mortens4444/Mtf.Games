using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;

namespace Mtf.Games.Airwolf;

/// <summary>
/// A ground enemy that travels with the terrain. Its barrel follows the helicopter,
/// but it cannot point lower than <see cref="MinElevation"/>, so a helicopter flying low is safe from it.
/// </summary>
public sealed class Turret(float x, float y, float size) : AirwolfEnemy(x, y, size, size, 100)
{
    /// <summary>
    /// The lowest angle of the barrel above the horizon, in radians.
    /// </summary>
    public const float MinElevation = 0.26f;

    public Cooldown FireCooldown { get; } = new(45);

    /// <summary>
    /// Horizontal part of the barrel direction. The direction is a unit vector.
    /// </summary>
    public float AimX { get; private set; }

    /// <summary>
    /// Vertical part of the barrel direction, positive when the barrel points up.
    /// </summary>
    public float AimY { get; private set; } = 1f;

    public float PivotX => CenterX;

    public float PivotY => Y + Height * 0.5f;

    private float BarrelLength => Width * 0.9f;

    /// <summary>
    /// Muzzle position, where the shells start from.
    /// </summary>
    public (float X, float Y) Muzzle => (PivotX + AimX * BarrelLength, PivotY - AimY * BarrelLength);

    /// <summary>
    /// Turns the barrel towards the target. Returns false when the target is too low to be aimed at.
    /// </summary>
    public bool AimAt(float targetX, float targetY)
    {
        var dx = targetX - PivotX;
        var dy = PivotY - targetY;
        var length = MathF.Sqrt(dx * dx + dy * dy);
        if (length < 1f)
        {
            return false;
        }

        var reachable = dy / length >= MathF.Sin(MinElevation);
        var angle = reachable
            ? MathF.Atan2(dy, dx)
            : dx >= 0f ? MinElevation : MathF.PI - MinElevation;
        AimX = MathF.Cos(angle);
        AimY = MathF.Sin(angle);
        return reachable;
    }

    public override void Draw(ICanvas canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        var baseTop = PivotY;
        var muzzle = Muzzle;
        var unit = Width / 3f;

        canvas.DrawBox(X, baseTop, Width, Height * 0.5f, true, AirwolfColors.TurretBase);
        canvas.DrawBox(CenterX - Width * 0.2f, baseTop - Height * 0.15f, Width * 0.4f, Height * 0.15f, true, AirwolfColors.TurretBarrel);
        canvas.DrawSegment(PivotX, baseTop - Height * 0.07f, muzzle.X, muzzle.Y, AirwolfColors.TurretBarrel, MathF.Max(1f, Height * 0.1f));
        canvas.DrawBox(muzzle.X - unit * 0.3f, muzzle.Y - unit * 0.3f, unit * 0.6f, unit * 0.6f, true, AirwolfColors.Muzzle);
    }
}
