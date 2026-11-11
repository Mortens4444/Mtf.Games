using Mtf.Drawing.Render;
using Mtf.Games.General;
using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;
using Mtf.Games.Tests.Fakes;

namespace Mtf.Games.Tests.Arcade;

[TestFixture]
public class ArcadeGameEngineBaseTests
{
    private sealed class TestEngine(IGameContext? context) : ArcadeGameEngineBase(context, new Random(1))
    {
        public int Setups;
        public int Updates;
        public int Draws;
        public int LifeLostCalls;
        public Action<TestEngine>? OnUpdate;

        public int Levels { get; set; } = 1;

        public ButtonTracker Tracker => Buttons;

        public float TopOfField => FieldTop;

        public string Message => message;

        protected override int LevelCount => Levels;

        protected override void SetupLevel() => Setups++;

        protected override void UpdateGame()
        {
            Updates++;
            OnUpdate?.Invoke(this);
        }

        protected override void DrawGame() => Draws++;

        protected override void OnLifeLost() => LifeLostCalls++;

        public void DoLoseLife() => LoseLife();

        public void DoCompleteLevel() => CompleteLevel();

        public void DoAddScore(int amount) => AddScore(amount);
    }

    [Test]
    public void Constructor_NullContext_Throws()
    {
        Assert.That(() => new TestEngine(null), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_StartsGameWithDefaults()
    {
        var engine = new TestEngine(new FakeGameContext());

        Assert.Multiple(() =>
        {
            Assert.That(engine.IsInGame, Is.True);
            Assert.That(engine.Score, Is.Zero);
            Assert.That(engine.Lives, Is.EqualTo(3));
            Assert.That(engine.Level, Is.EqualTo(1));
            Assert.That(engine.Tick, Is.Zero);
            Assert.That(engine.Setups, Is.EqualTo(1));
        });
    }

    [Test]
    public void GameMoment_UpdatesDrawsAndReturnsTheButtonStates()
    {
        var buttonStates = new FakeButtonStates { Left = true };
        var context = new FakeGameContext { DefaultButtonStates = buttonStates };
        var engine = new TestEngine(context);

        var result = engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(buttonStates));
            Assert.That(engine.Updates, Is.EqualTo(1));
            Assert.That(engine.Draws, Is.EqualTo(1));
            Assert.That(engine.Tick, Is.EqualTo(1));
            Assert.That(engine.Tracker.IsHeld(ArcadeButton.Left), Is.True);
            Assert.That(engine.Tracker.WasPressed(ArcadeButton.Left), Is.True);
        });
    }

    [Test]
    public void GameMoment_DrawsTheHud()
    {
        var context = new FakeGameContext();
        var engine = new TestEngine(context);
        engine.DoAddScore(25);

        engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(context.Drawn.OfType<TextPrimitive>().Single().Layout.Text, Is.EqualTo("S:25 L:3"));
            Assert.That(context.Drawn.OfType<LinePrimitive>().Count(), Is.EqualTo(1));
            Assert.That(engine.TopOfField, Is.EqualTo(11f).Within(0.001f));
        });
    }

    [Test]
    public void GameMoment_WithSeveralLevels_ShowsTheLevelInTheHud()
    {
        var context = new FakeGameContext();
        var engine = new TestEngine(context) { Levels = 3 };

        engine.GameMoment();

        Assert.That(context.Drawn.OfType<TextPrimitive>().Single().Layout.Text, Is.EqualTo("S:0 L:3 Lv:1"));
    }

    [Test]
    public void LoseLife_WithLivesLeft_KeepsPlaying()
    {
        var engine = new TestEngine(new FakeGameContext());

        engine.DoLoseLife();

        Assert.Multiple(() =>
        {
            Assert.That(engine.Lives, Is.EqualTo(2));
            Assert.That(engine.IsInGame, Is.True);
            Assert.That(engine.LifeLostCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void LoseLife_LastLife_LosesTheGame()
    {
        var engine = new TestEngine(new FakeGameContext());
        var lost = 0;
        engine.OnGameOver += () => lost++;

        engine.DoLoseLife();
        engine.DoLoseLife();
        engine.DoLoseLife();
        engine.DoLoseLife();

        Assert.Multiple(() =>
        {
            Assert.That(engine.Lives, Is.Zero);
            Assert.That(engine.IsInGame, Is.False);
            Assert.That(engine.Message, Is.EqualTo(GameEngineBase.GameOver));
            Assert.That(engine.LifeLostCalls, Is.EqualTo(2));
            Assert.That(lost, Is.EqualTo(1));
        });
    }

    [Test]
    public void CompleteLevel_LastLevel_WinsTheGame()
    {
        var engine = new TestEngine(new FakeGameContext());
        var won = 0;
        engine.OnWon += () => won++;

        engine.DoCompleteLevel();

        Assert.Multiple(() =>
        {
            Assert.That(engine.IsInGame, Is.False);
            Assert.That(engine.Message, Is.EqualTo(GameEngineBase.YouWon));
            Assert.That(engine.Level, Is.EqualTo(1));
            Assert.That(won, Is.EqualTo(1));
        });
    }

    [Test]
    public void CompleteLevel_WithMoreLevels_SetsUpTheNextOne()
    {
        var engine = new TestEngine(new FakeGameContext()) { Levels = 2 };

        engine.DoCompleteLevel();
        var levelAfterFirst = engine.Level;
        var inGameAfterFirst = engine.IsInGame;
        engine.DoCompleteLevel();

        Assert.Multiple(() =>
        {
            Assert.That(levelAfterFirst, Is.EqualTo(2));
            Assert.That(inGameAfterFirst, Is.True);
            Assert.That(engine.Setups, Is.EqualTo(2));
            Assert.That(engine.IsInGame, Is.False);
        });
    }

    [Test]
    public void GameMoment_AfterTheGameEnded_OnlyDrawsTheHud()
    {
        var context = new FakeGameContext();
        var engine = new TestEngine(context);
        engine.DoCompleteLevel();

        engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(engine.Updates, Is.Zero);
            Assert.That(engine.Draws, Is.Zero);
            Assert.That(engine.Tick, Is.Zero);
            Assert.That(context.Drawn, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void GameMoment_GameLostDuringUpdate_SkipsDrawing()
    {
        var engine = new TestEngine(new FakeGameContext())
        {
            OnUpdate = e =>
            {
                e.DoLoseLife();
                e.DoLoseLife();
                e.DoLoseLife();
            }
        };

        engine.GameMoment();

        Assert.Multiple(() =>
        {
            Assert.That(engine.Updates, Is.EqualTo(1));
            Assert.That(engine.Draws, Is.Zero);
            Assert.That(engine.IsInGame, Is.False);
        });
    }

    [Test]
    public void GameLoop_RestartAfterTheGameEnded_ResetsTheState()
    {
        var context = new FakeGameContext();
        var engine = new TestEngine(context)
        {
            OnUpdate = e =>
            {
                e.DoAddScore(10);
                if (e.Updates == 1)
                {
                    e.DoCompleteLevel();
                }
            }
        };
        context.ButtonStateQueue.Enqueue(new FakeButtonStates { Center = true });
        context.ButtonStateQueue.Enqueue(new FakeButtonStates { Back = true });

        engine.GameLoop(1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(engine.Setups, Is.EqualTo(2));
            Assert.That(engine.Updates, Is.EqualTo(2));
            Assert.That(engine.Score, Is.EqualTo(10));
            Assert.That(engine.Tick, Is.EqualTo(1));
            Assert.That(engine.IsInGame, Is.False);
        });
    }
}
