using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;
using System.Drawing;

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

    // Bright colors for the bricks, one for every row. A display without colors shows them light,
    // so the black frame and the black core of a strong brick stay visible.
    private static readonly Color[] RowColors = [Color.Tomato, Color.Orange, Color.LimeGreen, Color.DeepSkyBlue, Color.Violet];

    // Dark colors for the paddle and the ball, they have to be visible on a display without colors too.
    private static readonly Color PaddleColor = Color.MidnightBlue;
    private static readonly Color BallColor = Color.Crimson;

    // One destroyed brick in PerkDropChance drops a perk. The effects last EffectTicks game moments.
    private const int PerkDropChance = 5;
    private const int MaxPerks = 3;
    private const int MaxLives = 5;
    private const int EffectTicks = 600;
    private const float WidePaddleFactor = 2f;
    private const float SlowBallFactor = 0.65f;

    // Perks are picked from this list, so the extra life is the rarest one.
    private static readonly ArkanoidPerkKind[] PerkKinds =
    [
        ArkanoidPerkKind.WidePaddle,
        ArkanoidPerkKind.SlowBall,
        ArkanoidPerkKind.StickyPaddle,
        ArkanoidPerkKind.WidePaddle,
        ArkanoidPerkKind.SlowBall,
        ArkanoidPerkKind.ExtraLife
    ];

    private readonly List<ArkanoidBrick> bricks = [];
    private readonly List<ArkanoidPerk> perks = [];

    // Assigned by SetupLevel, which the base constructor invokes.
    private BoxSprite paddle = null!;
    private BallSprite ball = null!;
    private float ballSpeed;
    private float paddleWidth;
    private float stickOffset;
    private bool ballStuck;
    private int wideTicks;
    private int slowTicks;
    private int stickyTicks;

    public int RemainingBricks => bricks.Count;

    /// <summary>
    /// True while the paddle is twice as wide.
    /// </summary>
    public bool IsPaddleWide => wideTicks > 0;

    /// <summary>
    /// True while the ball is slowed down.
    /// </summary>
    public bool IsBallSlow => slowTicks > 0;

    /// <summary>
    /// True while the ball sticks to the paddle.
    /// </summary>
    public bool IsPaddleSticky => stickyTicks > 0;

    protected override int LevelCount => Layouts.Length;

    protected override bool CenterButtonPauses => false;

    protected override string HudText
    {
        get
        {
            var effects = (IsPaddleWide ? "W" : "") + (IsBallSlow ? "S" : "") + (IsPaddleSticky ? "G" : "");
            return effects.Length == 0 ? base.HudText : $"{base.HudText} {effects}";
        }
    }

    private float CurrentBallSpeed => IsBallSlow ? ballSpeed * SlowBallFactor : ballSpeed;

    protected override void SetupLevel()
    {
        bricks.Clear();
        perks.Clear();
        wideTicks = 0;
        slowTicks = 0;
        stickyTicks = 0;

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
                bricks.Add(new ArkanoidBrick(FieldLeft + column * brickWidth, top + row * brickHeight, brickWidth, brickHeight, hits)
                {
                    Color = RowColors[row % RowColors.Length]
                });
            }
        }

        paddleWidth = FieldWidth / 5f;
        var paddleHeight = MathF.Max(2f, Unit * 1.5f);
        paddle = new BoxSprite(FieldLeft + (FieldWidth - paddleWidth) / 2f, FieldBottom - paddleHeight - Unit, paddleWidth, paddleHeight)
        {
            Color = PaddleColor
        };
        ball = new BallSprite(Unit)
        {
            Color = BallColor
        };
        ballSpeed = Unit * (1.3f + 0.2f * Level);
        StickBall();
    }

    protected override void OnLifeLost()
    {
        // A lost life takes the perks away, the falling ones too.
        perks.Clear();
        wideTicks = 0;
        slowTicks = 0;
        stickyTicks = 0;
        SetPaddleWidth(paddleWidth);
        StickBall();
    }

    protected override void UpdateGame()
    {
        paddle.VelocityX = Buttons.HorizontalAxis * Unit * 2f;
        paddle.Update();
        paddle.ClampTo(FieldLeft, FieldTop, FieldRight, FieldBottom);
        MovePerks();
        CountDownEffects();

        if (ballStuck)
        {
            ball.CenterOn(paddle.CenterX + stickOffset, paddle.Top - ball.Height / 2f);
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

        foreach (var perk in perks)
        {
            perk.Draw(gameContext);
        }

        paddle.Draw(gameContext);
        ball.Draw(gameContext);
    }

    private void StickBall(float offset = 0f)
    {
        ballStuck = true;
        stickOffset = offset;
        ball.Stop();
        ball.CenterOn(paddle.CenterX + stickOffset, paddle.Top - ball.Height / 2f);
    }

    private void Launch()
    {
        ballStuck = false;

        // A ball caught away from the middle of the paddle leaves the way it would bounce off.
        var offset = stickOffset / (paddle.Width / 2f);
        ball.VelocityX = MathF.Abs(offset) > 0.05f
            ? MathF.Max(-1f, MathF.Min(1f, offset)) * CurrentBallSpeed * 0.85f
            : CurrentBallSpeed * (Randomizer.Next(2) == 0 ? -0.5f : 0.5f);
        ball.VelocityY = -VerticalSpeedFor(ball.VelocityX);
        PlayTone(660, 30);
    }

    private float VerticalSpeedFor(float horizontalSpeed)
    {
        var speed = CurrentBallSpeed;
        return MathF.Sqrt(MathF.Max(0f, speed * speed - horizontalSpeed * horizontalSpeed));
    }

    private void MovePerks()
    {
        foreach (var perk in perks)
        {
            perk.Update();
            if (perk.Intersects(paddle))
            {
                perk.Kill();
                Collect(perk.Kind);
            }
            else if (perk.Top > FieldBottom)
            {
                perk.Kill();
            }
        }

        perks.RemoveAll(perk => !perk.IsAlive);
    }

    private void Collect(ArkanoidPerkKind kind)
    {
        switch (kind)
        {
            case ArkanoidPerkKind.WidePaddle:
                wideTicks = EffectTicks;
                SetPaddleWidth(paddleWidth * WidePaddleFactor);
                break;
            case ArkanoidPerkKind.SlowBall:
                // The speed of a moving ball is scaled once, when the effect starts.
                if (!IsBallSlow)
                {
                    ball.VelocityX *= SlowBallFactor;
                    ball.VelocityY *= SlowBallFactor;
                }

                slowTicks = EffectTicks;
                break;
            case ArkanoidPerkKind.StickyPaddle:
                stickyTicks = EffectTicks;
                break;
            default:
                if (Lives < MaxLives)
                {
                    AddLife();
                }
                else
                {
                    AddScore(50);
                }
                break;
        }

        PlayTone(1100, 40);
    }

    private void CountDownEffects()
    {
        if (wideTicks > 0 && --wideTicks == 0)
        {
            SetPaddleWidth(paddleWidth);
        }

        if (slowTicks > 0 && --slowTicks == 0)
        {
            ball.VelocityX /= SlowBallFactor;
            ball.VelocityY /= SlowBallFactor;
        }

        if (stickyTicks > 0)
        {
            stickyTicks--;
        }
    }

    // The paddle grows and shrinks around its middle.
    private void SetPaddleWidth(float width)
    {
        var center = paddle.CenterX;
        paddle.Width = width;
        paddle.X = center - width / 2f;
        paddle.ClampTo(FieldLeft, FieldTop, FieldRight, FieldBottom);
    }

    private void DropPerk(ArkanoidBrick brick)
    {
        if (perks.Count >= MaxPerks || Randomizer.Next(PerkDropChance) != 0)
        {
            return;
        }

        var kind = PerkKinds[Randomizer.Next(PerkKinds.Length)];
        var width = Unit * 5f;
        var height = Unit * 3f;
        perks.Add(new ArkanoidPerk(brick.CenterX - width / 2f, brick.Top, width, height, kind)
        {
            VelocityY = Unit * 0.7f
        });
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
        if (IsPaddleSticky)
        {
            StickBall(ball.CenterX - paddle.CenterX);
            return;
        }

        ball.VelocityX = offset * CurrentBallSpeed * 0.85f;
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
                DropPerk(brick);
            }
            PlayTone(880, 20);

            // One brick per game moment, so the ball is not turned back twice.
            break;
        }

        bricks.RemoveAll(brick => !brick.IsAlive);
    }
}
