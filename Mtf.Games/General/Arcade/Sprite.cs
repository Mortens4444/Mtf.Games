using Mtf.Drawing.Geometry;
using Mtf.Games.Interfaces;

namespace Mtf.Games.General.Arcade;

/// <summary>
/// A game object with a position, a size and a velocity.
/// X and Y are the top-left corner, velocities are pixels per game moment.
/// </summary>
public abstract class Sprite
{
    protected Sprite()
    {
    }

    protected Sprite(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public float X { get; set; }

    public float Y { get; set; }

    public float Width { get; set; }

    public float Height { get; set; }

    public float VelocityX { get; set; }

    public float VelocityY { get; set; }

    public bool IsAlive { get; set; } = true;

    public float Left => X;

    public float Top => Y;

    public float Right => X + Width;

    public float Bottom => Y + Height;

    public float CenterX => X + Width / 2f;

    public float CenterY => Y + Height / 2f;

    public RectF Bounds => new(X, Y, Width, Height);

    /// <summary>
    /// Moves the sprite by its velocity. Called once per game moment.
    /// </summary>
    public virtual void Update()
    {
        X += VelocityX;
        Y += VelocityY;
    }

    public abstract void Draw(ICanvas canvas);

    public void Kill()
    {
        IsAlive = false;
    }

    public void MoveTo(float x, float y)
    {
        X = x;
        Y = y;
    }

    public void CenterOn(float centerX, float centerY)
    {
        X = centerX - Width / 2f;
        Y = centerY - Height / 2f;
    }

    public void Stop()
    {
        VelocityX = 0f;
        VelocityY = 0f;
    }

    /// <summary>
    /// True when both sprites are alive and their bounding boxes overlap.
    /// </summary>
    public bool Intersects(Sprite other)
    {
        return other != null && IsAlive && other.IsAlive && Collision.Intersects(Bounds, other.Bounds);
    }

    /// <summary>
    /// Keeps the sprite inside the given area.
    /// </summary>
    public void ClampTo(float left, float top, float right, float bottom)
    {
        if (X < left)
        {
            X = left;
        }
        else if (Right > right)
        {
            X = MathF.Max(left, right - Width);
        }

        if (Y < top)
        {
            Y = top;
        }
        else if (Bottom > bottom)
        {
            Y = MathF.Max(top, bottom - Height);
        }
    }

    /// <summary>
    /// True when no part of the sprite is inside the given area.
    /// </summary>
    public bool IsOutside(float left, float top, float right, float bottom)
    {
        return Right < left || Left > right || Bottom < top || Top > bottom;
    }
}
