using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;

namespace Mtf.Games.AlienAttack;

public sealed class Alien(float x, float y, float width, float height, int row, int column, int points) : Sprite(x, y, width, height)
{
    public int Row { get; } = row;

    public int Column { get; } = column;

    public int Points { get; } = points;

    public override void Draw(ICanvas canvas)
    {
        // Alternating rows are filled and outlined, one draw call per alien.
        canvas.DrawBox(X, Y, Width, Height, Row % 2 == 0);
    }
}
