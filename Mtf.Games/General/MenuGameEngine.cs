using Mtf.Drawing;
using Mtf.Drawing.Render;
using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;
using System.Drawing;

namespace Mtf.Games.General;

/// <summary>
/// A main menu around any game: New Game, Credits and Exit (and Continue while a game is running).
/// Up and down choose, the center button (or the secondary one) selects, the back button leaves the game or a page.
/// Pass it to a host instead of the game engine. It creates a fresh game for every New Game
/// and shows the result of a finished game before it returns to the menu.
/// </summary>
public sealed class MenuGameEngine : GameEngineBase
{
    /// <summary>
    /// Game moments a result stays on the screen at most.
    /// </summary>
    public const int ResultLength = 100;

    private const int BackdropBands = 24;
    private const int ResultLockedTicks = 6;

    private static readonly Color BackdropTop = Color.FromArgb(12, 16, 40);
    private static readonly Color BackdropBottom = Color.FromArgb(75, 50, 115);
    private static readonly Color TitleColor = Color.FromArgb(255, 205, 60);
    private static readonly Color TitleShadow = Color.FromArgb(10, 10, 25);
    private static readonly Color ItemColor = Color.FromArgb(225, 230, 245);
    private static readonly Color SelectedColor = Color.FromArgb(255, 205, 60);
    private static readonly Color SelectedText = Color.FromArgb(25, 30, 48);
    private static readonly Color HintColor = Color.FromArgb(140, 150, 185);

    private readonly ButtonTracker buttons = new();
    private readonly List<MenuAction> items = [];

    private readonly string title;
    private readonly Func<IGameContext, GameEngineBase> createGame;
    private readonly List<string> credits;
    private GameEngineBase? game;
    private Screen screen;
    private int selected;
    private string result = GameOver;
    private int resultTicks;

    private enum Screen
    {
        Menu,
        Playing,
        Credits,
        Result
    }

    private enum MenuAction
    {
        Continue,
        NewGame,
        Credits,
        Exit
    }

    /// <param name="context">The host of the game.</param>
    /// <param name="title">Shown above the menu.</param>
    /// <param name="createGame">Creates a new game. Called for every New Game.</param>
    /// <param name="credits">The lines of the Credits page.</param>
    public MenuGameEngine(IGameContext? context, string title, Func<IGameContext, GameEngineBase> createGame, IEnumerable<string>? credits = null)
        : base(context)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(createGame);

