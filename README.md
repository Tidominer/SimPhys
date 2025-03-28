# SimPhys - A C# Lightweight Physics Library

## Overview

SimPhys is a lightweight, deterministic physics library designed specifically for use in Unity multiplayer games. The motivation behind SimPhys stems from the non-deterministic nature of Unity's built-in physics engine, which can lead to inconsistencies across different clients in a multiplayer environment. SimPhys aims to provide a consistent and predictable physics simulation, ensuring that all players experience the same game state.

While SimPhys was initially created with performance in mind, it currently performs numerous calculations, some of which may be optimized in future releases. This trade-off allows for accurate collision detection and resolution between entities such as circles and rectangles.

## Features

- **Deterministic Physics**: Ensures consistent behavior across all clients in a multiplayer setting.
- **Entity Support**: Provides support for basic 2D shapes like circles and rectangles.
- **Collision Detection & Resolution**: Implements both static and continuous collision detection along with resolution logic for overlapping entities.
- **Customizable Settings**: Allows configuration of friction, sub-stepping speed, and space size through `SpaceSettings`.
- **Event Handling**: Includes event callbacks (`OnCollisionEnter`, `OnCollisionStep`, `OnCollisionExit`) for managing interactions between entities.
- **Border Collision Handling**: Automatically resolves collisions with predefined boundaries.

## Installation

To use SimPhys in your Unity project:

1. Clone this repository or download the source code.
2. Copy the `SimPhys` folder into your Unity project's `Assets` directory.
3. Reference the necessary classes in your scripts:
   ```csharp
   using SimPhys;
   using SimPhys.Entities;
   ```

## Usage

### Setting Up the Simulation Space

First, create an instance of `SimulationSpace` and configure its settings:

```csharp
var settings = new SpaceSettings
{
    Friction = 0.95f,
    SubSteppingSpeed = 4,
    SpaceSize = new Vector2(10, 10)
};

var simulationSpace = new SimulationSpace(settings);
```

### Adding Entities

You can add entities (circles or rectangles) to the simulation space:

```csharp
var circle = new Circle
{
    Position = new Vector2(1, 1),
    Velocity = new Vector2(0.5m, 0.5m),
    Radius = 0.5m,
    Mass = 1,
    Bounciness = 0.8m
};

simulationSpace.AddEntity(circle);

var rectangle = new Rectangle
{
    Position = new Vector2(-1, -1),
    Velocity = new Vector2(-0.5m, -0.5m),
    Width = 1,
    Height = 1,
    Mass = 2,
    Bounciness = 0.6m
};

simulationSpace.AddEntity(rectangle);
```

### Running the Simulation

Call the `SimulateStep` method in your game loop (e.g., in Unity’s `Update` or `FixedUpdate`):

```csharp
void FixedUpdate()
{
    simulationSpace.SimulateStep();
}
```

### Handling Collisions

You can define custom behaviors for collisions by subscribing to the collision events:

```csharp
circle.OnCollisionEnter += other =>
{
    Debug.Log($"Circle collided with {other}");
};
```

### Border Collision Handling

The `ResolveBorderCollision` method automatically handles collisions with predefined boundaries based on the `SpaceSettings`.

## Performance Considerations

While SimPhys is designed to be lightweight, the current implementation involves several calculations per frame, especially during collision detection and resolution.
For now, it's recommended to profile and optimize the usage of SimPhys according to your specific game requirements.

## Contributing

Contributions to SimPhys are welcome! If you have ideas for improvements, bug fixes, or new features, feel free to submit pull requests or open issues on the GitHub repository.

## License

SimPhys is released under the MIT License. See [LICENSE](LICENSE) for more information.

---

Thank you for considering SimPhys for your Unity multiplayer projects! If you encounter any issues or have suggestions, please don't hesitate to reach out via the GitHub repository.
