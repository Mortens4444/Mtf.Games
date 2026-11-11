using Mtf.Games.Pong;
using Mtf.Games.WinForms;

namespace Pong.WinForms.Game;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new GameForm("Mtf Pong", context => new PongGameEngine(context)));
    }
}
