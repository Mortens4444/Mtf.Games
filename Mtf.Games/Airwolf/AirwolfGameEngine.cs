using Mtf.Games.General.Arcade;
using Mtf.Games.Interfaces;
using System.Drawing;

namespace Mtf.Games.Airwolf;

/// <summary>
/// Side scrolling helicopter game: fly over the rolling hills, avoid the ground, the drones and the flak.
/// The four direction buttons move the helicopter, the center button fires.
/// The helicopter has inertia: it accelerates, drifts after a button is released and pitches into the flight.
/// A level is completed by staying alive for <see cref="LevelLength"/> game moments.
/// </summary>
public class AirwolfGameEngine(IGameContext? context, Random? randomSource = null) : ArcadeGameEngineBase(context, randomSource)
{
    public const int LevelLength = 900;

    private const int InvulnerabilityTicks = 40;
    private const int MaxEnemies = 4;
    private const int MaxShots = 3;
    private const int MaxBombs = 2;

    // The sky is blended from this many bands. About every sixth of this many columns carries a tree or a bush.
    private const int SkyBands = 32;
    private const int VegetationSpacing = 30;

    // The blast of the gunship lasts this many game moments before the level is over.
    private const int VictoryDelay = 25;

    // Terrain columns kept beyond the right edge, so new ground and turret pads are built off-screen.
    private const int BufferColumns = 8;

    // The helicopter keeps this part of its speed in every game moment.
    private const float Drag = 0.82f;

    // Relative position, height and length of the clouds.
    private static readonly (float X, float Y, float Length)[] Clouds =
    [
        (0.05f, 0.10f, 0.16f),
        (0.35f, 0.40f, 0.10f),
        (0.62f, 0.05f, 0.20f),
        (0.85f, 0.45f, 0.12f)
    ];

    // Height of the ground in every terrain column, from left to right.
    private readonly List<float> terrain = [];
    private readonly List<AirwolfEnemy> enemies = [];
    private readonly List<BoxSprite> shots = [];
    private readonly List<BoxSprite> flak = [];
    private readonly List<Explosion> explosions = [];
    private readonly Cooldown fireCooldown = new(6);
    private readonly Cooldown bombCooldown = new(20);
    private readonly List<Bomb> bombs = [];
    private readonly Cooldown spawnCooldown = new(40);

    // Assigned by SetupLevel, which the base constructor invokes.
    private Helicopter helicopter = null!;
    private float columnWidth;
    private float scrollOffset;
    private float scrollSpeed;
    private float worldPosition;
    private float slope;
    private int layoutWidth;
    private int layoutHeight;
    private int distance;
    private int firstColumn;
    private int victoryTicks;
    private bool bossFight;
    private int invulnerableTicks;

    /// <summary>
    /// Completed part of the current level in percent.
    /// </summary>
    public int Progress => distance * 100 / LevelLength;

    protected override int LevelCount => 2;

    protected override bool CenterButtonPauses => false;

    protected override string HudText => bossFight ? $"S:{Score} L:{Lives} BOSS" : $"S:{Score} L:{Lives} {Progress}%";

    private float MinTerrainHeight => Unit * 2f;

    private float MaxTerrainHeight => FieldHeight * (0.3f + 0.1f * Level);

    protected override void SetupLevel()
    {
        distance = 0;
        invulnerableTicks = 0;
        bossFight = false;
        victoryTicks = 0;
        BuildWorld();

        // A button that is still down from the menu must not drop a bomb at once.
        bombCooldown.Restart();
    }

    protected override void OnLifeLost()
    {
        flak.Clear();
        PlaceHelicopter();
        invulnerableTicks = InvulnerabilityTicks;
    }

    protected override void UpdateGame()
    {
        // The window can be resized or maximized any time, and the world is built for one size only.
        if (gameContext.ScreenWidth != layoutWidth || gameContext.ScreenHeight != layoutHeight)
        {
            BuildWorld();
            invulnerableTicks = InvulnerabilityTicks;
        }

        Scroll();
        MoveHelicopter();
        Fire();
        DropBomb();
        MoveEnemies();
        SpawnEnemy();
        MoveProjectiles();
        ResolveHits();
        if (!inGame)
        {
            return;
        }

        if (bossFight)
        {
            // The level is over when the gunship has blown up.
            if (victoryTicks > 0 && --victoryTicks == 0)
            {
                CompleteLevel();
            }

            return;
        }

        distance++;
        if (distance >= LevelLength)
        {
            StartBossFight();
        }
    }

