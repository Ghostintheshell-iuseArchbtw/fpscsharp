using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using FPS.Player;
using FPS.Survival;
using FPS.Weapons;
using FPS.Levels;

namespace FPS.Managers
{
    [System.Serializable]
    public class SaveData
    {
        public PlayerSaveData playerData;
        public WorldSaveData worldData;
        public string currentLevel;
        public float playTime;
        public string saveTime;
        public int saveVersion = 1;
    }
    
    [System.Serializable]
    public class PlayerSaveData
    {
        public Vector3 position;
        public Vector3 rotation;
        public float health;
        public float armor;
        public float hunger;
        public float thirst;
        public float stamina;
        public float temperature;
        public float radiation;
        public List<InventoryItemData> inventory;
        public List<WeaponSaveData> weapons;
        public int currentWeaponIndex;
    }
    
    [System.Serializable]
    public class InventoryItemData
    {
        public string itemName;
        public int quantity;
        public string itemType;
    }
    
    [System.Serializable]
    public class WeaponSaveData
    {
        public string weaponName;
        public int currentAmmo;
        public int reserveAmmo;
    }
    
    [System.Serializable]
    public class WorldSaveData
    {
        public List<string> completedObjectives;
        public List<CheckpointData> activeCheckpoints;
        public List<DestructibleObjectData> destroyedObjects;
        public List<LootableContainerData> lootedContainers;
    }
    
    [System.Serializable]
    public class DestructibleObjectData
    {
        public string objectId;
        public Vector3 position;
        public bool isDestroyed;
    }
    
