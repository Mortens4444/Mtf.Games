using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;

namespace Mtf.Games.Airwolf;

/// <summary>
/// The player: a helicopter facing right, drawn from a body, a tail and a spinning rotor.
/// </summary>
public sealed class Helicopter(float width, float height) : Sprite(0f, 0f, width, height)
{
    private bool rotorWide;

    public override void Update()
    {
        base.Update();
        rotorWide = !rotorWide;
    }

    public override void Draw(ICanvas canvas)
    {
        var bodyLeft = X + Width * 0.35f;
        var bodyTop = Y + Height * 0.3f;
        var tailY = bodyTop + Height * 0.2f;
        var rotorCenter = bodyLeft + Width * 0.325f;
        var rotorHalf = rotorWide ? Width * 0.32f : Width * 0.12f;

        canvas.DrawBox(bodyLeft, bodyTop, Width * 0.65f, Height * 0.7f);
        canvas.DrawSegment(X, tailY, bodyLeft, tailY);
        canvas.DrawSegment(rotorCenter - rotorHalf, Y, rotorCenter + rotorHalf, Y);
    }
}