    protected override void DrawHud()
    {
        var barHeight = MathF.Max(2f, Unit * 0.6f);
        var barTop = HudHeight - barHeight;
        gameContext.DrawBox(0f, 0f, gameContext.ScreenWidth, HudHeight, true, AirwolfColors.HudBar);
        gameContext.DrawBox(0f, barTop, gameContext.ScreenWidth, barHeight, true, AirwolfColors.HudTrack);
        gameContext.DrawBox(0f, barTop, gameContext.ScreenWidth * Progress / 100f, barHeight, true, AirwolfColors.HudProgress);
        gameContext.DrawLabel(Unit, 0f, HudText, color: AirwolfColors.HudText);
    }

    protected override void DrawGame()
    {
        DrawScenery();
        DrawGround();
        DrawShadow();

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

        foreach (var bomb in bombs)
        {
            bomb.Draw(gameContext);
        }

        foreach (var explosion in explosions)
        {
            explosion.Draw(gameContext);
        }

        // The helicopter blinks while it cannot be hit.
        if (invulnerableTicks % 4 < 2)
        {
            helicopter.Draw(gameContext);
        }
    }

    /// <summary>
    /// Builds the terrain and the helicopter for the current screen size, without touching the progress of the level.
    /// </summary>
    private void BuildWorld()
    {
        terrain.Clear();
        enemies.Clear();
        shots.Clear();
        flak.Clear();
        explosions.Clear();
        bombs.Clear();

        layoutWidth = gameContext.ScreenWidth;
        layoutHeight = gameContext.ScreenHeight;
        columnWidth = MathF.Max(2f, Unit * 0.8f);
        scrollSpeed = Unit * (0.75f + 0.25f * Level);
        scrollOffset = 0f;
        worldPosition = 0f;
        firstColumn = 0;
        slope = 0f;

        var columns = (int)MathF.Ceiling(FieldWidth / columnWidth) + BufferColumns;
        var height = MinTerrainHeight;
        for (var i = 0; i < columns; i++)
        {
            terrain.Add(height);
            height = NextTerrainHeight(height);
        }

        helicopter = new Helicopter(Unit * 8f, Unit * 4f);
        PlaceHelicopter();

        fireCooldown.Reset();
        spawnCooldown.Ticks = 40;
        spawnCooldown.Restart();

        // The world is built again when the window is resized in the middle of the boss fight.
        if (bossFight && victoryTicks == 0)
        {
            StartBossFight();
        }
    }

    private void StartBossFight()
    {
        bossFight = true;
        var width = Unit * 14f;
        var height = Unit * 7f;
        var swing = Unit * 2.5f;
        var highest = FieldTop + swing + Unit * 2f;
        var lowest = MathF.Max(highest, FieldBottom - MaxTerrainHeight - swing - height);
        var gunship = new Gunship(FieldRight, (highest + lowest) / 2f, width, height, swing, FieldRight - width - Unit * 3f, 6 + 4 * Level)
        {
            VelocityX = -Unit * 0.6f
        };
        gunship.FireCooldown.Ticks = Math.Max(20, 55 - Level * 12);
        enemies.Add(gunship);
        PlayTone(110, 200);
    }