    [System.Serializable]
    public class LootableContainerData
    {
        public string containerId;
        public Vector3 position;
        public bool hasBeenLooted;
    }
    
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }
        
        [Header("Save Settings")]
        [SerializeField] private string saveFileName = "gamedata.save";
        [SerializeField] private bool useEncryption = false;
        [SerializeField] private int maxSaveSlots = 5;
        
        private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);
        
        // Events
        public event System.Action OnGameSaved;
        public event System.Action OnGameLoaded;
        public event System.Action<string> OnSaveError;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        public void SaveGame(int slot = 0)
        {
            try
            {
                SaveData saveData = new SaveData();
                
                // Save player data
                saveData.playerData = GetPlayerSaveData();
                
                // Save world data
                saveData.worldData = GetWorldSaveData();
                
                // Save metadata
                saveData.currentLevel = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                saveData.playTime = Time.time;
                saveData.saveTime = DateTime.Now.ToString();
                
                // Create save path with slot
                string slotPath = GetSaveSlotPath(slot);
                
                // Convert to JSON
                string json = JsonUtility.ToJson(saveData, true);
                
                // Encrypt if enabled
                if (useEncryption)
                {
                    json = EncryptString(json);
                }
                
                // Write to file
                File.WriteAllText(slotPath, json);
                
                Debug.Log($"Game saved to slot {slot}: {slotPath}");
                OnGameSaved?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}");
                OnSaveError?.Invoke($"Failed to save game: {e.Message}");
            }
        }
        
        public bool LoadGame(int slot = 0)
        {
            string slotPath = GetSaveSlotPath(slot);
            
            if (!File.Exists(slotPath))
            {
                Debug.LogWarning($"Save file not found for slot {slot}!");
                OnSaveError?.Invoke($"Save file not found for slot {slot}");
                return false;
            }
            
            try
            {
                string json = File.ReadAllText(slotPath);
                
                // Decrypt if enabled
                if (useEncryption)
                {
                    json = DecryptString(json);
                }
                
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);
                
                // Load player data
                LoadPlayerData(saveData.playerData);
                
                // Load world data
                LoadWorldData(saveData.worldData);
                
                // Load level if different
                if (saveData.currentLevel != UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
                {
                    var levelManager = LevelManager.Instance;
                    if (levelManager != null)
                    {
                        levelManager.LoadLevel(saveData.currentLevel);
                    }
                }
                
                Debug.Log($"Game loaded successfully from slot {slot}!");
                OnGameLoaded?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load game: {e.Message}");
                OnSaveError?.Invoke($"Failed to load game: {e.Message}");
                return false;
            }
        }
        
        private PlayerSaveData GetPlayerSaveData()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return new PlayerSaveData();
            
            var playerHealth = player.GetComponent<PlayerHealth>();
            var survivalSystem = player.GetComponent<SurvivalSystem>();
            var inventory = player.GetComponent<PlayerInventory>();
            var weaponManager = player.GetComponent<WeaponManager>();
            
            PlayerSaveData data = new PlayerSaveData
            {
                position = player.transform.position,
                rotation = player.transform.eulerAngles,
                health = playerHealth?.GetCurrentHealth() ?? 100f,
                armor = playerHealth?.GetCurrentArmor() ?? 0f,
                hunger = survivalSystem?.GetHungerPercentage() ?? 1f,
                thirst = survivalSystem?.GetThirstPercentage() ?? 1f,
                stamina = survivalSystem?.GetStaminaPercentage() ?? 1f,
                temperature = survivalSystem?.GetTemperaturePercentage() ?? 0.5f,
                radiation = survivalSystem?.GetRadiationPercentage() ?? 0f,
                inventory = new List<InventoryItemData>(),
                weapons = new List<WeaponSaveData>(),
                currentWeaponIndex = weaponManager?.GetCurrentWeaponIndex() ?? 0
            };
            
            // Save inventory items
            if (inventory != null)
            {
                var items = inventory.GetAllItems();
                foreach (var item in items)
                {
                    data.inventory.Add(new InventoryItemData
                    {
                        itemName = item.itemName,
                        quantity = item.quantity,
                        itemType = item.itemType.ToString()
                    });
                }
            }
            
            // Save weapons
            if (weaponManager != null)
            {
                var weapons = weaponManager.GetAllWeapons();
                foreach (var weapon in weapons)
                {
                    if (weapon != null)
                    {
                        data.weapons.Add(new WeaponSaveData
                        {
                            weaponName = weapon.GetWeaponName(),
                            currentAmmo = weapon.GetCurrentAmmo(),
                            reserveAmmo = weapon.GetReserveAmmo()
                        });
                    }
                }
            }
            
            return data;
        }
        
        private WorldSaveData GetWorldSaveData()
        {
            var levelManager = LevelManager.Instance;
            
            WorldSaveData data = new WorldSaveData
            {
                completedObjectives = new List<string>(),
                activeCheckpoints = new List<CheckpointData>(),
                destroyedObjects = new List<DestructibleObjectData>(),
                lootedContainers = new List<LootableContainerData>()
            };
            
            // Get completed objectives
            if (levelManager != null)
            {
                var currentLevel = levelManager.GetCurrentLevel();
                if (currentLevel != null)
                {
                    foreach (var objective in currentLevel.Objectives)
                    {
                        if (objective.IsCompleted)
                        {
                            data.completedObjectives.Add(objective.Id);
                        }
                    }
                }
            }
            
            // Get destroyed objects
            var destructibleObjects = FindObjectsOfType<DestructibleObject>();
            foreach (var obj in destructibleObjects)
            {
                if (obj.IsDestroyed())
                {
                    data.destroyedObjects.Add(new DestructibleObjectData
                    {
                        objectId = obj.GetInstanceID().ToString(),
                        position = obj.transform.position,
                        isDestroyed = true
                    });
                }
            }
            
            return data;
        }
        
        private void LoadPlayerData(PlayerSaveData data)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            
            // Set position and rotation
            player.transform.position = data.position;
            player.transform.eulerAngles = data.rotation;
            
            // Load health and survival stats
            var playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.SetHealth(data.health);
                playerHealth.SetArmor(data.armor);
            }
            
            var survivalSystem = player.GetComponent<SurvivalSystem>();
            if (survivalSystem != null)
            {
                survivalSystem.SetHunger(data.hunger);
                survivalSystem.SetThirst(data.thirst);
                survivalSystem.SetStamina(data.stamina);
                survivalSystem.SetTemperature(data.temperature);
                survivalSystem.SetRadiation(data.radiation);
            }
            
            // Load inventory
            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.ClearInventory();
                foreach (var item in data.inventory)
                {
                    // Add item to inventory (implementation depends on your inventory system)
                    // inventory.AddItem(item.itemName, item.quantity);
                }
            }
            
            // Load weapons
            var weaponManager = player.GetComponent<WeaponManager>();
            if (weaponManager != null)
            {
                weaponManager.ClearAllWeapons();
                foreach (var weaponData in data.weapons)
                {
                    // Add weapon and set ammo (implementation depends on your weapon system)
                    // weaponManager.AddWeapon(weaponData.weaponName, weaponData.currentAmmo, weaponData.reserveAmmo);
                }
                weaponManager.EquipWeapon(data.currentWeaponIndex);
            }
        }
        
        private void LoadWorldData(WorldSaveData data)
        {
            var levelManager = LevelManager.Instance;
            if (levelManager != null)
            {
                // Restore completed objectives
                foreach (string objectiveId in data.completedObjectives)
                {
                    levelManager.CompleteObjective(objectiveId);
                }
            }
            
            // Restore destroyed objects
            var destructibleObjects = FindObjectsOfType<DestructibleObject>();
            foreach (var destroyedData in data.destroyedObjects)
            {
                foreach (var obj in destructibleObjects)
                {
                    if (Vector3.Distance(obj.transform.position, destroyedData.position) < 0.1f)
                    {
                        obj.ForceDestroy();
                        break;
                    }
                }
            }
        }
        
        public bool HasSaveFile(int slot = 0)
        {
            return File.Exists(GetSaveSlotPath(slot));
        }
        
        public void DeleteSave(int slot = 0)
        {
            string slotPath = GetSaveSlotPath(slot);
            if (File.Exists(slotPath))
            {
                File.Delete(slotPath);
                Debug.Log($"Save file deleted for slot {slot}");
            }
        }
        
        public SaveData GetSaveInfo(int slot = 0)
        {
            string slotPath = GetSaveSlotPath(slot);
            if (!File.Exists(slotPath)) return null;
            
            try
            {
                string json = File.ReadAllText(slotPath);
                if (useEncryption)
                {
                    json = DecryptString(json);
                }
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch
            {
                return null;
            }
        }
        
        private string GetSaveSlotPath(int slot)
        {
            string fileName = $"gamedata_slot{slot}.save";
            return Path.Combine(Application.persistentDataPath, fileName);
        }
        
        private string EncryptString(string input)
        {
            // Simple base64 encoding - replace with proper encryption for production
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
            return System.Convert.ToBase64String(bytes);
        }
        
        private string DecryptString(string input)
        {
            // Simple base64 decoding - replace with proper decryption for production
            byte[] bytes = System.Convert.FromBase64String(input);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
}
