using Mtf.Games.Circles;
using Mtf.Games.Tests.Fakes;
using System.Drawing;

namespace Mtf.Games.Tests.Circles;

[TestFixture]
public class CirclesGameEngineTests
{
    private const int EnemyCount = 15;

    private static FakeGameContext CreateContext() => new() { ScreenWidth = 200, ScreenHeight = 200 };

    private static CircleEater GetPlayer(CirclesGameEngine engine) => Reflection.GetField<CircleEater>(engine, "player");

    private static IList<CircleEater> GetEnemies(CirclesGameEngine engine) => Reflection.GetField<IList<CircleEater>>(engine, "enemies");

    [Test]
    public void Constructor_NullContext_Throws()
    {
        Assert.That(() => new CirclesGameEngine(null), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_StartsGameWithPlayerInCenter()
    {
        var context = CreateContext();

        var engine = new CirclesGameEngine(context);

        var player = GetPlayer(engine);
        Assert.Multiple(() =>
        {
            Assert.That(engine.IsInGame, Is.True);
            Assert.That(player.Circle.Center, Is.EqualTo(new PointF(100, 100)));
            Assert.That(player.Circle.Radius, Is.EqualTo(4));
            Assert.That(GetEnemies(engine), Has.Count.EqualTo(EnemyCount));
        });
    }

    [Test]
    public void Constructor_EnemiesSpawnOutsidePlayerZone()
    {
        var context = CreateContext();

        var engine = new CirclesGameEngine(context);

        foreach (var enemy in GetEnemies(engine))
        {
            var distance = Mtf.Drawing.Geometry.GeometryMath.Distance(enemy.Circle.Center, new PointF(100, 100));
            Assert.That(distance, Is.GreaterThanOrEqualTo(20 + enemy.Circle.Radius));
            Assert.That(enemy.Circle.Radius, Is.InRange(2, 9));
        }
    }

    [Test]
    public void GameMoment_NoCollision_DrawsEnemiesAndPlayer()
    {
        var context = CreateContext();
        var engine = new CirclesGameEngine(context);

        var result = engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(context.DefaultButtonStates));
            Assert.That(context.Drawn, Has.Count.EqualTo(EnemyCount + 1));
            Assert.That(engine.IsInGame, Is.True);
        });
    }

    [Test]
    public void GameMoment_EnemiesMoveEachMoment()
    {
        var context = CreateContext();
        var engine = new CirclesGameEngine(context);
        var before = GetEnemies(engine).Select(e => e.Circle.Center).ToList();

        engine.GameMoment();

        var after = GetEnemies(engine).Select(e => e.Circle.Center).ToList();
        Assert.That(after, Is.Not.EqualTo(before));
    }

    [Test]
    public void GameMoment_ButtonPressed_MovesPlayerNextMoment()
    {
        var context = CreateContext();
        context.DefaultButtonStates = new FakeButtonStates { Right = true, Down = true };
        var engine = new CirclesGameEngine(context);

        engine.GameMoment();
        engine.GameMoment();

        Assert.That(GetPlayer(engine).Circle.Center, Is.EqualTo(new PointF(102, 102)));
    }

    [Test]
    public void GameMoment_EatsSmallerEnemy_GrowsPlayer()
    {
        var context = CreateContext();
        var engine = new CirclesGameEngine(context);
        var small = new CircleEater(100, 100, 2);
        var far = new CircleEater(20, 20, 2);
        Reflection.SetField(engine, "enemies", new List<CircleEater> { small, far });

        engine.GameMoment();

        var player = GetPlayer(engine);
        Assert.Multiple(() =>
        {
            Assert.That(engine.IsInGame, Is.True);
            Assert.That(GetEnemies(engine), Is.EqualTo(new[] { far }));
            Assert.That(player.Circle.Radius, Is.EqualTo(4.5f));
            Assert.That(context.Drawn, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void GameMoment_EatsEqualSizedEnemy()
    {
        var context = CreateContext();
        var engine = new CirclesGameEngine(context);
        var equal = new CircleEater(100, 100, 4);
        var far = new CircleEater(20, 20, 2);
        Reflection.SetField(engine, "enemies", new List<CircleEater> { equal, far });

        engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(engine.IsInGame, Is.True);
            Assert.That(GetEnemies(engine), Has.Count.EqualTo(1));
            Assert.That(GetPlayer(engine).Circle.Radius, Is.EqualTo(5f));
        });
    }

    [Test]
    public void GameMoment_EatsLastEnemy_Wins()
    {
        var context = CreateContext();
        var engine = new CirclesGameEngine(context);
        var won = 0;
        var lost = 0;
        engine.OnWon += () => won++;
        engine.OnGameOver += () => lost++;
        Reflection.SetField(engine, "enemies", new List<CircleEater> { new(100, 100, 2) });

        engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(engine.IsInGame, Is.False);
            Assert.That(won, Is.EqualTo(1));
            Assert.That(lost, Is.Zero);
            Assert.That(GetEnemies(engine), Is.Empty);
        });
    }

    [Test]
    public void GameMoment_CollidesWithBiggerEnemy_Loses()
    {
        var context = CreateContext();
        var engine = new CirclesGameEngine(context);
        var won = 0;
        var lost = 0;
        engine.OnWon += () => won++;
        engine.OnGameOver += () => lost++;
        var big = new CircleEater(100, 100, 10);
        Reflection.SetField(engine, "enemies", new List<CircleEater> { big });

        engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(engine.IsInGame, Is.False);
            Assert.That(lost, Is.EqualTo(1));
            Assert.That(won, Is.Zero);
            Assert.That(context.Drawn, Is.EqualTo(new[] { big.Circle }));
            Assert.That(GetEnemies(engine), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void GameMoment_AfterGameEnded_DrawsNothing()
    {
        var context = CreateContext();
        var engine = new CirclesGameEngine(context);
        Reflection.SetField(engine, "enemies", new List<CircleEater> { new(100, 100, 10) });
        engine.GameMoment();
        context.Drawn.Clear();

        var result = engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(context.Drawn, Is.Empty);
            Assert.That(result, Is.SameAs(context.DefaultButtonStates));
        });
    }

    [Test]
    public void GameLoop_LostImmediately_FinishesWithGameOver()
    {
        var context = CreateContext();
        var engine = new CirclesGameEngine(context);
        Reflection.SetField(engine, "enemies", new List<CircleEater> { new(100, 100, 10) });

        engine.GameLoop(1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(context.Calls.First(), Is.EqualTo("StartGame"));
            Assert.That(context.Calls, Does.Contain("PlayGameOver"));
            Assert.That(context.Calls.Last(), Is.EqualTo("Finish"));
        });
    }
}
