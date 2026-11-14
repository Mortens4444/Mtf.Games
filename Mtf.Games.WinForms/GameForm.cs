using Mtf.Games.General;
using System.Runtime.ExceptionServices;

namespace Mtf.Games.WinForms;

/// <summary>
/// A window that hosts one game: it creates the game context, starts the game loop
/// on a background thread and forwards painting and key events to the context.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public class GameForm : Form
{
    private readonly WinFormsGameContext context;
    private readonly Func<WinFormsGameContext, GameEngineBase> createEngine;
    private readonly int frameTime;
    private GameEngineBase? engine;
    private bool fullScreen;
    private FormBorderStyle windowedBorderStyle;
    private FormWindowState windowedState;

    public GameForm(string title, Func<WinFormsGameContext, GameEngineBase> createEngine, int frameTime = 50)
    {
        ArgumentNullException.ThrowIfNull(createEngine);
        ArgumentOutOfRangeException.ThrowIfNegative(frameTime);

        this.createEngine = createEngine;
        this.frameTime = frameTime;

        DoubleBuffered = true;
        ResizeRedraw = true;

        Width = 400;
        Height = 300;
        StartPosition = FormStartPosition.CenterScreen;
        Text = title;

        context = new WinFormsGameContext(this);
    }

    /// <summary>
    /// Covers the whole screen without a border or a title bar. F11 and Alt+Enter switch it while the game is running.
    /// </summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool FullScreen
    {
        get => fullScreen;
        set
        {
            if (fullScreen == value)
            {
                return;
            }

            fullScreen = value;
            if (value)
            {
                windowedBorderStyle = FormBorderStyle;
                windowedState = WindowState;

                // A maximized window keeps the taskbar visible after its border is removed, unless it is maximized again.
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                Cursor.Hide();
            }
            else
            {
                Cursor.Show();
                FormBorderStyle = windowedBorderStyle;
                WindowState = windowedState;
            }
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        // The engine is built when the window is shown, so it sees the final client size (after the DPI scaling).
        // The game loop writes the game over and the won message and plays their sounds itself.
        var gameEngine = createEngine(context);
        engine = gameEngine;
        Task.Factory
            .StartNew(() => gameEngine.GameLoop(frameTime), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
            .ContinueWith(ShowGameLoopFailure, TaskContinuationOptions.OnlyOnFaulted);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        // Otherwise the game loop keeps running, and painting into a disposed window.
        engine?.RequestStop();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        context.Render(e.Graphics);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.F11 || (e.Alt && e.KeyCode == Keys.Enter))
        {
            FullScreen = !FullScreen;
            e.Handled = true;
            return;
        }

        context.KeyDown(e.KeyCode);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        context.KeyUp(e.KeyCode);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        context.ReleaseKeys();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        context.ReleaseKeys();
    }

    // A failed game loop would otherwise freeze the window without any message.
    private void ShowGameLoopFailure(Task loop)
    {
        var failure = loop.Exception?.GetBaseException();
        if (failure == null || IsDisposed)
        {
            return;
        }

        try
        {
            BeginInvoke(() => ExceptionDispatchInfo.Capture(failure).Throw());
        }
        catch (InvalidOperationException)
        {
        }
    }
}
