using Mtf.Games.General.Arcade;
using Mtf.Games.Tests.Fakes;

namespace Mtf.Games.Tests.Arcade;

[TestFixture]
public class ButtonTrackerTests
{
    [Test]
    public void Update_Null_Throws()
    {
        var tracker = new ButtonTracker();

        Assert.That(() => tracker.Update(null!), Throws.ArgumentNullException);
    }

    [Test]
    public void WasPressed_IsTrueOnlyInTheFirstMoment()
    {
        var tracker = new ButtonTracker();

        tracker.Update(new FakeButtonStates { Center = true });
        var pressedFirst = tracker.WasPressed(ArcadeButton.Center);
        tracker.Update(new FakeButtonStates { Center = true });

        Assert.Multiple(() =>
        {
            Assert.That(pressedFirst, Is.True);
            Assert.That(tracker.WasPressed(ArcadeButton.Center), Is.False);
            Assert.That(tracker.IsHeld(ArcadeButton.Center), Is.True);
        });
    }

    [Test]
    public void WasReleased_IsTrueWhenTheButtonGoesUp()
    {
        var tracker = new ButtonTracker();

        tracker.Update(new FakeButtonStates { Up = true });
        tracker.Update(new FakeButtonStates());

        Assert.Multiple(() =>
        {
            Assert.That(tracker.WasReleased(ArcadeButton.Up), Is.True);
            Assert.That(tracker.IsHeld(ArcadeButton.Up), Is.False);
        });
    }

    [Test]
    public void Axes_FollowTheDirectionButtons()
    {
        var tracker = new ButtonTracker();

        tracker.Update(new FakeButtonStates { Right = true, Up = true });
        var right = tracker.HorizontalAxis;
        var up = tracker.VerticalAxis;
        tracker.Update(new FakeButtonStates { Left = true, Down = true });
        var left = tracker.HorizontalAxis;
        var down = tracker.VerticalAxis;
        tracker.Update(new FakeButtonStates { Left = true, Right = true });

        Assert.Multiple(() =>
        {
            Assert.That(right, Is.EqualTo(1));
            Assert.That(up, Is.EqualTo(-1));
            Assert.That(left, Is.EqualTo(-1));
            Assert.That(down, Is.EqualTo(1));
            Assert.That(tracker.HorizontalAxis, Is.Zero);
            Assert.That(tracker.VerticalAxis, Is.Zero);
        });
    }

    [Test]
    public void Reset_ForgetsEveryButton()
    {
        var tracker = new ButtonTracker();
        tracker.Update(new FakeButtonStates { Back = true });

        tracker.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(tracker.IsHeld(ArcadeButton.Back), Is.False);
            Assert.That(tracker.WasReleased(ArcadeButton.Back), Is.False);
        });
    }
}
