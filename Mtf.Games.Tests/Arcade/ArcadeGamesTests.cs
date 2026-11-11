using Mtf.Drawing.Interfaces;
using Mtf.Drawing.Render;
using Mtf.Games.Airwolf;
using Mtf.Games.AlienAttack;
using Mtf.Games.Arkanoid;
using Mtf.Games.General;
using Mtf.Games.General.Arcade;
using Mtf.Games.Pong;
using Mtf.Games.Tests.Fakes;
using System.Drawing;

namespace Mtf.Games.Tests.Arcade;

[TestFixture]
public class ArcadeGamesTests
{
    private const string PongGame = "pong";
    private const string ArkanoidGame = "arkanoid";
    private const string AlienAttackGame = "alienattack";
    private const string AirwolfGame = "airwolf";

    private static readonly FakeButtonStates[] Inputs =
    [
        new FakeButtonStates(),
        new FakeButtonStates { Left = true },
        new FakeButtonStates { Right = true, Center = true },
        new FakeButtonStates { Up = true },
        new FakeButtonStates { Down = true, Center = true },
        new FakeButtonStates { Left = true, Up = true },
        new FakeButtonStates { Right = true, Down = true }
    ];

    private static FakeGameContext CreateContext(int width = 178, int height = 128)
    {
        return new FakeGameContext
        {
            ScreenWidth = width,
            ScreenHeight = height,
            MaxButtonStateReads = int.MaxValue
        };
    }

    private static ArcadeGameEngineBase CreateEngine(string game, FakeGameContext? context)
    {
        return game switch
        {
            PongGame => new PongGameEngine(context, new Random(7)),
            ArkanoidGame => new ArkanoidGameEngine(context, new Random(7)),
            AlienAttackGame => new AlienAttackGameEngine(context, new Random(7)),
            AirwolfGame => new AirwolfGameEngine(context, new Random(7)),
            _ => throw new ArgumentOutOfRangeException(nameof(game))
        };
    }

    private static void Play(GameEngineBase engine, FakeGameContext context, int moments, FakeButtonStates buttonStates)
    {
        context.DefaultButtonStates = buttonStates;
        for (var i = 0; i < moments; i++)
        {
            engine.GameMoment();
            context.Drawn.Clear();
            context.Calls.Clear();
        }
    }

    private static bool IsPointInside(PointF point, int width, int height)
    {
        return point.X >= 0 && point.Y >= 0 && point.X <= width - 1 && point.Y <= height - 1;
    }

    private static bool IsInside(IPrimitive primitive, int width, int height)
    {
        return primitive switch
        {
            RectanglePrimitive rectangle => rectangle.Rect.X >= 0 && rectangle.Rect.Y >= 0 &&
                rectangle.Rect.Width >= 1 && rectangle.Rect.Height >= 1 &&
                rectangle.Rect.X + rectangle.Rect.Width <= width &&
                rectangle.Rect.Y + rectangle.Rect.Height <= height,
            CirclePrimitive circle => circle.Radius >= 1 && IsPointInside(circle.Center, width, height),
            LinePrimitive line => IsPointInside(line.Line.A, width, height) && IsPointInside(line.Line.B, width, height),
            TextPrimitive text => IsPointInside(text.Layout.Position, width, height),
            _ => false
        };
    }

    [TestCase(PongGame)]
    [TestCase(ArkanoidGame)]
    [TestCase(AlienAttackGame)]
    [TestCase(AirwolfGame)]
    public void Constructor_NullContext_Throws(string game)
    {
        Assert.That(() => CreateEngine(game, null), Throws.ArgumentNullException);
    }

    [TestCase(PongGame)]
    [TestCase(ArkanoidGame)]
    [TestCase(AlienAttackGame)]
    [TestCase(AirwolfGame)]
    public void Constructor_StartsTheGame(string game)
    {
        var engine = CreateEngine(game, CreateContext());

        Assert.Multiple(() =>
        {
            Assert.That(engine.IsInGame, Is.True);
            Assert.That(engine.Score, Is.Zero);
            Assert.That(engine.Lives, Is.EqualTo(3));
            Assert.That(engine.Level, Is.EqualTo(1));
        });
    }

    // The EV3 brick converts every coordinate to a byte, so nothing may be drawn outside the screen.
    [TestCase(PongGame, 178, 128)]
    [TestCase(ArkanoidGame, 178, 128)]
    [TestCase(AlienAttackGame, 178, 128)]
    [TestCase(AirwolfGame, 178, 128)]
    [TestCase(PongGame, 400, 300)]
    [TestCase(ArkanoidGame, 400, 300)]
    [TestCase(AlienAttackGame, 400, 300)]
    [TestCase(AirwolfGame, 400, 300)]
    [TestCase(PongGame, 80, 25)]
    [TestCase(ArkanoidGame, 80, 25)]
    [TestCase(AlienAttackGame, 80, 25)]
    [TestCase(AirwolfGame, 80, 25)]
    public void GameMoment_PlayedWithChangingInput_DrawsOnlyInsideTheScreen(string game, int width, int height)
    {
        var context = CreateContext(width, height);
        var engine = CreateEngine(game, context);

        for (var moment = 0; moment < 3000; moment++)
        {
            context.DefaultButtonStates = Inputs[moment / 15 % Inputs.Length];

            engine.GameMoment();

            var offender = context.Drawn.FirstOrDefault(primitive => !IsInside(primitive, width, height));
            Assert.That(offender, Is.Null, $"A primitive was drawn outside the screen in moment {moment}.");
            Assert.That(context.Drawn, Is.Not.Empty);
            context.Drawn.Clear();
            context.Calls.Clear();
        }
    }

