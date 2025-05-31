using UnityEngine;
using System.Collections;
using FPS.ScriptableObjects;
using FPS.Combat;

public class Projectile : MonoBehaviour
{
    [Header("Base Projectile Properties")]
    [SerializeField] private float speed = 30f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private float maxLifetime = 5f;
    [SerializeField] private float gravityMultiplier = 1f;
    [SerializeField] private float radius = 0.1f;
    [SerializeField] private DamageType damageType = DamageType.Bullet;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private bool destroyOnImpact = true;
    [SerializeField] private float penetrationPower = 0f;
    
    [Header("Explosive Properties")]
    [SerializeField] private bool isExplosive = false;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionForce = 500f;
    [SerializeField] private float explosionDamage = 100f;
    [SerializeField] private float explosionDelay = 0f;
    
    [Header("Melee Properties")]
    [SerializeField] private bool isMelee = false;
    [SerializeField] private float meleeLifetime = 0.2f;
    [SerializeField] private float meleeWidth = 1f;
    [SerializeField] private float meleeSwingAngle = 60f;
    [SerializeField] private float knockbackForce = 2f;
    [SerializeField] private bool isThrown = false;
    
    [Header("Effects")]
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject trailEffectPrefab;
    [SerializeField] private GameObject meleeSwingEffectPrefab;
    [SerializeField] private AudioClip flybySound;
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private AudioClip explosionSound;
    
    // References
    private Rigidbody rb;
    private Transform playerCamera;
    private AudioSource audioSource;
    private ObjectPool objectPool;
    private MeshRenderer meshRenderer;
    private Collider projectileCollider;
    
    // State variables
    private float lifeTimer = 0f;
    private bool hasCollided = false;
    private bool hasExploded = false;
    private Vector3 previousPosition;
    private int penetrationsRemaining;
    private float currentDamage;
    private float baseDamage;
    private GameObject instigator;
    private bool isPooled = false;
    private bool isActive = true;
    
    // For melee weapons
    private float meleeTimer = 0f;
    private HashSet<GameObject> hitObjects = new HashSet<GameObject>();
    
    // Properties that can be set from outside
    public float Damage { get { return damage; } set { damage = value; baseDamage = value; currentDamage = value; } }
    public float Speed { get { return speed; } set { speed = value; } }
    public float PenetrationPower { get { return penetrationPower; } set { penetrationPower = value; penetrationsRemaining = Mathf.RoundToInt(value); } }
    public GameObject Instigator { get { return instigator; } set { instigator = value; } }
    public bool IsPooled { get { return isPooled; } set { isPooled = value; } }
    public bool IsMelee { get { return isMelee; } set { isMelee = value; if(isMelee) maxLifetime = meleeLifetime; } }
    public bool IsThrown { get { return isThrown; } set { isThrown = value; } }
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        meshRenderer = GetComponent<MeshRenderer>();
        projectileCollider = GetComponent<Collider>();
        
        // Find player camera for distance calculations
        playerCamera = Camera.main?.transform;
        
        // Get object pool reference
        objectPool = ObjectPool.Instance;
        
