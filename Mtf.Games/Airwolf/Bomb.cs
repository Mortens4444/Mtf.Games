using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;

namespace Mtf.Games.Airwolf;

/// <summary>
/// A bomb dropped from the helicopter. It keeps the sideways speed of the helicopter and falls faster and faster.
/// </summary>
public sealed class Bomb(float x, float y, float width, float height, float gravity, float maxFallSpeed) : Sprite(x, y, width, height)
{
    public override void Update()
    {
        VelocityY = MathF.Min(maxFallSpeed, VelocityY + gravity);
        base.Update();
    }

    public override void Draw(ICanvas canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        canvas.DrawBox(X, Y + Height * 0.2f, Width, Height * 0.8f, true, AirwolfColors.BombBody);
        canvas.DrawBox(X, Y + Height * 0.5f, Width, Height * 0.12f, true, AirwolfColors.BombBand);
        canvas.DrawBox(X - Width * 0.25f, Y, Width * 1.5f, Height * 0.15f, true, AirwolfColors.BombFin);
    }
}
