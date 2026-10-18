using Mtf.Games.General;
using Mtf.Games.Interfaces;
using Mtf.Games.Tests.Fakes;

namespace Mtf.Games.Tests.General;

[TestFixture]
public class GameEngineBaseTests
{
    private sealed class TestEngine(IGameContext? context) : GameEngineBase(context)
    {
        public int StartCount;
        public int Moments;
        public Func<TestEngine, IButtonStates?>? Moment;

        public string Message => message;

        protected override void StartNewGame()
        {
            StartCount++;
            inGame = true;
        }

        public override IButtonStates? GameMoment()
        {
            Moments++;
            return Moment?.Invoke(this);
        }

        public void DoLost() => Lost();

        public void DoWon() => Won();
    }

    [Test]
    public void Constructor_NullContext_Throws()
    {
        Assert.That(() => new TestEngine(null), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_StartsNewGame()
    {
        var engine = new TestEngine(new FakeGameContext());

        Assert.Multiple(() =>
        {
            Assert.That(engine.StartCount, Is.EqualTo(1));
            Assert.That(engine.IsInGame, Is.True);
            Assert.That(engine.Message, Is.Empty);
        });
    }

    [Test]
    public void Won_SetsMessageAndRaisesEvent()
    {
        var engine = new TestEngine(new FakeGameContext());
        var raised = 0;
        engine.OnWon += () => raised++;

        engine.DoWon();

        Assert.Multiple(() =>
        {
            Assert.That(engine.IsInGame, Is.False);
            Assert.That(engine.Message, Is.EqualTo(GameEngineBase.YouWon));
            Assert.That(raised, Is.EqualTo(1));
        });
    }

    [Test]
    public void Lost_SetsMessageAndRaisesEvent()
    {
        var engine = new TestEngine(new FakeGameContext());
        var raised = 0;
        engine.OnGameOver += () => raised++;

        engine.DoLost();

        Assert.Multiple(() =>
        {
            Assert.That(engine.IsInGame, Is.False);
            Assert.That(engine.Message, Is.EqualTo(GameEngineBase.GameOver));
            Assert.That(raised, Is.EqualTo(1));
        });
    }

    [Test]
    public void GameLoop_LostInMoment_ShowsGameOverAndFinishes()
    {
        var context = new FakeGameContext();
        var engine = new TestEngine(context)
        {
            Moment = e =>
            {
                e.DoLost();
                return null;
            }
        };

        engine.GameLoop(1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(engine.Moments, Is.EqualTo(1));
            Assert.That(context.Calls, Is.EqualTo(new[] { "StartGame", "Clear", "ShowOnMiddleOfScreen", "PlayGameOver", "Update", "Finish" }));
            Assert.That(context.Texts, Is.EqualTo(new[] { GameEngineBase.GameOver }));
        });
    }

    [Test]
    public void GameLoop_WonInMoment_PlaysYouWon()
    {
        var context = new FakeGameContext();
        var engine = new TestEngine(context)
        {
            Moment = e =>
            {
                e.DoWon();
                return null;
            }
        };

        engine.GameLoop(1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(context.Calls, Does.Contain("PlayYouWon"));
            Assert.That(context.Calls, Does.Not.Contain("PlayGameOver"));
            Assert.That(context.Texts, Is.EqualTo(new[] { GameEngineBase.YouWon }));
        });
    }