        // Initialize state
        previousPosition = transform.position;
        penetrationsRemaining = Mathf.RoundToInt(penetrationPower);
        currentDamage = damage;
        baseDamage = damage;
    }
    
    public void Initialize(Vector3 position, Quaternion rotation, float newDamage, float newSpeed, 
                          float newGravity, GameObject newInstigator, bool newIsMelee = false, bool newIsThrown = false)
    {
        transform.position = position;
        transform.rotation = rotation;
        previousPosition = position;
        
        damage = newDamage;
        baseDamage = newDamage;
        currentDamage = newDamage;
        speed = newSpeed;
        gravityMultiplier = newGravity;
        instigator = newInstigator;
        isMelee = newIsMelee;
        isThrown = newIsThrown;
        
        // Reset state
        lifeTimer = 0f;
        hasCollided = false;
        hasExploded = false;
        hitObjects.Clear();
        isActive = true;
        
        // For melee weapons, use different lifetime
        if (isMelee && !isThrown)
        {
            maxLifetime = meleeLifetime;
        }
        
        // For thrown weapons, enable physics
        if (isThrown && rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(transform.forward * speed, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse); // Add some spin
        }
        
        // Reset collider and renderer
        if (projectileCollider != null)
        {
            projectileCollider.enabled = true;
        }
        
        if (meshRenderer != null)
        {
            meshRenderer.enabled = true;
        }
        
        // Create melee swing effect if applicable
        if (isMelee && !isThrown && meleeSwingEffectPrefab != null)
        {
            GameObject swingEffect = Instantiate(meleeSwingEffectPrefab, transform.position, transform.rotation);
            swingEffect.transform.SetParent(transform);
            Destroy(swingEffect, meleeLifetime);
        }
        
        // Create trail effect if applicable
        if (trailEffectPrefab != null && !isMelee)
        {
            GameObject trail = Instantiate(trailEffectPrefab, transform);
            Destroy(trail, maxLifetime);
        }
    }
        
        damage = newDamage;
        baseDamage = newDamage;
        currentDamage = newDamage;
        speed = newSpeed;
        gravityMultiplier = newGravity;
        instigator = newInstigator;
        isMelee = newIsMelee;
        isThrown = newIsThrown;
        
        // Reset state
        lifeTimer = 0f;
        meleeTimer = 0f;
        hasCollided = false;
        hasExploded = false;
        isActive = true;
        hitObjects.Clear();
        
        if (isMelee)
        {
            maxLifetime = meleeLifetime;
            
            // Create melee swing effect
            if (meleeSwingEffectPrefab != null)
            {
                Instantiate(meleeSwingEffectPrefab, transform);
            }
        }
        
        // For thrown weapons
        if (isThrown && rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = transform.forward * speed;
            
            // Add some rotation for thrown weapons
            rb.angularVelocity = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f));
        }
        // For standard projectiles
        else if (!isMelee && rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = transform.forward * speed;
        }
        // For melee weapons
        else if (isMelee && rb != null)
        {
            rb.isKinematic = true; // Melee weapons don't use physics
        }
        
        // Create trail effect
        if (trailEffectPrefab != null && !isMelee)
        {
            Instantiate(trailEffectPrefab, transform);
        }
        
        // Make sure everything is visible and enabled
        if (meshRenderer != null) meshRenderer.enabled = true;
        if (projectileCollider != null) projectileCollider.enabled = true;
    }
    
    
    private void Update()
    {
        if (!isActive) return;
        
        // Handle lifetime
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
        {
            if (isMelee)
            {
                // Melee weapons just "expire" after their swing time
                ReturnToPool();
            }
            else if (isExplosive && !hasExploded)
            {
                // Explode when lifetime expires for things like grenades
                Explode();
            }
            else
            {
                // Standard projectiles just get destroyed/recycled
                ReturnToPool();
            }
            return;
        }
        
        // Handle melee weapons differently
        if (isMelee)
        {
            UpdateMeleeWeapon();
            return;
        }
        
        // Apply custom gravity if needed for projectiles
        if (gravityMultiplier != 0 && rb != null && !rb.isKinematic)
        {
            rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
        }
        
        // Cache current position for next frame
        previousPosition = transform.position;
    }
    
    private void UpdateMeleeWeapon()
    {
        // For melee weapons, we need to handle the swinging motion and hit detection
        meleeTimer += Time.deltaTime;
        float swingProgress = meleeTimer / meleeLifetime;
        
        // Swinging logic based on melee type (from weapon data)
        if (swingProgress <= 1f)
        {
            // Perform swing motion based on weapon type
            if (isThrown)
            {
                // For thrown melee weapons, physics is already handled in Initialize
                // Just check for collisions as normal
            }
            else
            {
                // For standard melee weapons, calculate swing arc
                float angle = Mathf.Lerp(0, meleeSwingAngle, swingProgress);
                
                // Create a sweep arc to detect hits
                Vector3 origin = transform.position;
                Vector3 direction = transform.forward;
                float radius = meleeWidth;
                
                // Perform spherecast to detect hits in swing arc
                RaycastHit[] hits = Physics.SphereCastAll(origin, radius, direction, meleeWidth, collisionMask);
                
                foreach (RaycastHit hit in hits)
                {
                    GameObject hitObject = hit.collider.gameObject;
                    
                    // Skip if we've already hit this object
                    if (hitObjects.Contains(hitObject))
                        continue;
                    
                    // Add to hit objects list to prevent multiple hits
                    hitObjects.Add(hitObject);
                    
                    // Apply damage and effects
                    ApplyDamage(hit);
                    SpawnImpactEffect(hit);
                    PlayImpactSound();
                    
                    // Apply knockback force
                    Rigidbody hitRigidbody = hit.collider.GetComponent<Rigidbody>();
                    if (hitRigidbody != null && !hitRigidbody.isKinematic)
                    {
                        Vector3 knockbackDirection = (hit.point - origin).normalized;
                        hitRigidbody.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
                    }
                }
            }
        }
    }
    
    private void FixedUpdate()
    {
        if (!isActive || isMelee || rb == null || rb.isKinematic) return;
        
        // Check for collision between frames using raycast
        // This prevents fast projectiles from passing through thin objects
        if (rb.velocity.magnitude > 0)
        {
            Vector3 movementThisStep = rb.position - previousPosition;
            float movementSqrMagnitude = movementThisStep.sqrMagnitude;
            
            if (movementSqrMagnitude > 0)
            {
                RaycastHit hit;
                if (Physics.Raycast(previousPosition, movementThisStep, out hit, Mathf.Sqrt(movementSqrMagnitude), collisionMask))
                {
                    // We hit something
                    HandleCollision(hit);
                }
            }
        }
    }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.GetContact(0));
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // For trigger-based collisions
        RaycastHit hit;
        if (Physics.Raycast(previousPosition, transform.position - previousPosition, out hit, Vector3.Distance(previousPosition, transform.position), collisionMask))
        {
            HandleCollision(hit);
        }
    }
    
    private void HandleCollision(ContactPoint contact)
    {
        HandleCollision(new RaycastHit
        {
            point = contact.point,
            normal = contact.normal,
            collider = contact.otherCollider
        });
    }
    
    private void HandleCollision(RaycastHit hit)
    {
        if (hasExploded) return;
        
        if (isExplosive)
        {
            Explode(hit.point);
        }
        else
        {
            // Apply damage to hit object if applicable
            ApplyDamage(hit);
            
            // Spawn impact effect
            SpawnImpactEffect(hit);
            
            // Play impact sound
            PlayImpactSound();
        }
        
        // Destroy the projectile
        DestroyProjectile();
    }
    
    private void ApplyDamage(RaycastHit hit)
    {
        // Check if we hit an enemy
        EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
        
        if (enemyHealth != null)
        {
            // Apply damage to enemy
            enemyHealth.TakeDamage(damage, hit.point, -hit.normal);
            
            // Notify UI about hit
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowHitMarker();
            }
        }
        
        // Check if we hit the player
        PlayerHealth playerHealth = hit.collider.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            // Apply damage to player
            playerHealth.TakeDamage(damage, -hit.normal, 2f);
        }
        
        // Apply physics force to rigidbody
        Rigidbody hitRigidbody = hit.collider.GetComponent<Rigidbody>();
        if (hitRigidbody != null && !hitRigidbody.isKinematic)
        {
            // Calculate force based on damage and projectile properties
            float impactForce = damage * 0.5f; // Scale force with damage
            
            // Apply force at impact point
            hitRigidbody.AddForceAtPosition(-hit.normal * impactForce, hit.point, ForceMode.Impulse);
        }
        
        // Check for destructible objects
        IDestructible destructible = hit.collider.GetComponent<IDestructible>();
        if (destructible != null)
        {
            destructible.TakeDamage(damage, hit.point, -hit.normal);
        }
    }
    
    private void Explode(Vector3 position)
    {
        hasExploded = true;
        
        // Spawn explosion effect
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, position, Quaternion.identity);
        }
        
        // Play explosion sound
        if (explosionSound != null && audioSource != null)
        {
            // Detach audio source so it continues playing after destruction
            audioSource.transform.parent = null;
            audioSource.PlayOneShot(explosionSound);
            Destroy(audioSource.gameObject, explosionSound.length);
        }
        
        // Apply damage to objects within radius
        Collider[] colliders = Physics.OverlapSphere(position, explosionRadius, collisionMask);
        
        foreach (Collider collider in colliders)
        {
            // Calculate distance-based damage
            float distance = Vector3.Distance(position, collider.ClosestPoint(position));
            float damageMultiplier = 1f - Mathf.Clamp01(distance / explosionRadius);
            float damageAmount = damage * damageMultiplier;
            
            // Apply damage to enemy
            EnemyHealth enemyHealth = collider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damageAmount, collider.ClosestPoint(position), (collider.transform.position - position).normalized);
            }
            
            // Apply damage to player
            PlayerHealth playerHealth = collider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount, (collider.transform.position - position).normalized, 5f * damageMultiplier);
            }
            
            // Apply physics force
            Rigidbody rb = collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, position, explosionRadius, 1f, ForceMode.Impulse);
            }
        }
    }
    
    private void SpawnImpactEffect(RaycastHit hit)
    {
        if (impactEffectPrefab != null)
        {
            GameObject impactEffect = Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            
            // Scale effect based on damage
            impactEffect.transform.localScale *= Mathf.Clamp(damage / 20f, 0.5f, 3f);
            
            // Destroy after a few seconds
            Destroy(impactEffect, 3f);
        }
    }
    
    private void PlayImpactSound()
    {
        if (impactSound != null && audioSource != null)
        {
            // Detach audio source so it continues playing after destruction
            audioSource.transform.parent = null;
            audioSource.PlayOneShot(impactSound);
            Destroy(audioSource.gameObject, impactSound.length);
        }
    }
    
    private void DestroyProjectile()
    {
        // Return to pool if applicable
        if (isPooled && objectPool != null)
        {
            objectPool.ReturnToPool(gameObject);
        }
        else
        {
            // Destroy this projectile
            Destroy(gameObject);
        }
    }
    
    // For object pooling
    private void ReturnToPool()
    {
        if (isPooled && objectPool != null)
        {
            objectPool.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // For exploding at a point in space (without collision)
    public void Explode()
    {
        Explode(transform.position);
    }
    
    // Gizmos for visualization in editor
    private void OnDrawGizmosSelected()
    {
        if (isExplosive)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
