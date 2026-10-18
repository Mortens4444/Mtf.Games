using Mtf.Drawing.Geometry;
using Mtf.Drawing.Render;
using Mtf.Games.General;
using Mtf.Games.Snake;
using Mtf.Games.Tests.Fakes;

namespace Mtf.Games.Tests.Snake;

[TestFixture]
public class SnakeGameEngineTests
{
    private const int CenterY = 50;

    private static FakeGameContext CreateContext() => new() { ScreenWidth = 100, ScreenHeight = 100 };

    private static Wormy GetWorm(SnakeGameEngine engine) => Reflection.GetField<Wormy>(engine, "worm");

    private static FoodProducer GetFoodProducer(SnakeGameEngine engine) => Reflection.GetField<FoodProducer>(engine, "foodProducer");

    private static ScoreCounter GetScoreCounter(SnakeGameEngine engine) => Reflection.GetField<ScoreCounter>(engine, "scoreCounter");

    private static CirclePrimitive PlaceFood(SnakeGameEngine engine, float x, float y)
    {
        var food = new CirclePrimitive
        {
            Circle = new CircleF(x, y, Constants.FoodRadius),
            Fill = true
        };
        Reflection.SetField(GetFoodProducer(engine), "food", food);
        return food;
    }

    [Test]
    public void Constructor_NullContext_Throws()
    {
        Assert.That(() => new SnakeGameEngine(null), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_StartsGame()
    {
        var engine = new SnakeGameEngine(CreateContext());

        Assert.Multiple(() =>
        {
            Assert.That(engine.IsInGame, Is.True);
            Assert.That(GetScoreCounter(engine).Score, Is.Zero);
            Assert.That(GetFoodProducer(engine).GetFoodLocation(), Is.Not.Null);
        });
    }

    [Test]
    public void GameMoment_DrawsWormFoodAndScore()
    {
        var context = CreateContext();
        var engine = new SnakeGameEngine(context);
        PlaceFood(engine, 80, 10);

        var result = engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(context.DefaultButtonStates));
            Assert.That(engine.IsInGame, Is.True);
            Assert.That(context.Drawn.OfType<CirclePrimitive>().Count(), Is.EqualTo(Constants.NumberOfBodyParts + 1));
            Assert.That(context.Drawn.OfType<TextPrimitive>().Single().Layout.Text, Is.EqualTo($"Score: {Constants.MaxPoints} / 0"));
        });
    }

    [Test]
    public void GameMoment_ButtonPressed_ChangesWormDirection()
    {
        var context = CreateContext();
        context.DefaultButtonStates = new FakeButtonStates { Down = true };
        var engine = new SnakeGameEngine(context);
        PlaceFood(engine, 80, 10);

        engine.GameMoment();

        Assert.That(GetWorm(engine).Direction, Is.EqualTo(Direction.South));
    }

    [Test]
    public void GameMoment_WormLeavesScreen_EndsGame()
    {
        var context = CreateContext();
        var engine = new SnakeGameEngine(context);
        PlaceFood(engine, 80, 10);
        var lost = 0;
        engine.OnGameOver += () => lost++;
        var moments = 0;

        while (engine.IsInGame && moments < 50)
        {
            engine.GameMoment();
            moments++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(engine.IsInGame, Is.False);
            Assert.That(moments, Is.EqualTo(19));
            Assert.That(lost, Is.EqualTo(1));
        });
    }

    [Test]
    public void GameMoment_AfterDeath_DoesNotRaiseGameOverAgain()
    {
        var context = CreateContext();
        var engine = new SnakeGameEngine(context);
        PlaceFood(engine, 80, 10);
        var lost = 0;
        engine.OnGameOver += () => lost++;
        for (int i = 0; engine.IsInGame && i < 50; i++)
        {
            engine.GameMoment();
        }

        engine.GameMoment();
        engine.GameMoment();

        Assert.That(lost, Is.EqualTo(1));
    }

    [Test]
    public void GameMoment_AfterWin_DeathDoesNotRaiseGameOver()
    {
        var context = CreateContext();
        var engine = new SnakeGameEngine(context);
        var lost = 0;
        engine.OnGameOver += () => lost++;
        GetScoreCounter(engine).Add(Constants.MaxPoints + 1);
        PlaceFood(engine, 32, CenterY);
        engine.GameMoment();

        for (int i = 0; i < 50; i++)
        {
            engine.GameMoment();
        }

        Assert.Multiple(() =>
        {
            Assert.That(engine.IsInGame, Is.False);
            Assert.That(lost, Is.Zero);
        });
    }

    [Test]
    public void GameMoment_AfterGameEnded_DrawsNothing()
    {
        var context = CreateContext();
        var engine = new SnakeGameEngine(context);
        PlaceFood(engine, 80, 10);
        for (int i = 0; engine.IsInGame && i < 50; i++)
        {
            engine.GameMoment();
        }
        Assert.That(engine.IsInGame, Is.False);
        context.Drawn.Clear();

        var result = engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(context.Drawn, Is.Empty);
            Assert.That(result, Is.SameAs(context.DefaultButtonStates));
        });
    }

    [Test]
    public void GameMoment_EatsFood_ProducesNewFoodAndUpdatesScore()
    {
        var context = CreateContext();
        var engine = new SnakeGameEngine(context);
        var food = PlaceFood(engine, 32, CenterY);

        engine.GameMoment();

        var score = GetScoreCounter(engine).Score;
        Assert.Multiple(() =>
        {
            Assert.That(engine.IsInGame, Is.True);
            Assert.That(GetFoodProducer(engine).GetFoodLocation(), Is.Not.SameAs(food));
            Assert.That(score, Is.InRange(0, (Constants.MaxNutrition - 1) * 2));
            Assert.That(score % 2, Is.Zero);
        });
    }

    [Test]
    public void GameMoment_ScoreExceedsMaxPoints_Wins()
    {
        var context = CreateContext();
        var engine = new SnakeGameEngine(context);
        var won = 0;
        engine.OnWon += () => won++;
        GetScoreCounter(engine).Add(Constants.MaxPoints + 1);
        PlaceFood(engine, 32, CenterY);

        engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(engine.IsInGame, Is.False);
            Assert.That(won, Is.EqualTo(1));
        });
    }

    [Test]
    public void GameLoop_WormDies_FinishesWithGameOver()
    {
        var context = CreateContext();
        var engine = new SnakeGameEngine(context);
        PlaceFood(engine, 80, 10);
        var lost = 0;
        engine.OnGameOver += () => lost++;

        engine.GameLoop(1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(engine.IsInGame, Is.False);
            Assert.That(lost, Is.EqualTo(1));
            Assert.That(context.Calls.First(), Is.EqualTo("StartGame"));
            Assert.That(context.Calls, Does.Contain("PlayGameOver"));
            Assert.That(context.Texts, Does.Contain(GameEngineBase.GameOver));
            Assert.That(context.Calls.Last(), Is.EqualTo("Finish"));
        });
    }
}
