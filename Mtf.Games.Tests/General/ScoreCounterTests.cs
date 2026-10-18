using Mtf.Games.General;

namespace Mtf.Games.Tests.General;

[TestFixture]
public class ScoreCounterTests
{
    [Test]
    public void Score_StartsAtZero()
    {
        Assert.That(new ScoreCounter().Score, Is.Zero);
    }

    [Test]
    public void Add_Accumulates()
    {
        var counter = new ScoreCounter();

        counter.Add(10);
        counter.Add(5);
        counter.Add(-3);

        Assert.That(counter.Score, Is.EqualTo(12));
    }
}
