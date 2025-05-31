using UnityEngine;
using System.Collections.Generic;

namespace FPS.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "FPS/Enemy Data", order = 2)]
    public class EnemyData : ScriptableObject
    {
        [Header("Enemy Info")]
        public string enemyName = "Default Enemy";
        public GameObject enemyPrefab;
        public EnemyType enemyType = EnemyType.Basic;
        
        [Header("Health Settings")]
        public float maxHealth = 100f;
        public float armorValue = 0f;                 // Damage reduction percentage (0-1)
        public bool hasWeakPoint = false;
        public float weakPointDamageMultiplier = 2f;
        
        [Header("Movement Settings")]
        public float walkSpeed = 2f;
        public float runSpeed = 5f;
        public float patrolSpeed = 1.5f;
        public float rotationSpeed = 5f;
        public float jumpForce = 5f;
        public bool canClimbLadders = true;
        
        [Header("Attack Settings")]
        public List<AttackData> attacks = new List<AttackData>();
        public float aggroRange = 15f;                // Range at which enemy detects player
        public float attackRange = 10f;               // Range at which enemy will attack
        public float attackRate = 2f;                 // Time between attacks
        public float meleeRange = 1.5f;               // Range for melee attacks
        
        [Header("AI Settings")]
        public float sightRange = 20f;                // How far enemy can see
        public float fieldOfView = 110f;              // Enemy FOV in degrees
        public float hearingRange = 15f;              // How far enemy can hear
        public float investigateDuration = 10f;       // How long enemy will investigate a noise
        public float searchDuration = 30f;            // How long enemy will search for player after losing sight
        public float coverSeekingProbability = 0.6f;  // Probability to seek cover when taking damage
        
        [Header("Loot Settings")]
        public List<LootDrop> possibleLoot = new List<LootDrop>();
        public int minLootDrops = 0;
        public int maxLootDrops = 2;
        public float rareDropChance = 0.1f;
        
        [Header("Audio Visual Effects")]
        public List<AudioClip> attackSounds;
        public List<AudioClip> hurtSounds;
        public List<AudioClip> deathSounds;
        public List<AudioClip> idleSounds;
        public List<AudioClip> alertSounds;
        public GameObject deathEffectPrefab;
        
        [Header("Animation Settings")]
        public string idleAnimationTrigger = "Idle";
        public string walkAnimationTrigger = "Walk";
        public string runAnimationTrigger = "Run";
        public string attackAnimationTrigger = "Attack";
        public string hurtAnimationTrigger = "Hurt";
        public string deathAnimationTrigger = "Death";
        
        [Header("Special Features")]
        public bool canSummonAllies = false;          // Can call for reinforcements
        public bool canSelfHeal = false;              // Can heal itself
        public bool isBoss = false;                   // Is this a boss enemy
        public float bossPhaseHealthPercentage = 0.5f; // Health percentage to trigger phase change
    }

    [System.Serializable]
    public class AttackData
    {
        public string attackName = "Default Attack";
        public float damage = 10f;
        public float range = 5f;
        public float cooldown = 2f;
        public bool isRanged = false;
        public GameObject projectilePrefab;
        public float projectileSpeed = 15f;
        public bool isAreaEffect = false;
        public float areaEffectRadius = 3f;
        public AnimationClip attackAnimation;
        public AudioClip attackSound;
        public GameObject attackEffectPrefab;
    }

    [System.Serializable]
    public class LootDrop
    {
        public GameObject lootPrefab;
        public float dropChance = 0.5f;  // 0-1 probability
        public int minAmount = 1;
        public int maxAmount = 1;
        public bool isRareDrop = false;
    }

    public enum EnemyType
    {
        Basic,
        Melee,
        Ranged,
        Armored,
        Fast,
        Flying,
        Boss,
        Special
    }
}
