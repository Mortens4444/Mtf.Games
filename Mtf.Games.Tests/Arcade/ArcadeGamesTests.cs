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
    public void Airwolf_ReachingTheEndOfTheLevel_StartsTheBossFightAndOnlyTheBossEndsTheLevel()
    {
        var context = CreateContext();
        var engine = new AirwolfGameEngine(context, new Random(7));
        var enemies = Reflection.GetField<List<AirwolfEnemy>>(engine, "enemies");

        Reflection.SetField(engine, "distance", AirwolfGameEngine.LevelLength - 1);
        engine.GameMoment();
        var gunships = enemies.OfType<Gunship>().Count();
        Play(engine, context, 30, new FakeButtonStates());

        Assert.Multiple(() =>
        {
            Assert.That(gunships, Is.EqualTo(1));
            Assert.That(engine.Level, Is.EqualTo(1));
            Assert.That(engine.Progress, Is.EqualTo(100));
            Assert.That(engine.IsInGame, Is.True);
        });
    }

    [Test]
    public void Airwolf_DefeatingTheBossOnBothLevels_WinsTheGame()
    {
        var context = CreateContext();
        var engine = new AirwolfGameEngine(context, new Random(7));

        Reflection.SetField(engine, "distance", AirwolfGameEngine.LevelLength - 1);
        engine.GameMoment();
        DefeatTheBoss(engine, context);
        var levelAfterFirst = engine.Level;
        var progressAfterFirst = engine.Progress;
        Reflection.SetField(engine, "distance", AirwolfGameEngine.LevelLength - 1);
        engine.GameMoment();
        DefeatTheBoss(engine, context);

        Assert.Multiple(() =>
        {
            Assert.That(levelAfterFirst, Is.EqualTo(2));
            Assert.That(progressAfterFirst, Is.Zero);
            Assert.That(engine.IsInGame, Is.False);
            Assert.That(engine.Lives, Is.EqualTo(3));
        });
    }

    [Test]
    public void Airwolf_Gunship_NeedsEveryHitAndRammingItOnlyHurtsThePlayer()
    {
        var context = CreateContext();
        var engine = new AirwolfGameEngine(context, new Random(7));
        var shots = Reflection.GetField<List<BoxSprite>>(engine, "shots");
        var helicopter = Reflection.GetField<Helicopter>(engine, "helicopter");
        Reflection.SetField(engine, "distance", AirwolfGameEngine.LevelLength - 1);
        engine.GameMoment();
        var gunship = WaitForTheBoss(engine);

        shots.Add(new BoxSprite(gunship.CenterX, gunship.CenterY, 2f, 2f));
        engine.GameMoment();
        var hitPointsAfterOneShot = gunship.HitPoints;
        helicopter.MoveTo(gunship.X, gunship.Y);
        engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(hitPointsAfterOneShot, Is.EqualTo(gunship.MaxHitPoints - 1));
            Assert.That(gunship.IsAlive, Is.True);
            Assert.That(engine.Lives, Is.EqualTo(2));
        });
    }

    [Test]
    public void Airwolf_Soldier_WalksOnTheGroundAndDrawsInsideTheScreen()
    {
        var context = CreateContext();
        var soldier = new Soldier(100f, 100f, 3f, 5f);
        var startX = soldier.X;

        soldier.VelocityX = -1f;
        soldier.Update();
        soldier.Draw(context);

        Assert.Multiple(() =>
        {
            Assert.That(soldier.X, Is.EqualTo(startX - 1f));
            Assert.That(soldier.Points, Is.EqualTo(25));
            Assert.That(context.Drawn, Is.Not.Empty);
            Assert.That(context.Drawn.All(primitive => IsInside(primitive, context.ScreenWidth, context.ScreenHeight)), Is.True);
        });
    }

    [Test]
    public void Airwolf_Bomb_FallsOnAnEnemyBelowTheHelicopterAndBlowsItUp()
    {
        var context = CreateContext();
        var engine = new AirwolfGameEngine(context, new Random(7));
        var enemies = Reflection.GetField<List<AirwolfEnemy>>(engine, "enemies");
        var bombs = Reflection.GetField<List<Bomb>>(engine, "bombs");
        var helicopter = Reflection.GetField<Helicopter>(engine, "helicopter");
        Play(engine, context, 25, new FakeButtonStates());
        var turret = new Turret(helicopter.CenterX - 3f, helicopter.Bottom + 12f, 6f);
        enemies.Add(turret);
        var scoreBefore = engine.Score;

        Play(engine, context, 1, new FakeButtonStates { Secondary = true });
        var dropped = bombs.Count;
        Play(engine, context, 20, new FakeButtonStates());

        Assert.Multiple(() =>
        {
            Assert.That(dropped, Is.EqualTo(1));
            Assert.That(turret.IsAlive, Is.False);
            Assert.That(engine.Score, Is.EqualTo(scoreBefore + turret.Points));
            Assert.That(bombs, Is.Empty);
        });
    }

    [Test]
    public void Airwolf_Bomb_NeedsAFreshPressForEveryBombAndWaitsAtTheStart()
    {
        var context = CreateContext();
        var engine = new AirwolfGameEngine(context, new Random(7));
        var bombs = Reflection.GetField<List<Bomb>>(engine, "bombs");

        Play(engine, context, 5, new FakeButtonStates { Secondary = true });
        var droppedAtTheStart = bombs.Count;
        Play(engine, context, 1, new FakeButtonStates());
        Play(engine, context, 25, new FakeButtonStates());
        Play(engine, context, 8, new FakeButtonStates { Secondary = true });

        Assert.Multiple(() =>
        {
            Assert.That(droppedAtTheStart, Is.Zero);
            Assert.That(bombs, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Airwolf_Bomb_AcceleratesWhileFallingAndDrawsInsideTheScreen()
    {
        var context = CreateContext();
        var bomb = new Bomb(50f, 20f, 2f, 4f, 0.2f, 1.5f);

        bomb.Update();
        var afterOne = bomb.VelocityY;
        for (var i = 0; i < 20; i++)
        {
            bomb.Update();
        }

        bomb.Draw(context);

        Assert.Multiple(() =>
        {
            Assert.That(afterOne, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(bomb.VelocityY, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(context.Drawn, Is.Not.Empty);
        });
    }

    // The gunship flies in from the right and can be hit only on the screen.
    private static Gunship WaitForTheBoss(AirwolfGameEngine engine)
    {
        var enemies = Reflection.GetField<List<AirwolfEnemy>>(engine, "enemies");
        var gunship = enemies.OfType<Gunship>().Single();
        for (var moment = 0; moment < 100 && !gunship.IsHovering; moment++)
        {
            engine.GameMoment();
        }

        return gunship;
    }

    private static void DefeatTheBoss(AirwolfGameEngine engine, FakeGameContext context)
    {
        var shots = Reflection.GetField<List<BoxSprite>>(engine, "shots");
        var gunship = WaitForTheBoss(engine);
        var level = engine.Level;
        for (var hit = 0; hit < gunship.MaxHitPoints; hit++)
        {
            shots.Add(new BoxSprite(gunship.CenterX, gunship.CenterY, 2f, 2f));
            engine.GameMoment();
        }

        // The level ends after the last blasts.
        for (var moment = 0; moment < 60 && engine.IsInGame && engine.Level == level; moment++)
        {
            engine.GameMoment();
        }

        context.Drawn.Clear();
    }

    [Test]
    public void Airwolf_Helicopter_AcceleratesAndDriftsInsteadOfJumpingToFullSpeed()
    {
        var context = CreateContext();
        var engine = new AirwolfGameEngine(context, new Random(7));
        var helicopter = Reflection.GetField<Helicopter>(engine, "helicopter");
        var maxSpeed = 3f;

        Play(engine, context, 1, new FakeButtonStates { Right = true });
        var afterFirstMoment = helicopter.VelocityX;
        Play(engine, context, 19, new FakeButtonStates { Right = true });
        var cruising = helicopter.VelocityX;
        Play(engine, context, 1, new FakeButtonStates());
        var afterRelease = helicopter.VelocityX;

        Assert.Multiple(() =>
        {
            Assert.That(afterFirstMoment, Is.GreaterThan(0f).And.LessThan(maxSpeed / 2f));
            Assert.That(cruising, Is.EqualTo(maxSpeed).Within(0.15f));
            Assert.That(afterRelease, Is.GreaterThan(0f).And.LessThan(cruising));
            Assert.That(helicopter.Pitch, Is.GreaterThan(0f));
        });
    }

    [Test]
    public void Airwolf_Turret_CannotAimBelowTheMinimumElevation()
    {
        var turret = new Turret(0f, 0f, 6f);

        var reachesLowTarget = turret.AimAt(40f, 50f);
        var lowAimY = turret.AimY;
        var reachesHighTarget = turret.AimAt(10f, -30f);

        Assert.Multiple(() =>
        {
            Assert.That(reachesLowTarget, Is.False);
            Assert.That(lowAimY, Is.EqualTo(MathF.Sin(Turret.MinElevation)).Within(0.001f));
            Assert.That(reachesHighTarget, Is.True);
            Assert.That(turret.AimX * turret.AimX + turret.AimY * turret.AimY, Is.EqualTo(1f).Within(0.001f));
            Assert.That(turret.AimY, Is.GreaterThan(0.9f));
        });
    }

    [Test]
    public void Airwolf_Explosion_DiesAfterItsDurationAndDrawsInsideTheScreen()
    {
        var context = CreateContext();
        var explosion = new Explosion(20f, 20f, 6f, 3);

        explosion.Draw(context);
        explosion.Update();
        explosion.Update();
        var aliveBeforeTheEnd = explosion.IsAlive;
        explosion.Update();

        Assert.Multiple(() =>
        {
            Assert.That(context.Drawn, Is.Not.Empty);
            Assert.That(context.Drawn.All(primitive => IsInside(primitive, context.ScreenWidth, context.ScreenHeight)), Is.True);
            Assert.That(aliveBeforeTheEnd, Is.True);
            Assert.That(explosion.IsAlive, Is.False);
        });
    }

    [Test]
    public void Airwolf_ResizingTheScreen_BuildsTheTerrainAgainAndKeepsTheProgress()
    {
        var context = CreateContext();
        var engine = new AirwolfGameEngine(context, new Random(7));
        Play(engine, context, 50, new FakeButtonStates());
        var distanceBefore = Reflection.GetField<int>(engine, "distance");

        context.ScreenWidth = 800;
        context.ScreenHeight = 500;
        engine.GameMoment();

        var terrain = Reflection.GetField<List<float>>(engine, "terrain");
        var columnWidth = Reflection.GetField<float>(engine, "columnWidth");
        context.Drawn.Clear();
        for (var moment = 0; moment < 200; moment++)
        {
            engine.GameMoment();
            var offender = context.Drawn.FirstOrDefault(primitive => !IsInside(primitive, 800, 500));
            Assert.That(offender, Is.Null, $"A primitive was drawn outside the screen in moment {moment}.");
            context.Drawn.Clear();
        }

        Assert.Multiple(() =>
        {
            Assert.That(terrain.Count * columnWidth, Is.GreaterThanOrEqualTo(800f));
            Assert.That(Reflection.GetField<int>(engine, "distance"), Is.EqualTo(distanceBefore + 201));
            Assert.That(engine.IsInGame, Is.True);
        });
    }

    [Test]
    public void Airwolf_Terrain_StaysInsideItsLimitsAndRollsSmoothly()
    {
        var context = CreateContext(400, 300);
        var engine = new AirwolfGameEngine(context, new Random(3));
        var terrain = Reflection.GetField<List<float>>(engine, "terrain");
        var columnWidth = Reflection.GetField<float>(engine, "columnWidth");
        var unit = 300f / 64f;

        Play(engine, context, 600, new FakeButtonStates { Up = true });

        Assert.Multiple(() =>
        {
            Assert.That(terrain.Min(), Is.GreaterThanOrEqualTo(unit * 2f - 0.001f));
            Assert.That(terrain.Max(), Is.LessThanOrEqualTo(300f * 0.5f));
            for (var i = 1; i < terrain.Count; i++)
            {
                Assert.That(Math.Abs(terrain[i] - terrain[i - 1]), Is.LessThanOrEqualTo(columnWidth * 0.5f), $"Column {i} is too steep.");
            }
        });
    }

    [Test]
    public void Arkanoid_BricksPaddleAndBall_AreDrawnWithTheirColors()
    {
        var context = CreateContext();
        var engine = new ArkanoidGameEngine(context, new Random(7));

        engine.GameMoment();

        var filledColors = context.DrawnColors
            .Where(drawn => drawn.Primitive is RectanglePrimitive { Fill: true })
            .Select(drawn => drawn.Color)
            .ToList();
        var ballColors = context.DrawnColors
            .Where(drawn => drawn.Primitive is CirclePrimitive)
            .Select(drawn => drawn.Color)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(filledColors, Does.Contain(Color.Tomato));
            Assert.That(filledColors, Does.Contain(Color.Orange));
            Assert.That(filledColors, Does.Contain(Color.LimeGreen));
            Assert.That(filledColors, Does.Contain(Color.MidnightBlue));
            Assert.That(ballColors, Is.EqualTo(new[] { Color.Crimson }));
        });
    }

    [Test]
    public void ArkanoidBrick_WithColor_HasABlackCoreOnlyWhileItNeedsMoreHits()
    {
        var context = CreateContext();
        var brick = new ArkanoidBrick(10f, 10f, 30f, 10f, 2) { Color = Color.Orange };

        brick.Draw(context);
        var strongPrimitives = context.DrawnColors.ToList();
        context.DrawnColors.Clear();
        brick.Hit();
        brick.Draw(context);
        var weakPrimitives = context.DrawnColors.ToList();

        Assert.Multiple(() =>
        {
            Assert.That(strongPrimitives, Has.Count.EqualTo(3));
            Assert.That(strongPrimitives[0].Color, Is.EqualTo(Color.Orange));
            Assert.That(strongPrimitives.Skip(1).Select(drawn => drawn.Color), Is.All.EqualTo(Color.Black));
            Assert.That(weakPrimitives, Has.Count.EqualTo(2));
            Assert.That(weakPrimitives[0].Color, Is.EqualTo(Color.Orange));
        });
    }

    [Test]
    public void ArkanoidBrick_WithoutColor_KeepsTheMonochromeLook()
    {
        var context = CreateContext();
        var brick = new ArkanoidBrick(10f, 10f, 30f, 10f, 2);

        brick.Draw(context);
        brick.Hit();
        brick.Draw(context);

        Assert.Multiple(() =>
        {
            Assert.That(context.DrawnColors.Select(drawn => drawn.Color), Is.All.EqualTo(Color.Black));
            Assert.That(context.Drawn, Has.Count.EqualTo(2));
            Assert.That(((RectanglePrimitive)context.Drawn[0]).Fill, Is.True);
            Assert.That(((RectanglePrimitive)context.Drawn[1]).Fill, Is.False);
        });
    }

    [Test]
    public void Sprites_DrawWithTheirColorAndAreBlackByDefault()
    {
        var context = CreateContext();

        new BoxSprite(10f, 10f, 5f, 5f).Draw(context);
        new BoxSprite(10f, 10f, 5f, 5f) { Color = Color.Red }.Draw(context);
        new BallSprite(3f) { Color = Color.Green }.Draw(context);
        new BallSprite(3f).Draw(context);

        Assert.That(
            context.DrawnColors.Select(drawn => drawn.Color),
            Is.EqualTo(new[] { Color.Black, Color.Red, Color.Green, Color.Black }));
    }

    private sealed class AlwaysFirstRandom : Random
    {
        public override int Next(int maxValue) => 0;

        public override int Next(int minValue, int maxValue) => minValue;
    }

    private static void CatchPerk(ArkanoidGameEngine engine, ArkanoidPerkKind kind)
    {
        var paddle = Reflection.GetField<BoxSprite>(engine, "paddle");
        var perks = Reflection.GetField<List<ArkanoidPerk>>(engine, "perks");
        perks.Add(new ArkanoidPerk(paddle.CenterX - 5f, paddle.Top - 2f, 10f, 6f, kind));
        engine.GameMoment();
    }

    [Test]
    public void Arkanoid_DestroyedBrick_DropsAPerkThatFalls()
    {
        var context = CreateContext();
        var engine = new ArkanoidGameEngine(context, new AlwaysFirstRandom());
        var bricks = Reflection.GetField<List<ArkanoidBrick>>(engine, "bricks");
        var perks = Reflection.GetField<List<ArkanoidPerk>>(engine, "perks");
        var ball = Reflection.GetField<BallSprite>(engine, "ball");
        Reflection.SetField(engine, "ballStuck", false);
        ball.CenterOn(bricks[0].CenterX, bricks[0].CenterY);
        ball.VelocityY = 1f;

        engine.GameMoment();
        var perk = perks.First();
        var droppedAt = perk.Y;
        engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(perk.Kind, Is.EqualTo(ArkanoidPerkKind.WidePaddle));
            Assert.That(perk.Y, Is.GreaterThan(droppedAt));
        });
    }

    [Test]
    public void Arkanoid_PerkMissedByThePaddle_Disappears()
    {
        var context = CreateContext();
        var engine = new ArkanoidGameEngine(context, new Random(7));
        var perks = Reflection.GetField<List<ArkanoidPerk>>(engine, "perks");
        perks.Add(new ArkanoidPerk(0f, context.ScreenHeight + 1f, 10f, 6f, ArkanoidPerkKind.ExtraLife));

        engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(perks, Is.Empty);
            Assert.That(engine.Lives, Is.EqualTo(3));
        });
    }

    [Test]
    public void Arkanoid_WidePaddlePerk_DoublesThePaddleUntilItExpires()
    {
        var context = CreateContext();
        var engine = new ArkanoidGameEngine(context, new Random(7));
        var paddle = Reflection.GetField<BoxSprite>(engine, "paddle");
        var normalWidth = paddle.Width;
        var center = paddle.CenterX;

        CatchPerk(engine, ArkanoidPerkKind.WidePaddle);
        var wideWidth = paddle.Width;
        var wideCenter = paddle.CenterX;
        var wideWhileActive = engine.IsPaddleWide;
        Reflection.SetField(engine, "wideTicks", 1);
        engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(wideWhileActive, Is.True);
            Assert.That(wideWidth, Is.EqualTo(normalWidth * 2f).Within(0.001f));
            Assert.That(wideCenter, Is.EqualTo(center).Within(0.001f));
            Assert.That(engine.IsPaddleWide, Is.False);
            Assert.That(paddle.Width, Is.EqualTo(normalWidth).Within(0.001f));
        });
    }

    [Test]
    public void Arkanoid_SlowBallPerk_SlowsTheBallDownAndRestoresItAtTheEnd()
    {
        var context = CreateContext();
        var engine = new ArkanoidGameEngine(context, new Random(7));
        var ball = Reflection.GetField<BallSprite>(engine, "ball");
        Play(engine, context, 1, new FakeButtonStates { Center = true });
        Play(engine, context, 3, new FakeButtonStates());
        float Speed() => MathF.Sqrt(ball.VelocityX * ball.VelocityX + ball.VelocityY * ball.VelocityY);
        var normalSpeed = Speed();

        CatchPerk(engine, ArkanoidPerkKind.SlowBall);
        var slowSpeed = Speed();
        Reflection.SetField(engine, "slowTicks", 1);
        engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(slowSpeed, Is.EqualTo(normalSpeed * 0.65f).Within(0.01f));
            Assert.That(engine.IsBallSlow, Is.False);
            Assert.That(Speed(), Is.EqualTo(normalSpeed).Within(0.01f));
        });
    }

    [Test]
    public void Arkanoid_ExtraLifePerk_GivesALife()
    {
        var context = CreateContext();
        var engine = new ArkanoidGameEngine(context, new Random(7));

        CatchPerk(engine, ArkanoidPerkKind.ExtraLife);

        Assert.That(engine.Lives, Is.EqualTo(4));
    }

    [Test]
    public void Arkanoid_StickyPaddlePerk_CatchesTheBallAndLaunchesItAgain()
    {
        var context = CreateContext();
        var engine = new ArkanoidGameEngine(context, new Random(7));
        var paddle = Reflection.GetField<BoxSprite>(engine, "paddle");
        var ball = Reflection.GetField<BallSprite>(engine, "ball");
        CatchPerk(engine, ArkanoidPerkKind.StickyPaddle);
        Reflection.SetField(engine, "ballStuck", false);
        ball.CenterOn(paddle.CenterX + 6f, paddle.Top - 1f);
        ball.VelocityX = 0.5f;
        ball.VelocityY = 1f;

        engine.GameMoment();
        var caught = Reflection.GetField<bool>(engine, "ballStuck");
        var caughtVelocity = ball.VelocityY;
        Play(engine, context, 1, new FakeButtonStates { Center = true });

        Assert.Multiple(() =>
        {
            Assert.That(caught, Is.True);
            Assert.That(caughtVelocity, Is.Zero);
            Assert.That(engine.IsPaddleSticky, Is.True);
            Assert.That(Reflection.GetField<bool>(engine, "ballStuck"), Is.False);
            Assert.That(ball.VelocityY, Is.LessThan(0f));
            Assert.That(ball.VelocityX, Is.GreaterThan(0f));
        });
    }

    [Test]
    public void Arkanoid_LosingALife_TakesThePerksAway()
    {
        var context = CreateContext();
        var engine = new ArkanoidGameEngine(context, new Random(7));
        var paddle = Reflection.GetField<BoxSprite>(engine, "paddle");
        var ball = Reflection.GetField<BallSprite>(engine, "ball");
        var normalWidth = paddle.Width;
        CatchPerk(engine, ArkanoidPerkKind.WidePaddle);
        CatchPerk(engine, ArkanoidPerkKind.SlowBall);
        CatchPerk(engine, ArkanoidPerkKind.StickyPaddle);
        Reflection.SetField(engine, "ballStuck", false);
        ball.Y = context.ScreenHeight + 5f;
        ball.VelocityY = 1f;

        engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(engine.Lives, Is.EqualTo(2));
            Assert.That(engine.IsPaddleWide, Is.False);
            Assert.That(engine.IsBallSlow, Is.False);
            Assert.That(engine.IsPaddleSticky, Is.False);
            Assert.That(paddle.Width, Is.EqualTo(normalWidth).Within(0.001f));
        });
    }
}
