@echo off
REM FPS Game Project Setup and Build Script for Windows
REM This script automates the setup and building process for the Realistic FPS game

echo ========================================================
echo    Realistic FPS Game Project Setup and Build Script     
echo ========================================================

REM Define the project path (assuming script is in the project root)
set "PROJECT_PATH=%~dp0"
cd /D "%PROJECT_PATH%"

echo Current directory: %CD%
echo.

REM Step 1: Check if Unity is installed and in PATH
echo Step 1: Checking if Unity is installed...
where Unity.exe >nul 2>nul
if %errorlevel% equ 0 (
    echo Unity is installed and in PATH!
) else (
    echo Unity.exe not found in PATH.
    echo Attempting to locate Unity Hub...
    set "UNITY_HUB_EXE_PATH="
    if exist "%ProgramFiles%\Unity Hub\Unity Hub.exe" ( 
        set "UNITY_HUB_EXE_PATH=%ProgramFiles%\Unity Hub\Unity Hub.exe"
    ) else if exist "%ProgramFiles(x86)%\Unity Hub\Unity Hub.exe" (
        set "UNITY_HUB_EXE_PATH=%ProgramFiles(x86)%\Unity Hub\Unity Hub.exe"
    )

    if defined UNITY_HUB_EXE_PATH (
        echo Unity Hub found at: %UNITY_HUB_EXE_PATH%
        echo Please ensure you have installed a Unity Editor version through Unity Hub.
    ) else (
        echo Unity Hub does not seem to be installed in default locations.
        echo This script can attempt to download Unity Hub for Windows.
        set /p download_hub="Do you want to download Unity Hub? (y/n): "
        if /i "%download_hub%"=="y" (
            echo Downloading Unity Hub...
            REM Using PowerShell for a more reliable download
            powershell -Command "(New-Object Net.WebClient).DownloadFile('https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.exe', '%TEMP%\UnityHubSetup.exe')"
            if %errorlevel% equ 0 (
                echo Unity Hub installer downloaded to %TEMP%\UnityHubSetup.exe
                echo Please run the installer manually to install Unity Hub.
                start "" "%TEMP%\UnityHubSetup.exe"
            ) else (
                echo Failed to download Unity Hub installer.
                echo Please download and install Unity Hub manually from https://unity.com/download
            )
        ) else (
            echo Skipping Unity Hub download.
            echo Please install Unity Hub from https://unity.com/download and then install an Editor version.
        )
    )
    echo.
    echo Next, you MUST add the Unity Editor's installation directory to your PATH.
    echo The Unity Editor is typically installed by Unity Hub in a path like:
    echo   C:\Program Files\Unity\Hub\Editor\YOUR_EDITOR_VERSION\Editor
    echo   (e.g., C:\Program Files\Unity\Hub\Editor\2022.3.10f1\Editor)
    echo.
    echo To add it to your PATH:
    echo 1. Search for "environment variables" in the Windows search bar.
    echo 2. Click "Edit the system environment variables".
    echo 3. In the System Properties window, click the "Environment Variables..." button.
    echo 4. Under "System variables", find and select the "Path" variable, then click "Edit...".
    echo 5. Click "New" and add the path to your Unity Editor directory.
    echo 6. Click OK on all windows to save the changes.
    echo You may need to restart your command prompt or PC for the changes to take effect.
    echo.
    echo After installing a Unity Editor and adding it to your PATH, please re-run this script.
    goto :eof
)
echo.