    private void PlaceHelicopter()
    {
        helicopter.Stop();
        helicopter.Pitch = 0f;
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

    /// <summary>
    /// The hills roll: the slope of the ground changes a little in every column, and turns around at the limits.
    /// </summary>
    private float NextTerrainHeight(float previous)
    {
        slope += (Randomizer.NextSingle() - 0.5f) * columnWidth * 0.24f;
        slope = Math.Max(-columnWidth * 0.48f, Math.Min(columnWidth * 0.48f, slope));

        var next = previous + slope;
        if (next < MinTerrainHeight)
        {
            slope = MathF.Abs(slope) * 0.5f;
            return MinTerrainHeight;
        }

        var highest = MathF.Max(MinTerrainHeight, MaxTerrainHeight);
        if (next > highest)
        {
            slope = -MathF.Abs(slope) * 0.5f;
            return highest;
        }

        return next;
    }

    private void Scroll()
    {
        scrollOffset -= scrollSpeed;
        worldPosition += scrollSpeed;
        while (scrollOffset <= -columnWidth)
        {
            scrollOffset += columnWidth;
            firstColumn++;
            var next = NextTerrainHeight(terrain[^1]);
            terrain.RemoveAt(0);
            terrain.Add(next);
        }
    }

    private void MoveHelicopter()
    {
        var maxSpeed = Unit * 1.5f;
        var acceleration = maxSpeed * (1f - Drag) / Drag;
        helicopter.VelocityX = (helicopter.VelocityX + Buttons.HorizontalAxis * acceleration) * Drag;
        helicopter.VelocityY = (helicopter.VelocityY + Buttons.VerticalAxis * acceleration) * Drag;
        helicopter.Pitch = helicopter.VelocityX / maxSpeed;
        helicopter.Update();

        var x = helicopter.X;
        var y = helicopter.Y;
        helicopter.ClampTo(FieldLeft, FieldTop, FieldRight, FieldBottom);
        if (helicopter.X != x)
        {
            helicopter.VelocityX = 0f;
        }

        if (helicopter.Y != y)
        {
            helicopter.VelocityY = 0f;
        }

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

        // The tracer is as long as it travels in one moment, so it cannot jump over a target.
        var height = MathF.Max(1f, Unit * 0.5f);
        var noseY = helicopter.Y + helicopter.Height * 0.55f;
        shots.Add(new BoxSprite(helicopter.Right, noseY, Unit * 4.5f, height)
        {
            VelocityX = Unit * 3.5f + MathF.Max(0f, helicopter.VelocityX) * 0.5f,
            Color = AirwolfColors.Tracer
        });
        PlayTone(880, 20);
    }

    private void DropBomb()
    {
        bombCooldown.Tick();
        if (!Buttons.WasPressed(ArcadeButton.Secondary) || bombs.Count >= MaxBombs || !bombCooldown.TryUse())
        {
            return;
        }

        var width = MathF.Max(2f, Unit * 1.1f);
        bombs.Add(new Bomb(helicopter.CenterX - width / 2f, helicopter.Bottom, width, width * 2f, Unit * 0.1f, Unit * 1.8f)
        {
            VelocityX = helicopter.VelocityX,
            VelocityY = MathF.Max(0f, helicopter.VelocityY)
        });
        PlayTone(300, 40);
    }

    private void SpawnEnemy()
    {
        spawnCooldown.Tick();
        if (bossFight || enemies.Count >= MaxEnemies || !spawnCooldown.TryUse())
        {
            return;
        }

        spawnCooldown.Ticks = Math.Max(15, Randomizer.Next(30, 70) - Level * 10);
        var roll = Randomizer.Next(5);
        if (roll < 2)
        {
            enemies.Add(CreateDrone());
        }
        else if (roll == 2)
        {
            enemies.Add(CreateTurret());
        }
        else
        {
            enemies.Add(CreateSoldier());
        }
    }

    private Drone CreateDrone()
    {
        var swing = Unit * 3f;
        var height = Unit * 2f;
        var highest = FieldTop + swing;
        var lowest = MathF.Max(highest, FieldBottom - MaxTerrainHeight - swing - height);
        var centerLine = highest + Randomizer.NextSingle() * (lowest - highest);

        return new Drone(FieldRight, centerLine, Unit * 4f, height, swing, Randomizer.NextSingle() * MathF.PI * 2f)
        {
            VelocityX = -(scrollSpeed + Unit * 0.75f)
        };
    }

    private Turret CreateTurret()
    {
        // The last terrain columns are still off-screen: they are leveled for the turret and scroll in with it.
        var size = Unit * 3f;
        var span = (int)MathF.Ceiling(size / columnWidth) + 1;
        var first = terrain.Count - span;
        var level = terrain[first - 1];
        for (var i = first; i < terrain.Count; i++)
        {
            terrain[i] = level;
        }

        slope = 0f;

        var x = ColumnLeft(first) + (span * columnWidth - size) / 2f;
        var y = FieldBottom - level - size;
        return new Turret(x, y, size)
        {
            VelocityX = -scrollSpeed
        };
    }

    private Soldier CreateSoldier()
    {
        var width = Unit * 1.4f;
        var height = Unit * 2.6f;
        return new Soldier(FieldRight, GroundTopAt(FieldRight) - height, width, height)
        {
            VelocityX = -(scrollSpeed + Unit * 0.2f)
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
            else if (enemy is Soldier soldier)
            {
                // The soldier walks over the hills.
                soldier.Y = GroundTopAt(soldier.CenterX) - soldier.Height;
                FireRifle(soldier);
            }
            else if (enemy is Gunship gunship)
            {
                FireSpread(gunship);
            }
        }
    }

    private void FireRifle(Soldier soldier)
    {
        soldier.FireCooldown.Tick();

        // He shoots only at a helicopter above him, and aims at where it is now.
        var directionX = helicopter.CenterX - soldier.CenterX;
        var directionY = soldier.Y + soldier.Height * 0.35f - helicopter.CenterY;
        var length = MathF.Sqrt(directionX * directionX + directionY * directionY);
        if (length < 1f || directionY <= 0f)
        {
            return;
        }

        soldier.AimX = directionX / length;
        soldier.AimY = directionY / length;
        if (soldier.Right > FieldRight || !soldier.FireCooldown.TryUse())
        {
            return;
        }

        var (shotX, shotY) = Rotate(soldier.AimX, soldier.AimY, (Randomizer.NextSingle() * 2f - 1f) * 0.15f);
        var speed = Unit * 1.0f;
        var size = MathF.Max(2f, Unit * 0.55f);
        var muzzle = soldier.Muzzle;
        flak.Add(new BoxSprite(muzzle.X - size / 2f, muzzle.Y - size / 2f, size, size)
        {
            VelocityX = shotX * speed,
            VelocityY = -shotY * speed,
            Color = AirwolfColors.Bullet
        });
        PlayTone(500, 15);
    }

    // Three shells fly apart from the belly gun of the gunship.
    private void FireSpread(Gunship gunship)
    {
        gunship.FireCooldown.Tick();
        if (!gunship.IsHovering || !gunship.FireCooldown.TryUse())
        {
            return;
        }

        var muzzle = gunship.Muzzle;
        var directionX = helicopter.CenterX - muzzle.X;
        var directionY = helicopter.CenterY - muzzle.Y;
        var length = MathF.Max(1f, MathF.Sqrt(directionX * directionX + directionY * directionY));
        var speed = Unit * 1.1f;
        var size = MathF.Max(2f, Unit * 1.1f);
        for (var i = -1; i <= 1; i++)
        {
            var (shellX, shellY) = Rotate(directionX / length, directionY / length, i * 0.22f);
            flak.Add(new BoxSprite(muzzle.X - size / 2f, muzzle.Y - size / 2f, size, size)
            {
                VelocityX = shellX * speed,
                VelocityY = shellY * speed,
                Color = AirwolfColors.Flak
            });
        }

        PlayTone(196, 40);
    }

    private static (float X, float Y) Rotate(float x, float y, float angle)
    {
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);
        return (x * cos - y * sin, x * sin + y * cos);
    }

