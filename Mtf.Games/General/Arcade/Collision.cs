using Mtf.Drawing.Geometry;

namespace Mtf.Games.General.Arcade;

public static class Collision
{
    /// <summary>
    /// Axis-aligned rectangle overlap. Rectangles that only touch do not intersect.
    /// </summary>
    public static bool Intersects(RectF a, RectF b)
    {
        return a.X < b.X + b.Width && b.X < a.X + a.Width &&
               a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
    }

    public static bool Intersects(CircleF circle, RectF rectangle)
    {
        var nearestX = MathF.Max(rectangle.X, MathF.Min(circle.Center.X, rectangle.X + rectangle.Width));
        var nearestY = MathF.Max(rectangle.Y, MathF.Min(circle.Center.Y, rectangle.Y + rectangle.Height));
        var dx = circle.Center.X - nearestX;
        var dy = circle.Center.Y - nearestY;
        return dx * dx + dy * dy < circle.Radius * circle.Radius;
    }

    /// <summary>
    /// Tells which side of the target was hit, based on the smaller penetration depth.
    /// </summary>
    public static CollisionSide GetHitSide(RectF moving, RectF target)
    {
        var overlapX = MathF.Min(moving.X + moving.Width, target.X + target.Width) - MathF.Max(moving.X, target.X);
        var overlapY = MathF.Min(moving.Y + moving.Height, target.Y + target.Height) - MathF.Max(moving.Y, target.Y);
        if (overlapX <= 0f || overlapY <= 0f)
        {
            return CollisionSide.None;
        }

        if (overlapX < overlapY)
        {
            return moving.Center.X < target.Center.X ? CollisionSide.Left : CollisionSide.Right;
        }

        return moving.Center.Y < target.Center.Y ? CollisionSide.Top : CollisionSide.Bottom;
    }
}
