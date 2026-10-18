using Mtf.Drawing.Geometry;
using Mtf.Drawing.Render;
using Mtf.Games.General;
using Mtf.Games.Snake;
using Mtf.Games.Tests.Fakes;

namespace Mtf.Games.Tests.Snake;

[TestFixture]
public class WormyTests
{
    private const int CenterY = 50;

    private static FakeGameContext CreateContext(int width = 100, int height = 100) => new() { ScreenWidth = width, ScreenHeight = height };

    private static CirclePrimitive Probe(float x, float y) => new()
    {
        Circle = new CircleF(x, y, 1),
        Fill = true
    };

    private static int CountBodyParts(Wormy worm, FakeGameContext context)
    {
        context.Drawn.Clear();
        worm.Draw(context);
        return context.Drawn.Count;
    }

    [Test]
    public void Constructor_CreatesInitialBodyParts()
    {
        var context = CreateContext();
        var worm = new Wormy(context);

        worm.Draw(context);

        Assert.Multiple(() =>
        {
            Assert.That(context.Drawn, Has.Count.EqualTo(Constants.NumberOfBodyParts).And.All.InstanceOf<CirclePrimitive>());
            Assert.That(worm.Direction, Is.EqualTo(Direction.East));
        });
    }

    [Test]
    public void Constructor_HeadIsAtExpectedPosition()
    {
        var context = CreateContext();
        var worm = new Wormy(context);

        Assert.Multiple(() =>
        {
            Assert.That(worm.CanConsumeFood(Probe(28, CenterY)), Is.True);
            Assert.That(worm.CanConsumeFood(Probe(20, CenterY)), Is.True);
            Assert.That(worm.CanConsumeFood(Probe(40, CenterY)), Is.False);
        });
    }

    [Test]
    public void MoveForward_East_AdvancesHead()
    {
        var context = CreateContext();
        var worm = new Wormy(context);

        var alive = worm.MoveForward(context);

        Assert.Multiple(() =>
        {
            Assert.That(alive, Is.True);
            Assert.That(CountBodyParts(worm, context), Is.EqualTo(Constants.NumberOfBodyParts));
            Assert.That(worm.CanConsumeFood(Probe(32, CenterY)), Is.True);
            Assert.That(worm.CanConsumeFood(Probe(20, CenterY)), Is.False);
        });
    }

    [Test]
    public void MoveForward_AfterDirectionChange_MovesInNewDirection()
    {
        var context = CreateContext();
        var worm = new Wormy(context);
        worm.ChangeDirection(Direction.South);

        worm.MoveForward(context);

        Assert.That(worm.CanConsumeFood(Probe(28, CenterY + 4)), Is.True);
    }

    [Test]
    public void MoveForward_North_DecreasesY()
    {
        var context = CreateContext();
        var worm = new Wormy(context);
        worm.ChangeDirection(Direction.North);

        worm.MoveForward(context);

        Assert.That(worm.CanConsumeFood(Probe(28, CenterY - 4)), Is.True);
    }

    [Test]
    public void MoveForward_OutOfScreen_ReturnsFalse()
    {
        var context = CreateContext(width: 30);
        var worm = new Wormy(context);

        var alive = worm.MoveForward(context);

        Assert.That(alive, Is.False);
    }

    [Test]
    public void MoveForward_UntilRightEdge_StaysAliveWhileInside()
    {
        var context = CreateContext(width: 100);
        var worm = new Wormy(context);
        var steps = 0;

        while (worm.MoveForward(context))
        {
            steps++;
        }

        Assert.That(steps, Is.EqualTo(18));
    }

    [Test]
    public void MoveForward_AboveTop_ReturnsFalse()
    {
        var context = CreateContext(height: 100);
        var worm = new Wormy(context);
        worm.ChangeDirection(Direction.North);

        var alive = true;
        for (int i = 0; i < 20 && alive; i++)
        {
            alive = worm.MoveForward(context);
        }

        Assert.That(alive, Is.False);
    }

    [Test]
    public void Grow_AddsBodyPartsBasedOnNutrition()
    {
        var context = CreateContext();
        var worm = new Wormy(context);

        worm.Grow(25);

        Assert.That(CountBodyParts(worm, context), Is.EqualTo(Constants.NumberOfBodyParts + 5));
    }

    [Test]
    public void Grow_SmallNutrition_DoesNotGrow()
    {
        var context = CreateContext();
        var worm = new Wormy(context);

        worm.Grow(4);

        Assert.That(CountBodyParts(worm, context), Is.EqualTo(Constants.NumberOfBodyParts));
    }

    [Test]
    public void Grow_NewPartsAppearAtTail()
    {
        var context = CreateContext();
        var worm = new Wormy(context);
        worm.Grow(10);

        worm.MoveForward(context);

        Assert.Multiple(() =>
        {
            Assert.That(CountBodyParts(worm, context), Is.EqualTo(Constants.NumberOfBodyParts + 2));
            Assert.That(worm.CanConsumeFood(Probe(20, CenterY)), Is.True);
            Assert.That(worm.CanConsumeFood(Probe(32, CenterY)), Is.True);
        });
    }

    [Test]
    public void MoveForward_IntoOwnBody_ReturnsFalse()
    {
        var context = CreateContext();
        var worm = new Wormy(context);
        worm.Grow(25);

        Assert.That(worm.MoveForward(context), Is.True);
        worm.ChangeDirection(Direction.South);
        Assert.That(worm.MoveForward(context), Is.True);
        worm.ChangeDirection(Direction.West);
        Assert.That(worm.MoveForward(context), Is.True);
        worm.ChangeDirection(Direction.North);

        Assert.That(worm.MoveForward(context), Is.False);
    }

    [Test]
    public void MoveForward_ShortWorm_CannotCollideWithItself()
    {
        var context = CreateContext();
        var worm = new Wormy(context);

        worm.MoveForward(context);
        worm.ChangeDirection(Direction.South);
        worm.MoveForward(context);
        worm.ChangeDirection(Direction.West);
        worm.MoveForward(context);
        worm.ChangeDirection(Direction.North);

        Assert.That(worm.MoveForward(context), Is.True);
    }

    [Test]
    public void CanConsumeFood_Null_ReturnsFalse()
    {
        var context = CreateContext();
        var worm = new Wormy(context);

        Assert.That(worm.CanConsumeFood(null), Is.False);
    }

    [Test]
    public void CanConsumeFood_TouchingBody_ReturnsTrue()
    {
        var context = CreateContext();
        var worm = new Wormy(context);
        var food = new CirclePrimitive
        {
            Circle = new CircleF(24, CenterY + 3, Constants.FoodRadius),
            Fill = true
        };

        Assert.That(worm.CanConsumeFood(food), Is.True);
    }

    [Test]
    public void CanConsumeFood_FarAway_ReturnsFalse()
    {
        var context = CreateContext();
        var worm = new Wormy(context);
        var food = new CirclePrimitive
        {
            Circle = new CircleF(80, 10, Constants.FoodRadius),
            Fill = true
        };

        Assert.That(worm.CanConsumeFood(food), Is.False);
    }

    [Test]
    public void Draw_DrawsEveryBodyPart()
    {
        var context = CreateContext();
        var worm = new Wormy(context);
        worm.Grow(15);

        worm.Draw(context);

        Assert.That(context.Drawn, Has.Count.EqualTo(Constants.NumberOfBodyParts + 3));
    }
}
