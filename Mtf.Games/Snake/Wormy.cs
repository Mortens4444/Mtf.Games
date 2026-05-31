using Mtf.Drawing.Geometry;
using Mtf.Drawing.Render;
using Mtf.Games.General;
using Mtf.Games.Interfaces;
using System.Drawing;

namespace Mtf.Games.Snake;

public class Wormy : Directable
{
    private readonly List<CirclePrimitive> bodyParts = [];
    private readonly MovingDifferenceProvider movingDifferenceProvider = new();

    public Wormy(ICanvas canvas)
    {
        for (byte i = 0; i < Constants.NumberOfBodyParts; i++)
        {
            bodyParts.Add(new CirclePrimitive()
            {
                Circle = new CircleF(
                    (byte)(20 + ((int)Direction * i * MovingDifferenceProvider.PixelsToMove)),
                    (byte)canvas.VerticalCenter,
                    Constants.WormBodyRadius),
                Fill = false
            });
        }
    }

    public void Draw(IGameContext gameContext)
    {
        for (int i = 0; i < bodyParts.Count; i++)
        {
            gameContext.Draw(bodyParts[i], Color.Black);
        }
    }

    public bool MoveForward(ICanvas canvas)
    {
        bodyParts.RemoveAt(0);
        var head = GetHead();
        var movingModifier = movingDifferenceProvider.GetMovingDifference(Direction);
        bodyParts.Add(new CirclePrimitive()
        {
            Circle = new CircleF(head.Center.X + movingModifier.DeltaX, head.Center.Y + movingModifier.DeltaY, Constants.WormBodyRadius),
            Fill = false
        });
        var newHead = GetHead();
        bool isPointInScreen = newHead.Center.X >= 0 && newHead.Center.X <= canvas.ScreenWidth &&
                               newHead.Center.Y >= 0 && newHead.Center.Y <= canvas.ScreenHeight;
        return isPointInScreen && !HasCollision();
    }

    public bool CanConsumeFood(CirclePrimitive? foodLocation)
    {
        if (foodLocation != null)
        {
            for (int i = 0; i < bodyParts.Count; i++)
            {
                if (bodyParts[i].IsColliding(foodLocation))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void Grow(int nutrition)
    {
        var count = nutrition / 5;
        var tail = GetTail();
        for (int i = 0; i < count; i++)
        {
            bodyParts.Insert(0, new CirclePrimitive
            {
                Circle = new CircleF(tail.Center.X, tail.Center.Y, Constants.WormBodyRadius),
                Fill = false
            });
        }
    }

    private bool HasCollision()
    {
        var headCenter = GetHead().Center;
        for (int i = 0; i < bodyParts.Count - 3; i++)
        {
            var distance = GeometryMath.Distance(bodyParts[i].Center, headCenter);
            if (distance < Constants.WormBodyRadius)
            {
                return true;
            }
        }
        return false;
    }

    private CirclePrimitive GetHead()
    {
        return bodyParts[^1];
    }

    private CirclePrimitive GetTail()
    {
        return bodyParts[0];
    }
}
