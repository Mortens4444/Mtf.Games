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

---

## Architecture Overview

The framework relies heavily on dependency injection and specific interfaces to keep the game logic completely decoupled from the rendering and physical input layers:

* **`GameEngineBase`**: The core abstract class managing the primary `GameLoop()`, pause states, frame pacing, and game-over/victory sequences.
* **`IGameContext`**: An aggregate interface combining `ICanvas`, `IInputContext`, `IAudioContext`, and `IApplicationContext`. It provides everything a game instance needs to interact with the host system.
* **`ICanvas`**: Handles the physical dimensions of the screen and the rendering of shapes (like `CirclePrimitive` and `TextPrimitive`).
* **`IInputContext` / `IButtonStates`**: Abstracts keyboard or controller inputs into simple, readable boolean checks (e.g., `IsUpButtonPressed()`).
* **`IMoveable` & `IDirectable`**: General interfaces for standardizing movement and directional heading for entities.

---

## Getting Started

To run or extend this project, you need a .NET environment that implements the required UI/rendering frontend (e.g., WinForms, WPF, MonoGame, or a custom display matrix) mapping to the `Mtf.Drawing` abstraction.

1. Implement the `IGameContext` interface in your host application to handle drawing and input.
2. Instantiate either `CirclesGameEngine` or `SnakeGameEngine`, passing your custom context into the constructor.
3. Call `.GameLoop()` to start playing.