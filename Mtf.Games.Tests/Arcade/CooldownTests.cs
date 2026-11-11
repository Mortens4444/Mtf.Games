using Mtf.Games.General.Arcade;

namespace Mtf.Games.Tests.Arcade;

[TestFixture]
public class CooldownTests
{
    [Test]
    public void Constructor_NegativeTicks_Throws()
    {
        Assert.That(() => new Cooldown(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void NewCooldown_IsReady()
    {
        Assert.That(new Cooldown(5).IsReady, Is.True);
    }

    [Test]
    public void TryUse_BecomesReadyAgainAfterTheGivenTicks()
    {
        var cooldown = new Cooldown(2);

        var first = cooldown.TryUse();
        var second = cooldown.TryUse();
        cooldown.Tick();
        var afterOneTick = cooldown.IsReady;
        cooldown.Tick();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(afterOneTick, Is.False);
            Assert.That(cooldown.IsReady, Is.True);
        });
    }

    [Test]
    public void Reset_MakesItReady()
    {
        var cooldown = new Cooldown(10);
        cooldown.Restart();

        cooldown.Reset();

        Assert.That(cooldown.IsReady, Is.True);
    }

    [Test]
    public void Ticks_ChangedValue_IsUsedAtNextRestart()
    {
        var cooldown = new Cooldown(10)
        {
            Ticks = 1
        };

        cooldown.Restart();
        cooldown.Tick();

        Assert.That(cooldown.IsReady, Is.True);
    }
}
