using Mtf.Games.Enums;

namespace Mtf.Games.Interfaces;

public interface IAudioContext
{
    void PlaySound(object sound, PlayType playOnce);
}
