# New Realistic FPS Features

This update transforms the game into a more realistic FPS experience similar to Rust and Escape from Tarkov. The following features have been implemented:

## 1. Projectile-Based Weapons

All weapons (including melee weapons) now use projectile physics:
- Melee weapons can be swung with realistic arcs or thrown as projectiles
- Bullets and projectiles are affected by gravity, air resistance, and penetration physics
- Impact effects vary based on surface materials
- Physical impacts apply force to objects in the world

## 2. Player Respawn System

A comprehensive respawn system has been implemented:
- Checkpoint-based respawning at the last activated checkpoint
- Players drop their current weapon when they die
- Configurable lives system with game over when lives are depleted
- Temporary invulnerability after respawning to prevent spawn killing

## 3. Looting System

Enemies and containers now drop lootable items:
- Enemies drop their weapons and equipment on death
- Players can pick up weapons and items from the environment
- Inventory system with weight limitations
- Physics-based dropped items that can be collected

## 4. Survival Mechanics

The game now features realistic survival mechanics:
- Hunger and thirst systems that require regular sustenance
- Temperature regulation in different environments
- Stamina system affecting sprinting and actions
- Radiation system for hazardous areas
- Status effects impacting movement speed and aim stability

## 5. Crafting System

Players can now craft items from collected resources:
- Recipe-based crafting with required ingredients
- Different crafting stations for different types of items
- Crafting times and effects during the process
- Craft weapons, tools, medical supplies, and more

## 6. Consumable Items

Various consumable items can now be used:
- Medical items for healing (bandages, medkits)
- Food and water for survival
- Special items for environmental protection
- Status effect items (stimulants, radiation pills, etc.)

## 7. Destructible Environment

The environment now includes destructible objects:
- Objects can take damage and be destroyed
- Destructible objects can drop loot
- Physics-based destruction effects

## New Controls

- E: Interact with objects, pick up items
- I: Open inventory
- C: Open crafting menu
- H: Use healing item
- J: Use food item
- K: Use water item

## Development Notes

This update focuses on making the game more realistic and immersive while adding survival elements. The next phase will include more weapon variations, enemy types, crafting recipes, and storage systems.

**Project Setup:**
Automated setup scripts (`setup_and_build.sh` for Linux/macOS and `setup_and_build.bat` for Windows) are now available in the project root to help initialize the project structure, create placeholder assets, and guide through Unity setup.
