# Realistic FPS Game

A first-person shooter game built with Unity, focusing on realistic graphics, physics, and gameplay.

## Features

- Realistic weapon mechanics with recoil, spread, and bullet physics
- Advanced enemy AI with tactical behaviors including cover usage
- Dynamic environment with day/night cycle and weather effects
- Immersive 3D sound system with spatial audio
- Comprehensive damage system with different damage types and effects
- ScriptableObject-based weapon and enemy configurations
- Object pooling for performance optimization
- Advanced post-processing effects for weapon impacts
- Mission-based gameplay with objectives and checkpoints
- Interactive environment with physics-based objects

## Project Structure

- `Assets/`: Main Unity assets folder
  - `Scripts/`: C# scripts organized by category
    - `Player/`: Player-related scripts (movement, health, interaction)
    - `Weapons/`: Weapon systems (controllers, projectiles, effects)
    - `Enemy/`: Enemy AI and behavior scripts
    - `Managers/`: Game management scripts (game state, pooling)
    - `UI/`: User interface components
    - `Audio/`: Audio management scripts
    - `Environment/`: Environmental effects and systems
    - `Combat/`: Damage handling and combat systems
    - `Levels/`: Level management and progression
    - `Effects/`: Visual effects scripts
    - `ScriptableObjects/`: Data containers for game entities
  - `Prefabs/`: Reusable game objects
  - `Materials/`: Material definitions and shaders
  - `Textures/`: Texture files
  - `Scenes/`: Game scenes/levels
  - `Audio/`: Sound effects and music
  - `Animations/`: Character and object animations
  - `Models/`: 3D models
  - `ScriptableObjects/`: Configured data assets

## Requirements

- Unity 2022.3 LTS or newer
- Visual Studio or Visual Studio Code with C# support
- New Input System package for Unity
- Post Processing package for Unity
- Shader Graph package (optional, for advanced visual effects)

## Setup Instructions

1. Clone or download this repository.
2. Run the setup script for your operating system (this will help create necessary folders and guide you):
   - For Linux/macOS: Open a terminal in the project root and run `sh setup_and_build.sh`
   - For Windows: Open a command prompt in the project root and run `setup_and_build.bat`
3. Follow the instructions provided by the script, which include:
   - Ensuring Unity Hub and a Unity Editor (2022.3 LTS or newer) are installed.
   - Adding the Unity Editor to your system's PATH if it isn't already.
4. Open the project in Unity Hub or directly via the Unity Editor.
5. Once the project is open in Unity, install required packages through the Package Manager (if not already installed or prompted by Unity):
   - Input System
   - Post Processing Stack
   - Shader Graph (optional)
6. The setup script creates a `SETUP_INSTRUCTIONS.md` file in the project root. Refer to this file for detailed steps on:
   - Importing Standard Assets (if needed for placeholders or specific character controllers).
   - Creating required prefabs (the script creates `.txt` placeholders in `Assets/Resources/Prefabs` to guide you).
   - Setting up the main game scene (an example scene setup script `Assets/Editor/SceneSetupScript.cs` is provided, accessible via Unity menu "Realistic FPS > Setup Game Scene").
   - Configuring the ObjectPool, ScriptableObjects, and other game elements.
7. Open the `MainScene` from the `Assets/Scenes` folder (or the scene created by the setup script).
8. Press Play to test the game.

## Controls

- WASD: Movement
- Mouse: Look around
- Left Mouse Button: Shoot
- Right Mouse Button: Aim down sights
- Shift: Sprint
- Space: Jump
- R: Reload
- 1-4: Switch weapons
- E: Interact
- Tab: Show objectives
- C: Crouch
- Q: Special ability/alternate fire
- F: Flashlight

## Key Components

### Weapon System
The weapon system uses ScriptableObjects to define different weapon types with customizable properties like damage, fire rate, recoil patterns, and more. Weapons can be easily configured and balanced using the Unity inspector.

### Enemy AI
Enemies use a state machine-based AI system that allows them to patrol, chase, attack, take cover, and react to sounds. The AI is configured using ScriptableObjects to create different enemy types with varying behaviors.

### Environment System
The environment system handles dynamic time of day, weather effects, and physics interactions. It can create realistic atmospheric conditions that affect gameplay, such as reduced visibility in fog or slippery surfaces in rain.

### Damage System
The damage system supports different damage types (bullet, explosion, fire, etc.) with customizable effects for different surface materials. It includes impact effects, decals, and audio feedback.

## Performance Optimization

- Object pooling for projectiles, particles, and other frequently instantiated objects
- Level of detail (LOD) systems for models
- Occlusion culling for large environments
- Optimized shaders for different quality settings

## Contributing

This project is open for contributions. Please follow these steps:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.
# fpscsharp
