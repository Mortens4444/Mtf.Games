using Mtf.Drawing;
using Mtf.Games.General;
using Mtf.Games.Snake;

namespace Snake.WinForms.Game;

public partial class GameForm : Form
{
    private readonly System.Windows.Forms.Timer timer;
    private readonly WinFormsGameContext context;
    private readonly SnakeGameEngine engine;
    private bool lastInGame = true;

    public GameForm()
    {
        DoubleBuffered = true;

        Width = 400;
        Height = 300;
        Text = "Mtf Snake";

        context = new WinFormsGameContext(this);
        engine = new SnakeGameEngine(context);

        timer = new System.Windows.Forms.Timer
        {
            Interval = 50
        };

        timer.Tick += (_, _) => GameLoop();
        timer.Start();
    }

    private void GameLoop()
    {
        context.Clear();
        engine.GameMoment();
        context.Update();
        Invalidate();

        if (!engine.IsInGame && lastInGame)
        {
            context.ShowOnMiddleOfScreen(GameEngineBase.GameOver, FontType.Normal, 0);
            context.PlayGameOver();
        }

        lastInGame = engine.IsInGame;
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