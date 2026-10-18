using Mtf.Games.Enums;
using Mtf.Games.General;

namespace Mtf.Games.Tests.General;

[TestFixture]
public class DirectionTests
{
    [Test]
    public void OppositeDirections_SumToZero()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)Direction.North + (int)Direction.South, Is.Zero);
            Assert.That((int)Direction.East + (int)Direction.West, Is.Zero);
        });
    }

    [Test]
    public void PlayType_HasExpectedValues()
    {
        Assert.That(Enum.GetValues<PlayType>(), Is.EquivalentTo(new[] { PlayType.WaitForCompletion, PlayType.PlayOnce, PlayType.Repeat }));
    }
}
