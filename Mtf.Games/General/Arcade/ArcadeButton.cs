namespace Mtf.Games.General.Arcade;

public enum ArcadeButton
{
    Up,
    Down,
    Left,
    Right,
    Center,
    Back,

    /// <summary>
    /// An extra action button, when the host has one (see <see cref="Interfaces.ISecondaryButtonStates"/>).
    /// </summary>
    Secondary
}