    [Test]
    public void GameLoop_BackButton_EndsGameAsLost()
    {
        var context = new FakeGameContext();
        var raised = 0;
        var engine = new TestEngine(context)
        {
            Moment = _ => new FakeButtonStates { Back = true }
        };
        engine.OnGameOver += () => raised++;

        engine.GameLoop(1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(engine.Moments, Is.EqualTo(1));
            Assert.That(engine.IsInGame, Is.False);
            Assert.That(raised, Is.EqualTo(1));
            Assert.That(context.Calls, Does.Contain("PlayGameOver"));
        });
    }

    [Test]
    public void GameLoop_BackButtonAfterGameEnded_DoesNotRaiseGameOverAgain()
    {
        var context = new FakeGameContext();
        var lost = 0;
        var engine = new TestEngine(context)
        {
            Moment = e =>
            {
                e.DoLost();
                return new FakeButtonStates { Back = true };
            }
        };
        engine.OnGameOver += () => lost++;

        engine.GameLoop(1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(lost, Is.EqualTo(1));
            Assert.That(context.Calls.Count(c => c == "PlayGameOver"), Is.EqualTo(1));
        });
    }

    [Test]
    public void GameLoop_BackButtonAfterWin_KeepsWonResult()
    {
        var context = new FakeGameContext();
        var lost = 0;
        var engine = new TestEngine(context)
        {
            Moment = e =>
            {
                e.DoWon();
                return new FakeButtonStates { Back = true };
            }
        };
        engine.OnGameOver += () => lost++;

        engine.GameLoop(1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(lost, Is.Zero);
            Assert.That(engine.Message, Is.EqualTo(GameEngineBase.YouWon));
            Assert.That(context.Calls, Does.Contain("PlayYouWon"));
        });
    }

    [TestCase(-1, 0)]
    [TestCase(0, -1)]
    [TestCase(-5, -5)]
    public void GameLoop_NegativeDelay_ThrowsBeforeStarting(int time, int endDelay)
    {
        var context = new FakeGameContext();
        var engine = new TestEngine(context);

        Assert.Multiple(() =>
        {
            Assert.That(() => engine.GameLoop(time, endDelay), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(engine.Moments, Is.Zero);
            Assert.That(context.Calls, Is.Empty);
        });
    }

    [Test]
    public void GameLoop_SingleArgumentOverload_NegativeTime_Throws()
    {
        var engine = new TestEngine(new FakeGameContext());

        Assert.That(() => engine.GameLoop(-1), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("time"));
    }

    [Test]
    public void GameLoop_SingleArgumentOverload_ForwardsToMainLoop()
    {
        var context = new FakeGameContext();
        var engine = new TestEngine(context)
        {
            Moment = _ => throw new InvalidOperationException("stop")
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => engine.GameLoop(5), Throws.InvalidOperationException.With.Message.EqualTo("stop"));
            Assert.That(engine.Moments, Is.EqualTo(1));
            Assert.That(context.Calls, Is.EqualTo(new[] { "StartGame", "Clear" }));
        });
    }

    [Test]
    public void GameLoop_KeepsBinaryCompatibleSingleIntOverload()
    {
        var method = typeof(GameEngineBase).GetMethod(nameof(GameEngineBase.GameLoop), [typeof(int)]);

        Assert.Multiple(() =>
        {
            Assert.That(method, Is.Not.Null);
            Assert.That(GameEngineBase.DefaultEndDelay, Is.EqualTo(3000));
        });
    }

    [Test]
    public void GameLoop_CenterButtonWhileInGame_PausesUntilUpOrDown()
    {
        var context = new FakeGameContext();
        context.ButtonStateQueue.Enqueue(new FakeButtonStates());
        context.ButtonStateQueue.Enqueue(new FakeButtonStates { Down = true });
        var engine = new TestEngine(context)
        {
            Moment = e =>
            {
                if (e.Moments == 1)
                {
                    return new FakeButtonStates { Center = true };
                }
                e.DoLost();
                return null;
            }
        };

        engine.GameLoop(1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(engine.Moments, Is.EqualTo(2));
            Assert.That(context.Texts, Does.Contain("Paused"));
            Assert.That(context.Texts, Does.Contain("Press down/up key"));
            Assert.That(context.ButtonStateQueue, Is.Empty);
            Assert.That(context.Calls.Count(c => c == "GetButtonStates"), Is.EqualTo(2));
        });
    }

    [Test]
    public void GameLoop_CenterButtonAfterGameEnded_RestartsGame()
    {
        var context = new FakeGameContext();
        var engine = new TestEngine(context)
        {
            Moment = e =>
            {
                e.DoLost();
                return e.Moments == 1 ? new FakeButtonStates { Center = true } : null;
            }
        };

        engine.GameLoop(1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(engine.Moments, Is.EqualTo(2));
            Assert.That(engine.StartCount, Is.EqualTo(2));
            Assert.That(context.Calls.Count(c => c == "PlayGameOver"), Is.EqualTo(1));
            Assert.That(context.Calls.Last(), Is.EqualTo("Finish"));
        });
    }
}
