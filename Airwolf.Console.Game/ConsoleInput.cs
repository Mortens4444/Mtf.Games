using Mtf.Games.Interfaces;

namespace Airwolf.Console.Game;

/// <summary>
/// Collects the key events of the console. A console reports key up as well, so a key stays pressed as long as it is held.
/// The game thread reads it: every call of <see cref="Poll"/> takes the events that arrived since the previous one.
/// </summary>
internal sealed class ConsoleInput
{
    private const ushort Escape = 0x1B;
    private const ushort Space = 0x20;
    private const ushort Left = 0x25;
    private const ushort Up = 0x26;
    private const ushort Right = 0x27;
    private const ushort Down = 0x28;
    private const ushort Enter = 0x0D;

    private readonly IntPtr handle = NativeConsole.GetStdHandle(NativeConsole.StandardInput);
    private readonly HashSet<ushort> pressed = [];
    private readonly NativeConsole.InputRecord[] buffer = new NativeConsole.InputRecord[64];
    private uint originalMode;
    private bool modeChanged;

    /// <summary>
    /// No line input, no echo and no quick edit (a click on the window would freeze the game).
    /// Ctrl+C still ends the program.
    /// </summary>
    public void Start()
    {
        if (NativeConsole.GetConsoleMode(handle, out originalMode))
        {
            modeChanged = NativeConsole.SetConsoleMode(
                handle,
                NativeConsole.EnableProcessedInput | NativeConsole.EnableWindowInput | NativeConsole.EnableExtendedFlags);
        }
    }

    public void Stop()
    {
        if (modeChanged)
        {
            NativeConsole.SetConsoleMode(handle, originalMode);
            modeChanged = false;
        }
    }

    public IButtonStates Poll()
    {
        while (NativeConsole.GetNumberOfConsoleInputEvents(handle, out var count) && count > 0)
        {
            if (!NativeConsole.ReadConsoleInput(handle, buffer, (uint)buffer.Length, out var read))
            {
                break;
            }

            for (var i = 0; i < read; i++)
            {
                Apply(buffer[i]);
            }
        }

        return new ConsoleButtonStates([.. pressed]);
    }

    private void Apply(NativeConsole.InputRecord record)
    {
        if (record.EventType == NativeConsole.KeyEvent)
        {
            if (record.KeyDown != 0)
            {
                pressed.Add(record.VirtualKeyCode);
            }
            else
            {
                pressed.Remove(record.VirtualKeyCode);
            }
        }
        else if (record.EventType == NativeConsole.FocusEvent && record.SetFocus == 0)
        {
            // The key up of a held key goes to another window when the console loses the focus.
            pressed.Clear();
        }
    }

    private sealed class ConsoleButtonStates(HashSet<ushort> keys) : IButtonStates, ISecondaryButtonStates
    {
        public bool IsUpButtonPressed() => keys.Contains(Up);

        public bool IsDownButtonPressed() => keys.Contains(Down);

        public bool IsLeftButtonPressed() => keys.Contains(Left);

        public bool IsRightButtonPressed() => keys.Contains(Right);

        public bool IsCenterButtonPressed() => keys.Contains(Space);

        public bool IsBackButtonPressed() => keys.Contains(Escape);

        public bool IsSecondaryButtonPressed() => keys.Contains(Enter);

        public bool IsAnyButtonPressed() => keys.Count > 0;
    }
}
