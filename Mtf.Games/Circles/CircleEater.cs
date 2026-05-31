using Mtf.Drawing.Geometry;
using Mtf.Drawing.Render;
using Mtf.Games.Interfaces;
using System.Drawing;

namespace Mtf.Games.Circles;

public class CircleEater : IMoveable
{
    private static readonly Random random = new(Environment.TickCount);

    private int currentCycles;
    private sbyte moveModifierRigth, moveModifierLeft, moveModifierUp, moveModifierDown;
    private readonly sbyte PixelsToMove = 2;
    public CirclePrimitive Circle { get; private init; }

    public CircleEater(byte x, byte y, byte radius)
    {
        Circle = new CirclePrimitive()
        {
            Circle = new CircleF(new PointF(x, y), radius),
            Fill = true
        };
    }

    public CircleEater(ICanvas canvas, byte radius)
    {
        Circle = new CirclePrimitive()
        {
            Circle = new CircleF((byte)random.Next(radius, canvas.ScreenWidth - radius),
                (byte)random.Next(radius, canvas.ScreenHeight - radius), radius),
            Fill = false
        };
        ChangeMoving();
    }

    public void ChangeMoving()
    {
        if (currentCycles > 0)
        {
            currentCycles--;
        }
        else
        {
            currentCycles = random.Next(20, 100);
            ChangeDirection();
        }
    }

    private void ChangeDirection()
    {
        var direction = random.Next(0, 3);
        bool goVertical = direction == 0 || direction == 2;
        bool goHorizontal = direction == 1 || direction == 2;

        if (goVertical)
        {
            var upDirection = random.Next(0, 2);
            if (upDirection == 0)
            {
                MoveDown(false);
                MoveUp(true);
            }
            else
            {
                MoveUp(false);
                MoveDown(true);
            }
        }
        else
        {
            MoveUp(false);
            MoveDown(false);
        }

        if (goHorizontal)
        {
            var leftDirection = random.Next(0, 2);
            if (leftDirection == 0)
            {
                MoveRight(false);
                MoveLeft(true);
            }
            else
            {
                MoveLeft(false);
                MoveRight(true);
            }
        }
        else
        {
            MoveLeft(false);
            MoveRight(false);
        }
    }

    public void MoveUp(bool move) => moveModifierUp = move ? (sbyte)-PixelsToMove : (sbyte)0;
    public void MoveDown(bool move) => moveModifierDown = move ? PixelsToMove : (sbyte)0;
    public void MoveRight(bool move) => moveModifierRigth = move ? PixelsToMove : (sbyte)0;
    public void MoveLeft(bool move) => moveModifierLeft = move ? (sbyte)-PixelsToMove : (sbyte)0;

    public void Move(ICanvas canvas)
    {
        var x = (byte)(Circle.Center.X + moveModifierRigth + moveModifierLeft);
        if (x - Circle.Radius < 0)
        {
            x = (byte)(canvas.ScreenWidth - Circle.Radius);
        }
        if (Circle.Center.X + Circle.Radius > canvas.ScreenWidth)
        {
            x = Convert.ToByte(Circle.Circle.Radius);
        }
        var y = (byte)(Circle.Center.Y + moveModifierUp + moveModifierDown);
        if (y - Circle.Radius < 0)
        {
            y = (byte)(canvas.ScreenHeight - Circle.Radius);
        }
        if (Circle.Center.Y + Circle.Radius > canvas.ScreenHeight)
        {
            y = Convert.ToByte(Circle.Circle.Radius);
        }
        Circle.Center = new PointF(x, y);
    }
}
