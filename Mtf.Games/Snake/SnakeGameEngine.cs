using Mtf.Drawing.Render;
using Mtf.Games.General;
using Mtf.Games.Interfaces;
using System.Drawing;

namespace Mtf.Games.Snake;

public class SnakeGameEngine : GameEngineBase
{
    private DirectionHandler? directionHandler;
    private ScoreCounter scoreCounter = new();

    // Assigned by StartNewGame, which the base constructor invokes.
    private Wormy worm = null!;
    private FoodProducer foodProducer = null!;

    public SnakeGameEngine(IGameContext? gameContext) : base(gameContext)
    {
    }

    protected override void StartNewGame()
    {
        worm = new Wormy(gameContext);
        directionHandler = new DirectionHandler(worm);
        foodProducer = new FoodProducer(gameContext);
        scoreCounter = new ScoreCounter();
        inGame = true;
        message = GameOver;
    }

    public override IButtonStates? GameMoment()
    {
        var alive = worm.MoveForward(gameContext);
        if (inGame && !alive)
        {
            Lost();
        }

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
                    Won();
                }
                foodProducer.ProduceFood(gameContext);
                worm.Grow(nutrition);
            }
            var text = new TextPrimitive(new PointPrimitive(0, 0), $"Score: {Constants.MaxPoints} / {scoreCounter.Score}");
            gameContext.Draw(text, Color.Black);
        }

        return directionHandler?.HandleKeyPress(gameContext);
    }
}
