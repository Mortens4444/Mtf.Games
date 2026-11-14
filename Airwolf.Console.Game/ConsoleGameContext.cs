using Mtf.Drawing;
using Mtf.Drawing.Interfaces;
using Mtf.Drawing.Render;
using Mtf.Games.Interfaces;
using System.Drawing;
using System.Text;

namespace Airwolf.Console.Game;

/// <summary>
/// Game context of a terminal with 24 bit colors (Windows Terminal, or the console of Windows 10 and later).
/// Every character cell shows two pixels one over the other: the upper half block character is painted
/// with the color of the upper pixel on the color of the lower one. So the pixels are square and the
/// screen is twice as high as the number of the text rows. A text is written into the cells, one character per pixel column.
/// </summary>
internal sealed class ConsoleGameContext : IGameContext, ISoundEffectContext, ITextMeasureContext, IDisposable
{
    private const int MinColumns = 40;
    private const int MinRows = 12;
    private const int MaxColumns = 220;
    private const int MaxRows = 60;
    private const char UpperHalf = '▀';

    private readonly Stream output = System.Console.OpenStandardOutput();
    private readonly ConsoleInput input = new();
    private readonly StringBuilder frame = new();
    private readonly uint originalOutputMode;
    private readonly bool outputModeChanged;
    private int columns;
    private int rows;
    private int[] pixels = [];
    private (char Character, int Color)[] glyphs = [];
    private (char Character, int Foreground, int Background)[] shown = [];
    private bool repaintAll = true;
    private bool disposed;
    private int toneIsPlaying;

    public ConsoleGameContext()
    {
        var handle = NativeConsole.GetStdHandle(NativeConsole.StandardOutput);
        if (NativeConsole.GetConsoleMode(handle, out originalOutputMode))
        {
            outputModeChanged = NativeConsole.SetConsoleMode(handle, originalOutputMode | NativeConsole.EnableVirtualTerminalProcessing);
        }

        System.Console.OutputEncoding = new UTF8Encoding(false);
        input.Start();

        // The alternate screen: the terminal looks as it did before when the game ends.
        Write("\u001b[?1049h\u001b[?25l\u001b[2J");
        FitToWindow();
    }

    /// <summary>
    /// The width of the screen in pixels: one pixel for a character column.
    /// </summary>
    public int ScreenWidth => columns;

    /// <summary>
    /// The height of the screen in pixels: two pixels for a text row.
    /// </summary>
    public int ScreenHeight => rows * 2;

    public int HorizontalCenter => ScreenWidth / 2;

    public int VerticalCenter => ScreenHeight / 2;

    // The window can be resized any time. The size changes only here, between two game moments.
    public void Clear()
    {
        FitToWindow();
        Array.Clear(pixels);
        Array.Clear(glyphs);
    }

    public void Draw(IPrimitive primitive, Color color)
    {
        var rgb = color.ToArgb();
        switch (primitive)
        {
            case RectanglePrimitive rectangle:
                DrawRectangle(rectangle, rgb);
                break;
            case LinePrimitive line:
                DrawLine((int)MathF.Round(line.Line.A.X), (int)MathF.Round(line.Line.A.Y), (int)MathF.Round(line.Line.B.X), (int)MathF.Round(line.Line.B.Y), rgb);
                break;
            case CirclePrimitive circle:
                DrawCircle((int)MathF.Round((float)circle.Circle.Center.X), (int)MathF.Round((float)circle.Circle.Center.Y), (int)MathF.Round(circle.Circle.Radius), circle.Fill, rgb);
                break;
            case PointPrimitive point:
                SetPixel((int)point.ShapeData.Point.X, (int)point.ShapeData.Point.Y, rgb);
                break;
            case TextPrimitive text:
                DrawText((int)MathF.Round((float)text.Layout.Position.X), (int)MathF.Round((float)text.Layout.Position.Y), text.Layout.Text, rgb);
                break;
        }
    }

