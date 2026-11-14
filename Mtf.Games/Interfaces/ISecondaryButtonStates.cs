namespace Mtf.Games.Interfaces;

/// <summary>
/// Optional capability of the button states of a host: one more action button next to the center button
/// (Enter on a keyboard). Airwolf drops its bombs with it. A host without it simply has no bombs.
/// </summary>
public interface ISecondaryButtonStates
{
    bool IsSecondaryButtonPressed();
}
