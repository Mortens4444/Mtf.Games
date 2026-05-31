using Mtf.Games.General;

namespace Mtf.Games.Interfaces;

public interface IDirectable
{
    Direction Direction { get; }

    void ChangeDirection(Direction newDirection);
}