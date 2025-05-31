#!/bin/zsh

# FPS Game Project Setup and Build Script
# This script automates the setup and building process for the Realistic FPS game

echo "========================================================"
echo "   Realistic FPS Game Project Setup and Build Script    "
echo "========================================================"

# Define colors for better readability
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Define the project path
PROJECT_PATH="/home/victoria/Desktop/fps"
cd "$PROJECT_PATH" || { echo "${RED}Error: Could not change to project directory!${NC}"; exit 1; }

echo "${BLUE}Current directory: $(pwd)${NC}"
echo

# Step 1: Check if Unity is installed
echo "${YELLOW}Step 1: Checking if Unity is installed...${NC}"
if command -v unity &> /dev/null; then
    echo "${GREEN}Unity is installed and in PATH!${NC}"
else
    echo "${RED}Unity is not found in your PATH.${NC}"
    echo "${YELLOW}Attempting to detect if Unity Hub is already installed...${NC}"
    UNITY_HUB_PATH=""
    # Common Unity Hub locations (this is a basic check)
    if [ -f "$HOME/.config/UnityHub/UnityHub.AppImage" ]; then # Default for AppImage after first run
        UNITY_HUB_PATH="$HOME/.config/UnityHub/UnityHub.AppImage"
    elif [ -f "/usr/bin/unityhub" ]; then # If installed via some package managers
        UNITY_HUB_PATH="/usr/bin/unityhub"
    elif [ -f "$HOME/Applications/Unity Hub.AppImage" ]; then # Common user install location
        UNITY_HUB_PATH="$HOME/Applications/Unity Hub.AppImage"
    fi

    if [ -n "$UNITY_HUB_PATH" ] && [ -x "$UNITY_HUB_PATH" ]; then
        echo "${GREEN}Unity Hub appears to be installed at: $UNITY_HUB_PATH${NC}"
        echo "Please ensure you have installed a Unity Editor version through Unity Hub."
    else
        echo "${YELLOW}Unity Hub does not seem to be installed or easily detectable in common locations.${NC}"
        echo "This script can attempt to download Unity Hub for Linux (AppImage)."
        # Changed from read -r -p to printf and read for better portability
        printf "Do you want to download Unity Hub AppImage? (y/n): "
        read -r download_hub
        if [[ "$download_hub" == "y" || "$download_hub" == "Y" ]]; then
            echo "Downloading Unity Hub AppImage..."
            UNITY_HUB_APPIMAGE_URL="https://public-cdn.cloud.unity3d.com/hub/prod/UnityHub.AppImage"
            # Download to a common applications directory if it exists, otherwise current dir
            DOWNLOAD_DIR="$PROJECT_PATH"
            if [ -d "$HOME/Applications" ]; then
                DOWNLOAD_DIR="$HOME/Applications"
            elif [ -d "$HOME/.local/bin" ]; then
                DOWNLOAD_DIR="$HOME/.local/bin"
            fi
            
            UNITY_HUB_TARGET_PATH="$DOWNLOAD_DIR/UnityHub.AppImage"

            wget "$UNITY_HUB_APPIMAGE_URL" -O "$UNITY_HUB_TARGET_PATH"
            if [ $? -eq 0 ]; then
                chmod +x "$UNITY_HUB_TARGET_PATH"
                echo "${GREEN}Unity Hub AppImage downloaded successfully to: $UNITY_HUB_TARGET_PATH${NC}"
                echo "You may need to run it once to complete its setup (e.g., $UNITY_HUB_TARGET_PATH)."
                echo "After running Unity Hub, install your desired Unity Editor version."
            else
                echo "${RED}Failed to download Unity Hub AppImage.${NC}"
                echo "Please download and install Unity Hub manually from https://unity.com/download"
                echo "After installing Unity and adding it to your PATH, please re-run this script."
                exit 1
            fi
        else
            echo "Skipping Unity Hub download."
            echo "Please install Unity Hub from https://unity.com/download, install an Editor version, and add it to your PATH."
            echo "After installing Unity and adding it to your PATH, please re-run this script."
            exit 1
        fi
    fi
    
    echo "${YELLOW}Next, you MUST add the Unity Editor's installation directory to your PATH.${NC}"
    echo "The Unity Editor is typically installed by Unity Hub in a path like:"
    echo "  '$HOME/Unity/Hub/Editor/YOUR_EDITOR_VERSION/Editor'"
    echo "  (e.g., '$HOME/Unity/Hub/Editor/2022.3.10f1/Editor')"
    echo
    echo "For Zsh (your current shell), you can add it to your PATH by editing your ~/.zshrc file:"
    echo "1. Open ~/.zshrc in a text editor (e.g., nano ~/.zshrc)."
    echo "2. Add the following line at the end (REPLACE '/path/to/your/Unity/Editor' with the actual path):"
    echo "   export PATH=\"\\\$PATH:/path/to/your/Unity/Editor\""
    echo "3. Save the file and run 'source ~/.zshrc' in your terminal, or open a new terminal."
    echo
    echo "${RED}After installing a Unity Editor via Unity Hub and adding it to your PATH, please re-run this script.${NC}"
    exit 1
