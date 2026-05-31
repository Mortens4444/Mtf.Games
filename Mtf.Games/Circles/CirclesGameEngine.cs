using Mtf.Drawing.Geometry;
using Mtf.Drawing.Render;
using Mtf.Games.General;
using Mtf.Games.Interfaces;
using System.Drawing;

namespace Mtf.Games.Circles;

public class CirclesGameEngine(IGameContext? gameContext) : GameEngineBase(gameContext)
{
    private MovingHandler? movingHandler;

    private CircleEater player = new(gameContext, 4);
    private IList<CircleEater> enemies = [];
    private readonly Random random = new(Environment.TickCount);

    private const int NumberOfEnemies = 15;

    protected override void StartNewGame()
    {
        inGame = true;
        player = new CircleEater((byte)gameContext.HorizontalCenter, (byte)gameContext.VerticalCenter, 4);
        movingHandler = new MovingHandler(player);
        enemies = [];

        var playerZone = new CirclePrimitive
        {
            Shape = new CircleF(gameContext.HorizontalCenter, gameContext.VerticalCenter, 20),
            Fill = false
        };
        for (int i = 0; i < NumberOfEnemies; i++)
        {
            CircleEater enemy;
            do
            {
                enemy = new CircleEater(gameContext, (byte)random.Next(2, 10));
            }
            while (playerZone.IsColliding(enemy.Circle));
            enemies.Add(enemy);
        }
    }

    protected override IButtonStates? GameMoment()
    {
        player.Move(gameContext);
        bool drawPlayer = true;

        if (inGame)
        {
            var enemy = enemies.FirstOrDefault(e => e.Circle.IsColliding(player.Circle));
            if (enemy == null)
            {
                foreach (var currentEnemy in enemies)
                {
                    currentEnemy.ChangeMoving();
                    currentEnemy.Move(gameContext);
                    gameContext.Draw(currentEnemy.Circle, Color.Black);
                }
            }
            else
            {
                if (player.Circle.Radius >= enemy.Circle.Radius)
                {
                    enemies.Remove(enemy);
                    gameContext.Draw(player.Circle, Color.Black);
                    player.Circle.Shape.Inflate((byte)(enemy.Circle.Radius / 4));
                    if (enemies.Count == 0)
                    {
                        message = "You won!";
                        inGame = false;
                    }
                }
                else
                {
                    message = GameOver;
                    drawPlayer = false;
                    gameContext.Draw(enemy.Circle, Color.Black);
                    inGame = false;
                }
            }

            if (drawPlayer)
            {
                gameContext.Draw(player.Circle, Color.Black);
            }
        }

        return movingHandler?.HandleKeyPress(gameContext);
    }
}
