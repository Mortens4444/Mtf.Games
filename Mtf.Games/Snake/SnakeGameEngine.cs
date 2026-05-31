using Mtf.Drawing;
using Mtf.Drawing.Render;
using Mtf.Games.General;
using Mtf.Games.Interfaces;
using System.Drawing;

namespace Mtf.Games.Snake;

public class SnakeGameEngine(IGameContext? gameContext) : GameEngineBase(gameContext)
{
    private Wormy worm = new(gameContext);
    private DirectionHandler? directionHandler;
    private FoodProducer foodProducer = new(gameContext);
    private ScoreCounter scoreCounter = new();

    protected override void StartNewGame()
    {
        worm = new Wormy(gameContext);
        directionHandler = new DirectionHandler(worm);
        foodProducer = new FoodProducer(gameContext);
        scoreCounter = new ScoreCounter();
        inGame = true;
        message = GameOver;
    }

    protected override IButtonStates? GameMoment()
    {
        inGame &= worm.MoveForward(gameContext);
        if (inGame)
        {
            worm.Draw(gameContext);

            foodProducer.DrawFood(gameContext);
            if (worm.CanConsumeFood(foodProducer.GetFoodLocation()))
            {
                var nutrition = FoodProducer.GetFoodNutrition();
                scoreCounter.Add(nutrition * 2);
                if (scoreCounter.Score > Constants.MaxPoints)
                {
                    message = "You won!";
                    inGame = false;
                }
                foodProducer.ProduceFood(gameContext);
                worm.Grow(nutrition);
            }
            var text = new TextPrimitive(new PointPrimitive(0, 0), $"Score: {Constants.MaxPoints} / {scoreCounter.Score}")
            {
                FontType = FontType.Tiny,
                //                Text = new TextLayout($"Score: {scoreCounter.Score}", FontType.Tiny, LCDColor.Black),
            };

            gameContext.Draw(text, Color.Black);
        }

        return directionHandler?.HandleKeyPress(gameContext);
    }
}