    [Test]
    public void Pong_BallWaitsBeforeTheServe()
    {
        var context = CreateContext();
        var engine = new PongGameEngine(context, new Random(7));
        var ball = Reflection.GetField<BallSprite>(engine, "ball");
        var startX = ball.X;

        Play(engine, context, 10, new FakeButtonStates());
        var waitingX = ball.X;
        Play(engine, context, 15, new FakeButtonStates());

        Assert.Multiple(() =>
        {
            Assert.That(waitingX, Is.EqualTo(startX).Within(0.001f));
            Assert.That(ball.X, Is.GreaterThan(startX));
        });
    }

    [Test]
    public void Pong_BallLeavingOnThePlayerSide_TakesALife()
    {
        var context = CreateContext();
        var engine = new PongGameEngine(context, new Random(7));
        var ball = Reflection.GetField<BallSprite>(engine, "ball");
        Play(engine, context, 25, new FakeButtonStates());

        ball.X = -50;
        ball.VelocityX = -1;
        engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(engine.Lives, Is.EqualTo(2));
            Assert.That(engine.Score, Is.Zero);
            Assert.That(engine.IsInGame, Is.True);
            Assert.That(ball.CenterX, Is.EqualTo(89f).Within(0.001f));
        });
    }

    [Test]
    public void Pong_BallLeavingOnTheOpponentSide_ScoresUntilTheGameIsWon()
    {
        var context = CreateContext();
        var engine = new PongGameEngine(context, new Random(7));
        var ball = Reflection.GetField<BallSprite>(engine, "ball");
        var won = 0;
        engine.OnWon += () => won++;

        for (var point = 0; point < PongGameEngine.WinningScore; point++)
        {
            Play(engine, context, 25, new FakeButtonStates());
            ball.X = context.ScreenWidth + 50;
            ball.VelocityX = 1;
            engine.GameMoment();
        }

        Assert.Multiple(() =>
        {
            Assert.That(engine.Score, Is.EqualTo(PongGameEngine.WinningScore));
            Assert.That(engine.Lives, Is.EqualTo(3));
            Assert.That(engine.IsInGame, Is.False);
            Assert.That(won, Is.EqualTo(1));
        });
    }

    [Test]
    public void Arkanoid_BallStaysOnThePaddleUntilItIsLaunched()
    {
        var context = CreateContext();
        var engine = new ArkanoidGameEngine(context, new Random(7));
        var bricks = engine.RemainingBricks;

        Play(engine, context, 300, new FakeButtonStates { Left = true });

        Assert.Multiple(() =>
        {
            Assert.That(bricks, Is.EqualTo(18));
            Assert.That(engine.RemainingBricks, Is.EqualTo(18));
            Assert.That(engine.Lives, Is.EqualTo(3));
            Assert.That(engine.Score, Is.Zero);
        });
    }

    [Test]
    public void Arkanoid_LaunchedBall_BreaksABrick()
    {
        var context = CreateContext();
        var engine = new ArkanoidGameEngine(context, new Random(7));

        Play(engine, context, 1, new FakeButtonStates { Center = true });
        Play(engine, context, 150, new FakeButtonStates());

        Assert.Multiple(() =>
        {
            Assert.That(engine.RemainingBricks, Is.LessThan(18));
            Assert.That(engine.Score, Is.GreaterThan(0));
        });
    }

    [Test]
    public void AlienAttack_StartsWithAFullFormation()
    {
        var engine = new AlienAttackGameEngine(CreateContext(), new Random(7));

        Assert.That(engine.RemainingAliens, Is.EqualTo(AlienAttackGameEngine.Columns * AlienAttackGameEngine.Rows));
    }

    [Test]
    public void AlienAttack_FiringShip_HitsAliens()
    {
        var context = CreateContext();
        var engine = new AlienAttackGameEngine(context, new Random(7));

        Play(engine, context, 400, new FakeButtonStates { Up = true });

        Assert.Multiple(() =>
        {
            Assert.That(engine.RemainingAliens, Is.LessThan(AlienAttackGameEngine.Columns * AlienAttackGameEngine.Rows));
            Assert.That(engine.Score, Is.GreaterThan(0));
        });
    }

    [Test]
    public void Airwolf_FlyingForward_MakesProgress()
    {
        var context = CreateContext();
        var engine = new AirwolfGameEngine(context, new Random(7));

        Play(engine, context, 90, new FakeButtonStates());

        Assert.Multiple(() =>
        {
            Assert.That(engine.IsInGame, Is.True);
            Assert.That(engine.Progress, Is.EqualTo(10));
            Assert.That(engine.Lives, Is.GreaterThanOrEqualTo(2));
        });
    }

    [Test]
    public void Airwolf_ReachingTheEndOfTheLevels_WinsTheGame()
    {
        var context = CreateContext();
        var engine = new AirwolfGameEngine(context, new Random(7));

        Reflection.SetField(engine, "distance", AirwolfGameEngine.LevelLength - 1);
        engine.GameMoment();
        var levelAfterFirst = engine.Level;
        var progressAfterFirst = engine.Progress;
        Reflection.SetField(engine, "distance", AirwolfGameEngine.LevelLength - 1);
        engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(levelAfterFirst, Is.EqualTo(2));
            Assert.That(progressAfterFirst, Is.Zero);
            Assert.That(engine.IsInGame, Is.False);
            Assert.That(engine.Lives, Is.EqualTo(3));
        });
    }
}