        this.title = title;
        this.createGame = createGame;
        this.credits = credits == null ? [] : [.. credits];
        BuildItems();
    }

    /// <summary>
    /// True while a game is started and not finished, and the menu was opened on top of it.
    /// </summary>
    public bool CanContinue => game is { IsInGame: true };

    // Called by the base constructor, before the fields of this class are assigned: only the initialized ones may be used.
    protected override void StartNewGame()
    {
        game = null;
        screen = Screen.Menu;
        selected = 0;
        buttons.Reset();
        inGame = true;
    }

    public override IButtonStates? GameMoment()
    {
        buttons.Update(gameContext.GetButtonStates());

        switch (screen)
        {
            case Screen.Playing:
                PlayMoment();
                break;
            case Screen.Menu:
                UpdateMenu();
                break;
            case Screen.Credits:
                UpdateCredits();
                break;
            default:
                UpdateResult();
                break;
        }

        switch (screen)
        {
            case Screen.Menu:
                DrawMenu();
                break;
            case Screen.Credits:
                DrawCredits();
                break;
            case Screen.Result:
                DrawResult();
                break;
        }

        // The buttons belong to the menu: the loop must not treat the back button as the end of the game.
        return null;
    }

    private void BuildItems()
    {
        items.Clear();
        if (CanContinue)
        {
            items.Add(MenuAction.Continue);
        }

        items.Add(MenuAction.NewGame);
        items.Add(MenuAction.Credits);
        items.Add(MenuAction.Exit);
        selected = Math.Max(0, Math.Min(selected, items.Count - 1));
    }

    private void OpenMenu()
    {
        screen = Screen.Menu;
        selected = 0;
        BuildItems();
    }

    private void PlayMoment()
    {
        if (buttons.WasPressed(ArcadeButton.Back))
        {
            OpenMenu();
            return;
        }

        game!.GameMoment();
        if (game.IsInGame)
        {
            return;
        }

        screen = Screen.Result;
        resultTicks = 0;
        if (result == YouWon)
        {
            gameContext.PlayYouWon();
        }
        else
        {
            gameContext.PlayGameOver();
        }
    }

    private void UpdateMenu()
    {
        if (buttons.WasPressed(ArcadeButton.Up))
        {
            selected = (selected + items.Count - 1) % items.Count;
            PlayTone(440, 15);
        }
        else if (buttons.WasPressed(ArcadeButton.Down))
        {
            selected = (selected + 1) % items.Count;
            PlayTone(440, 15);
        }
        else if (buttons.WasPressed(ArcadeButton.Center) || buttons.WasPressed(ArcadeButton.Secondary))
        {
            PlayTone(880, 30);
            Select(items[selected]);
        }
        else if (buttons.WasPressed(ArcadeButton.Back))
        {
            // Back goes back to the running game, or leaves the program from the main menu.
            Select(CanContinue ? MenuAction.Continue : MenuAction.Exit);
        }
    }

    private void Select(MenuAction action)
    {
        switch (action)
        {
            case MenuAction.Continue:
                screen = Screen.Playing;
                break;
            case MenuAction.NewGame:
                StartGame();
                break;
            case MenuAction.Credits:
                screen = Screen.Credits;
                break;
            default:
                RequestStop();
                break;
        }
    }

    private void StartGame()
    {
        result = GameOver;
        var newGame = createGame(gameContext);
        newGame.OnGameOver += () => result = GameOver;
        newGame.OnWon += () => result = YouWon;
        game = newGame;
        screen = Screen.Playing;
    }

    private void UpdateCredits()
    {
        if (buttons.WasPressed(ArcadeButton.Back) || buttons.WasPressed(ArcadeButton.Center) || buttons.WasPressed(ArcadeButton.Secondary))
        {
            OpenMenu();
            selected = items.IndexOf(MenuAction.Credits);
        }
    }

    private void UpdateResult()
    {
        resultTicks++;

        // A key that is still down from the game must not skip the result.
        var skipped = resultTicks > ResultLockedTicks
            && (buttons.WasPressed(ArcadeButton.Center) || buttons.WasPressed(ArcadeButton.Secondary) || buttons.WasPressed(ArcadeButton.Back));
        if (skipped || resultTicks >= ResultLength)
        {
            game = null;
            OpenMenu();
        }
    }

    private void DrawMenu()
    {
        DrawBackdrop();

        var titleHeight = TextHeight(FontType.Big);
        var itemHeight = TextHeight(FontType.Normal);
        var gap = itemHeight * 0.5f;
        var total = titleHeight + itemHeight * 1.5f + items.Count * (itemHeight + gap);
        var y = MathF.Max(0f, (gameContext.ScreenHeight - total) / 2f);

        DrawTitle(title, y);
        y += titleHeight + itemHeight * 1.5f;

        var barWidth = items.Max(item => TextWidth(Label(item), FontType.Normal)) + itemHeight * 2f;
        for (var i = 0; i < items.Count; i++)
        {
            var label = Label(items[i]);
            if (i == selected)
            {
                gameContext.DrawBox(
                    (gameContext.ScreenWidth - barWidth) / 2f,
                    y - gap / 2f,
                    barWidth,
                    itemHeight + gap,
                    true,
                    SelectedColor);
            }

            DrawCentered(label, y, FontType.Normal, i == selected ? SelectedText : ItemColor);
            y += itemHeight + gap;
        }

        DrawCentered("Up / Down: choose    Space: select", gameContext.ScreenHeight - itemHeight * 1.5f, FontType.Normal, HintColor);
    }

    private void DrawCredits()
    {
        DrawBackdrop();

        var titleHeight = TextHeight(FontType.Big);
        var itemHeight = TextHeight(FontType.Normal);
        var total = titleHeight + itemHeight * 1.5f + credits.Count * itemHeight * 1.4f;
        var y = MathF.Max(0f, (gameContext.ScreenHeight - total) / 2f);

        DrawTitle("Credits", y);
        y += titleHeight + itemHeight * 1.5f;
        foreach (var line in credits)
        {
            DrawCentered(line, y, FontType.Normal, ItemColor);
            y += itemHeight * 1.4f;
        }

        DrawCentered("Space: back", gameContext.ScreenHeight - itemHeight * 1.5f, FontType.Normal, HintColor);
    }

    private void DrawResult()
    {
        DrawBackdrop();

        var titleHeight = TextHeight(FontType.Big);
        var itemHeight = TextHeight(FontType.Normal);
        var lines = game is ArcadeGameEngineBase arcade ? 2 : 1;
        var y = MathF.Max(0f, (gameContext.ScreenHeight - titleHeight - lines * itemHeight * 1.5f) / 2f);

        DrawTitle(result, y);
        y += titleHeight + itemHeight * 0.5f;
        if (game is ArcadeGameEngineBase scored)
        {
            DrawCentered($"Score: {scored.Score}", y, FontType.Normal, ItemColor);
        }

        DrawCentered("Space: menu", gameContext.ScreenHeight - itemHeight * 1.5f, FontType.Normal, HintColor);
    }

    // A dark gradient from the top to the bottom, made of thin bands.
    private void DrawBackdrop()
    {
        var bandHeight = gameContext.ScreenHeight / (float)BackdropBands;
        for (var i = 0; i < BackdropBands; i++)
        {
            var amount = i / (float)(BackdropBands - 1);
            var color = Color.FromArgb(
                (int)(BackdropTop.R + (BackdropBottom.R - BackdropTop.R) * amount),
                (int)(BackdropTop.G + (BackdropBottom.G - BackdropTop.G) * amount),
                (int)(BackdropTop.B + (BackdropBottom.B - BackdropTop.B) * amount));
            var top = i * bandHeight;
            gameContext.DrawBox(0f, top, gameContext.ScreenWidth, i == BackdropBands - 1 ? gameContext.ScreenHeight - top : bandHeight + 1f, true, color);
        }
    }

    private void DrawTitle(string text, float y)
    {
        // A small text (on a terminal, where one character is one pixel) would only get blurred by a shadow.
        var shadow = TextHeight(FontType.Big) / 16f;
        if (shadow >= 1f)
        {
            DrawCentered(text, y + shadow, FontType.Big, TitleShadow, shadow);
        }

        DrawCentered(text, y, FontType.Big, TitleColor);
    }

    private void DrawCentered(string text, float y, FontType fontType, Color color, float shift = 0f)
    {
        // A text that is longer than the screen starts at the left edge.
        var width = TextWidth(text, fontType);
        gameContext.DrawLabel(MathF.Max(0f, (gameContext.ScreenWidth - width) / 2f + shift), MathF.Max(0f, y), text, fontType, color);
    }

    private float TextWidth(string text, FontType fontType)
    {
        if (gameContext is ITextMeasureContext measure)
        {
            return measure.MeasureText(text, fontType).Width;
        }

        return text.Length * FontSize(fontType) * 0.55f;
    }

    private float TextHeight(FontType fontType)
    {
        if (gameContext is ITextMeasureContext measure)
        {
            return measure.MeasureText("Ag", fontType).Height;
        }

        return FontSize(fontType) * 1.25f;
    }

    private static float FontSize(FontType fontType)
    {
        var size = new TextPrimitive(0f, 0f, "A", fontType).Layout.FontSize;
        return size > 0f ? size : 12f;
    }

    private static string Label(MenuAction action)
    {
        return action switch
        {
            MenuAction.Continue => "Continue",
            MenuAction.NewGame => "New Game",
            MenuAction.Credits => "Credits",
            _ => "Exit"
        };
    }

    private void PlayTone(int frequency, int durationMilliseconds)
    {
        if (gameContext is ISoundEffectContext soundEffects)
        {
            soundEffects.PlayTone(frequency, durationMilliseconds);
        }
    }
}
