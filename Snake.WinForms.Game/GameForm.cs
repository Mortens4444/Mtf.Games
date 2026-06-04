using Mtf.Drawing;
using Mtf.Games.Circles;
using Mtf.Games.General;
using Mtf.Games.Snake;

namespace Snake.WinForms.Game;

public partial class GameForm : Form
{
    private readonly WinFormsGameContext context;
    private readonly GameEngineBase engine;

    public GameForm()
    {
        DoubleBuffered = true;

        Width = 400;
        Height = 300;
        Text = "Mtf Snake";

        context = new WinFormsGameContext(this);
        //engine = new SnakeGameEngine(context);
        engine = new CirclesGameEngine(context);
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
        Task.Run(() => engine.GameLoop(50));
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