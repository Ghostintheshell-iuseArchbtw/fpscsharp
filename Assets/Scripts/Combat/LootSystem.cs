using UnityEngine;
using System.Collections.Generic;
using FPS.ScriptableObjects;

namespace FPS.Combat
{
    public class LootSystem : MonoBehaviour
    {
        [Header("Loot Settings")]
        [SerializeField] private Transform dropPoint;
        [SerializeField] private float dropForce = 3f;
        [SerializeField] private float dropUpwardForce = 2f;
        [SerializeField] private float spreadRadius = 1f;
        
        // List of equipment the enemy is wearing/using
        [SerializeField] private List<LootableItem> equippedItems = new List<LootableItem>();
        
        // Reference to the enemy health component
        private EnemyHealth enemyHealth;
        
        private void Awake()
        {
            enemyHealth = GetComponent<EnemyHealth>();
            
            // If no drop point is specified, use the transform position
            if (dropPoint == null)
            {
                dropPoint = transform;
            }
        }
        
        private void Start()
        {
            // Listen for death event
            if (enemyHealth != null)
            {
                enemyHealth.OnDeath.AddListener(DropLoot);
            }
        }
        
        public void AddLootItem(LootableItem item)
        {
            if (item != null && !equippedItems.Contains(item))
            {
                equippedItems.Add(item);
            }
        }
        
        public void RemoveLootItem(LootableItem item)
        {
            if (item != null && equippedItems.Contains(item))
            {
                equippedItems.Remove(item);
            }
        }
        
        public void DropLoot()
        {
            if (equippedItems.Count == 0) return;
            
            // Calculate drop position based on enemy position
            Vector3 dropPosition = dropPoint.position;
            
            // Drop each equipped item
            foreach (LootableItem item in equippedItems)
            {
                DropItem(item, dropPosition);
                
                // Add some randomness to next drop position
                dropPosition += new Vector3(
                    Random.Range(-spreadRadius, spreadRadius),
                    0f,
                    Random.Range(-spreadRadius, spreadRadius)
                );
            }
            
            // Clear equipped items
            equippedItems.Clear();
        }
        
        private void DropItem(LootableItem item, Vector3 position)
        {
            if (item == null) return;
            
            // Create a physical representation of the item in the world
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
                    // Add random force
                    Vector3 dropDir = new Vector3(
                        Random.Range(-0.5f, 0.5f),
                        1f,
                        Random.Range(-0.5f, 0.5f)
                    ).normalized;
                    
                    rb.AddForce(dropDir * dropForce + Vector3.up * dropUpwardForce, ForceMode.Impulse);
                    
                    // Add random rotation
                    rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
                }
            }
        }
    }
}
                equippedItems.Remove(item);
            }
        }
        
        private void DropLoot()
        {
            foreach (LootableItem item in equippedItems)
            {
                if (item != null && item.CanBeLooted && Random.value <= item.DropChance)
                {
                    SpawnLootInWorld(item);
                }
            }
        }
        
        private void SpawnLootInWorld(LootableItem item)
        {
            // Get the world model prefab
            GameObject worldModel = item.WorldModelPrefab;
            
            if (worldModel != null)
            {
                // Calculate random position within spread radius
                Vector3 randomOffset = Random.insideUnitSphere * spreadRadius;
                randomOffset.y = Mathf.Abs(randomOffset.y); // Ensure it drops above ground
                
                Vector3 dropPosition = dropPoint.position + randomOffset;
                
                // Instantiate the world model
                GameObject droppedItem = Instantiate(worldModel, dropPosition, Quaternion.identity);
                
                // Apply item-specific rotation
                droppedItem.transform.rotation = Quaternion.Euler(item.WorldModelRotation);
                
                // Add physics properties
                Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = droppedItem.AddComponent<Rigidbody>();
                }
                
                // Apply random drop force
                Vector3 dropDirection = new Vector3(
                    Random.Range(-1f, 1f),
                    dropUpwardForce,
                    Random.Range(-1f, 1f)
                ).normalized;
                
                rb.AddForce(dropDirection * dropForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * dropForce, ForceMode.Impulse);
                
                // Add a collider if it doesn't have one
                if (droppedItem.GetComponent<Collider>() == null)
                {
                    droppedItem.AddComponent<BoxCollider>();
                }
                
                // Add the lootable item component to the dropped item
                WorldLootItem worldLootItem = droppedItem.AddComponent<WorldLootItem>();
                worldLootItem.Initialize(item);
            }
        }
    }
    
    // Interface for all lootable items
    public interface ILootable
    {
        bool CanBeLooted { get; }
        float DropChance { get; }
        GameObject WorldModelPrefab { get; }
        Vector3 WorldModelRotation { get; }
        float ItemWeight { get; }
        string ItemName { get; }
        Sprite ItemIcon { get; }
    }
    
    // Base class for lootable items (weapons, armor, etc.)
    [System.Serializable]
    public abstract class LootableItem : ILootable
    {
        public abstract bool CanBeLooted { get; }
        public abstract float DropChance { get; }
        public abstract GameObject WorldModelPrefab { get; }
        public abstract Vector3 WorldModelRotation { get; }
        public abstract float ItemWeight { get; }
        public abstract string ItemName { get; }
        public abstract Sprite ItemIcon { get; }
    }
    
    // Weapon-specific lootable item
    [System.Serializable]
    public class WeaponLootItem : LootableItem
    {
        [SerializeField] private WeaponData weaponData;
        [SerializeField] private int currentAmmo;
        
        public WeaponData WeaponData => weaponData;
        public int CurrentAmmo => currentAmmo;
        
        public WeaponLootItem(WeaponData weapon, int ammo)
        {
            weaponData = weapon;
            
            // Calculate ammo based on weapon's drop settings
            if (weapon.dropWithAmmo)
            {
                currentAmmo = Mathf.RoundToInt(weapon.magazineSize * weapon.dropAmmoPercent);
            }
            else
            {
                currentAmmo = 0;
            }
        }
        
        public override bool CanBeLooted => weaponData.canBeLooted;
        public override float DropChance => weaponData.dropChance;
        public override GameObject WorldModelPrefab => weaponData.worldModelPrefab;
        public override Vector3 WorldModelRotation => weaponData.worldModelRotation;
        public override float ItemWeight => weaponData.itemWeight;
        public override string ItemName => weaponData.weaponName;
        public override Sprite ItemIcon => weaponData.weaponIcon;
    }
    
    // Component for items dropped in the world
    public class WorldLootItem : MonoBehaviour, IInteractable
    {
        private LootableItem lootItem;
        
        public void Initialize(LootableItem item)
        {
            lootItem = item;
        }
        
        public string GetInteractionPrompt()
        {
            return $"Pick up {lootItem.ItemName}";
        }
        
        public void Interact(GameObject player)
        {
            // Add to player's inventory
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                bool added = inventory.AddItem(lootItem);
                
                if (added)
                {
                    // Destroy the world object once picked up
                    Destroy(gameObject);
                }
            }
        }
    }
}
