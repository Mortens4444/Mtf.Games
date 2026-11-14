using Airwolf.Console.Game;
using Mtf.Games.General;

string[] credits =
[
    "Airwolf, a Mtf.Games sample",
    "",
    "Arrows: fly    Space: fire    Enter: bomb",
    "Esc: menu"
];

// The window of a terminal with 24 bit colors, the larger the better: every character is one pixel wide.
using var context = new ConsoleGameContext();

// Ctrl+C ends the program in the middle of a game: the terminal must get its screen back.
System.Console.CancelKeyPress += (_, _) => context.Dispose();

var engine = new MenuGameEngine(context, "AIRWOLF", game => new ConsoleAirwolfGameEngine(game), credits);
engine.GameLoop(50);
