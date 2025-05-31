using UnityEngine;
using System.Collections.Generic;
using FPS.ScriptableObjects;
using FPS.Combat;
using UnityEngine.Events;

namespace FPS.Player
{
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Inventory Settings")]
        [SerializeField] private float maxWeight = 100f;
        [SerializeField] private int maxWeapons = 4;
        [SerializeField] private int maxItems = 10;
        
        // Lists to store inventory items
        private List<LootableItem> inventory = new List<LootableItem>();
        private List<WeaponLootItem> weapons = new List<WeaponLootItem>();
        
        // Current weight of all items
        private float currentWeight = 0f;
        
        // Events
        public UnityEvent<LootableItem> OnItemAdded;
        public UnityEvent<LootableItem> OnItemRemoved;
        public UnityEvent<WeaponLootItem> OnWeaponAdded;
        public UnityEvent<WeaponLootItem> OnWeaponRemoved;
        public UnityEvent<float, float> OnWeightChanged;
        
        // References
        private WeaponManager weaponManager;
        private UIManager uiManager;
        
        private void Awake()
        {
            weaponManager = GetComponent<WeaponManager>();
            uiManager = FindObjectOfType<UIManager>();
        }
        
        public bool AddItem(LootableItem item)
        {
            if (item == null) return false;
            
            // Check if adding this item would exceed max weight
            if (currentWeight + item.ItemWeight > maxWeight)
            {
                // Inventory is too full
                if (uiManager != null)
                {
                    uiManager.ShowNotification("Inventory full - too heavy!");
                }
                return false;
            }
            
            // Handle weapon items specially
            if (item is WeaponLootItem weaponItem)
            {
                // Check if we already have max weapons
                if (weapons.Count >= maxWeapons)
                {
                    if (uiManager != null)
                    {
                        uiManager.ShowNotification("Cannot carry more weapons!");
                    }
                    return false;
                }
                
                // Add weapon to inventory
                weapons.Add(weaponItem);
                inventory.Add(item);
                
                // Update weight
                currentWeight += item.ItemWeight;
                
                // Add to weapon manager
                if (weaponManager != null)
                {
                    weaponManager.AddWeapon(weaponItem.WeaponData, weaponItem.CurrentAmmo);
                }
                
                // Fire events
                OnWeaponAdded?.Invoke(weaponItem);
                OnItemAdded?.Invoke(item);
                OnWeightChanged?.Invoke(currentWeight, maxWeight);
                
                return true;
            }
            else
            {
                // For non-weapon items
                if (inventory.Count >= maxItems + maxWeapons)
                {
                    if (uiManager != null)
                    {
                        uiManager.ShowNotification("Inventory full!");
                    }
                    return false;
                }
                
                // Add item to inventory
                inventory.Add(item);
                
                // Update weight
                currentWeight += item.ItemWeight;
                
                // Fire events
                OnItemAdded?.Invoke(item);
                OnWeightChanged?.Invoke(currentWeight, maxWeight);
                
                return true;
            }
        }
        
        public void RemoveItem(LootableItem item)
        {
            if (item == null) return;
            
            // Check if item exists in inventory
            if (inventory.Contains(item))
            {
                // Handle weapon items
                if (item is WeaponLootItem weaponItem)
                {
                    weapons.Remove(weaponItem);
                    
                    // Remove from weapon manager
                    if (weaponManager != null)
                    {
                        weaponManager.RemoveWeapon(weaponItem.WeaponData);
                    }
                    
                    OnWeaponRemoved?.Invoke(weaponItem);
                }
                
                // Remove from inventory
                inventory.Remove(item);
                
                // Update weight
                currentWeight -= item.ItemWeight;
                
                // Fire events
                OnItemRemoved?.Invoke(item);
                OnWeightChanged?.Invoke(currentWeight, maxWeight);
            }
        }
        