REM Step 2: Check for required folders and create them if they don't exist
echo Step 2: Checking and creating required folders...
setlocal EnableDelayedExpansion
set "required_folders[0]=Assets\Prefabs\Weapons"
set "required_folders[1]=Assets\Prefabs\Consumables"
set "required_folders[2]=Assets\Prefabs\Effects"
set "required_folders[3]=Assets\Prefabs\UI"
set "required_folders[4]=Assets\Prefabs\Environment"
set "required_folders[5]=Assets\Prefabs\Checkpoint"
set "required_folders[6]=Assets\ScriptableObjects\Weapons"
set "required_folders[7]=Assets\ScriptableObjects\Enemies"
set "required_folders[8]=Assets\ScriptableObjects\Items"
set "required_folders[9]=Assets\ScriptableObjects\Survival"
set "required_folders[10]=Assets\Resources\Prefabs"
set "required_folders[11]=Assets\Resources\Crafting"
set "required_folders[12]=Assets\Resources\Loot"
set "required_folders[13]=Build"

for /L %%i in (0,1,13) do (
    if not exist "!required_folders[%%i]!" (
        mkdir "!required_folders[%%i]!"
        echo Created folder: !required_folders[%%i]!
    ) else (
        echo Folder exists: !required_folders[%%i]!
    )
)
endlocal
echo Folder structure verified!
echo.

REM Step 3: Create basic prefabs placeholders
echo Step 3: Setting up required Resources prefabs placeholders...

set "prefab_placeholders[0]=Assets\Resources\Prefabs\DroppedWeapon.prefab.txt"
set "prefab_placeholders[1]=Assets\Resources\Prefabs\DroppedConsumable.prefab.txt"
set "prefab_placeholders[2]=Assets\Resources\Prefabs\BulletImpact.prefab.txt"
set "prefab_placeholders[3]=Assets\Resources\Prefabs\MeleeSwingEffect.prefab.txt"
set "prefab_placeholders[4]=Assets\Resources\Prefabs\Checkpoint.prefab.txt"
set "prefab_placeholders[5]=Assets\Resources\Prefabs\RadiationZone.prefab.txt"
set "prefab_placeholders[6]=Assets\Resources\Prefabs\StorageContainer.prefab.txt"
set "prefab_placeholders[7]=Assets\Resources\Prefabs\CraftingStation.prefab.txt"

set "prefab_descriptions[0]=DroppedWeapon prefab should contain:
- Mesh Renderer
- Collider
- Rigidbody
- DroppedWeapon.cs script"
set "prefab_descriptions[1]=DroppedConsumable prefab should contain:
- Mesh Renderer
- Collider
- Rigidbody
- ConsumableItem.cs script"
set "prefab_descriptions[2]=BulletImpact prefab should contain:
- Particle System
- Audio Source
- PooledObject component"
set "prefab_descriptions[3]=MeleeSwingEffect prefab should contain:
- Trail Renderer
- Particle System
- PooledObject component"
set "prefab_descriptions[4]=Checkpoint prefab should contain:
- Collider (trigger)
- Visual indicator
- Checkpoint.cs script
- Audio Source for activation"
set "prefab_descriptions[5]=RadiationZone prefab should contain:
- Trigger Collider
- Particle System for visual effect
- Script to apply radiation damage to player"
set "prefab_descriptions[6]=StorageContainer prefab should contain:
- Mesh Renderer
- Collider
- IInteractable implementation
- Script to store items"
set "prefab_descriptions[7]=CraftingStation prefab should contain:
- Mesh Renderer
- Collider
- IInteractable implementation
- CraftingStation.cs script"

