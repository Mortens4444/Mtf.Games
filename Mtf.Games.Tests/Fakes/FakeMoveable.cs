using Mtf.Games.Interfaces;

namespace Mtf.Games.Tests.Fakes;

public sealed class FakeMoveable : IMoveable
{
    public bool? Up { get; private set; }
    public bool? Down { get; private set; }
    public bool? Right { get; private set; }
    public bool? Left { get; private set; }

    public void MoveUp(bool move) => Up = move;
    public void MoveDown(bool move) => Down = move;
    public void MoveRight(bool move) => Right = move;
    public void MoveLeft(bool move) => Left = move;
}
