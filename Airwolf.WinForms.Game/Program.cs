using Mtf.Games.Airwolf;
using Mtf.Games.General;
using Mtf.Games.WinForms;

namespace Airwolf.WinForms.Game;

internal static class Program
{
    private static readonly string[] Credits =
    [
        "Airwolf, a Mtf.Games sample",
        "",
        "Arrows: fly    Space: fire    Enter: bomb",
        "Esc: menu    F11: full screen"
    ];

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new GameForm(
            "Mtf Airwolf",
            context => new MenuGameEngine(context, "AIRWOLF", game => new AirwolfGameEngine(game), Credits))
        {
            FullScreen = true
        });
    }
}