setlocal EnableDelayedExpansion
for /L %%i in (0,1,7) do (
    if not exist "!prefab_placeholders[%%i]!" (
        (echo # Unity Prefab Placeholder) > "!prefab_placeholders[%%i]!"
        (echo # Create this prefab in Unity Editor) >> "!prefab_placeholders[%%i]!"
        (echo # !prefab_descriptions[%%i]!) >> "!prefab_placeholders[%%i]!"
        echo Created placeholder for: !prefab_placeholders[%%i]!
    ) else (
        echo Placeholder exists: !prefab_placeholders[%%i]!
    )
)
endlocal
echo Resource prefab placeholders created!
echo.

REM Step 4: Create configuration file with game settings
echo Step 4: Creating game configuration file...
set "CONFIG_FILE=Assets\Resources\game_config.json"

if not exist "%CONFIG_FILE%" (
    (
        echo {
        echo     "gameSettings": {
        echo         "playerStartingHealth": 100,
        echo         "playerStartingArmor": 50,
        echo         "playerMaxWeight": 100,
        echo         "playerMaxWeapons": 4,
        echo         "hungerDecreaseRate": 0.05,
        echo         "thirstDecreaseRate": 0.1,
        echo         "temperatureChangeRate": 0.02,
        echo         "respawnDelay": 3.0,
        echo         "respawnLives": 3,
        echo         "respawnProtectionTime": 2.0,
        echo         "staminaMax": 100,
        echo         "staminaRegenRate": 10.0,
        echo         "radiationDamageRate": 5.0,
        echo         "environmentDamageMultiplier": 1.0
        echo     },
        echo     "weaponSettings": {
        echo         "defaultRecoil": 1.0,
        echo         "defaultSpread": 0.5,
        echo         "defaultDamage": 10,
        echo         "meleeSwingSpeed": 1.0,
        echo         "throwingForce": 20.0,
        echo         "bulletDrop": 0.1,
        echo         "bulletPenetration": {
        echo             "wood": 0.7,
        echo             "metal": 0.3,
        echo             "concrete": 0.1
        echo         },
        echo         "projectileGravity": 0.5,
        echo         "projectileAirResistance": 0.1
        echo     },
        echo     "enemySettings": {
        echo         "defaultDetectionRange": 20.0,
        echo         "defaultAttackRange": 15.0,
        echo         "defaultFieldOfView": 90.0,
        echo         "usesCover": true,
        echo         "coverSearchRadius": 15.0,
        echo         "dropChances": {
        echo             "weapon": 0.7,
        echo             "ammo": 0.8,
        echo             "medkit": 0.3,
        echo             "food": 0.5,
        echo             "water": 0.5,
        echo             "crafting": 0.4
        echo         }
        echo     },
        echo     "survivalSettings": {
        echo         "hungerDamageThreshold": 20.0,
        echo         "thirstDamageThreshold": 15.0,
        echo         "hungerDamageRate": 1.0,
        echo         "thirstDamageRate": 2.0,
        echo         "optimalTemperatureMin": 10.0,
        echo         "optimalTemperatureMax": 30.0,
        echo         "coldDamageRate": 1.5,
        echo         "heatDamageRate": 1.2
        echo     },
        echo     "craftingRecipes": [
        echo         {
        echo             "id": "recipe_bandage",
        echo             "name": "Bandage",
        echo             "ingredients": [
        echo                 {"type": "Cloth", "amount": 2}
        echo             ],
        echo             "result": {"type": "Bandage", "amount": 1},
        echo             "craftingTime": 2.0
        echo         },
        echo         {
        echo             "id": "recipe_medkit",
        echo             "name": "Med Kit",
        echo             "ingredients": [
        echo                 {"type": "Bandage", "amount": 2},
        echo                 {"type": "Herb", "amount": 1}
        echo             ],
        echo             "result": {"type": "MedKit", "amount": 1},
        echo             "craftingTime": 4.0
        echo         }
        echo         REM Add more recipes here following the same JSON structure
        echo     ],
        echo     "environmentEffects": {
        echo         "rain": {
        echo             "temperatureModifier": -5.0,
        echo             "wetness": 0.1,
        echo             "visibility": 0.7
        echo         },
        echo         "fog": {
        echo             "visibility": 0.4,
        echo             "soundPropagation": 1.2
        echo         },
        echo         "night": {
        echo             "temperatureModifier": -10.0,
        echo             "visibility": 0.3
        echo         }
        echo     }
        echo }
    ) > "%CONFIG_FILE%"
    echo Created game configuration file: %CONFIG_FILE%
) else (
    echo Configuration file already exists: %CONFIG_FILE%
)
echo.

REM Step 5: Create a simple README for setup instructions
echo Step 5: Creating setup instructions...
set "SETUP_README=SETUP_INSTRUCTIONS.md"

if not exist "%SETUP_README%" (
    (
        echo # FPS Game Setup Instructions
        echo.
        echo ## Required Steps
        echo.
        echo 1. Open the project in Unity Editor:
        echo    ^`^`^`
        echo    Unity.exe -projectPath "%PROJECT_PATH%"
        echo    ^`^`^`
        echo.
        echo 2. Import Standard Assets for:
        echo    - First Person Character Controller
        echo    - Effects
        echo    - Environment
        echo    - ParticleSystems
        echo    - Prototyping
        echo.
        echo 3. Create the required prefabs in Unity (see placeholder .txt files in Assets/Resources/Prefabs for details).
        echo.
        echo 4. Main Scene Setup (refer to Assets/Editor/SceneSetupScript.cs for an example or use the menu item "Realistic FPS / Setup Game Scene" in Unity Editor).
        echo.
        echo 5. ObjectPool Setup: Configure the ObjectPool in your main scene with necessary prefabs.
        echo.
        echo 6. Rust-like Features Setup: Follow specific setup steps for each feature as outlined in the main project README or documentation.
        echo.
        echo ## Building the Game
        echo.
        echo Run the build command:
        echo    ^`^`^`
        echo    cd /D "%PROJECT_PATH%"
        echo    Unity.exe -quit -batchmode -projectPath "%PROJECT_PATH%" -buildWindows64Player "%PROJECT_PATH%Build\RealisticFPS.exe"
        echo    ^`^`^`
        echo.
        echo ## Controls
        echo.
        echo - WASD: Movement
        echo - Mouse: Look around
        echo - Left Mouse Button: Shoot/Attack
        echo - Right Mouse Button: Aim down sights/Block
        echo - Shift: Sprint
        echo - Space: Jump
        echo - R: Reload
        echo - E: Interact with objects, pick up items
        echo - I: Open inventory
        echo - C: Open crafting menu
        echo - H: Use healing item
        echo - J: Use food item
        echo - K: Use water item
        echo - 1-4: Switch weapons
        echo - F: Flashlight
        echo - Q: Throw current weapon
    ) > "%SETUP_README%"
    echo Created setup instructions: %SETUP_README%
) else (
    echo Setup instructions already exist: %SETUP_README%
)
echo.

REM Step 6: Scene Setup Script (C# script is already created by the Linux script, this is just a note)
echo Step 6: Scene Setup Script Reminder
echo A C# script (Assets/Editor/SceneSetupScript.cs) should exist.
echo You can use it in Unity Editor via "Realistic FPS > Setup Game Scene" menu.
echo.

REM Step 7: Offer to open the project in Unity
echo Step 7: Would you like to open the project in Unity? (y/n)
set /p open_unity=
if /i "%open_unity%"=="y" (
    echo Opening project in Unity...
    start "" "Unity.exe" -projectPath "%PROJECT_PATH%"
    echo Unity should be opening the project.
    echo If Unity doesn't open, please open it manually and select the project folder: %PROJECT_PATH%
) else (
    echo Skipping Unity launch.
)
echo.

REM Step 8: Offer to build the project
echo Step 8: Would you like to build the project? (y/n)
set /p build_project=
if /i "%build_project%"=="y" (
    echo Building project...
    Unity.exe -quit -batchmode -projectPath "%PROJECT_PATH%" -buildWindows64Player "%PROJECT_PATH%Build\RealisticFPS.exe"
    if %errorlevel% equ 0 (
        echo Build completed successfully!
        echo The executable is located at: %PROJECT_PATH%Build\RealisticFPS.exe
    ) else (
        echo Build failed!
        echo Please check the Unity logs for more information (usually in %USERPROFILE%\AppData\Local\Unity\Editor\Editor.log).
    )
) else (
    echo Skipping build process.
)
echo.

echo ====================================================
echo Setup and build process completed!
echo Please follow the instructions in %SETUP_README% to complete the setup in Unity Editor.
echo ====================================================

pause
:eof
