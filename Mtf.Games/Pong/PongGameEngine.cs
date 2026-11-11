using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;

namespace Mtf.Games.Pong;

/// <summary>
/// One player Pong: the player moves the left paddle with the up and down buttons,
/// the right paddle is driven by the game. The first to miss loses: the player a life, the game a point.
/// </summary>
public class PongGameEngine(IGameContext? context, Random? randomSource = null) : ArcadeGameEngineBase(context, randomSource)
{
    public const int WinningScore = 5;

    private const int NetDashes = 4;

    private readonly Cooldown serveDelay = new(20);

    // Assigned by SetupLevel, which the base constructor invokes.
    private BoxSprite player = null!;
    private BoxSprite opponent = null!;
    private BallSprite ball = null!;
    private float ballSpeed;

    private float PaddleSpeed => Unit * 1.5f;

    private float StartBallSpeed => Unit * 1.5f;

    private float MaxBallSpeed => Unit * 2.5f;

    protected override void SetupLevel()
    {
        var paddleWidth = MathF.Max(2f, Unit * 1.5f);
        var paddleHeight = FieldHeight / 4f;
        var paddleTop = FieldTop + (FieldHeight - paddleHeight) / 2f;

        player = new BoxSprite(FieldLeft + Unit, paddleTop, paddleWidth, paddleHeight);
        opponent = new BoxSprite(FieldRight - Unit - paddleWidth, paddleTop, paddleWidth, paddleHeight);
        ball = new BallSprite(Unit);
        Serve(false);
    }

    protected override void OnLifeLost()
    {
        Serve(false);
    }

    protected override void UpdateGame()
    {
        player.VelocityY = Buttons.VerticalAxis * PaddleSpeed;
        player.Update();
        player.ClampTo(FieldLeft, FieldTop, FieldRight, FieldBottom);

        MoveOpponent();

        serveDelay.Tick();
        if (!serveDelay.IsReady)
        {
            return;
        }

        ball.Update();
        BounceOffWalls();

        if (ball.VelocityX < 0f && ball.Intersects(player))
        {
            ball.X = player.Right;
            Deflect(player, 1f);
        }
        else if (ball.VelocityX > 0f && ball.Intersects(opponent))
        {
            ball.X = opponent.Left - ball.Width;
            Deflect(opponent, -1f);
        }

        if (ball.Right < FieldLeft)
        {
            LoseLife();
        }
        else if (ball.Left > FieldRight)
        {
            AddScore(1);
            PlayTone(990, 80);
            if (Score >= WinningScore)
            {
                CompleteLevel();
            }
            else
            {
                Serve(true);
            }
        }
    }

    protected override void DrawGame()
    {
        var dashHeight = FieldHeight / (NetDashes * 2f);
        var netX = FieldLeft + FieldWidth / 2f;
        for (var i = 0; i < NetDashes; i++)
        {
            gameContext.DrawBox(netX, FieldTop + dashHeight * (i * 2f + 0.5f), 1f, dashHeight);
        }

        player.Draw(gameContext);
        opponent.Draw(gameContext);
        ball.Draw(gameContext);
    }

    private void Serve(bool towardsPlayer)
    {
        ballSpeed = StartBallSpeed;
        ball.CenterOn(FieldLeft + FieldWidth / 2f, FieldTop + FieldHeight / 2f);
        ball.VelocityX = towardsPlayer ? -ballSpeed : ballSpeed;
        ball.VelocityY = (Randomizer.NextSingle() - 0.5f) * ballSpeed;
        serveDelay.Restart();
    }

    private void MoveOpponent()
    {
        var speed = PaddleSpeed * 0.6f;
        var target = ball.VelocityX > 0f ? ball.CenterY : FieldTop + FieldHeight / 2f;
        var distance = target - opponent.CenterY;

        opponent.VelocityY = MathF.Max(-speed, MathF.Min(speed, distance));
        opponent.Update();
        opponent.ClampTo(FieldLeft, FieldTop, FieldRight, FieldBottom);
    }

    private void BounceOffWalls()
    {
        if (ball.Top < FieldTop)
        {
            ball.Y = FieldTop;
            ball.VelocityY = MathF.Abs(ball.VelocityY);
        }
        else if (ball.Bottom > FieldBottom)
        {
            ball.Y = FieldBottom - ball.Height;
            ball.VelocityY = -MathF.Abs(ball.VelocityY);
        }
    }

    private void Deflect(BoxSprite paddle, float direction)
    {
        var offset = (ball.CenterY - paddle.CenterY) / (paddle.Height / 2f);
        offset = MathF.Max(-1f, MathF.Min(1f, offset));

        ballSpeed = MathF.Min(ballSpeed * 1.05f, MaxBallSpeed);
        ball.VelocityX = direction * ballSpeed;
        ball.VelocityY = offset * ballSpeed;
        PlayTone(660, 30);
    }
}
