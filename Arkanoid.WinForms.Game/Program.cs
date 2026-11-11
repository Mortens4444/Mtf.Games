using Mtf.Games.Arkanoid;
using Mtf.Games.WinForms;

namespace Arkanoid.WinForms.Game;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new GameForm("Mtf Arkanoid", context => new ArkanoidGameEngine(context)));
    }
}
