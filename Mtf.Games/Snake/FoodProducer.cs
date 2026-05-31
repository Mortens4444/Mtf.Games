using Mtf.Drawing.Geometry;
using Mtf.Drawing.Render;
using Mtf.Games.Interfaces;
using System.Drawing;

namespace Mtf.Games.Snake;

public class FoodProducer
{
    private CirclePrimitive? food;
    private static readonly Random random = new(Environment.TickCount);

    public FoodProducer(ICanvas canvas)
    {
        ProduceFood(canvas);
    }

    public void DrawFood(ICanvas canvas)
    {
        if (food != null)
        {
            canvas.Draw(food, Color.Black);
        }
    }

    public CirclePrimitive? GetFoodLocation()
    {
        return food;
    }

    public void ProduceFood(ICanvas canvas)
    {
        var x = (byte)random.Next(Constants.FoodRadius, canvas.ScreenWidth - Constants.FoodRadius);
        var y = (byte)random.Next(Constants.FoodRadius, canvas.ScreenHeight - Constants.FoodRadius);
        food = new CirclePrimitive
        {
            Circle = new CircleF(x, y, Constants.FoodRadius),
            Fill = true
        };
    }

    public static int GetFoodNutrition()
    {
        return random.Next(Constants.MaxNutrition);
    }
}
