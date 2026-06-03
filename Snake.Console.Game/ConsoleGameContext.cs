using Mtf.Drawing;
using Mtf.Drawing.Interfaces;
using Mtf.Drawing.Render;
using Mtf.Games.Interfaces;
using System.Drawing;

namespace Snake.Console.Game;

public class ConsoleGameContext : IGameContext
{
    private readonly char[,] buffer;
    private readonly ConsoleInputContext inputContext;

    public int ScreenWidth { get; }

    public int ScreenHeight { get; }

    public int HorizontalCenter => ScreenWidth / 2;

    public int VerticalCenter => ScreenHeight / 2;

    public ConsoleGameContext(int width, int height)
    {
        inputContext = new ConsoleInputContext();
        ScreenWidth = width;
        ScreenHeight = height;

        buffer = new char[height, width];

        System.Console.CursorVisible = false;
        System.Console.SetWindowSize(
            Math.Min(width + 1, System.Console.LargestWindowWidth),
            Math.Min(height + 1, System.Console.LargestWindowHeight));
    }

    public void Clear()
    {
        for (var y = 0; y < ScreenHeight; y++)
        {
            for (var x = 0; x < ScreenWidth; x++)
            {
                buffer[y, x] = ' ';
            }
        }
    }

    public void Update()
    {
        System.Console.SetCursorPosition(0, 0);

        var chars = new char[(ScreenWidth + Environment.NewLine.Length) * ScreenHeight];
        var index = 0;

        for (var y = 0; y < ScreenHeight; y++)
        {
            for (var x = 0; x < ScreenWidth; x++)
            {
                chars[index++] = buffer[y, x];
            }

            foreach (var ch in Environment.NewLine)
            {
                chars[index++] = ch;
            }
        }

        System.Console.Write(chars);
    }

    public void Draw(IPrimitive primitive, Color color)
    {
        switch (primitive)
        {
            case PointPrimitive point:
                DrawPoint((int)point.Shape.Center.X, (int)point.Shape.Center.Y, '■');
                break;

            case CirclePrimitive circle:
                DrawPoint((int)circle.Center.X, (int)circle.Center.Y, 'O');
                break;

            case TextPrimitive text:
                DrawText((int)text.Shape.Center.X, (int)text.Shape.Center.Y, text.Layout.Text);
                break;
        }
    }

    private void DrawPoint(int x, int y, char c)
    {
        if (x < 0 || y < 0 || x >= ScreenWidth || y >= ScreenHeight)
        {
            return;
        }

        buffer[y, x] = c;
    }

    private void DrawText(int x, int y, string text)
    {
        if (y < 0 || y >= ScreenHeight)
        {
            return;
        }

        for (var i = 0; i < text.Length && x + i < ScreenWidth; i++)
        {
            if (x + i >= 0)
            {
                buffer[y, x + i] = text[i];
            }
        }
    }

    public void ShowOnMiddleOfScreen(string text, FontType fontType, int size)
    {
        var x = Math.Max(0, HorizontalCenter - text.Length / 2);
        var y = VerticalCenter;

        DrawText(x, y, text);
        Update();
    }

    public void StartGame()
    {
    }

    public void Start(string destinationAppName)
    {
    }

    public void StopApplication() => Environment.Exit(0);

    public void Finish() => Environment.Exit(0);

    public void PlayYouWon()
    {
        System.Console.Beep(2000, 200);
        System.Console.Beep(3000, 200);
        System.Console.Beep(4000, 200);
    }

    public void PlayGameOver()
    {
        System.Console.Beep(2000, 200);
        System.Console.Beep(1000, 200);
        System.Console.Beep(500, 200);
    }

    public IButtonStates GetButtonStates() => inputContext.GetButtonStates();
}