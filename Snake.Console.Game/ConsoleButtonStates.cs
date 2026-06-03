using Mtf.Games.Interfaces;

namespace Snake.Console.Game;

public class ConsoleButtonStates : IButtonStates
{
    public bool Up { get; set; }

    public bool Down { get; set; }

    public bool Left { get; set; }

    public bool Right { get; set; }

    public bool Back { get; set; }

    public bool Center { get; set; }

    public bool IsUpButtonPressed() => Up;

    public bool IsCenterButtonPressed() => Center;

    public bool IsDownButtonPressed() => Down;

    public bool IsRightButtonPressed() => Right;

    public bool IsLeftButtonPressed() => Left;

    public bool IsBackButtonPressed() => Back;

    public bool IsAnyButtonPressed()
    {
        return Up || Down || Left || Right || Back || Center;
    }
}