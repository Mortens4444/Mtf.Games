namespace Mtf.Games.Interfaces;

public interface IGameContext : ICanvas, IInputContext, IAudioContext, IApplicationContext
{
    void Finish();

    void PlayGameOver();

    void PlayYouWon();

    void StartGame();
}