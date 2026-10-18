using Mtf.Games.General;
using Mtf.Games.Tests.Fakes;

namespace Mtf.Games.Tests.General;

[TestFixture]
public class DirectionHandlerTests
{
    private sealed class TestDirectable : Directable;

    private static (DirectionHandler handler, TestDirectable directable, FakeGameContext context) Create(FakeButtonStates states, Direction initial = Direction.East)
    {
        var directable = new TestDirectable { Direction = initial };
        var context = new FakeGameContext { DefaultButtonStates = states };
        return (new DirectionHandler(directable), directable, context);
    }

    [Test]
    public void HandleKeyPress_ReturnsButtonStatesFromContext()
    {
        var states = new FakeButtonStates();
        var (handler, _, context) = Create(states);

        var result = handler.HandleKeyPress(context);

        Assert.That(result, Is.SameAs(states));
    }

    [Test]
    public void HandleKeyPress_NoButton_KeepsDirection()
    {
        var (handler, directable, context) = Create(new FakeButtonStates(), Direction.South);

        handler.HandleKeyPress(context);

        Assert.That(directable.Direction, Is.EqualTo(Direction.South));
    }

    [Test]
    public void HandleKeyPress_Right_SetsEast()
    {
        var (handler, directable, context) = Create(new FakeButtonStates { Right = true }, Direction.North);

        handler.HandleKeyPress(context);

        Assert.That(directable.Direction, Is.EqualTo(Direction.East));
    }

    [Test]
    public void HandleKeyPress_Left_SetsWest()
    {
        var (handler, directable, context) = Create(new FakeButtonStates { Left = true }, Direction.North);

        handler.HandleKeyPress(context);

        Assert.That(directable.Direction, Is.EqualTo(Direction.West));
    }

    [Test]
    public void HandleKeyPress_Down_SetsSouth()
    {
        var (handler, directable, context) = Create(new FakeButtonStates { Down = true });

        handler.HandleKeyPress(context);

        Assert.That(directable.Direction, Is.EqualTo(Direction.South));
    }

    [Test]
    public void HandleKeyPress_Up_SetsNorth()
    {
        var (handler, directable, context) = Create(new FakeButtonStates { Up = true });

        handler.HandleKeyPress(context);

        Assert.That(directable.Direction, Is.EqualTo(Direction.North));
    }

    [Test]
    public void HandleKeyPress_RightTakesPriorityOverOthers()
    {
        var (handler, directable, context) = Create(new FakeButtonStates { Right = true, Left = true, Up = true, Down = true }, Direction.North);

        handler.HandleKeyPress(context);

        Assert.That(directable.Direction, Is.EqualTo(Direction.East));
    }

    [Test]
    public void HandleKeyPress_OppositeDirection_IsIgnored()
    {
        var (handler, directable, context) = Create(new FakeButtonStates { Left = true }, Direction.East);

        handler.HandleKeyPress(context);

        Assert.That(directable.Direction, Is.EqualTo(Direction.East));
    }
}