        public void DropItem(LootableItem item)
        {
            if (item == null) return;
            
            // Check if item exists in inventory
            if (inventory.Contains(item))
            {
                // Create world model of item at player's position
                if (item.WorldModelPrefab != null)
                {
                    Vector3 dropPosition = transform.position + transform.forward * 1f + Vector3.up * 0.5f;
                    GameObject droppedItem = Instantiate(item.WorldModelPrefab, dropPosition, Quaternion.identity);
                    
                    // Apply item-specific rotation
                    droppedItem.transform.rotation = Quaternion.Euler(item.WorldModelRotation);
                    
                    // Add physics
                    Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
                    if (rb == null)
                    {
                        rb = droppedItem.AddComponent<Rigidbody>();
                    }
                    
                    // Apply drop force
                    rb.AddForce(transform.forward * 3f, ForceMode.Impulse);
                    
                    // Add collider if needed
                    if (droppedItem.GetComponent<Collider>() == null)
                    {
                        droppedItem.AddComponent<BoxCollider>();
                    }
                    
                    // Add interactable component
                    WorldLootItem worldLootItem = droppedItem.AddComponent<WorldLootItem>();
                    worldLootItem.Initialize(item);
                }
                
                // Remove from inventory
                RemoveItem(item);
            }
        }
        
