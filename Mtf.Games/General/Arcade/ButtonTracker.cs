using Mtf.Games.Interfaces;

namespace Mtf.Games.General.Arcade;

/// <summary>
/// Remembers the button states of the current and the previous game moment,
/// so a game can tell a held button from a freshly pressed one.
/// </summary>
public sealed class ButtonTracker
{
    private const int ButtonCount = 7;

    private readonly bool[] current = new bool[ButtonCount];
    private readonly bool[] previous = new bool[ButtonCount];

    /// <summary>
    /// -1 when only left is held, 1 when only right is held, otherwise 0.
    /// </summary>
    public int HorizontalAxis => (IsHeld(ArcadeButton.Right) ? 1 : 0) - (IsHeld(ArcadeButton.Left) ? 1 : 0);

    /// <summary>
    /// -1 when only up is held, 1 when only down is held, otherwise 0.
    /// </summary>
    public int VerticalAxis => (IsHeld(ArcadeButton.Down) ? 1 : 0) - (IsHeld(ArcadeButton.Up) ? 1 : 0);

    public void Update(IButtonStates buttonStates)
    {
        ArgumentNullException.ThrowIfNull(buttonStates);

        Array.Copy(current, previous, ButtonCount);
        current[(int)ArcadeButton.Up] = buttonStates.IsUpButtonPressed();
        current[(int)ArcadeButton.Down] = buttonStates.IsDownButtonPressed();
        current[(int)ArcadeButton.Left] = buttonStates.IsLeftButtonPressed();
        current[(int)ArcadeButton.Right] = buttonStates.IsRightButtonPressed();
        current[(int)ArcadeButton.Center] = buttonStates.IsCenterButtonPressed();
        current[(int)ArcadeButton.Back] = buttonStates.IsBackButtonPressed();
        current[(int)ArcadeButton.Secondary] = buttonStates is ISecondaryButtonStates secondary && secondary.IsSecondaryButtonPressed();
    }

    public void Reset()
    {
        Array.Clear(current, 0, ButtonCount);
        Array.Clear(previous, 0, ButtonCount);
    }

    /// <summary>
    /// True while the button is down.
    /// </summary>
    public bool IsHeld(ArcadeButton button) => current[(int)button];

    /// <summary>
    /// True only in the moment the button goes down.
    /// </summary>
    public bool WasPressed(ArcadeButton button) => current[(int)button] && !previous[(int)button];

    /// <summary>
    /// True only in the moment the button goes up.
    /// </summary>
    public bool WasReleased(ArcadeButton button) => !current[(int)button] && previous[(int)button];
}
