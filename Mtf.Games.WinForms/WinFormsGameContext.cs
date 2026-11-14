using Mtf.Drawing;
using Mtf.Drawing.Geometry;
using Mtf.Drawing.Interfaces;
using Mtf.Drawing.Render;
using Mtf.Games.Interfaces;

namespace Mtf.Games.WinForms;

/// <summary>
/// Game context of a Windows Forms window. The game thread fills the draw queue and reads the buttons,
/// the UI thread paints the finished frame and collects the key events.
/// </summary>
public class WinFormsGameContext : IGameContext, ISoundEffectContext, ITextMeasureContext
{
    private const float DefaultFontSize = 12f;

    // The text keeps its size up to this window height and grows with the window above it, so it stays readable on a full screen.
    private const float ReferenceHeight = 300f;
    private const int MinToneFrequency = 37;
    private const int MaxToneFrequency = 32767;

    private readonly Form form;
    private readonly object sync = new();
    private readonly HashSet<Keys> pressedKeys = [];
    private readonly List<(IPrimitive primitive, Color color)> drawQueue = [];
    private readonly Dictionary<float, Font> fonts = [];

    // The frame handed over to the UI thread. The game thread only replaces the reference.
    private (IPrimitive primitive, Color color)[] frame = [];
    private Size screenSize;
    private int toneIsPlaying;

    public WinFormsGameContext(Form form)
    {
        ArgumentNullException.ThrowIfNull(form);

        this.form = form;
        screenSize = ClientSizeOrDefault(form.ClientSize, new Size(1, 1));
        form.ClientSizeChanged += (_, _) => RememberScreenSize();
        form.Disposed += (_, _) => DisposeFonts();
    }

    public int ScreenWidth
    {
        get
        {
            lock (sync)
            {
                return screenSize.Width;
            }
        }
    }

    public int ScreenHeight
    {
        get
        {
            lock (sync)
            {
                return screenSize.Height;
            }
        }
    }

    public int HorizontalCenter => ScreenWidth / 2;
    public int VerticalCenter => ScreenHeight / 2;

    public void Clear()
    {
        drawQueue.Clear();
    }

    public void Draw(IPrimitive primitive, Color color)
    {
        drawQueue.Add((primitive, color));
    }

