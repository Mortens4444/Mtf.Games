using Mtf.Drawing;
using Mtf.Games.Interfaces;

namespace Mtf.Games.General;

public abstract class GameEngineBase
{
    protected IGameContext gameContext;
    protected bool inGame;
    protected string message = String.Empty;
    private volatile bool stopRequested;

    public const string GameOver = "Game over!";
    public const string YouWon = "You won!";

    public bool IsInGame => inGame;
    public event Action? OnGameOver;
    public event Action? OnWon;

    public GameEngineBase(IGameContext? gameContext)
    {
        this.gameContext = gameContext ?? throw new ArgumentNullException(nameof(gameContext));
        StartNewGame();
    }

    protected abstract void StartNewGame();

    public abstract IButtonStates? GameMoment();

    public const int DefaultEndDelay = 3000;

    /// <summary>
    /// When true, the center button pauses a running game and restarts a finished one.
    /// Games that use the center button as an action button (for example fire) return false.
    /// </summary>
    protected virtual bool CenterButtonPauses => true;

    /// <summary>
    /// Asks a running <see cref="GameLoop(int, int)"/> to return after the current game moment.
    /// Hosts call it when their window closes. It can be called from any thread.
    /// </summary>
    public void RequestStop() => stopRequested = true;

    public void GameLoop(int time = 30) => GameLoop(time, DefaultEndDelay);

    public void GameLoop(int time, int endDelay)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(time);
        ArgumentOutOfRangeException.ThrowIfNegative(endDelay);

        Initialize();

        while (inGame && !stopRequested)
        {
            gameContext.Clear();

            HandleGameState(GameMoment());

            if (!inGame)
            {
                gameContext.ShowOnMiddleOfScreen(message, FontType.Normal, 0);
                if (message == GameOver)
                {
                    gameContext.PlayGameOver();
                }
                else
                {
                    gameContext.PlayYouWon();
                }
            }

            gameContext.Update();
            Thread.Sleep(inGame ? time : endDelay);
        }

        Finish();
    }

    protected void Won()
    {
        message = YouWon;
        inGame = false;
        OnWon?.Invoke();
    }
    
    protected void Lost()
    {
        message = GameOver;
        inGame = false;
        OnGameOver?.Invoke();
    }

    private void HandleGameState(IButtonStates? buttonStates)
    {
        if (buttonStates != null)
        {
            if (inGame && buttonStates.IsBackButtonPressed())
            {
                Lost();
            }
            if (CenterButtonPauses && buttonStates.IsCenterButtonPressed())
            {
                if (!inGame)
                {
                    StartNewGame();
                }
                else
                {
                    Pause();
                }
            }
        }
    }

    private void Initialize()
    {
        gameContext.StartGame();
    }

    private void Pause()
    {
        gameContext.Clear();
        gameContext.ShowOnMiddleOfScreen("Paused", FontType.Big, 0);
        gameContext.ShowOnMiddleOfScreen("Press down/up key", FontType.Normal, 30);
        gameContext.Update();

        IButtonStates buttonStates;
        do
        {
            buttonStates = gameContext.GetButtonStates();
            Thread.Sleep(100);
        }
        while (!stopRequested && !buttonStates.IsDownButtonPressed() && !buttonStates.IsUpButtonPressed());
    }

    private void Finish()
    {
        gameContext.Finish();
    }
}
