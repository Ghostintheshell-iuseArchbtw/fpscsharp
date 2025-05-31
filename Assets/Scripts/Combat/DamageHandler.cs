using UnityEngine;
using System.Collections.Generic;

namespace FPS.Combat
{
    // Core damage type definitions
    public enum DamageType
    {
        Bullet,
        Explosion,
        Fire,
        Melee,
        Fall,
        Poison,
        Electric,
        Drowning,
        Crush,
        Unknown
    }
    
    // Damage info struct to pass damage data around
    public struct DamageInfo
    {
        public float Amount;
        public DamageType Type;
        public Vector3 Position;
        public Vector3 Direction;
        public float Force;
        public GameObject Instigator;
        public GameObject DamageCauser;
        
        public DamageInfo(float amount, DamageType type, Vector3 position, Vector3 direction, float force = 0f, GameObject instigator = null, GameObject damageCauser = null)
        {
            Amount = amount;
            Type = type;
            Position = position;
            Direction = direction;
            Force = force;
            Instigator = instigator;
            DamageCauser = damageCauser;
        }
    }
    
    // Interface for anything that can take damage
    public interface IDamageable
    {
        void TakeDamage(DamageInfo damageInfo);
        bool IsDead();
    }
    
    // Class to handle applying damage to objects
    public class DamageHandler : MonoBehaviour
    {
        public static DamageHandler Instance { get; private set; }
        
        [Header("Impact Effects")]
        [SerializeField] private GameObject[] bulletImpactEffects;
        [SerializeField] private GameObject[] explosionEffects;
        [SerializeField] private GameObject[] fireEffects;
        [SerializeField] private GameObject[] meleeImpactEffects;
        [SerializeField] private GameObject[] electricEffects;
        
        [Header("Surface Types")]
        [SerializeField] private List<SurfaceType> surfaceTypes = new List<SurfaceType>();
        
        // Dictionary for quick surface type lookup by material or tag
        private Dictionary<Material, SurfaceType> materialToSurface = new Dictionary<Material, SurfaceType>();
        private Dictionary<string, SurfaceType> tagToSurface = new Dictionary<string, SurfaceType>();
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSurfaceTypeDictionaries();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeSurfaceTypeDictionaries()
        {
            materialToSurface.Clear();
            tagToSurface.Clear();
            
            foreach (var surfaceType in surfaceTypes)
            {
                // Add materials
                foreach (var material in surfaceType.Materials)
                {
                    if (material != null && !materialToSurface.ContainsKey(material))
                    {
                        materialToSurface.Add(material, surfaceType);
                    }
                }
                
                // Add tags
                foreach (var tag in surfaceType.Tags)
                {
                    if (!string.IsNullOrEmpty(tag) && !tagToSurface.ContainsKey(tag))
                    {
                        tagToSurface.Add(tag, surfaceType);
                    }
                }
            }
        }
        
        // Main method to apply damage to a target
        public void ApplyDamage(GameObject target, DamageInfo damageInfo)
        {
            if (target == null) return;
            
            // Find IDamageable components
            IDamageable[] damageables = target.GetComponentsInChildren<IDamageable>();
            
            // If the object can take damage, apply it
            if (damageables.Length > 0)
            {
                foreach (var damageable in damageables)
                {
                    damageable.TakeDamage(damageInfo);
                }
                
                // Apply force if the object has a rigidbody
                ApplyForceToTarget(target, damageInfo);
                
                // Spawn hit effect based on damage type and target surface
                SpawnDamageEffect(target, damageInfo);
            }
            else
            {
                // Object can't take damage, just spawn impact effect
                SpawnImpactEffect(target, damageInfo);
            }
        }
        
