using Mtf.Games.General;

namespace Mtf.Games.Tests.General;

[TestFixture]
public class MovingDifferenceTests
{
    [Test]
    public void Constructor_StoresDeltas()
    {
        var difference = new MovingDifference(-3, 7);

        Assert.Multiple(() =>
        {
            Assert.That(difference.DeltaX, Is.EqualTo(-3));
            Assert.That(difference.DeltaY, Is.EqualTo(7));
        });
    }

    [TestCase(Direction.East, MovingDifferenceProvider.PixelsToMove, 0)]
    [TestCase(Direction.West, -MovingDifferenceProvider.PixelsToMove, 0)]
    [TestCase(Direction.South, 0, MovingDifferenceProvider.PixelsToMove)]
    [TestCase(Direction.North, 0, -MovingDifferenceProvider.PixelsToMove)]
    public void Provider_ReturnsDifferenceForDirection(Direction direction, int expectedX, int expectedY)
    {
        var provider = new MovingDifferenceProvider();

        var difference = provider.GetMovingDifference(direction);

        Assert.Multiple(() =>
        {
            Assert.That(difference.DeltaX, Is.EqualTo(expectedX));
            Assert.That(difference.DeltaY, Is.EqualTo(expectedY));
        });
    }

    [Test]
    public void Provider_UnknownDirection_Throws()
    {
        var provider = new MovingDifferenceProvider();

        Assert.That(() => provider.GetMovingDifference((Direction)42), Throws.TypeOf<KeyNotFoundException>());
    }
}
