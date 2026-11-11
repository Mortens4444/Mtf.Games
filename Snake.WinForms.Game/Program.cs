using Mtf.Games.Snake;
using Mtf.Games.WinForms;

namespace Snake.WinForms.Game;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new GameForm("Mtf Snake", context => new SnakeGameEngine(context)));
    }
}
