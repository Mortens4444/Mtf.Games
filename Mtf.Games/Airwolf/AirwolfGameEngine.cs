using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;

namespace Mtf.Games.Airwolf;

/// <summary>
/// Side scrolling helicopter game: fly over the hills, avoid the ground, the drones and the flak.
/// The four direction buttons move the helicopter, the center button fires.
/// A level is completed by staying alive for <see cref="LevelLength"/> game moments.
/// </summary>
public class AirwolfGameEngine(IGameContext? context, Random? randomSource = null) : ArcadeGameEngineBase(context, randomSource)
{
    public const int LevelLength = 900;

    private const int InvulnerabilityTicks = 40;
    private const int MaxEnemies = 4;
    private const int MaxShots = 3;

    // Height of the ground in every terrain column, from left to right.
    private readonly List<float> terrain = [];
    private readonly List<AirwolfEnemy> enemies = [];
    private readonly List<BoxSprite> shots = [];
    private readonly List<BoxSprite> flak = [];
    private readonly Cooldown fireCooldown = new(6);
    private readonly Cooldown spawnCooldown = new(40);

    // Assigned by SetupLevel, which the base constructor invokes.
    private Helicopter helicopter = null!;
    private float columnWidth;
    private float scrollOffset;
    private float scrollSpeed;
    private int distance;
    private int invulnerableTicks;

    /// <summary>
    /// Completed part of the current level in percent.
    /// </summary>
    public int Progress => distance * 100 / LevelLength;

    protected override int LevelCount => 2;

    protected override bool CenterButtonPauses => false;

    protected override string HudText => $"S:{Score} L:{Lives} {Progress}%";

    private float MinTerrainHeight => Unit * 2f;

    private float MaxTerrainHeight => FieldHeight * (0.3f + 0.1f * Level);

    protected override void SetupLevel()
    {
        terrain.Clear();
        enemies.Clear();
        shots.Clear();
        flak.Clear();

        columnWidth = MathF.Max(8f, FieldWidth / 10f);
        scrollSpeed = Unit * (0.75f + 0.25f * Level);
        scrollOffset = 0f;
        distance = 0;
        invulnerableTicks = 0;

        var columns = (int)MathF.Ceiling(FieldWidth / columnWidth) + 2;
        for (var i = 0; i < columns; i++)
        {
            terrain.Add(MinTerrainHeight);
        }

        helicopter = new Helicopter(Unit * 8f, Unit * 4f);
        PlaceHelicopter();

        fireCooldown.Reset();
        spawnCooldown.Ticks = 40;
        spawnCooldown.Restart();
    }

    protected override void OnLifeLost()
    {
        flak.Clear();
        PlaceHelicopter();
        invulnerableTicks = InvulnerabilityTicks;
    }

    protected override void UpdateGame()
    {
        Scroll();
        MoveHelicopter();
        Fire();
        SpawnEnemy();
        MoveEnemies();
        MoveProjectiles();
        ResolveHits();
        if (!inGame)
        {
            return;
        }

        distance++;
        if (distance >= LevelLength)
        {
            CompleteLevel();
        }
    }

    protected override void DrawGame()
    {
        for (var i = 0; i < terrain.Count; i++)
        {
            gameContext.DrawBox(ColumnLeft(i), FieldBottom - terrain[i], columnWidth + 1f, terrain[i]);
        }

        foreach (var enemy in enemies)
        {
            enemy.Draw(gameContext);
        }

        foreach (var shot in shots)
        {
            shot.Draw(gameContext);
        }

        foreach (var shell in flak)
        {
            shell.Draw(gameContext);
        }

        // The helicopter blinks while it cannot be hit.
        if (invulnerableTicks % 4 < 2)
        {
            helicopter.Draw(gameContext);
        }
    }

    private void PlaceHelicopter()
    {
        helicopter.Stop();
        helicopter.MoveTo(FieldLeft + Unit * 4f, FieldTop + FieldHeight * 0.25f);
    }

    private float ColumnLeft(int index)
    {
        return FieldLeft + index * columnWidth + scrollOffset;
    }

    private float GroundTopAt(float x)
    {
        var index = (int)MathF.Floor((x - FieldLeft - scrollOffset) / columnWidth);
        index = Math.Max(0, Math.Min(terrain.Count - 1, index));
        return FieldBottom - terrain[index];
    }

    private void Scroll()
    {
        scrollOffset -= scrollSpeed;
        while (scrollOffset <= -columnWidth)
        {
            scrollOffset += columnWidth;
            var next = terrain[^1] + Randomizer.Next(-1, 2) * Unit * 3f;
            terrain.RemoveAt(0);
            terrain.Add(MathF.Max(MinTerrainHeight, MathF.Min(MaxTerrainHeight, next)));
        }
    }