        // Drop an item at a specific position
        public void DropItemAtPosition(LootableItem item, Vector3 position)
        {
            if (item == null) return;
            
            // Remove from inventory first
            if (item.Type == ItemType.Weapon)
            {
                weapons.Remove(item as WeaponLootItem);
                OnWeaponRemoved?.Invoke(item as WeaponLootItem);
            }
            else
            {
                inventory.Remove(item);
                OnItemRemoved?.Invoke(item);
            }
            
            // Update weight
            float oldWeight = currentWeight;
            currentWeight -= item.ItemWeight;
            OnWeightChanged?.Invoke(currentWeight, maxWeight);
            
            // Create physical representation in the world
            GameObject droppedItem = null;
            
            // Handle different item types
            if (item is WeaponLootItem weaponLoot)
            {
                // Create dropped weapon object
                GameObject weaponPrefab = Resources.Load<GameObject>("Prefabs/DroppedWeapon");
                if (weaponPrefab != null)
                {
                    droppedItem = Instantiate(weaponPrefab, position, Quaternion.identity);
                    
                    // Setup dropped weapon component
                    DroppedWeapon droppedWeapon = droppedItem.GetComponent<DroppedWeapon>();
                    if (droppedWeapon != null)
                    {
                        droppedWeapon.Initialize(weaponLoot.WeaponData, weaponLoot.CurrentAmmo, weaponLoot.ReserveAmmo);
                    }
                }
            }
            else if (item is ConsumableLootItem consumableLoot)
            {
                // Create dropped consumable object
                GameObject consumablePrefab = Resources.Load<GameObject>("Prefabs/DroppedConsumable");
                if (consumablePrefab != null)
                {
                    droppedItem = Instantiate(consumablePrefab, position, Quaternion.identity);
                    
                    // Setup consumable component
                    ConsumableItem consumable = droppedItem.GetComponent<ConsumableItem>();
                    if (consumable != null)
                    {
                        // Initialize with data from loot item
                        consumable.InitializeFromData(consumableLoot.GetItemData());
                    }
                }
            }
            // Add other item types as needed
            
            // Apply physics to dropped item
            if (droppedItem != null)
            {
                Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Add upward force
                    rb.AddForce(Vector3.up * 2f, ForceMode.Impulse);
                    
                    // Add random rotation
                    rb.AddTorque(Random.insideUnitSphere, ForceMode.Impulse);
                }
            }
        }
        
        // Getters
        public List<LootableItem> GetInventory() => inventory;
        public List<WeaponLootItem> GetWeapons() => weapons;
        public float GetCurrentWeight() => currentWeight;
        public float GetMaxWeight() => maxWeight;
        
        // Inventory helpers for crafting system
        public int CountItemsOfType(ItemType itemType)
        {
            int count = 0;
            
            foreach (LootableItem item in inventory)
            {
                // Check if item matches the type we're looking for
                if (item is ConsumableLootItem consumable && TypeMatchesConsumable(consumable, itemType))
                {
                    count++;
                }
                else if (item is WeaponLootItem weapon && TypeMatchesWeapon(weapon, itemType))
                {
                    count++;
                }
            }
            
            return count;
        }
        
        public void RemoveItemsOfType(ItemType itemType, int amount)
        {
            if (amount <= 0) return;
            
            // Create a temporary list of matching items
            List<LootableItem> matchingItems = new List<LootableItem>();
            
            foreach (LootableItem item in inventory)
            {
                // Check if item matches the type we're looking for
                if (item is ConsumableLootItem consumable && TypeMatchesConsumable(consumable, itemType))
                {
                    matchingItems.Add(item);
                }
                else if (item is WeaponLootItem weapon && TypeMatchesWeapon(weapon, itemType))
                {
                    matchingItems.Add(item);
                }
                
                // If we have enough matching items, stop searching
                if (matchingItems.Count >= amount)
                {
                    break;
                }
            }
            
            // Remove the required number of items
            int itemsToRemove = Mathf.Min(amount, matchingItems.Count);
            
            for (int i = 0; i < itemsToRemove; i++)
            {
                RemoveItem(matchingItems[i]);
            }
        }
        
        // Helper methods for item type matching
        private bool TypeMatchesConsumable(ConsumableLootItem consumable, ItemType type)
        {
            if (consumable == null) return false;
            
            switch (type)
            {
                case ItemType.Bandage:
                    return consumable.ItemName.Contains("Bandage");
                
                case ItemType.MedKit:
                    return consumable.ItemName.Contains("Med Kit");
                
                case ItemType.Food:
                    return consumable.ItemName.Contains("Food") || 
                           consumable.ItemName.Contains("Ration") || 
                           consumable.ItemName.Contains("Meat") ||
                           consumable.ItemName.Contains("Canned");
                
                case ItemType.Water:
                    return consumable.ItemName.Contains("Water") || 
                           consumable.ItemName.Contains("Drink") ||
                           consumable.ItemName.Contains("Juice");
                
                default:
                    return false;
            }
        }
    
        private bool TypeMatchesWeapon(WeaponLootItem weapon, ItemType type)
        {
            if (weapon == null) return false;
        
            switch (type)
            {
                case ItemType.Weapon:
                    return true;
                
                case ItemType.Tool:
                    return weapon.WeaponData.weaponType == WeaponType.Melee &&
                           (weapon.ItemName.Contains("Axe") || 
                            weapon.ItemName.Contains("Pickaxe") || 
                            weapon.ItemName.Contains("Hammer"));
                
                default:
                    return false;
            }
        }
        
        // Check if inventory has items of a specific type and amount
        public bool HasItem(ItemType type, int amount)
        {
            int count = 0;
            
            // Count items of the specified type
            foreach (var item in inventory)
            {
                if (item.Type == type)
                {
                    count++;
                    
                    if (count >= amount)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        // Consume items of a specific type
        public bool ConsumeItem(ItemType type, int amount)
        {
            if (!HasItem(type, amount))
            {
                return false;
            }
            
            int remaining = amount;
            
            // Create a copy of the list to iterate safely
            List<LootableItem> itemsToRemove = new List<LootableItem>();
            
            // Find items of the specified type
            foreach (var item in inventory)
            {
                if (item.Type == type)
                {
                    itemsToRemove.Add(item);
                    remaining--;
                    
                    if (remaining <= 0)
                    {
                        break;
                    }
                }
            }
            
            // Remove the items
            foreach (var item in itemsToRemove)
            {
                RemoveItem(item);
            }
            
            return true;
        }
    }
}
