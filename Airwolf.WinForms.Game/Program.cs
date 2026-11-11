using Mtf.Games.Airwolf;
using Mtf.Games.WinForms;

namespace Airwolf.WinForms.Game;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new GameForm("Mtf Airwolf", context => new AirwolfGameEngine(context)));
    }
}
