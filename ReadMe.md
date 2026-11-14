# Mtf.Games

A lightweight, custom C# 2D game engine and arcade game collection. Built on bespoke rendering (`Mtf.Drawing`) and input abstractions, this framework is designed to make implementing classic 2D arcade games fast, modular, and intuitive.

---

## Included Games

### CircleEater
An *Agar.io*-style survival game where the player controls a circle and must consume wandering enemy circles to grow.
* **Objective:** Consume all enemy circles (default: 15) on the canvas to win.
* **Rules:** You can only eat circles smaller than your current size. Every time you consume an enemy, your circle's radius increases.
* **Game Over:** Colliding with an enemy circle that is larger than your player circle.

### Snake (Wormy)
A custom take on the classic snake game, built visually using connected circle primitives (`CirclePrimitive`).
* **Objective:** Navigate the canvas to consume randomly generated food, score points, and grow your worm.
* **Rules:** The worm grows longer based on the randomly generated nutritional value of the food consumed. 
* **Game Over:** Colliding with the screen boundaries or the worm's own body. The player wins by reaching the maximum score (1000 points).

### Pong
One player Pong against a paddle driven by the game.
* **Controls:** Up and down move the left paddle.
* **Objective:** Score 5 points. A point is scored when the opponent misses the ball.
* **Game Over:** Missing the ball three times.

### Arkanoid
A brick breaker with three levels. The bricks are colored by row. A brick with a black core needs two hits, the others need one. On a display without colors the bricks are outlined (one hit) or filled (two hits).
* **Controls:** Left and right move the paddle, center or up launches the ball.
* **Perks:** One destroyed brick in five drops a perk, catch it with the paddle. Wide paddle (gold, `W` in the HUD): the paddle is twice as wide. Slow ball (blue, `S`): the ball moves slower. Sticky paddle (purple, `G`): the ball sticks to the paddle until you launch it again. Extra life (pink, up to 5 lives). The effects last about 30 seconds at the default frame time, and a lost life takes them away.
* **Objective:** Clear every brick on all three levels.
* **Game Over:** Losing the ball three times.

### Alien Attack
A fixed shooter with three levels. The alien formation marches down the screen and drops bombs.
* **Controls:** Left and right move the ship, center or up fires.
* **Objective:** Shoot down the whole formation on all three levels.
* **Game Over:** Being hit by three bombs, or letting the aliens reach the ship.

### Airwolf
A side scrolling helicopter game over rolling hills with a colored sky, sun, parallax mountains, clouds, trees and bushes. The first level is a day, the second one a dusk. Drones fly in the air, turrets and foot soldiers wait on the ground, and a gunship boss closes every level.
* **Controls:** The four direction buttons fly the helicopter, center (Space) fires and the secondary button (Enter) drops a bomb. A bomb falls with the speed of the helicopter, blows up on the ground or on the first enemy it touches, and destroys every enemy in its blast. Two bombs can be in the air, and a new one is ready after about a second. The gunship takes three hits from a blast. The helicopter has inertia: it accelerates, drifts after a button is released and pitches into the flight. Turrets track the helicopter with their barrel but cannot aim below a minimum elevation, so flying low is safe from them. Shots and rammed enemies explode. Soldiers walk over the hills and shoot at a helicopter above them.
* **Objective:** Stay alive until the end of a level (the HUD shows the progress in percent), then destroy the gunship boss. It takes several hits, shows its remaining armor above itself, fires spreads of shells and is not destroyed by ramming it. Clear both levels.
* **Game Over:** Crashing three times into the ground, an enemy or flak.

---

## Architecture Overview

The framework relies heavily on dependency injection and specific interfaces to keep the game logic completely decoupled from the rendering and physical input layers:

