using Mtf.Games.Snake;
using Mtf.Games.Tests.Fakes;

namespace Mtf.Games.Tests.Snake;

[TestFixture]
public class FoodProducerTests
{
    [Test]
    public void Constructor_ProducesFoodInsideCanvas()
    {
        var context = new FakeGameContext { ScreenWidth = 40, ScreenHeight = 30 };

        for (int i = 0; i < 200; i++)
        {
            var producer = new FoodProducer(context);
            var food = producer.GetFoodLocation();

            Assert.That(food, Is.Not.Null);
            Assert.That(food!.Fill, Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(food.Radius, Is.EqualTo(Constants.FoodRadius));
                Assert.That(food.Center.X, Is.InRange(Constants.FoodRadius, 40 - Constants.FoodRadius - 1));
                Assert.That(food.Center.Y, Is.InRange(Constants.FoodRadius, 30 - Constants.FoodRadius - 1));
            });
        }
    }

    [Test]
    public void ProduceFood_ReplacesFood()
    {
        var context = new FakeGameContext();
        var producer = new FoodProducer(context);
        var first = producer.GetFoodLocation();

        producer.ProduceFood(context);

        Assert.That(producer.GetFoodLocation(), Is.Not.SameAs(first));
    }

    [Test]
    public void DrawFood_DrawsCurrentFood()
    {
        var context = new FakeGameContext();
        var producer = new FoodProducer(context);

        producer.DrawFood(context);

        Assert.That(context.Drawn, Is.EqualTo(new[] { producer.GetFoodLocation() }));
    }

    [Test]
    public void GetFoodNutrition_IsWithinRange()
    {
        for (int i = 0; i < 200; i++)
        {
            Assert.That(FoodProducer.GetFoodNutrition(), Is.InRange(0, Constants.MaxNutrition - 1));
        }
    }

    [Test]
    public void Constants_HaveExpectedValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Constants.WormBodyRadius, Is.EqualTo(2));
            Assert.That(Constants.FoodRadius, Is.EqualTo(3));
            Assert.That(Constants.MaxNutrition, Is.EqualTo(10));
            Assert.That(Constants.MaxPoints, Is.EqualTo(1000));
            Assert.That(Constants.NumberOfBodyParts, Is.EqualTo(3));
        });
    }
}
