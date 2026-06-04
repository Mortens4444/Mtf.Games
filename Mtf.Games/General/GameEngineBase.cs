using Mtf.Drawing;
using Mtf.Games.Interfaces;

namespace Mtf.Games.General;

public abstract class GameEngineBase
{
    protected IGameContext gameContext;
    protected bool inGame;
    protected string message = String.Empty;

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

    public void GameLoop(int time = 30)
    {
        Initialize();

        while (inGame)
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
            Thread.Sleep(inGame ? time : 3000);
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
            if (buttonStates.IsBackButtonPressed())
            {
                Lost();
            }
            if (buttonStates.IsCenterButtonPressed())
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

        IButtonStates buttonStates;
        do
        {
            buttonStates = gameContext.GetButtonStates();
            Thread.Sleep(100);
        }
        while (!buttonStates.IsDownButtonPressed() && !buttonStates.IsUpButtonPressed());
    }

    private void Finish()
    {
        gameContext.Finish();
    }
}
