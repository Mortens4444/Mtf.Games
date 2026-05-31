namespace Mtf.Games.Interfaces;

public interface IMoveable
{
    void MoveUp(bool move);

    void MoveDown(bool move);

    void MoveRight(bool move);

    void MoveLeft(bool move);
}