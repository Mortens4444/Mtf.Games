using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;

namespace Mtf.Games.Airwolf;

/// <summary>
/// A flying enemy that comes from the right on a wavy path. Its rotor spins and it pitches into the waves.
/// </summary>
public sealed class Drone(float x, float centerLine, float width, float height, float swing, float phase = 0f) : AirwolfEnemy(x, centerLine, width, height, 50)
{
    private static readonly float[] RotorSpans = [0.5f, 0.2f, 0.35f];

    private readonly float waveCenter = centerLine;
    private readonly float waveHeight = swing;
    private readonly float wavePhase = phase;
    private int age;

    public override void Update()
    {
        age++;
        X += VelocityX;
        Y = waveCenter + MathF.Sin(age * 0.15f + wavePhase) * waveHeight;
    }

    public override void Draw(ICanvas canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        var bodyTop = Y + Height * 0.4f;
        var rotorY = Y + Height * 0.1f;
        var rotorHalf = Width * RotorSpans[age % RotorSpans.Length];

        var line = MathF.Max(1f, Height * 0.06f);

        canvas.DrawSegment(CenterX - rotorHalf, rotorY, CenterX + rotorHalf, rotorY, AirwolfColors.Rotor, line);
        canvas.DrawSegment(CenterX, rotorY, CenterX, bodyTop, AirwolfColors.Rotor, line);
        canvas.DrawBox(X + Width * 0.1f, bodyTop, Width * 0.8f, Height * 0.5f, true, AirwolfColors.DroneBody);
        canvas.DrawBox(X, bodyTop + Height * 0.15f, Width * 0.15f, Height * 0.2f, true, AirwolfColors.DroneNose);
        canvas.DrawBox(X + Width * 0.55f, bodyTop + Height * 0.1f, Width * 0.25f, Height * 0.18f, true, AirwolfColors.Glass);
        canvas.DrawSegment(X + Width * 0.25f, Y + Height * 0.9f, X + Width * 0.75f, Y + Height * 0.9f, AirwolfColors.Rotor, line);
    }
}
