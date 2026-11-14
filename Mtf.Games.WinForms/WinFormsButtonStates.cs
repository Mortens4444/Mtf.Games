using Mtf.Games.Interfaces;

namespace Mtf.Games.WinForms;

/// <summary>
/// The buttons of one game moment. The set of the pressed keys is not copied, so it must not change while it is read.
/// </summary>
public class WinFormsButtonStates(HashSet<Keys> keys) : IButtonStates, ISecondaryButtonStates
{
    private readonly HashSet<Keys> keys = keys;

    public bool IsUpButtonPressed() => keys.Contains(Keys.Up);

    public bool IsDownButtonPressed() => keys.Contains(Keys.Down);

    public bool IsLeftButtonPressed() => keys.Contains(Keys.Left);

    public bool IsRightButtonPressed() => keys.Contains(Keys.Right);

    public bool IsCenterButtonPressed() => keys.Contains(Keys.Space);

    public bool IsSecondaryButtonPressed() => keys.Contains(Keys.Enter);

    public bool IsBackButtonPressed() => keys.Contains(Keys.Escape);

    public bool IsAnyButtonPressed() => keys.Count > 0;
}
