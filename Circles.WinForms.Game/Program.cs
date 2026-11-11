using Mtf.Games.Circles;
using Mtf.Games.WinForms;

namespace Circles.WinForms.Game;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new GameForm("Mtf Circles", context => new CirclesGameEngine(context)));
    }
}
