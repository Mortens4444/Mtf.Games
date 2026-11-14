using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;

namespace Mtf.Games.Airwolf;

/// <summary>
/// The player: a helicopter facing right, drawn from a body with a cockpit window, a tail boom with a tail rotor,
/// skids and a spinning main rotor. The rotor disc tilts and the tail lifts when the helicopter flies forward.
/// </summary>
public sealed class Helicopter(float width, float height) : Sprite(0f, 0f, width, height)
{
    // Half length of the main rotor blade as a part of the width, in the three phases of the rotation.
    private static readonly float[] RotorSpans = [0.38f, 0.14f, 0.28f];

    // Half length of the tail rotor as a part of the height, in the same phases.
    private static readonly float[] TailRotorSpans = [0.28f, 0.1f, 0.2f];

    private int phase;

    /// <summary>
    /// Forward speed from -1 (full speed backwards) to 1 (full speed forward). Only changes the look.
    /// </summary>
    public float Pitch { get; set; }

    public override void Update()
    {
        base.Update();
        phase = (phase + 1) % RotorSpans.Length;
    }

    public override void Draw(ICanvas canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        var pitch = Math.Max(-1f, Math.Min(1f, Pitch));
        var bodyLeft = X + Width * 0.4f;
        var bodyTop = Y + Height * 0.3f;
        var bodyHeight = Height * 0.45f;
        var bodyBottom = bodyTop + bodyHeight;
        var rotorCenter = X + Width * 0.65f;
        var rotorY = Y + Height * 0.1f;
        var rotorHalf = Width * RotorSpans[phase];
        var tilt = pitch * Height * 0.08f;
        var tailY = bodyTop + bodyHeight * 0.3f - pitch * Height * 0.12f;
        var tailRotor = Height * TailRotorSpans[phase];
        var skidY = Y + Height * 0.97f;
        var line = MathF.Max(1f, Height * 0.05f);

        canvas.DrawSegment(rotorCenter - rotorHalf, rotorY - tilt, rotorCenter + rotorHalf, rotorY + tilt, AirwolfColors.Rotor, line);
        canvas.DrawSegment(rotorCenter, rotorY, rotorCenter, bodyTop, AirwolfColors.HelicopterDetail, line);
        canvas.DrawBox(bodyLeft, bodyTop, Width * 0.6f, bodyHeight, true, AirwolfColors.HelicopterBody);
        canvas.DrawBox(bodyLeft, bodyTop + bodyHeight * 0.7f, Width * 0.6f, bodyHeight * 0.3f, true, AirwolfColors.HelicopterDetail);
        canvas.DrawBox(X + Width * 0.82f, bodyTop + bodyHeight * 0.15f, Width * 0.14f, bodyHeight * 0.4f, true, AirwolfColors.Glass);
        canvas.DrawSegment(bodyLeft, bodyTop + bodyHeight * 0.3f, X, tailY, AirwolfColors.HelicopterBody, line * 1.6f);
        canvas.DrawSegment(X, tailY - tailRotor, X, tailY + tailRotor, AirwolfColors.Rotor, line);
        canvas.DrawSegment(X + Width * 0.42f, skidY, X + Width * 0.98f, skidY, AirwolfColors.Rotor, line);
        canvas.DrawSegment(X + Width * 0.5f, bodyBottom, X + Width * 0.5f, skidY, AirwolfColors.Rotor, line);
        canvas.DrawSegment(X + Width * 0.9f, bodyBottom, X + Width * 0.9f, skidY, AirwolfColors.Rotor, line);
    }
}
