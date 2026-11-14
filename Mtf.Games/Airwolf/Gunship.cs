using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;

namespace Mtf.Games.Airwolf;

/// <summary>
/// The boss at the end of a level: a heavy gunship that flies in from the right, hovers on a wavy path
/// and fires spreads of shells at the helicopter. It takes several hits, and ramming it only hurts the player.
/// </summary>
public sealed class Gunship : AirwolfEnemy
{
    private const int HitFlashTicks = 3;

    private static readonly float[] RotorSpans = [0.5f, 0.15f, 0.35f];

    private readonly float waveCenter;
    private readonly float waveHeight;
    private readonly float hoverX;
    private int age;
    private int flash;

    /// <param name="x">Where the gunship enters the screen.</param>
    /// <param name="centerLine">Height of the top of the gunship in the middle of its wave.</param>
    /// <param name="hoverX">Where it stops to hover.</param>
    public Gunship(float x, float centerLine, float width, float height, float swing, float hoverX, int hitPoints)
        : base(x, centerLine, width, height, 1000)
    {
        waveCenter = centerLine;
        waveHeight = swing;
        this.hoverX = hoverX;
        MaxHitPoints = Math.Max(1, hitPoints);
        HitPoints = MaxHitPoints;
    }

    public int MaxHitPoints { get; }

    public int HitPoints { get; private set; }

    public Cooldown FireCooldown { get; } = new(40);

    /// <summary>
    /// True when the gunship has reached its place and fights.
    /// </summary>
    public bool IsHovering => X <= hoverX;

    /// <summary>
    /// Muzzle position of the belly gun, where the shells start from.
    /// </summary>
    public (float X, float Y) Muzzle => (X + Width * 0.12f, Y + Height * 0.85f);

    /// <summary>
    /// Takes one hit. Returns true when that was the last one.
    /// </summary>
    public bool Hit()
    {
        HitPoints = Math.Max(0, HitPoints - 1);
        flash = HitFlashTicks;
        return HitPoints == 0;
    }

    public override void Update()
    {
        age++;
        if (flash > 0)
        {
            flash--;
        }

        if (IsHovering)
        {
            X = hoverX;
        }
        else
        {
            X += VelocityX;
        }

        Y = waveCenter + MathF.Sin(age * 0.06f) * waveHeight;
    }

    public override void Draw(ICanvas canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        var hull = flash > 0 ? AirwolfColors.GunshipHit : AirwolfColors.GunshipHull;
        var armor = flash > 0 ? AirwolfColors.GunshipHit : AirwolfColors.GunshipArmor;
        var line = MathF.Max(1f, Height * 0.04f);
        var bodyTop = Y + Height * 0.3f;
        var bodyHeight = Height * 0.45f;
        var rotorY = Y + Height * 0.08f;
        var rotorHalf = Width * 0.5f * RotorSpans[age % RotorSpans.Length];
        var muzzle = Muzzle;

        // The tail points right, the nose and the belly gun left, towards the helicopter.
        canvas.DrawSegment(CenterX - rotorHalf, rotorY, CenterX + rotorHalf, rotorY, AirwolfColors.Rotor, line * 1.5f);
        canvas.DrawSegment(CenterX, rotorY, CenterX, bodyTop, AirwolfColors.GunshipDark, line * 2f);
        canvas.DrawSegment(X + Width * 0.7f, bodyTop + bodyHeight * 0.3f, Right, bodyTop + bodyHeight * 0.1f, hull, line * 3f);
        canvas.DrawSegment(Right, bodyTop - Height * 0.1f, Right, bodyTop + Height * 0.3f, AirwolfColors.Rotor, line);
        canvas.DrawBox(X + Width * 0.12f, bodyTop, Width * 0.62f, bodyHeight, true, hull);
        canvas.DrawBox(X + Width * 0.12f, bodyTop + bodyHeight * 0.65f, Width * 0.62f, bodyHeight * 0.35f, true, armor);
        canvas.DrawBox(X, bodyTop + bodyHeight * 0.1f, Width * 0.18f, bodyHeight * 0.55f, true, AirwolfColors.Glass);
        canvas.DrawBox(X + Width * 0.4f, bodyTop + bodyHeight * 0.2f, Width * 0.22f, bodyHeight * 0.3f, true, AirwolfColors.GunshipDark);
        canvas.DrawBox(X + Width * 0.3f, bodyTop - Height * 0.07f, Width * 0.3f, Height * 0.07f, true, AirwolfColors.GunshipDark);
        canvas.DrawSegment(X + Width * 0.3f, bodyTop + bodyHeight, muzzle.X, muzzle.Y, AirwolfColors.GunshipDark, line * 2f);
        canvas.DrawBox(muzzle.X - Width * 0.02f, muzzle.Y - Width * 0.02f, Width * 0.04f, Width * 0.04f, true, AirwolfColors.Muzzle);
        canvas.DrawSegment(X + Width * 0.2f, Y + Height * 0.97f, X + Width * 0.68f, Y + Height * 0.97f, AirwolfColors.Rotor, line);
        canvas.DrawSegment(X + Width * 0.3f, Y + Height * 0.75f, X + Width * 0.3f, Y + Height * 0.97f, AirwolfColors.Rotor, line);
        canvas.DrawSegment(X + Width * 0.6f, Y + Height * 0.75f, X + Width * 0.6f, Y + Height * 0.97f, AirwolfColors.Rotor, line);

        // The remaining armor above the gunship.
        var barHeight = MathF.Max(2f, Height * 0.08f);
        var barTop = Y - barHeight * 2f;
        canvas.DrawBox(X, barTop, Width, barHeight, true, AirwolfColors.BossBarBack);
        canvas.DrawBox(X, barTop, Width * HitPoints / MaxHitPoints, barHeight, true, AirwolfColors.BossBar);
    }
}
