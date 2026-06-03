using Mtf.Games.Snake;
using Snake.Console.Game;

var gameContext = new ConsoleGameContext(80, 25);
var snakeGame = new SnakeGameEngine(gameContext);
snakeGame.GameLoop(300);
