using System.Runtime.InteropServices;

namespace Airwolf.Console.Game;

/// <summary>
/// The few Windows console functions the game needs: real key down and key up events, and the terminal modes.
/// </summary>
internal static class NativeConsole
{
    public const int StandardInput = -10;
    public const int StandardOutput = -11;

    public const uint EnableProcessedInput = 0x0001;
    public const uint EnableWindowInput = 0x0008;
    public const uint EnableExtendedFlags = 0x0080;
    public const uint EnableVirtualTerminalProcessing = 0x0004;

    public const ushort KeyEvent = 0x0001;
    public const ushort FocusEvent = 0x0010;

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetStdHandle(int handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetConsoleMode(IntPtr handle, out uint mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetConsoleMode(IntPtr handle, uint mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetNumberOfConsoleInputEvents(IntPtr handle, out uint count);

    [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "ReadConsoleInputW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ReadConsoleInput(IntPtr handle, [Out] InputRecord[] buffer, uint length, out uint read);

    /// <summary>
    /// INPUT_RECORD: 20 bytes. Only the key and the focus events are used.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public struct InputRecord
    {
        [FieldOffset(0)]
        public ushort EventType;

        // KEY_EVENT_RECORD
        [FieldOffset(4)]
        public int KeyDown;

        [FieldOffset(10)]
        public ushort VirtualKeyCode;

        // FOCUS_EVENT_RECORD
        [FieldOffset(4)]
        public int SetFocus;
    }
}
