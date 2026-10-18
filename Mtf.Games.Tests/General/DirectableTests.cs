using Mtf.Games.General;

namespace Mtf.Games.Tests.General;

[TestFixture]
public class DirectableTests
{
    private sealed class TestDirectable : Directable;

    [Test]
    public void DefaultDirection_IsEast()
    {
        var directable = new TestDirectable();

        Assert.That(directable.Direction, Is.EqualTo(Direction.East));
    }

    [TestCase(Direction.East, Direction.North, Direction.North)]
    [TestCase(Direction.East, Direction.South, Direction.South)]
    [TestCase(Direction.East, Direction.East, Direction.East)]
    [TestCase(Direction.North, Direction.West, Direction.West)]
    [TestCase(Direction.North, Direction.East, Direction.East)]
    public void ChangeDirection_AllowsNonOppositeDirection(Direction initial, Direction requested, Direction expected)
    {
        var directable = new TestDirectable { Direction = initial };

        directable.ChangeDirection(requested);

        Assert.That(directable.Direction, Is.EqualTo(expected));
    }

    [TestCase(Direction.East, Direction.West)]
    [TestCase(Direction.West, Direction.East)]
    [TestCase(Direction.North, Direction.South)]
    [TestCase(Direction.South, Direction.North)]
    public void ChangeDirection_BlocksOppositeDirection(Direction initial, Direction opposite)
    {
        var directable = new TestDirectable { Direction = initial };

        directable.ChangeDirection(opposite);

        Assert.That(directable.Direction, Is.EqualTo(initial));
    }
}
