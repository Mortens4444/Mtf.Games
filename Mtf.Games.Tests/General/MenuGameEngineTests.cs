using Mtf.Drawing.Render;
using Mtf.Games.Airwolf;
using Mtf.Games.General;
using Mtf.Games.Interfaces;
using Mtf.Games.Tests.Fakes;

namespace Mtf.Games.Tests.General;

[TestFixture]
public class MenuGameEngineTests
{
    private static readonly FakeButtonStates None = new();
    private static readonly FakeButtonStates Up = new() { Up = true };
    private static readonly FakeButtonStates Down = new() { Down = true };
    private static readonly FakeButtonStates Center = new() { Center = true };
    private static readonly FakeButtonStates Back = new() { Back = true };

    private static readonly string[] Credits = ["First credit", "Second credit"];

    private static FakeGameContext CreateContext()
    {
        return new FakeGameContext
        {
            ScreenWidth = 178,
            ScreenHeight = 128,
            MaxButtonStateReads = int.MaxValue
        };
    }

    private static MenuGameEngine CreateMenu(FakeGameContext context, Func<IGameContext, GameEngineBase> createGame)
    {
        return new MenuGameEngine(context, "TITLE", createGame, Credits);
    }

    // Plays one game moment with the given buttons and returns the texts that were drawn in it.
    private static List<string> Moment(MenuGameEngine engine, FakeGameContext context, FakeButtonStates buttons)
    {
        context.DefaultButtonStates = buttons;
        context.Drawn.Clear();
        engine.GameMoment();
        return [.. context.Drawn.OfType<TextPrimitive>().Select(text => text.Layout.Text)];
    }

    private static AirwolfGameEngine CreateAirwolf(IGameContext context)
    {
        return new AirwolfGameEngine(context, new Random(7));
    }

    [Test]
    public void Menu_ShowsTheTitleAndTheItems()
    {
        var context = CreateContext();
        var engine = CreateMenu(context, CreateAirwolf);

        var texts = Moment(engine, context, None);

        Assert.Multiple(() =>
        {
            Assert.That(texts, Does.Contain("TITLE"));
            Assert.That(texts, Does.Contain("New Game"));
            Assert.That(texts, Does.Contain("Credits"));
            Assert.That(texts, Does.Contain("Exit"));
            Assert.That(texts, Does.Not.Contain("Continue"));
            Assert.That(engine.IsInGame, Is.True);
        });
    }

    [Test]
    public void NewGame_StartsAFreshGameEveryTime()
    {
        var context = CreateContext();
        var created = 0;
        var engine = CreateMenu(context, game =>
        {
            created++;
            return CreateAirwolf(game);
        });

        Moment(engine, context, Center);
        var playing = Moment(engine, context, None);
        Moment(engine, context, Back);
        Moment(engine, context, None);
        Moment(engine, context, Down);
        Moment(engine, context, None);
        Moment(engine, context, Center);

        Assert.Multiple(() =>
        {
            Assert.That(playing.Any(text => text.StartsWith("S:", StringComparison.Ordinal)), Is.True);
            Assert.That(created, Is.EqualTo(2));
        });
    }

    [Test]
    public void BackDuringTheGame_OpensTheMenuWithContinue()
    {
        var context = CreateContext();
        var created = 0;
        var engine = CreateMenu(context, game =>
        {
            created++;
            return CreateAirwolf(game);
        });
        Moment(engine, context, Center);
        Moment(engine, context, None);

        var menu = Moment(engine, context, Back);
        Moment(engine, context, None);
        var resumed = Moment(engine, context, Center);
        var playing = Moment(engine, context, None);

        Assert.Multiple(() =>
        {
            Assert.That(menu, Does.Contain("Continue"));
            Assert.That(resumed, Does.Not.Contain("Continue"));
            Assert.That(playing.Any(text => text.StartsWith("S:", StringComparison.Ordinal)), Is.True);
            Assert.That(created, Is.EqualTo(1));
        });
    }

    [Test]
    public void Credits_ShowTheLinesAndBackReturnsToTheMenu()
    {
        var context = CreateContext();
        var engine = CreateMenu(context, CreateAirwolf);

        Moment(engine, context, Down);
        Moment(engine, context, None);
        var credits = Moment(engine, context, Center);
        Moment(engine, context, None);
        var menu = Moment(engine, context, Back);

        Assert.Multiple(() =>
        {
            Assert.That(credits, Does.Contain("Credits"));
            Assert.That(credits, Is.SupersetOf(Credits));
            Assert.That(menu, Does.Contain("New Game"));
            Assert.That(menu, Does.Not.Contain("First credit"));
        });
    }

