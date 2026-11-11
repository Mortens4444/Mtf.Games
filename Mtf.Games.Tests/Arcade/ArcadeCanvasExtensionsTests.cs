using Mtf.Drawing.Geometry;
using Mtf.Drawing.Render;
using Mtf.Games.General.Arcade;
using Mtf.Games.Tests.Fakes;

namespace Mtf.Games.Tests.Arcade;

[TestFixture]
public class ArcadeCanvasExtensionsTests
{
    private const float Tolerance = 0.001f;

    private static FakeGameContext CreateContext() => new() { ScreenWidth = 100, ScreenHeight = 100 };

    [Test]
    public void DrawBox_InsideTheScreen_DrawsTheSameRectangle()
    {
        var context = CreateContext();

        var drawn = context.DrawBox(10, 20, 30, 40, false);

        var rectangle = context.Drawn.OfType<RectanglePrimitive>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(drawn, Is.True);
            Assert.That(rectangle.Rect, Is.EqualTo(new RectF(10, 20, 30, 40)));
            Assert.That(rectangle.Fill, Is.False);
        });
    }

    [Test]
    public void DrawBox_OverTheTopLeftCorner_IsClipped()
    {
        var context = CreateContext();

        context.DrawBox(-5, -5, 10, 10);

        var rectangle = context.Drawn.OfType<RectanglePrimitive>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(rectangle.Rect, Is.EqualTo(new RectF(0, 0, 5, 5)));
            Assert.That(rectangle.Fill, Is.True);
        });
    }

    [Test]
    public void DrawBox_OverTheBottomRightCorner_IsClipped()
    {
        var context = CreateContext();

        context.DrawBox(95, 95, 10, 10);

        var rectangle = context.Drawn.OfType<RectanglePrimitive>().Single();
        Assert.That(rectangle.Rect, Is.EqualTo(new RectF(95, 95, 5, 5)));
    }

    [TestCase(200, 10, 5, 5)]
    [TestCase(-20, 10, 5, 5)]
    [TestCase(10, 200, 5, 5)]
    [TestCase(10, 10, 0, 5)]
    [TestCase(10, 10, 5, -1)]
    public void DrawBox_OutsideOrEmpty_DrawsNothing(float x, float y, float width, float height)
    {
        var context = CreateContext();

        var drawn = context.DrawBox(x, y, width, height);

        Assert.Multiple(() =>
        {
            Assert.That(drawn, Is.False);
            Assert.That(context.Drawn, Is.Empty);
        });
    }

    [Test]
    public void DrawDisc_InsideTheScreen_DrawsRoundedCircle()
    {
        var context = CreateContext();

        var drawn = context.DrawDisc(10.4f, 20.6f, 2.2f);

        var circle = context.Drawn.OfType<CirclePrimitive>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(drawn, Is.True);
            Assert.That(circle.Center.X, Is.EqualTo(10f).Within(Tolerance));
            Assert.That(circle.Center.Y, Is.EqualTo(21f).Within(Tolerance));
            Assert.That(circle.Radius, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(circle.Fill, Is.True);
        });
    }

    [TestCase(-1, 50)]
    [TestCase(50, -1)]
    [TestCase(100, 50)]
    [TestCase(50, 100)]
    public void DrawDisc_CenterOutsideTheScreen_DrawsNothing(float x, float y)
    {
        var context = CreateContext();

        var drawn = context.DrawDisc(x, y, 3);

        Assert.Multiple(() =>
        {
            Assert.That(drawn, Is.False);
            Assert.That(context.Drawn, Is.Empty);
        });
    }

    [Test]
    public void DrawSegment_InsideTheScreen_KeepsTheEndPoints()
    {
        var context = CreateContext();

        context.DrawSegment(1, 2, 30, 40);

        var line = context.Drawn.OfType<LinePrimitive>().Single().Line;
        Assert.Multiple(() =>
        {
            Assert.That(line.A.X, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(line.A.Y, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(line.B.X, Is.EqualTo(30f).Within(Tolerance));
            Assert.That(line.B.Y, Is.EqualTo(40f).Within(Tolerance));
        });
    }

    [Test]
    public void DrawSegment_CrossingTheScreenEdges_IsClipped()
    {
        var context = CreateContext();

        var drawn = context.DrawSegment(-10, 50, 150, 50);

        var line = context.Drawn.OfType<LinePrimitive>().Single().Line;
        Assert.Multiple(() =>
        {
            Assert.That(drawn, Is.True);
            Assert.That(line.A.X, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(line.A.Y, Is.EqualTo(50f).Within(Tolerance));
            Assert.That(line.B.X, Is.EqualTo(99f).Within(Tolerance));
            Assert.That(line.B.Y, Is.EqualTo(50f).Within(Tolerance));
        });
    }

    [Test]
    public void DrawSegment_OutsideTheScreen_DrawsNothing()
    {
        var context = CreateContext();

        var drawn = context.DrawSegment(-10, -10, -1, -5);

        Assert.Multiple(() =>
        {
            Assert.That(drawn, Is.False);
            Assert.That(context.Drawn, Is.Empty);
        });
    }

    [Test]
    public void DrawLabel_InsideTheScreen_DrawsTheText()
    {
        var context = CreateContext();

        var drawn = context.DrawLabel(3, 4, "Score");

        var text = context.Drawn.OfType<TextPrimitive>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(drawn, Is.True);
            Assert.That(text.Layout.Text, Is.EqualTo("Score"));
            Assert.That(text.Layout.Position.X, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(text.Layout.Position.Y, Is.EqualTo(4f).Within(Tolerance));
        });
    }

    [Test]
    public void DrawLabel_EmptyTextOrOutside_DrawsNothing()
    {
        var context = CreateContext();

        var empty = context.DrawLabel(3, 4, "");
        var outside = context.DrawLabel(-3, 4, "Score");

        Assert.Multiple(() =>
        {
            Assert.That(empty, Is.False);
            Assert.That(outside, Is.False);
            Assert.That(context.Drawn, Is.Empty);
        });
    }

    [Test]
    public void DrawBox_NullCanvas_Throws()
    {
        Assert.That(() => ArcadeCanvasExtensions.DrawBox(null!, 0, 0, 1, 1), Throws.ArgumentNullException);
    }
}
