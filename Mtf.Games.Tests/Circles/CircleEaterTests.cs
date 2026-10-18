using Mtf.Games.Circles;
using Mtf.Games.Tests.Fakes;
using System.Drawing;

namespace Mtf.Games.Tests.Circles;

[TestFixture]
public class CircleEaterTests
{
    [Test]
    public void Constructor_WithCoordinates_CreatesFilledCircle()
    {
        var eater = new CircleEater(10, 20, 4);

        Assert.Multiple(() =>
        {
            Assert.That(eater.Circle.Center, Is.EqualTo(new PointF(10, 20)));
            Assert.That(eater.Circle.Radius, Is.EqualTo(4));
            Assert.That(eater.Circle.Fill, Is.True);
        });
    }

    [Test]
    public void Constructor_WithCanvas_PlacesCircleInsideCanvas()
    {
        var canvas = new FakeGameContext { ScreenWidth = 100, ScreenHeight = 60 };

        for (int i = 0; i < 200; i++)
        {
            var eater = new CircleEater(canvas, 5);

            Assert.Multiple(() =>
            {
                Assert.That(eater.Circle.Fill, Is.False);
                Assert.That(eater.Circle.Radius, Is.EqualTo(5));
                Assert.That(eater.Circle.Center.X, Is.InRange(5, 94));
                Assert.That(eater.Circle.Center.Y, Is.InRange(5, 54));
            });
        }
    }

    [Test]
    public void Constructor_WithCanvas_StartsMoving()
    {
        var canvas = new FakeGameContext { ScreenWidth = 200, ScreenHeight = 200 };
        var eater = new CircleEater(canvas, 5);
        var start = eater.Circle.Center;

        eater.Move(canvas);

        Assert.That(eater.Circle.Center, Is.Not.EqualTo(start));
    }

    [Test]
    public void Move_WithoutModifiers_StaysInPlace()
    {
        var canvas = new FakeGameContext();
        var eater = new CircleEater(10, 20, 4);

        eater.Move(canvas);

        Assert.That(eater.Circle.Center, Is.EqualTo(new PointF(10, 20)));
    }

    [Test]
    public void Move_Right_IncreasesX()
    {
        var canvas = new FakeGameContext();
        var eater = new CircleEater(10, 20, 4);
        eater.MoveRight(true);

        eater.Move(canvas);

        Assert.That(eater.Circle.Center, Is.EqualTo(new PointF(12, 20)));
    }

    [Test]
    public void Move_Left_DecreasesX()
    {
        var canvas = new FakeGameContext();
        var eater = new CircleEater(10, 20, 4);
        eater.MoveLeft(true);

        eater.Move(canvas);

        Assert.That(eater.Circle.Center, Is.EqualTo(new PointF(8, 20)));
    }

    [Test]
    public void Move_Up_DecreasesY()
    {
        var canvas = new FakeGameContext();
        var eater = new CircleEater(10, 20, 4);
        eater.MoveUp(true);

        eater.Move(canvas);

        Assert.That(eater.Circle.Center, Is.EqualTo(new PointF(10, 18)));
    }

    [Test]
    public void Move_Down_IncreasesY()
    {
        var canvas = new FakeGameContext();
        var eater = new CircleEater(10, 20, 4);
        eater.MoveDown(true);

        eater.Move(canvas);

        Assert.That(eater.Circle.Center, Is.EqualTo(new PointF(10, 22)));
    }

    [Test]
    public void Move_OppositeModifiers_CancelOut()
    {
        var canvas = new FakeGameContext();
        var eater = new CircleEater(10, 20, 4);
        eater.MoveRight(true);
        eater.MoveLeft(true);
        eater.MoveUp(true);
        eater.MoveDown(true);

        eater.Move(canvas);

        Assert.That(eater.Circle.Center, Is.EqualTo(new PointF(10, 20)));
    }

    [Test]
    public void Move_DisablingModifier_StopsMovement()
    {
        var canvas = new FakeGameContext();
        var eater = new CircleEater(10, 20, 4);
        eater.MoveRight(true);
        eater.Move(canvas);
        eater.MoveRight(false);

        eater.Move(canvas);

        Assert.That(eater.Circle.Center, Is.EqualTo(new PointF(12, 20)));
    }

    [Test]
    public void Move_PastLeftEdge_WrapsToRight()
    {
        var canvas = new FakeGameContext { ScreenWidth = 50, ScreenHeight = 50 };
        var eater = new CircleEater(5, 20, 4);
        eater.MoveLeft(true);

        eater.Move(canvas);

        Assert.That(eater.Circle.Center.X, Is.EqualTo(46));
    }

    [Test]
    public void Move_PastRightEdge_WrapsToLeft()
    {
        var canvas = new FakeGameContext { ScreenWidth = 50, ScreenHeight = 50 };
        var eater = new CircleEater(47, 20, 4);
        eater.MoveRight(true);

        eater.Move(canvas);

        Assert.That(eater.Circle.Center.X, Is.EqualTo(4));
    }

    [Test]
    public void Move_PastTopEdge_WrapsToBottom()
    {
        var canvas = new FakeGameContext { ScreenWidth = 50, ScreenHeight = 50 };
        var eater = new CircleEater(20, 5, 4);
        eater.MoveUp(true);

        eater.Move(canvas);

        Assert.That(eater.Circle.Center.Y, Is.EqualTo(46));
    }

    [Test]
    public void Move_PastBottomEdge_WrapsToTop()
    {
        var canvas = new FakeGameContext { ScreenWidth = 50, ScreenHeight = 50 };
        var eater = new CircleEater(20, 47, 4);
        eater.MoveDown(true);

        eater.Move(canvas);

        Assert.That(eater.Circle.Center.Y, Is.EqualTo(4));
    }

    [Test]
    public void ChangeMoving_FirstCall_PicksADirection()
    {
        var canvas = new FakeGameContext { ScreenWidth = 200, ScreenHeight = 200 };
        var eater = new CircleEater(100, 100, 4);

        eater.ChangeMoving();
        eater.Move(canvas);

        Assert.That(eater.Circle.Center, Is.Not.EqualTo(new PointF(100, 100)));
    }

    [Test]
    public void ChangeMoving_KeepsDirectionForSeveralCycles()
    {
        var canvas = new FakeGameContext { ScreenWidth = 255, ScreenHeight = 255 };
        var eater = new CircleEater(128, 128, 1);
        eater.ChangeMoving();
        eater.Move(canvas);
        var firstStep = eater.Circle.Center;
        var delta = new PointF(firstStep.X - 128, firstStep.Y - 128);

        for (int i = 0; i < 10; i++)
        {
            eater.ChangeMoving();
            eater.Move(canvas);
        }

        var expected = new PointF(128 + delta.X * 11, 128 + delta.Y * 11);
        Assert.That(eater.Circle.Center, Is.EqualTo(expected));
    }

    [Test]
    public void ChangeMoving_ManyCalls_AlwaysKeepsMoving()
    {
        var canvas = new FakeGameContext { ScreenWidth = 255, ScreenHeight = 255 };
        var eater = new CircleEater(128, 128, 1);

        for (int i = 0; i < 60; i++)
        {
            var before = eater.Circle.Center;
            eater.ChangeMoving();
            eater.Move(canvas);
            Assert.That(eater.Circle.Center, Is.Not.EqualTo(before), $"Iteration {i}");
        }
    }
}
