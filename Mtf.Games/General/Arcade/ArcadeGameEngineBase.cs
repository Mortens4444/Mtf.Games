using Mtf.Games.Interfaces;

namespace Mtf.Games.General.Arcade;

/// <summary>
/// Base class of arcade games: score, lives, levels, a playfield below a HUD line,
/// button tracking and a fixed update-then-draw order in every game moment.
/// Sizes and speeds should be derived from <see cref="Unit"/>, so a game fits any screen.
/// </summary>
public abstract class ArcadeGameEngineBase(IGameContext? context, Random? randomSource) : GameEngineBase(context)
{
    private ScoreCounter scoreCounter = new();

    /// <summary>
    /// Random source of the game. Pass a seeded instance to the constructor for repeatable runs.
    /// </summary>
    protected Random Randomizer { get; } = randomSource ?? new Random();

    protected ButtonTracker Buttons { get; } = new();

    public int Score => scoreCounter.Score;

    public int Lives { get; private set; }

    /// <summary>
    /// The current level, starting from 1.
    /// </summary>
    public int Level { get; private set; }

    /// <summary>
    /// Number of game moments played since the game was started.
    /// </summary>
    public long Tick { get; private set; }

    protected virtual int InitialLives => 3;

    /// <summary>
    /// Completing the last level wins the game.
    /// </summary>
    protected virtual int LevelCount => 1;

    /// <summary>
    /// Height of the status line at the top of the screen.
    /// </summary>
    protected virtual int HudHeight => Math.Max(11, gameContext.ScreenHeight / 12);

    protected virtual string HudText => LevelCount > 1
        ? $"S:{Score} L:{Lives} Lv:{Level}"
        : $"S:{Score} L:{Lives}";

    /// <summary>
    /// Base size of the game objects: 2 pixels on a 128 pixel high screen.
    /// </summary>
    protected float Unit => MathF.Max(1f, gameContext.ScreenHeight / 64f);

    protected float FieldLeft => 0f;

    protected float FieldTop => HudHeight;

    protected float FieldRight => gameContext.ScreenWidth;

    protected float FieldBottom => gameContext.ScreenHeight;

    protected float FieldWidth => FieldRight - FieldLeft;

    protected float FieldHeight => FieldBottom - FieldTop;

    protected override void StartNewGame()
    {
        scoreCounter = new ScoreCounter();
        Lives = InitialLives;
        Level = 1;
        Tick = 0;
        Buttons.Reset();
        inGame = true;
        SetupLevel();
    }

    public override IButtonStates? GameMoment()
    {
        var buttonStates = gameContext.GetButtonStates();
        Buttons.Update(buttonStates);

        if (inGame)
        {
            Tick++;
            UpdateGame();
        }

        if (inGame)
        {
            DrawGame();
        }

        DrawHud();
        return buttonStates;
    }

    /// <summary>
    /// Builds the level given by <see cref="Level"/>. Called at game start and after every completed level.
    /// </summary>
    protected abstract void SetupLevel();

    /// <summary>
    /// Moves the game forward by one moment. <see cref="Buttons"/> already holds the current input.
    /// </summary>
    protected abstract void UpdateGame();

    protected abstract void DrawGame();

    /// <summary>
    /// Called after a life was lost and the game goes on.
    /// </summary>
    protected virtual void OnLifeLost()
    {
    }

    protected virtual void DrawHud()
    {
        var separatorY = HudHeight - 1;
        gameContext.DrawLabel(0f, 0f, HudText);
        gameContext.DrawSegment(0f, separatorY, gameContext.ScreenWidth - 1, separatorY);
    }

    protected void AddScore(int amount)
    {
        scoreCounter.Add(amount);
    }

    /// <summary>
    /// Takes a life. The game is lost when no life remains.
    /// </summary>
    protected void LoseLife()
    {
        if (!inGame)
        {
            return;
        }

        Lives--;
        if (Lives <= 0)
        {
            Lives = 0;
            Lost();
        }
        else
        {
            PlayTone(220, 150);
            OnLifeLost();
        }
    }

    /// <summary>
    /// Moves to the next level. The game is won when the last level is completed.
    /// </summary>
    protected void CompleteLevel()
    {
        if (!inGame)
        {
            return;
        }

        if (Level >= LevelCount)
        {
            Won();
        }
        else
        {
            Level++;
            PlayTone(1320, 150);
            SetupLevel();
        }
    }

    /// <summary>
    /// Plays a short tone when the game context supports sound effects.
    /// </summary>
    protected void PlayTone(int frequency, int durationMilliseconds)
    {
        if (gameContext is ISoundEffectContext soundEffects)
        {
            soundEffects.PlayTone(frequency, durationMilliseconds);
        }
    }
}
