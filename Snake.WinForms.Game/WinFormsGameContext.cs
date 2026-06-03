using Mtf.Drawing;
using Mtf.Drawing.Geometry;
using Mtf.Drawing.Interfaces;
using Mtf.Drawing.Render;
using Mtf.Games.Interfaces;

namespace Snake.WinForms.Game;

public class WinFormsGameContext(Form form) : IGameContext
{
    private readonly Form form = form;
    private readonly HashSet<Keys> pressedKeys = [];
    private readonly List<(IPrimitive primitive, Color color)> drawQueue = [];

    public int ScreenWidth => form.ClientSize.Width;
    public int ScreenHeight => form.ClientSize.Height;

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
        form.Invalidate();
    }

    public void Render(Graphics g)
    {
        g.Clear(Color.White);

        foreach (var (primitive, color) in drawQueue)
        {
            using var brush = new SolidBrush(color);
            using var pen = new Pen(color);

            switch (primitive)
            {
                case CirclePrimitive circle:
                    if (circle.Fill)
                    {
                        g.FillEllipse(
                            brush,
                            (float)circle.Circle.Center.X - circle.Circle.Radius,
                            (float)circle.Circle.Center.Y - circle.Circle.Radius,
                            circle.Circle.Radius * 2,
                            circle.Circle.Radius * 2);
                    }
                    else
                    {
                        g.DrawEllipse(
                            pen,
                            (float)circle.Circle.Center.X - circle.Circle.Radius,
                            (float)circle.Circle.Center.Y - circle.Circle.Radius,
                            circle.Circle.Radius * 2,
                            circle.Circle.Radius * 2);
                    }
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
                        SystemFonts.DefaultFont,
                        brush,
                        (float)text.Layout.Position.X,
                        (float)text.Layout.Position.Y);
                    break;
            }
        }
    }
    public IButtonStates GetButtonStates()
        => new WinFormsButtonStates(pressedKeys);

    public void KeyDown(Keys key) => pressedKeys.Add(key);

    public void KeyUp(Keys key) => pressedKeys.Remove(key);

    public void Start(string destinationAppName) { }

    public void StopApplication() => Application.Exit();

    public void Finish() => Application.Exit();

    public void PlayGameOver()
    {
        System.Media.SystemSounds.Hand.Play();
    }

    public void PlayYouWon()
    {
        System.Media.SystemSounds.Exclamation.Play();
    }

    public void StartGame() { }

    public void ShowOnMiddleOfScreen(string text, FontType fontType, int v2)
    {
        var primitive = new TextPrimitive(
            new TextLayout
            {
                Text = text,
                Position = new PointF(HorizontalCenter - (text.Length * 3), VerticalCenter)
            });

        Draw(primitive, Color.Black);
    }
}