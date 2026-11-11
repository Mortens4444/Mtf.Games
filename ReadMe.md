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
A brick breaker with three levels. Outlined bricks need one hit, filled bricks need two.
* **Controls:** Left and right move the paddle, center or up launches the ball.
* **Objective:** Clear every brick on all three levels.
* **Game Over:** Losing the ball three times.

### Alien Attack
A fixed shooter with three levels. The alien formation marches down the screen and drops bombs.
* **Controls:** Left and right move the ship, center or up fires.
* **Objective:** Shoot down the whole formation on all three levels.
* **Game Over:** Being hit by three bombs, or letting the aliens reach the ship.

### Airwolf
A side scrolling helicopter game over hills, with drones in the air and turrets on the ground.
* **Controls:** The four direction buttons fly the helicopter, center fires.
* **Objective:** Stay alive until the end of both levels. The HUD shows the progress in percent.
* **Game Over:** Crashing three times into the ground, an enemy or flak.

---

## Architecture Overview

The framework relies heavily on dependency injection and specific interfaces to keep the game logic completely decoupled from the rendering and physical input layers:

* **`GameEngineBase`**: The core abstract class managing the primary `GameLoop()`, pause states, frame pacing, and game-over/victory sequences.
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

Every game has its own WinForms sample project (`Snake.WinForms.Game`, `Circles.WinForms.Game`, `Pong.WinForms.Game`, `Arkanoid.WinForms.Game`, `AlienAttack.WinForms.Game`, `Airwolf.WinForms.Game`). They share the form and the game context from the `Mtf.Games.WinForms` project. Arrow keys are the direction buttons, space is the center button and escape is the back button.
