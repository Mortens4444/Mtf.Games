namespace Mtf.Games.Interfaces;

/// <summary>
/// Optional capability of a game context: short sound effects.
/// Arcade games use it when the context implements it and stay silent otherwise.
/// The call should not block the game loop.
/// </summary>
public interface ISoundEffectContext
{
    void PlayTone(int frequency, int durationMilliseconds);
}