fi
echo

# Step 2: Check for required folders and create them if they don't exist
echo "${YELLOW}Step 2: Checking and creating required folders...${NC}"
required_folders=(
    "Assets/Prefabs/Weapons"
    "Assets/Prefabs/Consumables"
    "Assets/Prefabs/Effects"
    "Assets/Prefabs/UI"
    "Assets/Prefabs/Environment"
    "Assets/Prefabs/Checkpoint"
    "Assets/ScriptableObjects/Weapons"
    "Assets/ScriptableObjects/Enemies"
    "Assets/ScriptableObjects/Items"
    "Assets/ScriptableObjects/Survival"
    "Assets/Resources/Prefabs"
    "Assets/Resources/Crafting"
    "Assets/Resources/Loot"
    "Build"
)

for folder in "${required_folders[@]}"; do
    if [ ! -d "$folder" ]; then
        mkdir -p "$folder"
        echo "Created folder: $folder"
    else
        echo "Folder exists: $folder"
    fi
done
echo "${GREEN}Folder structure verified!${NC}"
echo

# Step 3: Create basic prefabs if they don't exist
echo "${YELLOW}Step 3: Setting up required Resources prefabs...${NC}"
# We can't directly create Unity prefabs from a script, but we can create placeholder files
# that remind the user to create these in Unity

prefab_placeholders=(
    "Assets/Resources/Prefabs/DroppedWeapon.prefab.txt"
    "Assets/Resources/Prefabs/DroppedConsumable.prefab.txt"
    "Assets/Resources/Prefabs/BulletImpact.prefab.txt"
    "Assets/Resources/Prefabs/MeleeSwingEffect.prefab.txt"
    "Assets/Resources/Prefabs/Checkpoint.prefab.txt"
    "Assets/Resources/Prefabs/RadiationZone.prefab.txt"
    "Assets/Resources/Prefabs/StorageContainer.prefab.txt"
    "Assets/Resources/Prefabs/CraftingStation.prefab.txt"
)

prefab_descriptions=(
    "DroppedWeapon prefab should contain:\n- Mesh Renderer\n- Collider\n- Rigidbody\n- DroppedWeapon.cs script"
    "DroppedConsumable prefab should contain:\n- Mesh Renderer\n- Collider\n- Rigidbody\n- ConsumableItem.cs script"
    "BulletImpact prefab should contain:\n- Particle System\n- Audio Source\n- PooledObject component"
    "MeleeSwingEffect prefab should contain:\n- Trail Renderer\n- Particle System\n- PooledObject component"
    "Checkpoint prefab should contain:\n- Collider (trigger)\n- Visual indicator\n- Checkpoint.cs script\n- Audio Source for activation"
    "RadiationZone prefab should contain:\n- Trigger Collider\n- Particle System for visual effect\n- Script to apply radiation damage to player"
    "StorageContainer prefab should contain:\n- Mesh Renderer\n- Collider\n- IInteractable implementation\n- Script to store items"
    "CraftingStation prefab should contain:\n- Mesh Renderer\n- Collider\n- IInteractable implementation\n- CraftingStation.cs script"
)

