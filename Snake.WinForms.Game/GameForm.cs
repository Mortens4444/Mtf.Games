using Mtf.Drawing;
using Mtf.Games.Airwolf;
using Mtf.Games.AlienAttack;
using Mtf.Games.Arkanoid;
using Mtf.Games.Circles;
using Mtf.Games.General;
using Mtf.Games.Pong;
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

        // The game is chosen by the first command line argument:
        // snake, circles, pong, arkanoid, alienattack or airwolf.
        var game = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault() ?? "circles";
        Text = $"Mtf Games - {game}";

        context = new WinFormsGameContext(this);
        engine = CreateEngine(game, context);
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

    private static GameEngineBase CreateEngine(string game, WinFormsGameContext context)
    {
        return game.ToLowerInvariant() switch
        {
            "snake" => new SnakeGameEngine(context),
            "pong" => new PongGameEngine(context),
            "arkanoid" => new ArkanoidGameEngine(context),
            "alienattack" => new AlienAttackGameEngine(context),
            "airwolf" => new AirwolfGameEngine(context),
            _ => new CirclesGameEngine(context)
        };
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