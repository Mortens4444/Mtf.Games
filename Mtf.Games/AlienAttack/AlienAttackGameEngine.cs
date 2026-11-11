using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;

namespace Mtf.Games.AlienAttack;

/// <summary>
/// Fixed shooter: a formation of aliens marches down the screen and drops bombs.
/// The ship moves with the left and right buttons and fires with the center or the up button.
/// The game is lost when the lives run out or the aliens reach the ship.
/// </summary>
public class AlienAttackGameEngine(IGameContext? context, Random? randomSource = null) : ArcadeGameEngineBase(context, randomSource)
{
    public const int Columns = 5;
    public const int Rows = 3;

    private const int MaxShots = 2;
    private const int MaxBombs = 3;

    private readonly List<Alien> aliens = [];
    private readonly List<BoxSprite> shots = [];
    private readonly List<BoxSprite> bombs = [];
    private readonly Cooldown fireCooldown = new(8);
    private readonly Cooldown bombCooldown = new(30);
    private readonly Cooldown stepCooldown = new(6);

    // Assigned by SetupLevel, which the base constructor invokes.
    private BoxSprite ship = null!;
    private int direction;

    public int RemainingAliens => aliens.Count;

    protected override int LevelCount => 3;

    protected override bool CenterButtonPauses => false;

    protected override void SetupLevel()
    {
        aliens.Clear();
        shots.Clear();
        bombs.Clear();

        var alienWidth = Unit * 4f;
        var alienHeight = Unit * 3f;
        var gapX = Unit * 3f;
        var gapY = Unit * 2f;
        var formationWidth = Columns * alienWidth + (Columns - 1) * gapX;
        var left = FieldLeft + (FieldWidth - formationWidth) / 2f;
        var top = FieldTop + Unit * 2f;
        for (var row = 0; row < Rows; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                var x = left + column * (alienWidth + gapX);
                var y = top + row * (alienHeight + gapY);
                aliens.Add(new Alien(x, y, alienWidth, alienHeight, row, column, (Rows - row) * 10));
            }
        }

        var shipWidth = Unit * 5f;
        var shipHeight = Unit * 2f;
        ship = new BoxSprite(FieldLeft + (FieldWidth - shipWidth) / 2f, FieldBottom - shipHeight - Unit, shipWidth, shipHeight);

        direction = 1;
        fireCooldown.Reset();
        stepCooldown.Ticks = StepInterval;
        stepCooldown.Restart();
        bombCooldown.Ticks = Math.Max(8, 36 - Level * 6);
        bombCooldown.Restart();
    }

    // The fewer aliens are alive and the higher the level is, the faster the formation moves.
    private int StepInterval => Math.Max(1, aliens.Count / 3 + LevelCount - Level);

    protected override void OnLifeLost()
    {
        bombs.Clear();
        ship.X = FieldLeft + (FieldWidth - ship.Width) / 2f;
    }

    protected override void UpdateGame()
    {
        MoveShip();
        Fire();
        MoveAliens();
        if (!inGame)
        {
            return;
        }

        DropBomb();
        MoveProjectiles();
        ResolveHits();

        if (inGame && aliens.Count == 0)
        {
            CompleteLevel();
        }
    }

    protected override void DrawGame()
    {
        foreach (var alien in aliens)
        {
            alien.Draw(gameContext);
        }

        foreach (var shot in shots)
        {
            shot.Draw(gameContext);
        }

        foreach (var bomb in bombs)
        {
            bomb.Draw(gameContext);
        }

        ship.Draw(gameContext);
        gameContext.DrawBox(ship.CenterX - Unit / 2f, ship.Top - Unit, Unit, Unit);
    }

    private void MoveShip()
    {
        ship.VelocityX = Buttons.HorizontalAxis * Unit * 2f;
        ship.Update();
        ship.ClampTo(FieldLeft, FieldTop, FieldRight, FieldBottom);
    }

    private void Fire()
    {
        fireCooldown.Tick();

        var firing = Buttons.IsHeld(ArcadeButton.Center) || Buttons.IsHeld(ArcadeButton.Up);
        if (!firing || shots.Count >= MaxShots || !fireCooldown.TryUse())
        {
            return;
        }

        var width = MathF.Max(1f, Unit * 0.5f);
        var height = Unit * 2f;
        shots.Add(new BoxSprite(ship.CenterX - width / 2f, ship.Top - height, width, height)
        {
            VelocityY = -Unit * 3f
        });
        PlayTone(880, 20);
    }

    private void MoveAliens()
    {
        stepCooldown.Tick();
        if (aliens.Count == 0 || !stepCooldown.IsReady)
        {
            return;
        }

        stepCooldown.Ticks = StepInterval;
        stepCooldown.Restart();

        var step = Unit * 2f * direction;
        var left = aliens.Min(alien => alien.Left);
        var right = aliens.Max(alien => alien.Right);
        if (left + step < FieldLeft || right + step > FieldRight)
        {
            direction = -direction;
            foreach (var alien in aliens)
            {
                alien.Y += Unit * 2f;
            }
        }
        else
        {
            foreach (var alien in aliens)
            {
                alien.X += step;
            }
        }

        if (aliens.Max(alien => alien.Bottom) >= ship.Top)
        {
            Lost();
        }
    }

    private void DropBomb()
    {
        bombCooldown.Tick();
        if (aliens.Count == 0 || bombs.Count >= MaxBombs || !bombCooldown.TryUse())
        {
            return;
        }

        // The lowest alien of a random column drops the bomb.
        var column = aliens[Randomizer.Next(aliens.Count)].Column;
        Alien? bomber = null;
        foreach (var alien in aliens)
        {
            if (alien.Column == column && (bomber == null || alien.Bottom > bomber.Bottom))
            {
                bomber = alien;
            }
        }

        if (bomber == null)
        {
            return;
        }

        var width = MathF.Max(2f, Unit);
        bombs.Add(new BoxSprite(bomber.CenterX - width / 2f, bomber.Bottom, width, Unit * 1.5f)
        {
            VelocityY = Unit * (0.75f + 0.25f * Level)
        });
    }

    private void MoveProjectiles()
    {
        foreach (var shot in shots)
        {
            shot.Update();
            if (shot.Bottom < FieldTop)
            {
                shot.Kill();
            }
        }

        foreach (var bomb in bombs)
        {
            bomb.Update();
            if (bomb.Top > FieldBottom)
            {
                bomb.Kill();
            }
        }
    }

    private void ResolveHits()
    {
        foreach (var shot in shots)
        {
            var target = aliens.FirstOrDefault(alien => shot.Intersects(alien));
            if (target == null)
            {
                continue;
            }

            shot.Kill();
            target.Kill();
            AddScore(target.Points * Level);
            PlayTone(330, 40);
        }

        var shipHit = false;
        foreach (var bomb in bombs)
        {
            if (bomb.Intersects(ship))
            {
                bomb.Kill();
                shipHit = true;
            }
        }

        aliens.RemoveAll(alien => !alien.IsAlive);
        shots.RemoveAll(shot => !shot.IsAlive);
        bombs.RemoveAll(bomb => !bomb.IsAlive);

        if (shipHit)
        {
            LoseLife();
        }
    }
}