    // Only the cells that changed since the previous frame are written.
    public void Update()
    {
        frame.Clear();
        var cursorColumn = -1;
        var cursorRow = -1;
        var foreground = -1;
        var background = -1;

        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var cell = Compose(column, row);
                var index = row * columns + column;
                if (!repaintAll && shown[index] == cell)
                {
                    continue;
                }

                shown[index] = cell;
                if (cursorRow != row || cursorColumn != column)
                {
                    frame.Append("\u001b[").Append(row + 1).Append(';').Append(column + 1).Append('H');
                }

                if (cell.Character != ' ' && cell.Foreground != foreground)
                {
                    AppendColor(38, cell.Foreground);
                    foreground = cell.Foreground;
                }

                if (cell.Background != background)
                {
                    AppendColor(48, cell.Background);
                    background = cell.Background;
                }

                frame.Append(cell.Character);
                cursorRow = row;
                cursorColumn = column + 1;
            }
        }

        repaintAll = false;
        if (frame.Length > 0)
        {
            Write(frame.ToString());
        }
    }

    public IButtonStates GetButtonStates() => input.Poll();

    public void ShowOnMiddleOfScreen(string text, FontType fontType, int v2)
    {
        ArgumentNullException.ThrowIfNull(text);

        DrawText(Math.Max(0, (columns - text.Length) / 2), ScreenHeight / 2 + v2 / 2, text, Color.White.ToArgb());
    }

    // A character is one pixel wide, and a text row is two pixels high. The menu keeps two rows of room around a line of text.
    public SizeF MeasureText(string text, FontType fontType)
    {
        ArgumentNullException.ThrowIfNull(text);

        return new SizeF(text.Length, 4f);
    }

    public void PlayTone(int frequency, int durationMilliseconds)
    {
        if (Interlocked.Exchange(ref toneIsPlaying, 1) == 1)
        {
            return;
        }

        var hertz = Math.Max(37, Math.Min(32767, frequency));
        var milliseconds = Math.Max(1, Math.Min(1000, durationMilliseconds));
        Task.Run(() => Beep(hertz, milliseconds, () => Volatile.Write(ref toneIsPlaying, 0)));
    }

    public void PlayGameOver() => Task.Run(() => Beep(200, 300, null));

    public void PlayYouWon() => Task.Run(() => Beep(880, 200, null));

    public void Start(string destinationAppName)
    {
    }

    public void StopApplication()
    {
    }

    public void Finish()
    {
    }

    public void StartGame()
    {
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Write("\u001b[0m\u001b[?25h\u001b[?1049l");
        input.Stop();
        if (outputModeChanged)
        {
            NativeConsole.SetConsoleMode(NativeConsole.GetStdHandle(NativeConsole.StandardOutput), originalOutputMode);
        }
    }

    private static void Beep(int frequency, int milliseconds, Action? done)
    {
        try
        {
            System.Console.Beep(frequency, milliseconds);
        }
        catch (Exception ex) when (ex is PlatformNotSupportedException or IOException)
        {
        }
        finally
        {
            done?.Invoke();
        }
    }

    private void Write(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        output.Write(bytes, 0, bytes.Length);
        output.Flush();
    }

    private void AppendColor(int layer, int rgb)
    {
        frame.Append("\u001b[").Append(layer).Append(";2;")
            .Append((rgb >> 16) & 0xFF).Append(';')
            .Append((rgb >> 8) & 0xFF).Append(';')
            .Append(rgb & 0xFF).Append('m');
    }

    private void FitToWindow()
    {
        // The last row stays free: a character in the bottom right corner would scroll the window.
        var newColumns = Math.Max(MinColumns, Math.Min(MaxColumns, System.Console.WindowWidth));
        var newRows = Math.Max(MinRows, Math.Min(MaxRows, System.Console.WindowHeight - 1));
        if (newColumns == columns && newRows == rows)
        {
            return;
        }

        columns = newColumns;
        rows = newRows;
        pixels = new int[columns * rows * 2];
        glyphs = new (char, int)[columns * rows];
        shown = new (char, int, int)[columns * rows];
        repaintAll = true;
        Write("\u001b[0m\u001b[2J");
    }

    private (char Character, int Foreground, int Background) Compose(int column, int row)
    {
        var upper = pixels[row * 2 * columns + column];
        var lower = pixels[(row * 2 + 1) * columns + column];
        var glyph = glyphs[row * columns + column];
        if (glyph.Character != 0)
        {
            return (glyph.Character, glyph.Color, upper);
        }

        return upper == lower ? (' ', 0, upper) : (UpperHalf, upper, lower);
    }

    // The empty screen is black: the pixel color has the alpha of its own, and a half transparent color blends into what is there.
    private void SetPixel(int x, int y, int argb)
    {
        if (x < 0 || y < 0 || x >= columns || y >= rows * 2)
        {
            return;
        }

        var alpha = (argb >> 24) & 0xFF;
        var index = y * columns + x;
        if (alpha < 255)
        {
            var old = pixels[index];
            var red = (((argb >> 16) & 0xFF) * alpha + ((old >> 16) & 0xFF) * (255 - alpha)) / 255;
            var green = (((argb >> 8) & 0xFF) * alpha + ((old >> 8) & 0xFF) * (255 - alpha)) / 255;
            var blue = ((argb & 0xFF) * alpha + (old & 0xFF) * (255 - alpha)) / 255;
            pixels[index] = (red << 16) | (green << 8) | blue;
        }
        else
        {
            pixels[index] = argb & 0xFFFFFF;
        }
    }

    private void DrawRectangle(RectanglePrimitive rectangle, int argb)
    {
        var left = (int)MathF.Round(rectangle.Rect.X);
        var top = (int)MathF.Round(rectangle.Rect.Y);
        var right = left + Math.Max(1, (int)MathF.Round(rectangle.Rect.Width)) - 1;
        var bottom = top + Math.Max(1, (int)MathF.Round(rectangle.Rect.Height)) - 1;
        for (var y = Math.Max(0, top); y <= Math.Min(rows * 2 - 1, bottom); y++)
        {
            for (var x = Math.Max(0, left); x <= Math.Min(columns - 1, right); x++)
            {
                if (rectangle.Fill || x == left || x == right || y == top || y == bottom)
                {
                    SetPixel(x, y, argb);
                }
            }
        }
    }

    private void DrawLine(int x1, int y1, int x2, int y2, int argb)
    {
        var dx = Math.Abs(x2 - x1);
        var dy = -Math.Abs(y2 - y1);
        var stepX = x1 < x2 ? 1 : -1;
        var stepY = y1 < y2 ? 1 : -1;
        var error = dx + dy;
        while (true)
        {
            SetPixel(x1, y1, argb);
            if (x1 == x2 && y1 == y2)
            {
                return;
            }

            var doubled = 2 * error;
            if (doubled >= dy)
            {
                error += dy;
                x1 += stepX;
            }

            if (doubled <= dx)
            {
                error += dx;
                y1 += stepY;
            }
        }
    }

    private void DrawCircle(int centerX, int centerY, int radius, bool fill, int argb)
    {
        for (var y = -radius; y <= radius; y++)
        {
            for (var x = -radius; x <= radius; x++)
            {
                var distance = Math.Sqrt(x * x + y * y);
                if (fill ? distance <= radius + 0.25 : Math.Abs(distance - radius) < 0.7)
                {
                    SetPixel(centerX + x, centerY + y, argb);
                }
            }
        }
    }

    // The text goes into the cells: the pixel row y is in the text row (y + 1) / 2, so a text of a menu line is centered in its four pixel rows.
    private void DrawText(int x, int y, string text, int argb)
    {
        var row = (y + 1) / 2;
        if (row < 0 || row >= rows)
        {
            return;
        }

        for (var i = 0; i < text.Length; i++)
        {
            var column = x + i;
            if (column >= 0 && column < columns && text[i] != ' ')
            {
                glyphs[row * columns + column] = (text[i], argb & 0xFFFFFF);
            }
        }
    }
}
