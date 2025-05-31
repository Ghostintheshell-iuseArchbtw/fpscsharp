using UnityEngine;
using System.Collections.Generic;
using FPS.ScriptableObjects;
using FPS.Audio;

namespace FPS.Weapons
{
    public class WeaponManager : MonoBehaviour
    {
        [Header("Weapon Management")]
        [SerializeField] private List<WeaponData> availableWeapons = new List<WeaponData>();
        [SerializeField] private Transform weaponHolderTransform;
        [SerializeField] private Transform weaponViewModelTransform;
        [SerializeField] private int maxWeaponsCarried = 2;
        
        [Header("Loot Settings")]
        [SerializeField] private float dropForce = 5f;
        [SerializeField] private float dropUpwardForce = 2f;
        
        [Header("Weapon References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private LayerMask aimLayerMask;
        
        private List<WeaponController> equippedWeapons = new List<WeaponController>();
        private WeaponController currentWeapon;
        private int currentWeaponIndex = 0;
        
        private bool isChangingWeapon = false;
        private float weaponSwitchCooldown = 0.5f;
        private float lastWeaponSwitchTime = 0f;
        
        private void Start()
        {
            InitializeWeapons();
        }
        
        private void Update()
        {
            if (currentWeapon == null || isChangingWeapon) return;
            
            // Update current weapon (for ammo, reloading status, etc)
            currentWeapon.UpdateWeapon();
        }
        
        private void InitializeWeapons()
        {
            // Clear any existing weapons
            foreach (WeaponController weapon in equippedWeapons)
            {
                if (weapon != null && weapon.gameObject != null)
                {
                    Destroy(weapon.gameObject);
                }
            }
            
            equippedWeapons.Clear();
            
            // Add starting weapons (limited by maxWeaponsCarried)
            int weaponsToEquip = Mathf.Min(availableWeapons.Count, maxWeaponsCarried);
            
            for (int i = 0; i < weaponsToEquip; i++)
            {
                AddWeapon(availableWeapons[i]);
            }
            
            // Equip first weapon if available
            if (equippedWeapons.Count > 0)
            {
                EquipWeapon(0, true);
            }
        }
        
        public void AddWeapon(WeaponData weaponData)
        {
            // Check if already have max weapons
            if (equippedWeapons.Count >= maxWeaponsCarried)
            {
                // Replace current weapon
                if (currentWeapon != null)
                {
                    Destroy(currentWeapon.gameObject);
                    equippedWeapons.RemoveAt(currentWeaponIndex);
                }
                else
                {
                    // Remove last weapon if current is null
                    if (equippedWeapons.Count > 0)
                    {
                        Destroy(equippedWeapons[equippedWeapons.Count - 1].gameObject);
                        equippedWeapons.RemoveAt(equippedWeapons.Count - 1);
                    }
                }
            }
            
            // Instantiate weapon
            if (weaponData.weaponPrefab != null)
            {
                GameObject weaponObj = Instantiate(weaponData.weaponPrefab, weaponHolderTransform);
                WeaponController weapon = weaponObj.GetComponent<WeaponController>();
                
                if (weapon == null)
                {
                    weapon = weaponObj.AddComponent<WeaponController>();
                }
                
                // Setup weapon
                weapon.InitializeWeapon(weaponData, playerCamera, aimLayerMask);
                
                // Instantiate view model if available
                if (weaponData.weaponViewModelPrefab != null && weaponViewModelTransform != null)
                {
                    GameObject viewModelObj = Instantiate(weaponData.weaponViewModelPrefab, weaponViewModelTransform);
                    weapon.SetViewModel(viewModelObj);
                }
                
                // Add to equipped weapons
                equippedWeapons.Add(weapon);
                
                // Disable initially
                weaponObj.SetActive(false);
                
                // If this is the first weapon, equip it
                if (equippedWeapons.Count == 1)
                {
                    EquipWeapon(0, true);
                }
            }
        }
        
        public void EquipWeapon(int index, bool forceEquip = false)
        {
            // Check if weapon change is on cooldown
            if (!forceEquip && Time.time - lastWeaponSwitchTime < weaponSwitchCooldown)
            {
                return;
            }
            
            // Validate index
            if (index < 0 || index >= equippedWeapons.Count)
            {
                return;
            }
            
            // Don't switch to the same weapon
            if (!forceEquip && index == currentWeaponIndex && currentWeapon != null)
            {
                return;
            }
            
            // Start weapon switching process
            StartCoroutine(SwitchWeaponCoroutine(index));
        }
        
        private System.Collections.IEnumerator SwitchWeaponCoroutine(int newIndex)
        {
            isChangingWeapon = true;
            lastWeaponSwitchTime = Time.time;
            
            // Holster current weapon if any
            if (currentWeapon != null)
            {
                currentWeapon.Holster();
                
                // Wait for holster animation
                float holsterTime = 0.2f; // This could be from the weapon data
                yield return new WaitForSeconds(holsterTime);
                
                currentWeapon.gameObject.SetActive(false);
            }
            
            // Update current weapon index
            currentWeaponIndex = newIndex;
            currentWeapon = equippedWeapons[currentWeaponIndex];
            
            // Equip new weapon
            currentWeapon.gameObject.SetActive(true);
            currentWeapon.Draw();
            
            // Wait for draw animation
            float drawTime = 0.3f; // This could be from the weapon data
            yield return new WaitForSeconds(drawTime);
            
            isChangingWeapon = false;
        }
        
        public void NextWeapon()
        {
            int nextIndex = (currentWeaponIndex + 1) % equippedWeapons.Count;
            EquipWeapon(nextIndex);
        }
        
        public void PreviousWeapon()
        {
            int prevIndex = (currentWeaponIndex - 1 + equippedWeapons.Count) % equippedWeapons.Count;
            EquipWeapon(prevIndex);
        }
        
        public void WeaponSlotInput(int slotNumber)
        {
            // Slot numbers are 1-based in input, but 0-based in our array
            int index = slotNumber - 1;
            if (index >= 0 && index < equippedWeapons.Count)
            {
                EquipWeapon(index);
            }
        }
        
        public WeaponController GetCurrentWeapon()
        {
            return currentWeapon;
        }
        
        // Input handling for weapon actions
        public void OnFireInput(bool isPressed)
        {
            if (currentWeapon == null || isChangingWeapon) return;
            
            if (isPressed)
            {
                currentWeapon.StartFiring();
            }
            else
            {
                currentWeapon.StopFiring();
            }
        }
        
        public void OnReloadInput()
        {
            if (currentWeapon == null || isChangingWeapon) return;
            
            currentWeapon.Reload();
        }
        
        public void OnAimInput(bool isAiming)
        {
            if (currentWeapon == null || isChangingWeapon) return;
            
            currentWeapon.SetAiming(isAiming);
        }
        
        // Interface with the inventory system
        public bool HasAmmoForWeapon(WeaponType weaponType)
        {
            PlayerInventory inventory = GetComponent<PlayerInventory>();
            if (inventory == null) return false;
            
            // Different weapons use different ammunition types
            ItemType ammoType = GetAmmoTypeForWeapon(weaponType);
            
            // Check if player has this ammo type
            return inventory.HasItem(ammoType, 1);
        }
        
        public int GetAmmoForWeapon(WeaponType weaponType, int requestedAmount)
        {
            PlayerInventory inventory = GetComponent<PlayerInventory>();
            if (inventory == null) return 0;
            
            // Different weapons use different ammunition types
            ItemType ammoType = GetAmmoTypeForWeapon(weaponType);
            
            // Count how many ammo items of this type the player has
            int availableAmmo = inventory.CountItemsOfType(ammoType);
            
            // Calculate how much to consume (up to requested amount)
            int ammoToConsume = Mathf.Min(availableAmmo, requestedAmount);
            
            // Consume the ammo from inventory
            if (ammoToConsume > 0)
            {
                inventory.ConsumeItem(ammoType, ammoToConsume);
            }
            
            return ammoToConsume;
        }
        
        private ItemType GetAmmoTypeForWeapon(WeaponType weaponType)
        {
            // Map weapon types to ammunition types
            switch (weaponType)
            {
                case WeaponType.Pistol:
                case WeaponType.SMG:
                    return ItemType.Ammunition; // Light ammo
                    
                case WeaponType.Shotgun:
                    return ItemType.Ammunition; // Shotgun shells
                    
                case WeaponType.AssaultRifle:
                case WeaponType.SniperRifle:
                    return ItemType.Ammunition; // Heavy ammo
                    
                default:
                    return ItemType.Ammunition; // Default ammo type
            }
        }
        
        // Weapon looting and dropping methods
        public void DropCurrentWeapon()
        {
            if (currentWeaponIndex < 0 || currentWeaponIndex >= weapons.Count)
                return;
                
            WeaponController weaponController = GetComponentInChildren<WeaponController>();
            if (weaponController == null || weaponController.GetCurrentWeapon() == null)
                return;
                
            // Get weapon data and ammo
            WeaponData weaponData = weaponController.GetCurrentWeapon();
            int currentAmmo = weaponController.GetCurrentAmmo();
            int reserveAmmo = weaponController.GetReserveAmmo();
            
            // Create drop position in front of player
            Vector3 dropPosition = transform.position + transform.forward * 1f + Vector3.up * 0.5f;
            
            // Create the physical weapon in the world
            GameObject weaponPrefab = Resources.Load<GameObject>("Prefabs/DroppedWeapon");
            if (weaponPrefab != null)
            {
                GameObject droppedWeapon = Instantiate(weaponPrefab, dropPosition, Quaternion.identity);
                
                // Setup dropped weapon component
                DroppedWeapon droppedWeaponComp = droppedWeapon.GetComponent<DroppedWeapon>();
                if (droppedWeaponComp != null)
                {
                    droppedWeaponComp.Initialize(weaponData, currentAmmo, reserveAmmo);
                }
                
                // Apply physics to dropped weapon
                Rigidbody rb = droppedWeapon.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(transform.forward * 3f + Vector3.up * 2f, ForceMode.Impulse);
                    rb.AddTorque(Random.insideUnitSphere * 1f, ForceMode.Impulse);
                }
            }
            
            // Remove the weapon from inventory
            RemoveCurrentWeapon();
        }
        
        public void RemoveCurrentWeapon()
        {
            if (currentWeaponIndex < 0 || currentWeaponIndex >= weapons.Count)
                return;
                
            // Remove the weapon from the list
            weapons.RemoveAt(currentWeaponIndex);
            
            // Update current weapon index
            if (weapons.Count > 0)
            {
                currentWeaponIndex = Mathf.Clamp(currentWeaponIndex, 0, weapons.Count - 1);
                EquipWeapon(currentWeaponIndex);
            }
            else
            {
                // No weapons left
                currentWeaponIndex = -1;
                
                // Clear the weapon controller
                WeaponController weaponController = GetComponentInChildren<WeaponController>();
                if (weaponController != null)
                {
                    weaponController.ClearWeapon();
                }
            }
        }
        
        public void LootWeapon(WeaponData weaponData, int currentAmmo, int reserveAmmo)
        {
            if (weaponData == null) return;
            
            // Check if we have room for another weapon
            if (weapons.Count >= maxWeapons)
            {
                // Drop the current weapon to make room
                DropCurrentWeapon();
            }
            
            // Add the new weapon to the list
            weapons.Add(weaponData);
            
            // Equip the new weapon immediately
            currentWeaponIndex = weapons.Count - 1;
            EquipWeapon(currentWeaponIndex);
            
            // Set the ammo
            WeaponController weaponController = GetComponentInChildren<WeaponController>();
            if (weaponController != null)
            {
                weaponController.SetAmmo(currentAmmo, reserveAmmo);
            }
        }
        
        // UI information getters
        public string GetCurrentWeaponName()
        {
            WeaponController weaponController = GetComponentInChildren<WeaponController>();
            if (weaponController != null && weaponController.GetCurrentWeapon() != null)
            {
                return weaponController.GetCurrentWeapon().weaponName;
            }
            
            return "None";
        }
        
        public Sprite GetCurrentWeaponIcon()
        {
            WeaponController weaponController = GetComponentInChildren<WeaponController>();
            if (weaponController != null && weaponController.GetCurrentWeapon() != null)
            {
                return weaponController.GetCurrentWeapon().weaponIcon;
            }
            
            return null;
        }
        
        public int GetCurrentAmmoInMagazine()
        {
            WeaponController weaponController = GetComponentInChildren<WeaponController>();
            if (weaponController != null)
            {
                return weaponController.GetCurrentAmmo();
            }
            
            return 0;
        }
        
        public int GetCurrentTotalAmmo()
        {
            WeaponController weaponController = GetComponentInChildren<WeaponController>();
            if (weaponController != null)
            {
                return weaponController.GetReserveAmmo();
            }
            
            return 0;
        }
        
        public bool IsReloading()
        {
            return currentWeapon != null && currentWeapon.IsReloading();
        }
    }
}
