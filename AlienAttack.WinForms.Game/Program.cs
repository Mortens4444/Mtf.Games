using Mtf.Games.AlienAttack;
using Mtf.Games.WinForms;

namespace AlienAttack.WinForms.Game;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new GameForm("Mtf Alien Attack", context => new AlienAttackGameEngine(context)));
    }
}
