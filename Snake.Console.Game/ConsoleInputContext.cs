using Mtf.Games.Interfaces;

namespace Snake.Console.Game;

public class ConsoleInputContext : IInputContext
{
    private readonly ConsoleButtonStates buttonStates = new();

    public IButtonStates GetButtonStates()
    {
        buttonStates.Up = false;
        buttonStates.Down = false;
        buttonStates.Left = false;
        buttonStates.Right = false;
        buttonStates.Center = false;
        buttonStates.Back = false;

        while (System.Console.KeyAvailable)
        {
            var key = System.Console.ReadKey(true).Key;

            switch (key)
            {
                case ConsoleKey.UpArrow:
                    buttonStates.Up = true;
                    break;

                case ConsoleKey.DownArrow:
                    buttonStates.Down = true;
                    break;

                case ConsoleKey.LeftArrow:
                    buttonStates.Left = true;
                    break;

                case ConsoleKey.RightArrow:
                    buttonStates.Right = true;
                    break;

                case ConsoleKey.Spacebar:
                case ConsoleKey.Enter:
                    buttonStates.Center = true;
                    break;

                case ConsoleKey.Escape:
                    buttonStates.Back = true;
                    break;
            }
        }

        return buttonStates;
    }
}