namespace Mtf.Games.General.Arcade;

/// <summary>
/// Counts game moments. Used for fire rates, spawn intervals and short delays.
/// </summary>
public sealed class Cooldown
{
    private int ticks;
    private int remaining;

    public Cooldown(int ticks)
    {
        Ticks = ticks;
    }

    /// <summary>
    /// Number of game moments between two uses. Takes effect at the next restart.
    /// </summary>
    public int Ticks
    {
        get => ticks;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ticks = value;
        }
    }

    public bool IsReady => remaining <= 0;

    public void Tick()
    {
        if (remaining > 0)
        {
            remaining--;
        }
    }

    public void Restart()
    {
        remaining = ticks;
    }

    public void Reset()
    {
        remaining = 0;
    }

    /// <summary>
    /// Restarts the cooldown and returns true when it is ready, otherwise returns false.
    /// </summary>
    public bool TryUse()
    {
        if (!IsReady)
        {
            return false;
        }

        Restart();
        return true;
    }
}