        // Specialized method for radius damage (explosions, etc.)
        public void ApplyRadiusDamage(Vector3 center, float radius, float damage, DamageType damageType, GameObject instigator, float falloff = 1f, float minDamage = 0f, float force = 0f)
        {
            // Find all colliders in the radius
            Collider[] colliders = Physics.OverlapSphere(center, radius);
            
            // Keep track of objects we've already damaged to avoid duplicates
            HashSet<GameObject> damagedObjects = new HashSet<GameObject>();
            
            foreach (var collider in colliders)
            {
                GameObject obj = collider.gameObject;
                
                // Skip if we've already damaged this object
                if (damagedObjects.Contains(obj)) continue;
                
                // Skip the instigator to avoid self-damage if specified
                if (obj == instigator) continue;
                
                // Calculate distance from explosion center
                float distance = Vector3.Distance(center, collider.ClosestPoint(center));
                
                // Calculate damage based on distance
                float damageMultiplier = 1f - Mathf.Clamp01(Mathf.Pow(distance / radius, falloff));
                float actualDamage = Mathf.Lerp(minDamage, damage, damageMultiplier);
                
                // Only apply damage if it's significant
                if (actualDamage >= 1f)
                {
                    // Calculate direction for force
                    Vector3 direction = (collider.transform.position - center).normalized;
                    
                    // Create damage info
                    DamageInfo damageInfo = new DamageInfo(
                        actualDamage,
                        damageType,
                        collider.ClosestPoint(center),
                        direction,
                        force * damageMultiplier,
                        instigator,
                        null
                    );
                    
                    // Apply damage
                    ApplyDamage(obj, damageInfo);
                    
                    // Mark as damaged
                    damagedObjects.Add(obj);
                }
            }
        }
        
        // Method to apply physics force to damaged objects
        private void ApplyForceToTarget(GameObject target, DamageInfo damageInfo)
        {
            if (damageInfo.Force <= 0f) return;
            
            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.AddForceAtPosition(
                    damageInfo.Direction * damageInfo.Force,
                    damageInfo.Position,
                    ForceMode.Impulse
                );
            }
        }
        
        // Spawn appropriate damage effect
        private void SpawnDamageEffect(GameObject target, DamageInfo damageInfo)
        {
            // Get the surface type for the target
            SurfaceType surfaceType = GetSurfaceType(target);
            
            // Get appropriate effect based on damage type and surface
            GameObject effectPrefab = GetEffectPrefab(damageInfo.Type, surfaceType);
            
            // Spawn the effect
            if (effectPrefab != null)
            {
                SpawnEffect(effectPrefab, damageInfo.Position, damageInfo.Direction, target.transform);
            }
            
            // Play impact sound
            PlayImpactSound(damageInfo.Type, surfaceType, damageInfo.Position);
        }
        
        // Spawn impact effect for non-damageable objects
        private void SpawnImpactEffect(GameObject target, DamageInfo damageInfo)
        {
            // Get the surface type for the target
            SurfaceType surfaceType = GetSurfaceType(target);
            
            // Get appropriate effect
            GameObject effectPrefab = GetImpactEffectPrefab(damageInfo.Type, surfaceType);
            
            // Spawn the effect
            if (effectPrefab != null)
            {
                SpawnEffect(effectPrefab, damageInfo.Position, damageInfo.Direction, target.transform);
            }
            
            // Play impact sound
            PlayImpactSound(damageInfo.Type, surfaceType, damageInfo.Position);
        }
        
        // Helper to get surface type from a GameObject
        private SurfaceType GetSurfaceType(GameObject target)
        {
            // First try by tag
            if (tagToSurface.TryGetValue(target.tag, out SurfaceType surfaceByTag))
            {
                return surfaceByTag;
            }
            
            // Then try by material
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                if (materialToSurface.TryGetValue(renderer.sharedMaterial, out SurfaceType surfaceByMaterial))
                {
                    return surfaceByMaterial;
                }
            }
            