* **`GameEngineBase`**: The core abstract class managing the primary `GameLoop()`, pause states, frame pacing, and game-over/victory sequences. A host calls `RequestStop()` (from any thread) to end a running loop, for example when its window closes.
* **`MenuGameEngine`**: A main menu around any game (New Game, Credits, Exit, and Continue while a game is running). Pass it to a host instead of the game engine: `new MenuGameEngine(context, "TITLE", game => new AirwolfGameEngine(game), creditLines)`. Up and down choose, the center button selects, back leaves the game or a page, and a finished game shows its result before the menu returns. The Airwolf sample uses it.
* **`IGameContext`**: An aggregate interface combining `ICanvas`, `IInputContext`, `IAudioContext`, and `IApplicationContext`. It provides everything a game instance needs to interact with the host system.
* **`ICanvas`**: Handles the physical dimensions of the screen and the rendering of shapes (like `CirclePrimitive` and `TextPrimitive`).
* **`IInputContext` / `IButtonStates`**: Abstracts keyboard or controller inputs into simple, readable boolean checks (e.g., `IsUpButtonPressed()`).
* **`IMoveable` & `IDirectable`**: General interfaces for standardizing movement and directional heading for entities.

### Arcade layer (`Mtf.Games.General.Arcade`)

Pong, Arkanoid, Alien Attack and Airwolf are built on a small layer above `GameEngineBase`:

* **`ArcadeGameEngineBase`**: Adds score, lives, levels, a HUD line, a playfield below it and a fixed update-then-draw order. A game implements `SetupLevel()`, `UpdateGame()` and `DrawGame()`, and calls `AddScore()`, `LoseLife()` and `CompleteLevel()`. Sizes and speeds are derived from `Unit` (2 pixels on a 128 pixel high screen), so the same game runs on a 178x128 EV3 display and in a desktop window.
* **`Sprite`, `BoxSprite`, `BallSprite`**: Game objects with position, size, velocity and bounding box collision.
* **`Collision`**: Rectangle and circle overlap tests, and the side on which a moving rectangle hit its target.
* **`ButtonTracker`**: Tells a held button from a freshly pressed one and gives the direction buttons as axes.
* **`Cooldown`**: Counts game moments for fire rates, spawn intervals and delays.
* **`ArcadeCanvasExtensions`**: `DrawBox`, `DrawDisc`, `DrawSegment` and `DrawLabel` clip or skip everything that is off-screen, so a host that converts coordinates to bytes never gets a negative or too large value.
* **`ISoundEffectContext`**: Optional interface of the game context for short tones. Games stay silent when the context does not implement it.

The center button pauses the game by default. A game that needs it as an action button overrides `CenterButtonPauses` and returns `false`.

A host has to draw `RectanglePrimitive`, `CirclePrimitive`, `LinePrimitive` and `TextPrimitive` for the arcade games.

---

## Getting Started

To run or extend this project, you need a .NET environment that implements the required UI/rendering frontend (e.g., WinForms, WPF, MonoGame, or a custom display matrix) mapping to the `Mtf.Drawing` abstraction.

1. Implement the `IGameContext` interface in your host application to handle drawing and input.
2. Instantiate one of the game engines (`CirclesGameEngine`, `SnakeGameEngine`, `PongGameEngine`, `ArkanoidGameEngine`, `AlienAttackGameEngine` or `AirwolfGameEngine`), passing your custom context into the constructor.
3. Call `.GameLoop()` to start playing.

Every game has its own WinForms sample project (`Snake.WinForms.Game`, `Circles.WinForms.Game`, `Pong.WinForms.Game`, `Arkanoid.WinForms.Game`, `AlienAttack.WinForms.Game`, `Airwolf.WinForms.Game`). They share the form and the game context from the `Mtf.Games.WinForms` project. Arrow keys are the direction buttons, space is the center button and escape is the back button. Enter is the secondary button (`ISecondaryButtonStates`, optional): Airwolf drops its bombs with it, and it selects in the menu. The context implements `ITextMeasureContext`, so the menu can center its texts, and `ISoundEffectContext` with short beeps, and the form stops the game loop when the window is closed. Airwolf rebuilds its terrain when the window is resized or maximized. `GameForm.FullScreen` makes the window borderless over the whole screen (the Airwolf sample starts that way), F11 or Alt+Enter switches it, and the text grows with the window height.