    private void MoveHelicopter()
    {
        var speed = Unit * 1.5f;
        helicopter.VelocityX = Buttons.HorizontalAxis * speed;
        helicopter.VelocityY = Buttons.VerticalAxis * speed;
        helicopter.Update();
        helicopter.ClampTo(FieldLeft, FieldTop, FieldRight, FieldBottom);

        if (invulnerableTicks > 0)
        {
            invulnerableTicks--;
        }
    }

    private void Fire()
    {
        fireCooldown.Tick();
        if (!Buttons.IsHeld(ArcadeButton.Center) || shots.Count >= MaxShots || !fireCooldown.TryUse())
        {
            return;
        }

        var height = MathF.Max(1f, Unit * 0.5f);
        shots.Add(new BoxSprite(helicopter.Right, helicopter.CenterY, Unit * 3f, height)
        {
            VelocityX = Unit * 4f
        });
        PlayTone(880, 20);
    }

    private void SpawnEnemy()
    {
        spawnCooldown.Tick();
        if (enemies.Count >= MaxEnemies || !spawnCooldown.TryUse())
        {
            return;
        }

        spawnCooldown.Ticks = Math.Max(15, Randomizer.Next(30, 70) - Level * 10);
        if (Randomizer.Next(2) == 0)
        {
            enemies.Add(CreateDrone());
        }
        else
        {
            enemies.Add(CreateTurret());
        }
    }

    private Drone CreateDrone()
    {
        var swing = Unit * 3f;
        var height = Unit * 2f;
        var highest = FieldTop + swing;
        var lowest = MathF.Max(highest, FieldBottom - MaxTerrainHeight - swing - height);
        var centerLine = highest + Randomizer.NextSingle() * (lowest - highest);

        return new Drone(FieldRight, centerLine, Unit * 4f, height, swing)
        {
            VelocityX = -(scrollSpeed + Unit * 0.75f)
        };
    }

    private Turret CreateTurret()
    {
        // The last terrain column is still off-screen, the turret scrolls in with it.
        var size = Unit * 3f;
        var column = terrain.Count - 1;
        var x = ColumnLeft(column) + (columnWidth - size) / 2f;
        var y = FieldBottom - terrain[column] - size;

        return new Turret(x, y, size)
        {
            VelocityX = -scrollSpeed
        };
    }

    private void MoveEnemies()
    {
        foreach (var enemy in enemies)
        {
            enemy.Update();
            if (enemy.Right < FieldLeft)
            {
                enemy.Kill();
            }
            else if (enemy is Turret turret)
            {
                FireFlak(turret);
            }
        }
    }

    private void FireFlak(Turret turret)
    {
        turret.FireCooldown.Tick();
        if (turret.Right > FieldRight || !turret.FireCooldown.TryUse())
        {
            return;
        }

        var dx = helicopter.CenterX - turret.CenterX;
        var dy = helicopter.CenterY - turret.Top;
        var length = MathF.Sqrt(dx * dx + dy * dy);
        if (length < 1f)
        {
            return;
        }

        var speed = Unit * 1.25f;
        var size = MathF.Max(2f, Unit);
        flak.Add(new BoxSprite(turret.CenterX - size / 2f, turret.Top - size, size, size)
        {
            VelocityX = dx / length * speed,
            VelocityY = dy / length * speed
        });
    }

    private void MoveProjectiles()
    {
        foreach (var shot in shots)
        {
            shot.Update();
            if (shot.Left > FieldRight || shot.Bottom > GroundTopAt(shot.Right))
            {
                shot.Kill();
            }
        }

        foreach (var shell in flak)
        {
            shell.Update();
            if (shell.IsOutside(FieldLeft, FieldTop, FieldRight, FieldBottom))
            {
                shell.Kill();
            }
        }
    }

    private void ResolveHits()
    {
        foreach (var shot in shots)
        {
            var target = enemies.FirstOrDefault(enemy => shot.Intersects(enemy));
            if (target == null)
            {
                continue;
            }

            shot.Kill();
            target.Kill();
            AddScore(target.Points);
            PlayTone(330, 40);
        }

        var crashed = invulnerableTicks == 0 && (HitsGround() ||
            enemies.Any(enemy => helicopter.Intersects(enemy)) ||
            flak.Any(shell => helicopter.Intersects(shell)));

        enemies.RemoveAll(enemy => !enemy.IsAlive);
        shots.RemoveAll(shot => !shot.IsAlive);
        flak.RemoveAll(shell => !shell.IsAlive);

        if (crashed)
        {
            LoseLife();
        }
    }

    private bool HitsGround()
    {
        var ground = MathF.Min(GroundTopAt(helicopter.Left), MathF.Min(GroundTopAt(helicopter.CenterX), GroundTopAt(helicopter.Right)));
        return helicopter.Bottom > ground;
    }
}