            // Default to first surface type, or create a generic one if none exist
            return surfaceTypes.Count > 0 ? surfaceTypes[0] : CreateGenericSurfaceType();
        }
        
        // Create a generic surface type as fallback
        private SurfaceType CreateGenericSurfaceType()
        {
            SurfaceType generic = new SurfaceType
            {
                SurfaceName = "Generic",
                Tags = new List<string>(),
                Materials = new List<Material>(),
                BulletImpactEffectPrefab = bulletImpactEffects.Length > 0 ? bulletImpactEffects[0] : null,
                BulletImpactSounds = new List<AudioClip>()
            };
            
            return generic;
        }
        
        // Get appropriate effect prefab based on damage type and surface
        private GameObject GetEffectPrefab(DamageType damageType, SurfaceType surfaceType)
        {
            switch (damageType)
            {
                case DamageType.Bullet:
                    return surfaceType.BulletImpactEffectPrefab;
                case DamageType.Explosion:
                    return surfaceType.ExplosionEffectPrefab;
                case DamageType.Fire:
                    return surfaceType.FireEffectPrefab;
                case DamageType.Melee:
                    return surfaceType.MeleeImpactEffectPrefab;
                case DamageType.Electric:
                    return surfaceType.ElectricEffectPrefab;
                default:
                    return surfaceType.DefaultEffectPrefab;
            }
        }
        
        // Get appropriate impact effect for non-damageable objects
        private GameObject GetImpactEffectPrefab(DamageType damageType, SurfaceType surfaceType)
        {
            return GetEffectPrefab(damageType, surfaceType);
        }
        
        // Play appropriate impact sound
        private void PlayImpactSound(DamageType damageType, SurfaceType surfaceType, Vector3 position)
        {
            List<AudioClip> availableSounds = null;
            
            // Get list of sounds based on damage type
            switch (damageType)
            {
                case DamageType.Bullet:
                    availableSounds = surfaceType.BulletImpactSounds;
                    break;
                case DamageType.Explosion:
                    availableSounds = surfaceType.ExplosionSounds;
                    break;
                case DamageType.Fire:
                    availableSounds = surfaceType.FireSounds;
                    break;
                case DamageType.Melee:
                    availableSounds = surfaceType.MeleeImpactSounds;
                    break;
                default:
                    availableSounds = surfaceType.DefaultImpactSounds;
                    break;
            }
            
            // Play random sound from available sounds
            if (availableSounds != null && availableSounds.Count > 0)
            {
                AudioClip soundToPlay = availableSounds[Random.Range(0, availableSounds.Count)];
                if (soundToPlay != null)
                {
                    AudioSource.PlayClipAtPoint(soundToPlay, position, 1f);
                }
            }
        }
        
        // Spawn effect with correct orientation
        private void SpawnEffect(GameObject effectPrefab, Vector3 position, Vector3 normal, Transform parent = null)
        {
            if (effectPrefab == null) return;
            
            // Create rotation to align with surface normal
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
            
            // Instantiate the effect
            GameObject effect = Instantiate(effectPrefab, position, rotation);
            
            // Optionally parent to hit object
            if (parent != null)
            {
                effect.transform.SetParent(parent);
            }
            
            // Clean up after a delay
            Destroy(effect, 5f);
        }
    }
    
    // Surface type definition
    [System.Serializable]
    public class SurfaceType
    {
        public string SurfaceName = "Default";
        public List<string> Tags = new List<string>();
        public List<Material> Materials = new List<Material>();
        
        [Header("Effects")]
        public GameObject BulletImpactEffectPrefab;
        public GameObject ExplosionEffectPrefab;
        public GameObject FireEffectPrefab;
        public GameObject MeleeImpactEffectPrefab;
        public GameObject ElectricEffectPrefab;
        public GameObject DefaultEffectPrefab;
        
        [Header("Sounds")]
        public List<AudioClip> BulletImpactSounds = new List<AudioClip>();
        public List<AudioClip> ExplosionSounds = new List<AudioClip>();
        public List<AudioClip> FireSounds = new List<AudioClip>();
        public List<AudioClip> MeleeImpactSounds = new List<AudioClip>();
        public List<AudioClip> DefaultImpactSounds = new List<AudioClip>();
        
        [Header("Footsteps")]
        public List<AudioClip> FootstepSounds = new List<AudioClip>();
        
        [Header("Decals")]
        public GameObject BulletHoleDecal;
        public GameObject BloodSplatterDecal;
        public float DecalLifetime = 30f;
    }
}