    public void Update()
    {
        Volatile.Write(ref frame, [.. drawQueue]);

        // The window can be closed while the game thread is still running.
        if (form.IsDisposed)
        {
            return;
        }

        try
        {
            form.Invalidate();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public void Render(Graphics g)
    {
        ArgumentNullException.ThrowIfNull(g);

        g.Clear(Color.White);

        var brushes = new Dictionary<Color, SolidBrush>();
        var pens = new Dictionary<(Color, float), Pen>();
        try
        {
            foreach (var (primitive, color) in Volatile.Read(ref frame))
            {
                if (!brushes.TryGetValue(color, out var brush))
                {
                    brush = new SolidBrush(color);
                    brushes.Add(color, brush);
                }

                RenderPrimitive(g, primitive, color, brush, pens);
            }
        }
        finally
        {
            foreach (var brush in brushes.Values)
            {
                brush.Dispose();
            }

            foreach (var pen in pens.Values)
            {
                pen.Dispose();
            }
        }
    }

    public IButtonStates GetButtonStates()
    {
        // A copy: the UI thread keeps changing the set of the pressed keys.
        lock (sync)
        {
            return new WinFormsButtonStates([.. pressedKeys]);
        }
    }

    public void KeyDown(Keys key)
    {
        lock (sync)
        {
            pressedKeys.Add(key);
        }
    }

    public void KeyUp(Keys key)
    {
        lock (sync)
        {
            pressedKeys.Remove(key);
        }
    }

    /// <summary>
    /// Forgets every pressed key. The window gets no key up event when it loses the focus while a key is held.
    /// </summary>
    public void ReleaseKeys()
    {
        lock (sync)
        {
            pressedKeys.Clear();
        }
    }

    public void Start(string destinationAppName) { }

    public void StopApplication() => RunOnUiThread(Application.Exit);

    public void Finish() => RunOnUiThread(form.Close);

    public void PlayGameOver()
    {
        System.Media.SystemSounds.Hand.Play();
    }

    public void PlayYouWon()
    {
        System.Media.SystemSounds.Exclamation.Play();
    }

    public void StartGame() { }

    /// <summary>
    /// Plays a short tone without blocking the game loop. A tone is dropped when the previous one is still playing.
    /// </summary>
    public void PlayTone(int frequency, int durationMilliseconds)
    {
        if (Interlocked.Exchange(ref toneIsPlaying, 1) == 1)
        {
            return;
        }

        var hertz = Math.Max(MinToneFrequency, Math.Min(MaxToneFrequency, frequency));
        var milliseconds = Math.Max(1, Math.Min(1000, durationMilliseconds));
        Task.Run(() =>
        {
            try
            {
                Console.Beep(hertz, milliseconds);
            }
            catch (Exception ex) when (ex is PlatformNotSupportedException or IOException)
            {
            }
            finally
            {
                Volatile.Write(ref toneIsPlaying, 0);
            }
        });
    }

    /// <summary>
    /// The size of a text as it is painted: the same font and scale as the game draws it with.
    /// </summary>
    public SizeF MeasureText(string text, FontType fontType)
    {
        ArgumentNullException.ThrowIfNull(text);

        var layout = new TextPrimitive(0f, 0f, text, fontType).Layout;
        using var font = CreateFont(ScaledFontSize(layout.FontSize));
        using var graphics = Graphics.FromHwnd(IntPtr.Zero);
        return graphics.MeasureString(text, font);
    }

    public void ShowOnMiddleOfScreen(string text, FontType fontType, int v2)
    {
        ArgumentNullException.ThrowIfNull(text);

        var primitive = new TextPrimitive(0f, 0f, text, fontType);
        using var font = CreateFont(ScaledFontSize(primitive.Layout.FontSize));
        var size = TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding);
        primitive.Layout = new TextLayout
        {
            Text = text,
            FontSize = primitive.Layout.FontSize,
            Position = new PointF(
                MathF.Max(0f, HorizontalCenter - size.Width / 2f),
                MathF.Max(0f, VerticalCenter - size.Height / 2f + v2))
        };

        Draw(primitive, Color.Black);
    }

    private static Size ClientSizeOrDefault(Size size, Size fallback)
    {
        return size.Width > 0 && size.Height > 0 ? size : fallback;
    }

    private static Font CreateFont(float size)
    {
        return new Font(SystemFonts.DefaultFont.FontFamily, size, FontStyle.Regular, GraphicsUnit.Pixel);
    }

    private float ScaledFontSize(float size)
    {
        var scale = MathF.Max(1f, MathF.Round(ScreenHeight / ReferenceHeight * 4f) / 4f);
        return (size > 0f ? size : DefaultFontSize) * scale;
    }

    // A minimized window reports an empty client area. The game keeps the last real size instead of collapsing.
    private void RememberScreenSize()
    {
        var size = form.ClientSize;
        if (size.Width <= 0 || size.Height <= 0)
        {
            return;
        }

        lock (sync)
        {
            screenSize = size;
        }
    }

    // Closing or exiting has to happen on the UI thread, the game loop calls these from its own thread.
    private void RunOnUiThread(Action action)
    {
        if (form.IsDisposed)
        {
            return;
        }

        try
        {
            form.BeginInvoke(action);
        }
        catch (InvalidOperationException)
        {
            // No window handle: the window was not shown yet or is already gone.
        }
    }

    private void RenderPrimitive(Graphics g, IPrimitive primitive, Color color, SolidBrush brush, Dictionary<(Color, float), Pen> pens)
    {
        switch (primitive)
        {
            case CirclePrimitive circle:
                var circleBounds = new RectangleF(
                    (float)circle.Circle.Center.X - circle.Circle.Radius,
                    (float)circle.Circle.Center.Y - circle.Circle.Radius,
                    circle.Circle.Radius * 2,
                    circle.Circle.Radius * 2);
                if (circle.Fill)
                {
                    g.FillEllipse(brush, circleBounds);
                }
                else
                {
                    g.DrawEllipse(GetPen(pens, color, 1f), circleBounds);
                }
                break;

            case RectanglePrimitive rectangle:
                if (rectangle.Fill)
                {
                    g.FillRectangle(brush, rectangle.Rect.X, rectangle.Rect.Y, rectangle.Rect.Width, rectangle.Rect.Height);
                }
                else
                {
                    g.DrawRectangle(GetPen(pens, color, rectangle.Thickness), rectangle.Rect.X, rectangle.Rect.Y, rectangle.Rect.Width - 1, rectangle.Rect.Height - 1);
                }
                break;

            case LinePrimitive line:
                g.DrawLine(GetPen(pens, color, line.Thickness), line.Line.A, line.Line.B);
                break;

            case PointPrimitive point:
                g.FillRectangle(
                    brush,
                    (float)point.ShapeData.Point.X,
                    (float)point.ShapeData.Point.Y,
                    3, 3);
                break;

            case TextPrimitive text:
                g.DrawString(
                    text.Layout.Text,
                    GetFont(text.Layout.FontSize),
                    brush,
                    (float)text.Layout.Position.X,
                    (float)text.Layout.Position.Y);
                break;

            case EllipsePrimitive or PolylinePrimitive:
                primitive.DrawOnGraphics(g, color);
                break;
        }
    }

    private static Pen GetPen(Dictionary<(Color, float), Pen> pens, Color color, float thickness)
    {
        var width = thickness > 0f ? thickness : 1f;
        if (!pens.TryGetValue((color, width), out var pen))
        {
            pen = new Pen(color, width);
            pens.Add((color, width), pen);
        }

        return pen;
    }

    // Painting runs on the UI thread only, so the fonts need no lock.
    private Font GetFont(float size)
    {
        size = ScaledFontSize(size);
        if (!fonts.TryGetValue(size, out var font))
        {
            font = CreateFont(size);
            fonts.Add(size, font);
        }

        return font;
    }

    private void DisposeFonts()
    {
        foreach (var font in fonts.Values)
        {
            font.Dispose();
        }

        fonts.Clear();
    }
}
