using Mtf.Games.General;
using Mtf.Games.Interfaces;
using Mtf.Games.Tests.Fakes;

namespace Mtf.Games.Tests.General;

[TestFixture]
public class GameEngineBaseCenterButtonTests
{
    private sealed class ActionButtonEngine(IGameContext? context) : GameEngineBase(context)
    {
        public int StartCount;
        public int Moments;

        protected override bool CenterButtonPauses => false;

        protected override void StartNewGame()
        {
            StartCount++;
            inGame = true;
        }

        public override IButtonStates? GameMoment()
        {
            Moments++;
            if (Moments == 3)
            {
                Lost();
            }
            return new FakeButtonStates { Center = true };
        }
    }

    [Test]
    public void GameLoop_CenterButtonUsedAsActionButton_DoesNotPauseOrRestart()
    {
        var context = new FakeGameContext();
        var engine = new ActionButtonEngine(context);

        engine.GameLoop(1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(engine.Moments, Is.EqualTo(3));
            Assert.That(engine.StartCount, Is.EqualTo(1));
            Assert.That(engine.IsInGame, Is.False);
            Assert.That(context.Texts, Is.EqualTo(new[] { GameEngineBase.GameOver }));
            Assert.That(context.Calls, Does.Not.Contain("GetButtonStates"));
        });
    }
}
