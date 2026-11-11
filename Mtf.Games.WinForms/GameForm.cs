using Mtf.Drawing;
using Mtf.Games.General;

namespace Mtf.Games.WinForms;

/// <summary>
/// A window that hosts one game: it creates the game context, starts the game loop
/// on a background thread and forwards painting and key events to the context.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public class GameForm : Form
{
    private readonly WinFormsGameContext context;
    private readonly GameEngineBase engine;

    public GameForm(string title, Func<WinFormsGameContext, GameEngineBase> createEngine, int frameTime = 50)
    {
        ArgumentNullException.ThrowIfNull(createEngine);

        DoubleBuffered = true;

        Width = 400;
        Height = 300;
        StartPosition = FormStartPosition.CenterScreen;
        Text = title;

        context = new WinFormsGameContext(this);
        engine = createEngine(context);
        engine.OnGameOver += () =>
        {
            context.ShowOnMiddleOfScreen(GameEngineBase.GameOver, FontType.Normal, 0);
            context.PlayGameOver();
        };
        engine.OnWon += () =>
        {
            context.ShowOnMiddleOfScreen(GameEngineBase.YouWon, FontType.Normal, 0);
            context.PlayYouWon();
        };
        Task.Run(() => engine.GameLoop(frameTime));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        context.Render(e.Graphics);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        context.KeyDown(e.KeyCode);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        context.KeyUp(e.KeyCode);
    }
}