    private void FireFlak(Turret turret)
    {
        turret.FireCooldown.Tick();

        // From the second level the gunners aim ahead of the helicopter.
        var speed = Unit * 1.25f;
        var lead = (Level - 1) * 0.35f;
        var distanceToTarget = MathF.Sqrt(
            (helicopter.CenterX - turret.PivotX) * (helicopter.CenterX - turret.PivotX) +
            (helicopter.CenterY - turret.PivotY) * (helicopter.CenterY - turret.PivotY));
        var flightTime = distanceToTarget / speed * lead;
        var canAim = turret.AimAt(
            helicopter.CenterX + helicopter.VelocityX * flightTime,
            helicopter.CenterY + helicopter.VelocityY * flightTime);

        if (!canAim || turret.Right > FieldRight || !turret.FireCooldown.TryUse())
        {
            return;
        }

        // Nobody shoots perfectly: the shell leaves the barrel a little off the aimed direction.
        var (directionX, directionY) = Rotate(turret.AimX, turret.AimY, (Randomizer.NextSingle() * 2f - 1f) * 0.12f);

        var size = MathF.Max(2f, Unit);
        var muzzle = turret.Muzzle;
        flak.Add(new BoxSprite(muzzle.X - size / 2f, muzzle.Y - size / 2f, size, size)
        {
            VelocityX = directionX * speed,
            VelocityY = -directionY * speed,
            Color = AirwolfColors.Flak
        });
        PlayTone(262, 30);
    }