    [Test]
    public void Selection_WrapsAroundFromTheFirstItemToExit()
    {
        var context = CreateContext();
        var engine = CreateMenu(context, CreateAirwolf);

        // Up from the first item goes to Exit, which leaves the program: the loop has nothing to run.
        Moment(engine, context, Up);
        Moment(engine, context, None);
        Moment(engine, context, Center);
        context.Calls.Clear();
        engine.GameLoop(0, 0);

        Assert.That(context.Calls, Does.Not.Contain("Clear"));
    }

    [TestCase("exit item")]
    [TestCase("back button")]
    public void Exit_EndsTheGameLoopAndClosesTheHost(string how)
    {
        var context = CreateContext();
        var engine = CreateMenu(context, CreateAirwolf);
        if (how == "exit item")
        {
            context.ButtonStateQueue.Enqueue(Down);
            context.ButtonStateQueue.Enqueue(None);
            context.ButtonStateQueue.Enqueue(Down);
            context.ButtonStateQueue.Enqueue(None);
            context.ButtonStateQueue.Enqueue(Center);
        }
        else
        {
            context.ButtonStateQueue.Enqueue(None);
            context.ButtonStateQueue.Enqueue(Back);
        }

        engine.GameLoop(0, 0);

        Assert.That(context.Calls, Does.Contain("Finish"));
    }

    [Test]
    public void FinishedGame_ShowsTheResultAndReturnsToTheMenuWithoutContinue()
    {
        var context = CreateContext();
        var engine = CreateMenu(context, game => new EndingGame(game));

        Moment(engine, context, Center);
        var result = Moment(engine, context, None);
        var callsAtTheEnd = context.Calls.ToList();
        for (var moment = 0; moment < 10; moment++)
        {
            result = Moment(engine, context, None);
        }

        var menu = Moment(engine, context, Center);

        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Contain(GameEngineBase.GameOver));
            Assert.That(callsAtTheEnd, Does.Contain("PlayGameOver"));
            Assert.That(menu, Does.Contain("New Game"));
            Assert.That(menu, Does.Not.Contain("Continue"));
            Assert.That(menu, Does.Not.Contain(GameEngineBase.GameOver));
        });
    }

    [Test]
    public void FinishedGame_IsNotSkippedByAButtonThatIsStillDown()
    {
        var context = CreateContext();
        var engine = CreateMenu(context, game => new EndingGame(game));

        Moment(engine, context, Center);
        var stillHeld = Moment(engine, context, Center);
        var next = Moment(engine, context, Center);

        Assert.Multiple(() =>
        {
            Assert.That(stillHeld, Does.Contain(GameEngineBase.GameOver));
            Assert.That(next, Does.Contain(GameEngineBase.GameOver));
        });
    }

    [Test]
    public void FinishedGame_LeavesTheResultByItself()
    {
        var context = CreateContext();
        var engine = CreateMenu(context, game => new EndingGame(game));

        Moment(engine, context, Center);
        List<string> texts = [];
        for (var moment = 0; moment <= MenuGameEngine.ResultLength; moment++)
        {
            texts = Moment(engine, context, None);
        }

        Assert.That(texts, Does.Contain("New Game"));
    }

    [Test]
    public void EverythingIsDrawnInsideTheScreen_OnABigAndOnATinyScreen()
    {
        foreach (var (width, height) in new[] { (178, 128), (80, 25), (1920, 1080) })
        {
            var context = new FakeGameContext { ScreenWidth = width, ScreenHeight = height, MaxButtonStateReads = int.MaxValue };
            var engine = CreateMenu(context, CreateAirwolf);

            Assert.DoesNotThrow(() =>
            {
                Moment(engine, context, None);
                Moment(engine, context, Down);
                Moment(engine, context, None);
                Moment(engine, context, Center);
                Moment(engine, context, None);
                Moment(engine, context, Back);
            });
        }
    }

    private sealed class EndingGame(IGameContext context) : GameEngineBase(context)
    {
        protected override void StartNewGame()
        {
            inGame = true;
        }

        public override IButtonStates? GameMoment()
        {
            Lost();
            return null;
        }
    }
}
