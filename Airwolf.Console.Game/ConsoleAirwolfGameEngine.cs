using Mtf.Games.Airwolf;
using Mtf.Games.Interfaces;

namespace Airwolf.Console.Game;

/// <summary>
/// Airwolf with the status line of a terminal: a text row takes two pixels, so the line and the progress bar need four.
/// </summary>
internal sealed class ConsoleAirwolfGameEngine(IGameContext? context, Random? randomSource = null) : AirwolfGameEngine(context, randomSource)
{
    protected override int HudHeight => 4;
}
