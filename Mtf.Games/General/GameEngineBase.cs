using Mtf.Drawing;
using Mtf.Games.Interfaces;

namespace Mtf.Games.General;

public abstract class GameEngineBase
{
    protected IGameContext gameContext;
    protected bool inGame;
    protected string message = String.Empty;

    protected const string GameOver = "Game over!";

    public GameEngineBase(IGameContext? gameContext)
    {
        this.gameContext = gameContext ?? throw new ArgumentNullException(nameof(gameContext));
        StartNewGame();
    }

    protected abstract void StartNewGame();
    
    protected abstract IButtonStates? GameMoment();

    public void GameLoop()
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
            Thread.Sleep(inGame ? 30 : 3000);
        }

        Finish();
    }

    private void HandleGameState(IButtonStates? buttonStates)
    {
        if (buttonStates != null)
        {
            if (buttonStates.IsBackButtonPressed())
            {
                inGame = false;
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