for i in {0..7}; do
    if [ ! -f "${prefab_placeholders[$i]}" ]; then
        mkdir -p "$(dirname "${prefab_placeholders[$i]}")"
        echo "# Unity Prefab Placeholder" > "${prefab_placeholders[$i]}"
        echo "# Create this prefab in Unity Editor" >> "${prefab_placeholders[$i]}"
        echo "# ${prefab_descriptions[$i]}" >> "${prefab_placeholders[$i]}"
        echo "Created placeholder for: ${prefab_placeholders[$i]}"
    else
        echo "Placeholder exists: ${prefab_placeholders[$i]}"
    fi
done
echo "${GREEN}Resource prefab placeholders created!${NC}"
echo

# Step 4: Create configuration file with game settings
echo "${YELLOW}Step 4: Creating game configuration file...${NC}"
CONFIG_FILE="Assets/Resources/game_config.json"

if [ ! -f "$CONFIG_FILE" ]; then
    cat > "$CONFIG_FILE" << EOL
{
    "gameSettings": {
        "playerStartingHealth": 100,
        "playerStartingArmor": 50,
        "playerMaxWeight": 100,
        "playerMaxWeapons": 4,
        "hungerDecreaseRate": 0.05,
        "thirstDecreaseRate": 0.1,
        "temperatureChangeRate": 0.02,
        "respawnDelay": 3.0,
        "respawnLives": 3,
        "respawnProtectionTime": 2.0,
        "staminaMax": 100,
        "staminaRegenRate": 10.0,
        "radiationDamageRate": 5.0,
        "environmentDamageMultiplier": 1.0
    },
    "weaponSettings": {
        "defaultRecoil": 1.0,
        "defaultSpread": 0.5,
        "defaultDamage": 10,
        "meleeSwingSpeed": 1.0,
        "throwingForce": 20.0,
        "bulletDrop": 0.1,
        "bulletPenetration": {
            "wood": 0.7,
            "metal": 0.3,
            "concrete": 0.1
        },
        "projectileGravity": 0.5,
        "projectileAirResistance": 0.1
    },
    "enemySettings": {
        "defaultDetectionRange": 20.0,
        "defaultAttackRange": 15.0,
        "defaultFieldOfView": 90.0,
        "usesCover": true,
        "coverSearchRadius": 15.0,
        "dropChances": {
            "weapon": 0.7,
            "ammo": 0.8,
            "medkit": 0.3,
            "food": 0.5,
            "water": 0.5,
            "crafting": 0.4
        }
    },
    "survivalSettings": {
        "hungerDamageThreshold": 20.0,
        "thirstDamageThreshold": 15.0,
        "hungerDamageRate": 1.0,
        "thirstDamageRate": 2.0,
        "optimalTemperatureMin": 10.0,
        "optimalTemperatureMax": 30.0,
        "coldDamageRate": 1.5,
        "heatDamageRate": 1.2
    },
    "craftingRecipes": [
        {
            "id": "recipe_bandage",
            "name": "Bandage",
            "ingredients": [
                {"type": "Cloth", "amount": 2}
            ],
            "result": {"type": "Bandage", "amount": 1},
            "craftingTime": 2.0
        },
        {
            "id": "recipe_medkit",
            "name": "Med Kit",
            "ingredients": [
                {"type": "Bandage", "amount": 2},
                {"type": "Herb", "amount": 1}
            ],
            "result": {"type": "MedKit", "amount": 1},
            "craftingTime": 4.0
        },
        {
            "id": "recipe_water_purifier",
            "name": "Water Purifier",
            "ingredients": [
                {"type": "Metal", "amount": 1},
                {"type": "Cloth", "amount": 1}
            ],
            "result": {"type": "WaterPurifier", "amount": 1},
            "craftingTime": 3.0
        },
        {
            "id": "recipe_makeshift_armor",
            "name": "Makeshift Armor",
            "ingredients": [
                {"type": "Metal", "amount": 3},
                {"type": "Cloth", "amount": 2}
            ],
            "result": {"type": "MakeshiftArmor", "amount": 1},
            "craftingTime": 6.0
        },
        {
            "id": "recipe_radiation_pills",
            "name": "Radiation Pills",
            "ingredients": [
                {"type": "Chemical", "amount": 2},
                {"type": "Herb", "amount": 1}
            ],
            "result": {"type": "RadiationPills", "amount": 1},
            "craftingTime": 5.0
        }
    ],
    "environmentEffects": {
        "rain": {
            "temperatureModifier": -5.0,
            "wetness": 0.1,
            "visibility": 0.7
        },
        "fog": {
            "visibility": 0.4,
            "soundPropagation": 1.2
        },
        "night": {
            "temperatureModifier": -10.0,
            "visibility": 0.3
        }
    }
}
EOL
    echo "${GREEN}Created game configuration file: $CONFIG_FILE${NC}"
