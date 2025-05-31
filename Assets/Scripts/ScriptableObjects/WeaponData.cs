using UnityEngine;

namespace FPS.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "FPS/Weapon Data", order = 1)]
    public class WeaponData : ScriptableObject
    {
        [Header("Weapon Info")]
        public string weaponName = "Default Weapon";
        public GameObject weaponPrefab;
        public GameObject weaponViewModelPrefab;
        public Sprite weaponIcon;
        public WeaponType weaponType = WeaponType.Pistol;
        
        [Header("Weapon Stats")]
        public float damage = 10f;
        public float fireRate = 0.2f;           // Time between shots
        public float range = 100f;              // For hitscan weapons
        public int magazineSize = 30;
        public int reserveAmmoMax = 210;
        public float reloadTime = 2.0f;
        public bool isAutomatic = false;
        
        [Header("Projectile Settings (For non-hitscan weapons)")]
        public bool usesProjectile = true;  // Now all weapons use projectiles by default
        public GameObject projectilePrefab;
        public float projectileSpeed = 20f;
        public float projectileGravity = 1f;    // 0 for no gravity
        public float projectileLifetime = 5f;
        public float projectileSize = 1f;       // Scale multiplier for projectile
        public bool destroyOnImpact = true;     // Should projectile be destroyed on impact
        
        [Header("Melee Weapon Settings")]
        public bool isMelee = false;
        public float meleeRange = 2f;
        public float meleeWidth = 1f;           // For swing arc
        public float meleeAttackDuration = 0.5f;
        public float meleeSwingAngle = 60f;     // Angle of melee swing
        public bool throwableMelee = false;     // Can this melee weapon be thrown
        public float throwSpeed = 10f;          // Speed when thrown
        
        [Header("Recoil Settings")]
        public Vector2 recoilPerShot = new Vector2(1f, 2f);   // X (horizontal) and Y (vertical) recoil
        public float recoilRecoverySpeed = 5f;                // How fast recoil recovers
        public float recoilMaxDistance = 5f;                  // Maximum recoil buildup
        public AnimationCurve recoilPattern;                  // For more advanced recoil patterns
        
        [Header("Accuracy Settings")]
        public float baseSpread = 0.02f;                      // Base inaccuracy
        public float maxSpread = 0.1f;                        // Maximum inaccuracy when firing continuously
        public float spreadIncrease = 0.01f;                  // How much spread increases per shot
        public float spreadRecovery = 0.05f;                  // How fast spread recovers per second
        public float movementSpreadMultiplier = 1.5f;         // How much movement affects spread
        
        [Header("Audio Visual Effects")]
        public AudioClip fireSound;
        public AudioClip reloadSound;
        public AudioClip emptySound;
        public GameObject muzzleFlashPrefab;
        public GameObject impactEffectPrefab;
        
        [Header("Animation Settings")]
        public string idleAnimationTrigger = "Idle";
        public string fireAnimationTrigger = "Fire";
        public string reloadAnimationTrigger = "Reload";
        public string drawAnimationTrigger = "Draw";
        
        [Header("Advanced Settings")]
        public int bulletsPerShot = 1;           // For shotguns
        public float penetrationPower = 0f;      // How many surfaces the bullet can penetrate
        public float aimDownSightsSpeed = 0.2f;  // How fast to transition to ADS
        public float aimDownSightsFOV = 40f;     // FOV when aiming
        public Vector3 aimDownSightsPosition;    // Position offset when aiming
        
        [Header("Special Features")]
        public bool hasAlternateFireMode = false;
        public string alternateFireDescription = "";
        public bool canBeAkimbo = false;         // Can be dual-wielded

        [Header("Loot Settings")]
        public bool canBeLooted = true;          // Can this weapon be looted from enemies
        public float dropChance = 0.5f;          // Chance of dropping when enemy dies
        public bool dropWithAmmo = true;         // Does the weapon drop with ammo
        public float dropAmmoPercent = 0.3f;     // Percent of max ammo to drop with
        public GameObject worldModelPrefab;      // Model shown when weapon is dropped in world
        public Vector3 worldModelRotation;       // Rotation adjustment for world model
        public float itemWeight = 1f;            // Weight in inventory system
    }

    public enum WeaponType
    {
        Pistol,
        SMG,
        AssaultRifle,
        Shotgun,
        Sniper,
        LMG,
        Bow,
        Crossbow,
        Throwable,
        Knife,
        Axe,
        Spear,
        Club,
        Tool,
        Special
    }
}
