using Mtf.Drawing.Geometry;
using Mtf.Games.General.Arcade;

namespace Mtf.Games.Tests.Arcade;

[TestFixture]
public class CollisionTests
{
    private static readonly RectF Target = new(10, 0, 10, 10);

    [Test]
    public void Intersects_OverlappingRectangles_ReturnsTrue()
    {
        Assert.That(Collision.Intersects(new RectF(0, 0, 10, 10), new RectF(5, 5, 10, 10)), Is.True);
    }

    [Test]
    public void Intersects_TouchingRectangles_ReturnsFalse()
    {
        Assert.That(Collision.Intersects(new RectF(0, 0, 10, 10), new RectF(10, 0, 10, 10)), Is.False);
    }

    [Test]
    public void Intersects_DistantRectangles_ReturnsFalse()
    {
        Assert.That(Collision.Intersects(new RectF(0, 0, 10, 10), new RectF(30, 30, 10, 10)), Is.False);
    }

    [Test]
    public void Intersects_CircleReachingRectangle_ReturnsTrue()
    {
        Assert.That(Collision.Intersects(new CircleF(15, 5, 6), new RectF(0, 0, 10, 10)), Is.True);
    }

    [Test]
    public void Intersects_CircleAwayFromRectangle_ReturnsFalse()
    {
        Assert.That(Collision.Intersects(new CircleF(20, 5, 6), new RectF(0, 0, 10, 10)), Is.False);
    }

    [Test]
    public void GetHitSide_ReturnsTheSideWithSmallerPenetration()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Collision.GetHitSide(new RectF(8, 2, 4, 4), Target), Is.EqualTo(CollisionSide.Left));
            Assert.That(Collision.GetHitSide(new RectF(18, 3, 4, 4), Target), Is.EqualTo(CollisionSide.Right));
            Assert.That(Collision.GetHitSide(new RectF(12, -2, 4, 4), Target), Is.EqualTo(CollisionSide.Top));
            Assert.That(Collision.GetHitSide(new RectF(12, 8, 4, 4), Target), Is.EqualTo(CollisionSide.Bottom));
        });
    }

    [Test]
    public void GetHitSide_NoOverlap_ReturnsNone()
    {
        Assert.That(Collision.GetHitSide(new RectF(30, 30, 4, 4), Target), Is.EqualTo(CollisionSide.None));
    }
}
