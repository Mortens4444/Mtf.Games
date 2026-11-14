using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;

namespace Mtf.Games.Airwolf;

/// <summary>
/// A foot soldier who walks on the ground towards the helicopter and shoots at it with a rifle.
/// The game keeps him on the ground, he only moves sideways.
/// </summary>
public sealed class Soldier(float x, float y, float width, float height) : AirwolfEnemy(x, y, width, height, 25)
{
    private int age;

    public Cooldown FireCooldown { get; } = new(80);

    /// <summary>
    /// Horizontal part of the rifle direction. The direction is a unit vector.
    /// </summary>
    public float AimX { get; set; } = 0.7071f;

    /// <summary>
    /// Vertical part of the rifle direction, positive when the rifle points up.
    /// </summary>
    public float AimY { get; set; } = 0.7071f;

    private float ChestY => Y + Height * 0.35f;

    /// <summary>
    /// Muzzle position, where the bullets start from.
    /// </summary>
    public (float X, float Y) Muzzle => (CenterX + AimX * Width * 1.3f, ChestY - AimY * Width * 1.3f);

    public override void Update()
    {
        age++;
        base.Update();
    }

    public override void Draw(ICanvas canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        var step = age / 3 % 2 == 0 ? 1f : -1f;
        var line = MathF.Max(1f, Width * 0.18f);
        var muzzle = Muzzle;

        canvas.DrawSegment(CenterX - Width * 0.12f, Y + Height * 0.6f, CenterX - Width * 0.12f + step * Width * 0.25f, Bottom, AirwolfColors.Boots, line);
        canvas.DrawSegment(CenterX + Width * 0.12f, Y + Height * 0.6f, CenterX + Width * 0.12f - step * Width * 0.25f, Bottom, AirwolfColors.Boots, line);
        canvas.DrawBox(CenterX - Width * 0.3f, Y + Height * 0.2f, Width * 0.6f, Height * 0.4f, true, AirwolfColors.Uniform);
        canvas.DrawBox(CenterX - Width * 0.2f, Y + Height * 0.06f, Width * 0.4f, Height * 0.14f, true, AirwolfColors.Skin);
        canvas.DrawBox(CenterX - Width * 0.26f, Y, Width * 0.52f, Height * 0.1f, true, AirwolfColors.Helmet);
        canvas.DrawSegment(CenterX, ChestY, muzzle.X, muzzle.Y, AirwolfColors.TurretBarrel, line);
    }
}
