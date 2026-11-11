using Mtf.Games.General.Arcade;

namespace Mtf.Games.Airwolf;

public abstract class AirwolfEnemy(float x, float y, float width, float height, int points) : Sprite(x, y, width, height)
{
    public int Points { get; } = points;
}