    private void MoveProjectiles()
    {
        foreach (var shot in shots)
        {
            shot.Update();
            if (shot.Left > FieldRight)
            {
                shot.Kill();
            }
            else if (shot.Bottom > GroundTopAt(shot.Right))
            {
                shot.Kill();
                AddExplosion(shot.Right, GroundTopAt(shot.Right), Unit, -scrollSpeed);
            }
        }

        foreach (var shell in flak)
        {
            shell.Update();
            if (shell.IsOutside(FieldLeft, FieldTop, FieldRight, FieldBottom))
            {
                shell.Kill();
            }
            else if (shell.Bottom > GroundTopAt(shell.CenterX))
            {
                shell.Kill();
                AddExplosion(shell.CenterX, GroundTopAt(shell.CenterX), Unit, -scrollSpeed);
            }
        }

        foreach (var bomb in bombs)
        {
            bomb.Update();
            if (bomb.Left > FieldRight || bomb.Right < FieldLeft)
            {
                bomb.Kill();
            }
            else if (bomb.Bottom > GroundTopAt(bomb.CenterX))
            {
                Detonate(bomb);
            }
        }

        foreach (var explosion in explosions)
        {
            explosion.Update();
            if (explosion.Right < FieldLeft)
            {
                explosion.Kill();
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
            if (target is Gunship gunship && !gunship.Hit())
            {
                AddExplosion(shot.Right, shot.CenterY, Unit, gunship.VelocityX);
                AddScore(10);
                PlayTone(660, 15);
                continue;
            }

            Destroy(target);
            AddScore(target.Points);
        }

        foreach (var bomb in bombs.Where(bomb => bomb.IsAlive).ToList())
        {
            if (enemies.Any(enemy => bomb.Intersects(enemy)))
            {
                Detonate(bomb);
            }
        }

        var crashed = false;
        if (invulnerableTicks == 0)
        {
            crashed = HitsGround() || flak.Any(shell => helicopter.Intersects(shell));
            foreach (var enemy in enemies.Where(enemy => helicopter.Intersects(enemy)).ToList())
            {
                // Ramming an enemy destroys it too, except the gunship.
                if (enemy is not Gunship)
                {
                    Destroy(enemy);
                }

                crashed = true;
            }
        }

        enemies.RemoveAll(enemy => !enemy.IsAlive);
        shots.RemoveAll(shot => !shot.IsAlive);
        flak.RemoveAll(shell => !shell.IsAlive);
        bombs.RemoveAll(bomb => !bomb.IsAlive);
        explosions.RemoveAll(explosion => !explosion.IsAlive);

        if (crashed)
        {
            AddExplosion(helicopter.CenterX, helicopter.CenterY, Unit * 4f, -scrollSpeed * 0.5f);
            LoseLife();
        }
    }

    // The blast destroys every enemy it reaches. The gunship only takes a few hits from it.
    private void Detonate(Bomb bomb)
    {
        bomb.Kill();
        var x = bomb.CenterX;
        var y = bomb.Bottom;
        var radius = Unit * 4.5f;
        AddExplosion(x, y, radius * 0.8f, -scrollSpeed);
        PlayTone(150, 60);

        foreach (var enemy in enemies.Where(enemy => enemy.IsAlive).ToList())
        {
            var dx = MathF.Max(MathF.Max(enemy.Left - x, x - enemy.Right), 0f);
            var dy = MathF.Max(MathF.Max(enemy.Top - y, y - enemy.Bottom), 0f);
            if (dx * dx + dy * dy > radius * radius)
            {
                continue;
            }

            if (enemy is Gunship gunship)
            {
                HitGunship(gunship, 3);
            }
            else
            {
                Destroy(enemy);
                AddScore(enemy.Points);
            }
        }
    }

    private void HitGunship(Gunship gunship, int hits)
    {
        for (var i = 0; i < hits; i++)
        {
            if (gunship.Hit())
            {
                Destroy(gunship);
                AddScore(gunship.Points);
                return;
            }
        }

        AddScore(30);
    }

    private void Destroy(AirwolfEnemy enemy)
    {
        enemy.Kill();
        AddExplosion(enemy.CenterX, enemy.CenterY, Unit * 2.5f, enemy.VelocityX);
        PlayTone(330, 40);

        if (enemy is Gunship)
        {
            // A chain of blasts along the whole gunship, then the level is over.
            for (var i = 0; i < 4; i++)
            {
                AddExplosion(enemy.X + enemy.Width * (0.15f + 0.23f * i), enemy.Y + enemy.Height * (i % 2 == 0 ? 0.35f : 0.7f), Unit * 3f, 0f);
            }

            flak.Clear();
            victoryTicks = VictoryDelay;
        }
    }

    private void AddExplosion(float centerX, float centerY, float radius, float velocityX)
    {
        explosions.Add(new Explosion(centerX, centerY, radius)
        {
            VelocityX = velocityX
        });
    }

    private bool HitsGround()
    {
        var ground = MathF.Min(GroundTopAt(helicopter.Left), MathF.Min(GroundTopAt(helicopter.CenterX), GroundTopAt(helicopter.Right)));
        return helicopter.Bottom > ground;
    }

    private void DrawScenery()
    {
        var theme = AirwolfColors.ForLevel(Level);
        DrawSky(theme);
        DrawRidge(0.12f, 0.36f, 0.26f, 40f, theme.FarMountain);
        DrawRidge(0.25f, 0.3f, 0.22f, 0f, theme.NearMountain);
        DrawClouds(theme);
    }

    // The sky blends from the top color to the horizon color in thin bands, with the sun in it.
    private void DrawSky(AirwolfTheme theme)
    {
        var bandHeight = FieldHeight / SkyBands;
        for (var i = 0; i < SkyBands; i++)
        {
            var top = FieldTop + i * bandHeight;
            var height = i == SkyBands - 1 ? FieldBottom - top : bandHeight + 1f;
            var color = AirwolfColors.Blend(theme.SkyTop, theme.SkyHorizon, (float)i / (SkyBands - 1));
            gameContext.DrawBox(FieldLeft, top, FieldWidth, height, true, color);
        }

        gameContext.DrawDisc(FieldLeft + FieldWidth * 0.82f, FieldTop + FieldHeight * theme.SunHeight, Unit * 3.5f, true, theme.Sun);
    }

    // The distant mountains move slowly: that gives the feeling of depth.
    private void DrawRidge(float parallax, float baselineShare, float heightShare, float phase, Color color)
    {
        var baseline = FieldBottom - FieldHeight * baselineShare;
        var shift = worldPosition * parallax;
        var step = MathF.Max(2f, Unit * 0.3f);
        for (var x = FieldLeft; x < FieldRight; x += step)
        {
            var top = baseline - MountainHeight((x + shift) / Unit + phase) * FieldHeight * heightShare;
            gameContext.DrawBox(x, top, step + 1f, FieldBottom - top, true, color);
        }
    }

    private static float MountainHeight(float position)
    {
        return (MathF.Sin(position * 0.09f) + 0.6f * MathF.Sin(position * 0.22f + 1.3f) + 1.6f) / 3.2f;
    }

    private void DrawClouds(AirwolfTheme theme)
    {
        var span = FieldWidth * 1.5f;
        var thickness = Unit * 0.9f;
        foreach (var (x, y, length) in Clouds)
        {
            var width = length * FieldWidth;
            var left = (x * span - worldPosition * 0.5f) % span;
            if (left < 0f)
            {
                left += span;
            }

            left -= width;
            var top = FieldTop + y * FieldHeight * 0.6f + Unit;
            gameContext.DrawBox(left, top, width, thickness, true, theme.Cloud);
            gameContext.DrawBox(left + width * 0.2f, top - thickness * 0.6f, width * 0.45f, thickness * 0.6f, true, theme.Cloud);
            gameContext.DrawBox(left + width * 0.3f, top + thickness, width * 0.5f, thickness * 0.5f, true, theme.Cloud);
        }
    }

    // Grass on top, light soil below it and darker soil deeper down.
    private void DrawGround()
    {
        var theme = AirwolfColors.ForLevel(Level);
        var grass = MathF.Max(1f, Unit * 0.6f);
        var topSoil = Unit * 2.5f;
        for (var i = 0; i < terrain.Count; i++)
        {
            var left = ColumnLeft(i);
            var height = terrain[i];
            var top = FieldBottom - height;
            var grassHeight = MathF.Min(grass, height);
            var soilHeight = MathF.Min(topSoil, height - grassHeight);
            var deepTop = top + grassHeight + soilHeight;
            gameContext.DrawBox(left, top, columnWidth + 1f, grassHeight, true, theme.Grass);
            gameContext.DrawBox(left, top + grassHeight, columnWidth + 1f, soilHeight, true, theme.Soil);
            gameContext.DrawBox(left, deepTop, columnWidth + 1f, FieldBottom - deepTop, true, theme.DeepSoil);
        }

        DrawVegetation(theme);
    }

    // Pine trees and bushes stand on the flat parts of the ground.
    // They belong to a terrain column, so they scroll with the ground and stay where they are.
    private void DrawVegetation(AirwolfTheme theme)
    {
        for (var i = 0; i < terrain.Count - 1; i++)
        {
            var hash = unchecked((uint)(firstColumn + i) * 2654435761u);
            var roll = (hash >> 16) % VegetationSpacing;
            if (roll >= 6 || MathF.Abs(terrain[i + 1] - terrain[i]) > columnWidth * 0.3f)
            {
                continue;
            }

            var center = ColumnLeft(i) + columnWidth / 2f;
            var ground = FieldBottom - terrain[i];
            if (roll < 2)
            {
                DrawTree(center, ground, Unit * (2f + (hash >> 8) % 3), theme.Tree);
            }
            else
            {
                DrawBush(center, ground, Unit * (1f + (hash >> 8) % 2 * 0.5f), theme.Bush);
            }
        }
    }

    private void DrawTree(float center, float ground, float size, Color leaves)
    {
        var trunk = size * 0.25f;
        gameContext.DrawBox(center - size * 0.08f, ground - trunk, size * 0.16f, trunk, true, AirwolfColors.TreeTrunk);
        for (var layer = 0; layer < 3; layer++)
        {
            var layerHeight = size * 0.27f;
            var width = size * (0.8f - 0.2f * layer);
            gameContext.DrawBox(center - width / 2f, ground - trunk - (layer + 1) * layerHeight, width, layerHeight, true, leaves);
        }
    }

    private void DrawBush(float center, float ground, float size, Color leaves)
    {
        var width = size * 1.6f;
        gameContext.DrawBox(center - width / 2f, ground - size * 0.5f, width, size * 0.5f, true, leaves);
        gameContext.DrawBox(center - width * 0.3f, ground - size * 0.8f, width * 0.6f, size * 0.3f, true, leaves);
    }

    // The shadow of the helicopter on the ground gets smaller when it flies higher.
    private void DrawShadow()
    {
        var ground = GroundTopAt(helicopter.CenterX);
        var altitude = ground - helicopter.Bottom;
        if (altitude <= 0f)
        {
            return;
        }

        var width = helicopter.Width * 0.9f * MathF.Max(0.4f, 1f - altitude / (FieldHeight * 0.8f));
        gameContext.DrawBox(helicopter.CenterX - width / 2f, ground, width, MathF.Max(1f, Unit * 0.3f), true, AirwolfColors.Shadow);
    }
}
