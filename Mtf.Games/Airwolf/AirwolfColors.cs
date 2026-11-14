using System.Drawing;

namespace Mtf.Games.Airwolf;

/// <summary>
/// The colors of Airwolf. A host that cannot show colors simply draws everything in its own way.
/// </summary>
public static class AirwolfColors
{
    /// <summary>
    /// Day on the first level, dusk on the next ones.
    /// </summary>
    public static readonly AirwolfTheme Day = new(
        SkyTop: Color.FromArgb(40, 90, 170),
        SkyHorizon: Color.FromArgb(205, 230, 245),
        Sun: Color.FromArgb(255, 236, 140),
        SunHeight: 0.2f,
        Cloud: Color.FromArgb(250, 252, 255),
        FarMountain: Color.FromArgb(135, 160, 200),
        NearMountain: Color.FromArgb(90, 115, 150),
        Grass: Color.FromArgb(70, 150, 60),
        Soil: Color.FromArgb(120, 85, 50),
        DeepSoil: Color.FromArgb(88, 60, 36),
        Tree: Color.FromArgb(35, 100, 50),
        Bush: Color.FromArgb(75, 140, 55));

    public static readonly AirwolfTheme Dusk = new(
        SkyTop: Color.FromArgb(45, 35, 95),
        SkyHorizon: Color.FromArgb(250, 150, 90),
        Sun: Color.FromArgb(255, 205, 100),
        SunHeight: 0.48f,
        Cloud: Color.FromArgb(255, 190, 170),
        FarMountain: Color.FromArgb(150, 105, 150),
        NearMountain: Color.FromArgb(95, 65, 105),
        Grass: Color.FromArgb(55, 105, 60),
        Soil: Color.FromArgb(95, 65, 50),
        DeepSoil: Color.FromArgb(62, 42, 38),
        Tree: Color.FromArgb(25, 65, 45),
        Bush: Color.FromArgb(50, 95, 55));

    public static readonly Color TreeTrunk = Color.FromArgb(80, 55, 35);
    public static readonly Color Shadow = Color.FromArgb(90, 0, 0, 0);

    public static readonly Color HudBar = Color.FromArgb(25, 30, 48);
    public static readonly Color HudText = Color.FromArgb(245, 245, 250);
    public static readonly Color HudTrack = Color.FromArgb(60, 68, 95);
    public static readonly Color HudProgress = Color.FromArgb(255, 200, 50);

    public static readonly Color HelicopterBody = Color.FromArgb(60, 85, 55);
    public static readonly Color HelicopterDetail = Color.FromArgb(35, 50, 35);
    public static readonly Color Glass = Color.FromArgb(170, 220, 250);
    public static readonly Color Rotor = Color.FromArgb(50, 50, 55);

    public static readonly Color DroneBody = Color.FromArgb(190, 40, 40);
    public static readonly Color DroneNose = Color.FromArgb(120, 20, 20);

    public static readonly Color TurretBase = Color.FromArgb(90, 90, 95);
    public static readonly Color TurretBarrel = Color.FromArgb(45, 45, 50);
    public static readonly Color Muzzle = Color.FromArgb(230, 120, 30);

    public static readonly Color Skin = Color.FromArgb(235, 190, 150);
    public static readonly Color Helmet = Color.FromArgb(70, 80, 50);
    public static readonly Color Uniform = Color.FromArgb(110, 120, 70);
    public static readonly Color Boots = Color.FromArgb(50, 40, 30);

    public static readonly Color GunshipHull = Color.FromArgb(75, 80, 100);
    public static readonly Color GunshipArmor = Color.FromArgb(150, 35, 40);
    public static readonly Color GunshipDark = Color.FromArgb(40, 42, 55);
    public static readonly Color GunshipHit = Color.FromArgb(255, 255, 255);
    public static readonly Color BossBarBack = Color.FromArgb(60, 20, 25);
    public static readonly Color BossBar = Color.FromArgb(230, 50, 50);

    public static readonly Color BombBody = Color.FromArgb(70, 72, 82);
    public static readonly Color BombBand = Color.FromArgb(230, 180, 40);
    public static readonly Color BombFin = Color.FromArgb(40, 42, 52);

    public static readonly Color Tracer = Color.FromArgb(255, 220, 40);
    public static readonly Color Flak = Color.FromArgb(255, 110, 30);
    public static readonly Color Bullet = Color.FromArgb(255, 245, 200);

    public static readonly Color Flash = Color.FromArgb(255, 245, 160);
    public static readonly Color Fire = Color.FromArgb(255, 120, 20);
    public static readonly Color Spark = Color.FromArgb(255, 200, 50);

    public static AirwolfTheme ForLevel(int level)
    {
        return level > 1 ? Dusk : Day;
    }

    public static Color Blend(Color from, Color to, float amount)
    {
        var t = Math.Max(0f, Math.Min(1f, amount));
        return Color.FromArgb(
            (int)MathF.Round(from.R + (to.R - from.R) * t),
            (int)MathF.Round(from.G + (to.G - from.G) * t),
            (int)MathF.Round(from.B + (to.B - from.B) * t));
    }
}

/// <summary>
/// The colors of the scenery of one level. The sky blends from <see cref="SkyTop"/> to <see cref="SkyHorizon"/>.
/// <see cref="SunHeight"/> is the position of the sun from the top of the field as a part of its height.
/// </summary>
public sealed record AirwolfTheme(
    Color SkyTop,
    Color SkyHorizon,
    Color Sun,
    float SunHeight,
    Color Cloud,
    Color FarMountain,
    Color NearMountain,
    Color Grass,
    Color Soil,
    Color DeepSoil,
    Color Tree,
    Color Bush);
