using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;

namespace Mtf.Games.Arkanoid;

/// <summary>
/// Brick breaker: the paddle moves with the left and right buttons,
/// the center or the up button launches the ball. Clearing every brick of the last level wins.
/// </summary>
public class ArkanoidGameEngine(IGameContext? context, Random? randomSource = null) : ArcadeGameEngineBase(context, randomSource)
{
    public const int Columns = 6;

    private const char EmptyCell = '.';
    private const char StrongCell = '@';

    // '#' needs one hit, '@' needs two hits, '.' is empty. Every row has Columns cells.
    private static readonly string[][] Layouts =
    [
        ["######", "######", "######"],
        ["#.##.#", ".#..#.", "#.##.#", ".#..#."],
        ["@@@@@@", "#.##.#", "######", "@.@@.@"]
    ];

    private readonly List<ArkanoidBrick> bricks = [];

    // Assigned by SetupLevel, which the base constructor invokes.
    private BoxSprite paddle = null!;
    private BallSprite ball = null!;
    private float ballSpeed;
    private bool ballStuck;

    public int RemainingBricks => bricks.Count;

    protected override int LevelCount => Layouts.Length;

    protected override bool CenterButtonPauses => false;

    protected override void SetupLevel()
    {
        bricks.Clear();

        var layout = Layouts[(Level - 1) % Layouts.Length];
        var brickWidth = FieldWidth / Columns;
        var brickHeight = MathF.Max(3f, Unit * 2.5f);
        var top = FieldTop + brickHeight;
        for (var row = 0; row < layout.Length; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                var cell = layout[row][column];
                if (cell == EmptyCell)
                {
                    continue;
                }

                var hits = cell == StrongCell ? 2 : 1;
                bricks.Add(new ArkanoidBrick(FieldLeft + column * brickWidth, top + row * brickHeight, brickWidth, brickHeight, hits));
            }
        }

        var paddleWidth = FieldWidth / 5f;
        var paddleHeight = MathF.Max(2f, Unit * 1.5f);
        paddle = new BoxSprite(FieldLeft + (FieldWidth - paddleWidth) / 2f, FieldBottom - paddleHeight - Unit, paddleWidth, paddleHeight);
        ball = new BallSprite(Unit);
        ballSpeed = Unit * (1.3f + 0.2f * Level);
        StickBall();
    }

    protected override void OnLifeLost()
    {
        StickBall();
    }

    protected override void UpdateGame()
    {
        paddle.VelocityX = Buttons.HorizontalAxis * Unit * 2f;
        paddle.Update();
        paddle.ClampTo(FieldLeft, FieldTop, FieldRight, FieldBottom);

        if (ballStuck)
        {
            ball.CenterOn(paddle.CenterX, paddle.Top - ball.Height / 2f);
            if (Buttons.WasPressed(ArcadeButton.Center) || Buttons.WasPressed(ArcadeButton.Up))
            {
                Launch();
            }
            return;
        }

        ball.Update();
        BounceOffWalls();
        BounceOffPaddle();
        BounceOffBricks();

        if (ball.Top > FieldBottom)
        {
            LoseLife();
        }
        else if (bricks.Count == 0)
        {
            CompleteLevel();
        }
    }

    protected override void DrawGame()
    {
        foreach (var brick in bricks)
        {
            brick.Draw(gameContext);
        }

        paddle.Draw(gameContext);
        ball.Draw(gameContext);
    }

    private void StickBall()
    {
        ballStuck = true;
        ball.Stop();
        ball.CenterOn(paddle.CenterX, paddle.Top - ball.Height / 2f);
    }

    private void Launch()
    {
        ballStuck = false;
        ball.VelocityX = ballSpeed * (Randomizer.Next(2) == 0 ? -0.5f : 0.5f);
        ball.VelocityY = -VerticalSpeedFor(ball.VelocityX);
        PlayTone(660, 30);
    }

    private float VerticalSpeedFor(float horizontalSpeed)
    {
        return MathF.Sqrt(MathF.Max(0f, ballSpeed * ballSpeed - horizontalSpeed * horizontalSpeed));
    }

    private void BounceOffWalls()
    {
        if (ball.Left < FieldLeft)
        {
            ball.X = FieldLeft;
            ball.VelocityX = MathF.Abs(ball.VelocityX);
        }
        else if (ball.Right > FieldRight)
        {
            ball.X = FieldRight - ball.Width;
            ball.VelocityX = -MathF.Abs(ball.VelocityX);
        }

        if (ball.Top < FieldTop)
        {
            ball.Y = FieldTop;
            ball.VelocityY = MathF.Abs(ball.VelocityY);
        }
    }

    private void BounceOffPaddle()
    {
        if (ball.VelocityY <= 0f || !ball.Intersects(paddle))
        {
            return;
        }

        // The further from the middle of the paddle, the flatter the ball leaves.
        var offset = (ball.CenterX - paddle.CenterX) / (paddle.Width / 2f);
        offset = MathF.Max(-1f, MathF.Min(1f, offset));

        ball.Y = paddle.Top - ball.Height;
        ball.VelocityX = offset * ballSpeed * 0.85f;
        ball.VelocityY = -VerticalSpeedFor(ball.VelocityX);
        PlayTone(660, 30);
    }

    private void BounceOffBricks()
    {
        foreach (var brick in bricks)
        {
            if (!ball.Intersects(brick))
            {
                continue;
            }

            switch (Collision.GetHitSide(ball.Bounds, brick.Bounds))
            {
                case CollisionSide.Left:
                    ball.VelocityX = -MathF.Abs(ball.VelocityX);
                    break;
                case CollisionSide.Right:
                    ball.VelocityX = MathF.Abs(ball.VelocityX);
                    break;
                case CollisionSide.Top:
                    ball.VelocityY = -MathF.Abs(ball.VelocityY);
                    break;
                default:
                    ball.VelocityY = MathF.Abs(ball.VelocityY);
                    break;
            }

            if (brick.Hit())
            {
                AddScore(10 * Level);
            }
            PlayTone(880, 20);

            // One brick per game moment, so the ball is not turned back twice.
            break;
        }

        bricks.RemoveAll(brick => !brick.IsAlive);
    }
}
