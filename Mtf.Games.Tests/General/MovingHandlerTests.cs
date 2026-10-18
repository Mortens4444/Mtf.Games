using Mtf.Games.General;
using Mtf.Games.Tests.Fakes;

namespace Mtf.Games.Tests.General;

[TestFixture]
public class MovingHandlerTests
{
    [Test]
    public void HandleKeyPress_ForwardsAllButtonStates()
    {
        var moveable = new FakeMoveable();
        var handler = new MovingHandler(moveable);
        var states = new FakeButtonStates { Right = true, Up = true };
        var context = new FakeGameContext { DefaultButtonStates = states };

        var result = handler.HandleKeyPress(context);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(states));
            Assert.That(moveable.Right, Is.True);
            Assert.That(moveable.Left, Is.False);
            Assert.That(moveable.Up, Is.True);
            Assert.That(moveable.Down, Is.False);
        });
    }

    [Test]
    public void HandleKeyPress_NoButtons_ClearsAllMovement()
    {
        var moveable = new FakeMoveable();
        var handler = new MovingHandler(moveable);
        var context = new FakeGameContext();

        handler.HandleKeyPress(context);

        Assert.Multiple(() =>
        {
            Assert.That(moveable.Right, Is.False);
            Assert.That(moveable.Left, Is.False);
            Assert.That(moveable.Up, Is.False);
            Assert.That(moveable.Down, Is.False);
        });
    }
}
