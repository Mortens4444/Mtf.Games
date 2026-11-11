using Mtf.Drawing.Render;
using Mtf.Games.General.Arcade;
using Mtf.Games.Tests.Fakes;

namespace Mtf.Games.Tests.Arcade;

[TestFixture]
public class SpriteTests
{
    private const float Tolerance = 0.001f;

    [Test]
    public void Update_MovesByVelocity()
    {
        var sprite = new BoxSprite(10, 20, 4, 6)
        {
            VelocityX = 2,
            VelocityY = -3
        };

        sprite.Update();

        Assert.Multiple(() =>
        {
            Assert.That(sprite.X, Is.EqualTo(12f).Within(Tolerance));
            Assert.That(sprite.Y, Is.EqualTo(17f).Within(Tolerance));
            Assert.That(sprite.Right, Is.EqualTo(16f).Within(Tolerance));
            Assert.That(sprite.Bottom, Is.EqualTo(23f).Within(Tolerance));
            Assert.That(sprite.CenterX, Is.EqualTo(14f).Within(Tolerance));
            Assert.That(sprite.CenterY, Is.EqualTo(20f).Within(Tolerance));
        });
    }

    [Test]
    public void ClampTo_KeepsTheSpriteInsideTheArea()
    {
        var topLeft = new BoxSprite(-5, -5, 10, 10);
        var bottomRight = new BoxSprite(95, 95, 10, 10);

        topLeft.ClampTo(0, 10, 100, 100);
        bottomRight.ClampTo(0, 10, 100, 100);

        Assert.Multiple(() =>
        {
            Assert.That(topLeft.X, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(topLeft.Y, Is.EqualTo(10f).Within(Tolerance));
            Assert.That(bottomRight.X, Is.EqualTo(90f).Within(Tolerance));
            Assert.That(bottomRight.Y, Is.EqualTo(90f).Within(Tolerance));
        });
    }

    [Test]
    public void Intersects_OverlappingLiveSprites_ReturnsTrue()
    {
        var first = new BoxSprite(0, 0, 10, 10);
        var second = new BoxSprite(5, 5, 10, 10);

        Assert.That(first.Intersects(second), Is.True);
    }

    [Test]
    public void Intersects_DeadSprite_ReturnsFalse()
    {
        var first = new BoxSprite(0, 0, 10, 10);
        var second = new BoxSprite(5, 5, 10, 10);

        second.Kill();

        Assert.Multiple(() =>
        {
            Assert.That(second.IsAlive, Is.False);
            Assert.That(first.Intersects(second), Is.False);
            Assert.That(second.Intersects(first), Is.False);
        });
    }

    [Test]
    public void IsOutside_TellsWhetherTheSpriteLeftTheArea()
    {
        var inside = new BoxSprite(95, 50, 10, 10);
        var outside = new BoxSprite(101, 50, 10, 10);

        Assert.Multiple(() =>
        {
            Assert.That(inside.IsOutside(0, 0, 100, 100), Is.False);
            Assert.That(outside.IsOutside(0, 0, 100, 100), Is.True);
        });
    }

    [Test]
    public void CenterOn_And_Stop_Work()
    {
        var ball = new BallSprite(3)
        {
            VelocityX = 5,
            VelocityY = 5
        };

        ball.CenterOn(50, 40);
        ball.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(ball.Radius, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(ball.X, Is.EqualTo(47f).Within(Tolerance));
            Assert.That(ball.Y, Is.EqualTo(37f).Within(Tolerance));
            Assert.That(ball.Circle.Center.X, Is.EqualTo(50f).Within(Tolerance));
            Assert.That(ball.Circle.Center.Y, Is.EqualTo(40f).Within(Tolerance));
            Assert.That(ball.VelocityX, Is.Zero);
            Assert.That(ball.VelocityY, Is.Zero);
        });
    }

    [Test]
    public void Draw_UsesTheMatchingPrimitive()
    {
        var context = new FakeGameContext();

        new BoxSprite(10, 10, 5, 5).Draw(context);
        var ball = new BallSprite(3);
        ball.CenterOn(50, 50);
        ball.Draw(context);

        Assert.Multiple(() =>
        {
            Assert.That(context.Drawn, Has.Count.EqualTo(2));
            Assert.That(context.Drawn[0], Is.InstanceOf<RectanglePrimitive>());
            Assert.That(context.Drawn[1], Is.InstanceOf<CirclePrimitive>());
        });
    }
}