else
    echo "Configuration file already exists: $CONFIG_FILE"
fi
echo

# Step 5: Create a simple README for setup instructions
echo "${YELLOW}Step 5: Creating setup instructions...${NC}"
SETUP_README="SETUP_INSTRUCTIONS.md"

if [ ! -f "$SETUP_README" ]; then
    cat > "$SETUP_README" << EOL
# FPS Game Setup Instructions

## Required Steps

1. Open the project in Unity Editor:
   \`\`\`
   unity -projectPath $PROJECT_PATH
   \`\`\`

2. Import Standard Assets for:
   - First Person Character Controller
   - Effects
   - Environment
   - ParticleSystems
   - Prototyping

3. Create the required prefabs in Unity:
   - DroppedWeapon.prefab (using DroppedWeapon.cs)
   - DroppedConsumable.prefab (using ConsumableItem.cs)
   - BulletImpact.prefab (for projectile impacts)
   - MeleeSwingEffect.prefab (for melee weapons)
   - Checkpoint.prefab (for respawn system)
   - RadiationZone.prefab (for hazardous areas)
   - StorageContainer.prefab (for item storage, like Rust)
   - CraftingStation.prefab (for crafting items)

4. Main Scene Setup:
   1. Create a new scene
   2. Add a Player GameObject with:
      - PlayerController.cs
      - PlayerHealth.cs
      - PlayerInventory.cs
      - InteractionSystem.cs
      - SurvivalSystem.cs
      - WeaponManager.cs
   3. Add a GameManager GameObject with:
      - GameManager.cs
      - ObjectPool.cs
      - LevelManager.cs
      - DamageHandler.cs
      - UIManager.cs
      - LootSystem.cs
   4. Add a UI Canvas with elements for:
      - Health/Armor
      - Hunger/Thirst
      - Temperature
      - Radiation
      - Stamina
      - Inventory
      - Crafting
      - Objective markers

5. ObjectPool Setup:
   - Configure the ObjectPool with:
     - Projectiles
     - Impact effects
     - Particle systems
     - Blood effects
     - UI notifications
     - Melee swing effects
     - Thrown weapon effects

6. Rust-like Features Setup:
   1. Projectile-Based Weapons:
      - Set up projectile prefabs with gravity and air resistance
      - Configure melee weapons with swing arcs and throw capabilities
      - Set up material-specific impact effects
   
   2. Player Respawn System:
      - Place checkpoint objects throughout the level
      - Configure GameManager respawn settings in the Inspector
      - Test the respawn system with temporary invulnerability
   
   3. Looting System:
      - Configure enemy drop tables in EnemyData ScriptableObjects
      - Set up LootSystem component on the GameManager
      - Create lootable container prefabs and place them in the level
   
   4. Survival Mechanics:
      - Configure SurvivalSystem parameters in the Inspector
      - Create consumable items for hunger/thirst/health
      - Set up temperature zones in the environment
      - Create radiation zones and protective equipment
   
   5. Crafting System:
      - Place crafting station objects in the level
      - Configure crafting recipes in the game_config.json
      - Set up the crafting UI to display available recipes
   
   6. Destructible Environment:
      - Add DestructibleObject.cs to objects that can be destroyed
      - Configure health and damage settings for destructible objects
      - Set up destruction effects and loot drops

## Building the Game

Run the build task:
\`\`\`
cd $PROJECT_PATH
unity -quit -batchmode -projectPath $PROJECT_PATH -buildWindows64Player $PROJECT_PATH/Build/RealisticFPS.exe
\`\`\`

## Automated Build

You can also use the provided VS Code tasks:
1. "Open with Unity Editor" - Opens the project in Unity
2. "Build Unity Project" - Builds the Windows executable

## Controls

- WASD: Movement
- Mouse: Look around
- Left Mouse Button: Shoot/Attack
- Right Mouse Button: Aim down sights/Block
- Shift: Sprint
- Space: Jump
- R: Reload
- E: Interact with objects, pick up items
- I: Open inventory
- C: Open crafting menu
- H: Use healing item
- J: Use food item
- K: Use water item
- 1-4: Switch weapons
- F: Flashlight
- Q: Throw current weapon
EOL
    echo "${GREEN}Created setup instructions: $SETUP_README${NC}"
else
    echo "Setup instructions already exist: $SETUP_README"
fi
echo

# Step 6: Create a sample scene setup script
echo "${YELLOW}Step 6: Creating sample scene setup script...${NC}"
SCENE_SETUP_SCRIPT="Assets/Editor/SceneSetupScript.cs"

if [ ! -d "Assets/Editor" ]; then
    mkdir -p "Assets/Editor"
fi

if [ ! -f "$SCENE_SETUP_SCRIPT" ]; then
    cat > "$SCENE_SETUP_SCRIPT" << EOL
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class SceneSetupScript : EditorWindow
{
    [MenuItem("Realistic FPS/Setup Game Scene")]
    public static void SetupGameScene()
    {
        // Create a new scene
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
        
        // Setup player
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0, 1, 0);
        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerHealth>();
        player.AddComponent<PlayerInventory>();
        player.AddComponent<InteractionSystem>();
        player.AddComponent<SurvivalSystem>();
        player.AddComponent<WeaponManager>();
        player.AddComponent<CharacterController>();
        
        // Add camera to player
        GameObject camera = new GameObject("PlayerCamera");
        camera.transform.parent = player.transform;
        camera.transform.localPosition = new Vector3(0, 0.8f, 0);
        camera.AddComponent<Camera>();
        camera.AddComponent<AudioListener>();
        
        // Setup game manager
        GameObject manager = new GameObject("GameManager");
        manager.AddComponent<GameManager>();
        manager.AddComponent<ObjectPool>();
        manager.AddComponent<LevelManager>();
        manager.AddComponent<DamageHandler>();
        manager.AddComponent<UIManager>();
        manager.AddComponent<LootSystem>();
        
        // Setup UI canvas
        GameObject canvas = new GameObject("UICanvas");
        Canvas canvasComponent = canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Create UI elements
        CreateUIElement(canvas, "HealthDisplay", new Vector2(100, 30), new Vector2(10, 10));
        CreateUIElement(canvas, "ArmorDisplay", new Vector2(100, 30), new Vector2(10, 50));
        CreateUIElement(canvas, "HungerDisplay", new Vector2(100, 30), new Vector2(10, 90));
        CreateUIElement(canvas, "ThirstDisplay", new Vector2(100, 30), new Vector2(10, 130));
        CreateUIElement(canvas, "TemperatureDisplay", new Vector2(100, 30), new Vector2(10, 170));
        CreateUIElement(canvas, "RadiationDisplay", new Vector2(100, 30), new Vector2(10, 210));
        CreateUIElement(canvas, "StaminaDisplay", new Vector2(100, 30), new Vector2(10, 250));
        
        // Add event system
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        
        // Setup environment
        GameObject environment = new GameObject("Environment");
        
        // Create a floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.parent = environment.transform;
        floor.transform.localScale = new Vector3(10, 1, 10);
        
        // Create some walls
        CreateWall(environment.transform, new Vector3(0, 2.5f, 50), new Vector3(100, 5, 1));
        CreateWall(environment.transform, new Vector3(0, 2.5f, -50), new Vector3(100, 5, 1));
        CreateWall(environment.transform, new Vector3(50, 2.5f, 0), new Vector3(1, 5, 100));
        CreateWall(environment.transform, new Vector3(-50, 2.5f, 0), new Vector3(1, 5, 100));
        
        // Add a checkpoint
        GameObject checkpoint = new GameObject("Checkpoint");
        checkpoint.transform.position = new Vector3(5, 0.5f, 5);
        checkpoint.AddComponent<BoxCollider>().isTrigger = true;
        checkpoint.AddComponent<Checkpoint>();
        
        // Add a directional light
        GameObject light = new GameObject("DirectionalLight");
        Light lightComponent = light.AddComponent<Light>();
        lightComponent.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        
        // Save the scene
        string scenePath = "Assets/Scenes/MainScene.unity";
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
        
        Debug.Log("Basic game scene has been set up at: " + scenePath);
        EditorUtility.DisplayDialog("Scene Setup Complete", "Basic game scene has been created. You'll need to configure components and add more objects to create a full game environment.", "OK");
    }
    
    private static void CreateUIElement(GameObject parent, string name, Vector2 size, Vector2 position)
    {
        GameObject element = new GameObject(name);
        element.transform.parent = parent.transform;
        
        RectTransform rectTransform = element.AddComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = position;
        
        UnityEngine.UI.Text text = element.AddComponent<UnityEngine.UI.Text>();
        text.text = name;
        text.fontSize = 14;
        text.color = Color.white;
    }
    
    private static void CreateWall(Transform parent, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.parent = parent;
        wall.transform.position = position;
        wall.transform.localScale = scale;
    }
}
EOL
    echo "${GREEN}Created scene setup script: $SCENE_SETUP_SCRIPT${NC}"
    echo "This script will appear in Unity Editor under 'Realistic FPS > Setup Game Scene'"
else
    echo "Scene setup script already exists: $SCENE_SETUP_SCRIPT"
fi
echo

# Step 7: Check if Unity is available to open the project
echo "${YELLOW}Step 7: Would you like to open the project in Unity? (y/n)${NC}"
# This read command does not use -p, so it should be fine
read -r open_unity

if [[ $open_unity == "y" || $open_unity == "Y" ]]; then
    echo "Opening project in Unity..."
    unity -projectPath "$PROJECT_PATH" &
    echo "${GREEN}Unity should be opening the project.${NC}"
    echo "If Unity doesn't open, please open it manually and select the project folder."
else
    echo "Skipping Unity launch."
fi
echo

# Step 7: Offer to build the project
echo "${YELLOW}Step 7: Would you like to build the project? (y/n)${NC}" # Note: This was Step 7, should be Step 8 or renumbered
# This read command does not use -p, so it should be fine
read -r build_project

if [[ $build_project == "y" || $build_project == "Y" ]]; then
    echo "Building project..."
    unity -quit -batchmode -projectPath "$PROJECT_PATH" -buildWindows64Player "$PROJECT_PATH/Build/RealisticFPS.exe"
    
    if [ $? -eq 0 ]; then
        echo "${GREEN}Build completed successfully!${NC}"
        echo "The executable is located at: $PROJECT_PATH/Build/RealisticFPS.exe"
    else
        echo "${RED}Build failed!${NC}"
        echo "Please check the Unity logs for more information."
    fi
else
    echo "Skipping build process."
fi
echo

echo "${BLUE}====================================================${NC}"
echo "${GREEN}Setup and build process completed!${NC}"
echo "Please follow the instructions in $SETUP_README to complete the setup."
echo "${BLUE}====================================================${NC}"
