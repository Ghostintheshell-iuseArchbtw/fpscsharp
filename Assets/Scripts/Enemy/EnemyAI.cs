using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Enemy Properties")]
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private float sightRadius = 20f;
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private float fieldOfView = 90f;
    
    [Header("Patrolling")]
    [SerializeField] private bool patrolWhenIdle = true;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolWaitTime = 2f;
    
    [Header("Cover System")]
    [SerializeField] private bool usesCover = true;
    [SerializeField] private float coverSearchRadius = 15f;
    [SerializeField] private LayerMask coverMask;
    [SerializeField] private float minCoverHeight = 1.0f;
    [SerializeField] private float maxDistanceFromTarget = 30f;
    [SerializeField] private float minDistanceFromTarget = 5f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip[] alertSounds;
    [SerializeField] private AudioClip[] attackSounds;
    [SerializeField] private AudioClip[] hurtSounds;
    [SerializeField] private AudioClip[] deathSounds;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    // Components
    private NavMeshAgent navMeshAgent;
    private EnemyHealth health;
    private AudioSource audioSource;
    
    // State management
    private enum EnemyState { Patrolling, Alerted, Attacking, TakingCover, Searching, Dead }
    private EnemyState currentState = EnemyState.Patrolling;
    
    // Target tracking
    private Transform playerTransform;
    private Vector3 lastKnownPlayerPosition;
    private bool canSeePlayer = false;
    private float timeLastSeenPlayer = 0f;
    
    // Cover system variables
    private Transform currentCoverPoint;
    private float coverEvaluationTimer = 0f;
    private float nextCoverEvalTime = 2f;
    
    // Patrolling variables
    private int currentPatrolIndex = 0;
    private bool waitingAtPatrolPoint = false;
    private float patrolWaitTimer = 0f;
    
    // Combat variables
    private float nextAttackTime = 0f;
    private float nextRepositionTime = 0f;
    
    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();
        audioSource = GetComponent<AudioSource>();
        
        // Find the player
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // Set up NavMeshAgent based on enemy data
        if (enemyData != null)
        {
            navMeshAgent.speed = enemyData.moveSpeed;
            navMeshAgent.angularSpeed = enemyData.turnSpeed;
            navMeshAgent.acceleration = enemyData.acceleration;
        }
    }
    
    void Update()
    {
        if (health.IsDead)
        {
            if (currentState != EnemyState.Dead)
            {
                Die();
            }
            return;
        }
        
        // Check if we can see the player
        CheckPlayerVisibility();
        
        // Update state machine
        switch (currentState)
        {
            case EnemyState.Patrolling:
                UpdatePatrolling();
                break;
                
            case EnemyState.Alerted:
                UpdateAlerted();
                break;
                
            case EnemyState.Attacking:
                UpdateAttacking();
                break;
                
            case EnemyState.TakingCover:
                UpdateTakingCover();
                break;
                
            case EnemyState.Searching:
                UpdateSearching();
                break;
        }
        
        // Update animations
        UpdateAnimations();
    }
    
    #region State Updates
    
    private void UpdatePatrolling()
    {
        if (canSeePlayer)
        {
            // Player spotted, become alerted
            ChangeState(EnemyState.Alerted);
            return;
        }
        
        if (!patrolWhenIdle || patrolPoints.Length == 0)
        {
            // Just stand still if we don't patrol
            return;
        }
        
        if (waitingAtPatrolPoint)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitTime)
            {
                waitingAtPatrolPoint = false;
                MoveToNextPatrolPoint();
            }
        }
        else
        {
            // Check if we've reached the patrol point
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.5f)
            {
                waitingAtPatrolPoint = true;
                patrolWaitTimer = 0f;
                
                // Face random direction while waiting
                transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            }
        }
    }
    
    private void UpdateAlerted()
    {
        // Move to the last known player position
        navMeshAgent.SetDestination(lastKnownPlayerPosition);
        
        // Check if player is still visible
        if (canSeePlayer)
        {
            // If within attack range, start attacking
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= attackRange)
            {
                ChangeState(EnemyState.Attacking);
            }
        }
        else
        {
            // If we've reached the last known position and still can't see the player
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.5f)
            {
                ChangeState(EnemyState.Searching);
            }
        }
    }
    
    private void UpdateAttacking()
    {
        if (!canSeePlayer)
        {
            // Lost sight of player
            ChangeState(EnemyState.Alerted);
            return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // If player moved too far, go back to alerted state
        if (distanceToPlayer > attackRange * 1.5f)
        {
            ChangeState(EnemyState.Alerted);
            return;
        }
        
        // Look at player
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        directionToPlayer.y = 0; // Keep on horizontal plane
        Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * enemyData.turnSpeed);
        
        // Attack if cooldown has elapsed
        if (Time.time >= nextAttackTime)
        {
            Attack();
            nextAttackTime = Time.time + enemyData.attackRate;
        }
        
        // Periodically reposition during combat
        if (usesCover && Time.time >= nextRepositionTime)
        {
            // Chance to take cover based on health percentage
            float coverChance = Mathf.Lerp(0.2f, 0.8f, 1 - (health.CurrentHealth / health.MaxHealth));
            
            if (Random.value < coverChance)
            {
                ChangeState(EnemyState.TakingCover);
            }
            else
            {
                // Reposition around the player
                Vector3 newPosition = FindRepositionPoint();
                navMeshAgent.SetDestination(newPosition);
            }
            
            nextRepositionTime = Time.time + Random.Range(3f, 6f);
        }
    }
    
    private void UpdateTakingCover()
    {
        // Evaluate cover periodically
        coverEvaluationTimer += Time.deltaTime;
        
        if (coverEvaluationTimer >= nextCoverEvalTime)
        {
            coverEvaluationTimer = 0f;
            
            // If health has recovered enough or no player visible for a while, leave cover
            if (health.CurrentHealth > health.MaxHealth * 0.7f || 
                Time.time - timeLastSeenPlayer > 5f)
            {
                ChangeState(EnemyState.Alerted);
                return;
            }
            
            // Find new cover if current is not good anymore
            if (canSeePlayer && currentCoverPoint != null)
            {
                // Check if current cover still protects from player
                if (!IsCoverEffective(currentCoverPoint))
                {
                    FindCoverPoint();
                }
            }
        }
        
        // If we have reached cover position and can see player, peek and attack occasionally
        if (currentCoverPoint != null && 
            !navMeshAgent.pathPending && 
            navMeshAgent.remainingDistance < 0.5f && 
            canSeePlayer && 
            Time.time >= nextAttackTime)
        {
            // Peek out and attack
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            directionToPlayer.y = 0;
            transform.rotation = Quaternion.LookRotation(directionToPlayer);
            
            Attack();
            
            nextAttackTime = Time.time + enemyData.attackRate * 1.5f; // Slower attack rate from cover
        }
    }
    
    private void UpdateSearching()
    {
        // If player becomes visible again during search
        if (canSeePlayer)
        {
            ChangeState(EnemyState.Alerted);
            return;
        }
        
        // If we've been searching for too long without seeing the player
        if (Time.time - timeLastSeenPlayer > 10f)
        {
            ChangeState(EnemyState.Patrolling);
            return;
        }
        
        // Check if we've reached the current search point
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.5f)
        {
            // Find a new point to search around the last known position
            Vector3 searchPoint = lastKnownPlayerPosition + Random.insideUnitSphere * 10f;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(searchPoint, out hit, 10f, NavMesh.AllAreas))
            {
                navMeshAgent.SetDestination(hit.position);
            }
        }
    }
    
    #endregion
    
    #region Combat Methods
    
    private void Attack()
    {
        // Play attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        // Play attack sound
        if (attackSounds.Length > 0)
        {
            int index = Random.Range(0, attackSounds.Length);
            audioSource.PlayOneShot(attackSounds[index]);
        }
        
        // Implement your attack logic here. For example:
        // - Ranged attack: Instantiate a projectile
        // - Melee attack: Perform raycast or overlap check
        
        // Example for a ranged attack
        if (enemyData.attackType == EnemyData.AttackType.Ranged)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            
            // Add some inaccuracy based on difficulty
            float inaccuracy = enemyData.attackInaccuracy;
            direction += new Vector3(
                Random.Range(-inaccuracy, inaccuracy),
                Random.Range(-inaccuracy, inaccuracy),
                Random.Range(-inaccuracy, inaccuracy)
            );
            
            // Shoot projectile
            if (enemyData.projectilePrefab != null)
            {
                GameObject projectile = Instantiate(
                    enemyData.projectilePrefab, 
                    transform.position + transform.forward * 1.5f + Vector3.up * 1.5f, 
                    Quaternion.LookRotation(direction)
                );
                
                // Set projectile properties (assuming it has a Projectile component)
                Projectile projectileComponent = projectile.GetComponent<Projectile>();
                if (projectileComponent != null)
                {
                    projectileComponent.Damage = enemyData.attackDamage;
                    projectileComponent.Speed = enemyData.projectileSpeed;
                }
            }
        }
        else if (enemyData.attackType == EnemyData.AttackType.Melee)
        {
            // For melee enemies, check if player is within attack range
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distanceToPlayer <= enemyData.meleeRange)
            {
                // Apply damage to player
                PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(enemyData.attackDamage);
                }
            }
        }
    }
    
    private Vector3 FindRepositionPoint()
    {
        // Find a position around the player to move to
        float distance = Random.Range(minDistanceFromTarget, attackRange);
        float angle = Random.Range(0, 360);
        Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        
        Vector3 targetPosition = playerTransform.position + direction * distance;
        
        // Make sure it's on the navmesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, 5f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        
        // Fall back to current position if no valid point found
        return transform.position;
    }
    
    #endregion
    
    #region Cover System
    
    private void FindCoverPoint()
    {
        if (!usesCover || playerTransform == null) return;
        
        // Get potential cover objects within radius
        Collider[] colliders = Physics.OverlapSphere(transform.position, coverSearchRadius, coverMask);
        
        float bestCoverScore = 0f;
        Transform bestCoverPoint = null;
        
        foreach (Collider col in colliders)
        {
            // Skip small objects that wouldn't provide good cover
            if (col.bounds.size.y < minCoverHeight) continue;
            
            // Get multiple points around the cover object
            Vector3[] coverPositions = GetCoverPositionsAroundObject(col);
            
            foreach (Vector3 position in coverPositions)
            {
                float coverScore = EvaluateCoverPosition(position, col);
                
                if (coverScore > bestCoverScore)
                {
                    NavMeshHit hit;
                    // Make sure the position is on the navmesh
                    if (NavMesh.SamplePosition(position, out hit, 2f, NavMesh.AllAreas))
                    {
                        bestCoverScore = coverScore;
                        bestCoverPoint = col.transform;
                        
                        // Set destination to this cover position
                        navMeshAgent.SetDestination(hit.position);
                    }
                }
            }
        }
        
        currentCoverPoint = bestCoverPoint;
        
        // If no cover found, fallback to evasive movement
        if (currentCoverPoint == null)
        {
            navMeshAgent.SetDestination(FindRepositionPoint());
        }
    }
    
    private Vector3[] GetCoverPositionsAroundObject(Collider coverCollider)
    {
        // Generate positions around the cover object to test
        List<Vector3> positions = new List<Vector3>();
        Vector3 center = coverCollider.bounds.center;
        center.y = transform.position.y; // Keep at enemy's height level
        
        // Get 8 positions around the cover
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            float distance = coverCollider.bounds.extents.magnitude + 1f; // 1 meter away from edge
            
            positions.Add(center + direction * distance);
        }
        
        return positions.ToArray();
    }
    
    private float EvaluateCoverPosition(Vector3 position, Collider coverCollider)
    {
        if (playerTransform == null) return 0f;
        
        float score = 0f;
        
        // Check distance from player (prefer positions farther from player but not too far)
        float distanceToPlayer = Vector3.Distance(position, playerTransform.position);
        if (distanceToPlayer < minDistanceFromTarget || distanceToPlayer > maxDistanceFromTarget)
        {
            return 0f; // Invalid position
        }
        
        // Distance score (medium distance is best)
        float normalizedDistance = Mathf.Clamp01((distanceToPlayer - minDistanceFromTarget) / 
                                                (maxDistanceFromTarget - minDistanceFromTarget));
        float distanceScore = 1f - Mathf.Abs(normalizedDistance - 0.5f) * 2f; // 1.0 at middle distance, 0 at extremes
        score += distanceScore * 3f;
        
        // Check if position is protected from player line of sight
        Vector3 directionToPlayer = (playerTransform.position - position).normalized;
        Ray ray = new Ray(position + Vector3.up * 1f, directionToPlayer);
        
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistanceFromTarget))
        {
            if (hit.collider == coverCollider)
            {
                // Cover blocks line of sight to player
                score += 5f;
            }
        }
        
        // Check for accessibility
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(position, out navHit, 1f, NavMesh.AllAreas))
        {
            score += 2f;
        }
        else
        {
            return 0f; // Not accessible
        }
        
        return score;
    }
    
    private bool IsCoverEffective(Transform coverTransform)
    {
        if (playerTransform == null || coverTransform == null) return false;
        
        // Cast a ray from current position to player
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Ray ray = new Ray(transform.position + Vector3.up * 1f, directionToPlayer);
        
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistanceFromTarget))
        {
            // Check if the cover object is what's blocking our view
            if (hit.transform == coverTransform)
            {
                return true;
            }
        }
        
        return false;
    }
    
    #endregion
    
    #region Patrolling
    
    private void MoveToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        
        // Set destination to the next patrol point
        navMeshAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
        
        // Update index for next patrol point
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }
    
    #endregion
    
    #region Perception
    
    private void CheckPlayerVisibility()
    {
        if (playerTransform == null) return;
        
        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Player is too far to be seen
        if (distanceToPlayer > sightRadius)
        {
            canSeePlayer = false;
            return;
        }
        
        // Check if player is within field of view
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        
        if (angle > fieldOfView * 0.5f)
        {
            canSeePlayer = false;
            return;
        }
        
        // Line of sight check (are there obstacles blocking view?)
        Ray ray = new Ray(transform.position + Vector3.up * 1.5f, directionToPlayer);
        
        if (Physics.Raycast(ray, out RaycastHit hit, sightRadius))
        {
            if (hit.transform == playerTransform || 
                hit.transform.CompareTag("Player"))
            {
                // Player is visible
                canSeePlayer = true;
                lastKnownPlayerPosition = playerTransform.position;
                timeLastSeenPlayer = Time.time;
                
                // If we just spotted the player
                if (currentState == EnemyState.Patrolling)
                {
                    OnPlayerSpotted();
                }
            }
            else
            {
                canSeePlayer = false;
            }
        }
        else
        {
            canSeePlayer = false;
        }
    }
    
    private void OnPlayerSpotted()
    {
        // Play alert sound
        if (alertSounds.Length > 0)
        {
            int index = Random.Range(0, alertSounds.Length);
            audioSource.PlayOneShot(alertSounds[index]);
        }
    }
    
    #endregion
    
    #region State Management
    
    private void ChangeState(EnemyState newState)
    {
        // Exit actions for current state
        switch (currentState)
        {
            case EnemyState.TakingCover:
                // Reset cover timer when leaving cover state
                coverEvaluationTimer = 0f;
                break;
                
            case EnemyState.Attacking:
                // Reset attack timer when leaving attack state
                nextAttackTime = Time.time + 1f;
                break;
        }
        
        // Enter actions for new state
        switch (newState)
        {
            case EnemyState.Patrolling:
                // Start patrolling if idle
                if (patrolWhenIdle && patrolPoints.Length > 0)
                {
                    MoveToNextPatrolPoint();
                }
                else
                {
                    navMeshAgent.ResetPath();
                }
                break;
                
            case EnemyState.Alerted:
                // Move to last known player position
                navMeshAgent.SetDestination(lastKnownPlayerPosition);
                break;
                
            case EnemyState.TakingCover:
                // Find a cover point
                FindCoverPoint();
                break;
                
            case EnemyState.Searching:
                // Initial search point is last known position
                navMeshAgent.SetDestination(lastKnownPlayerPosition);
                break;
        }
        
        currentState = newState;
    }
    
    #endregion
    
    #region Animation & Audio
    
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        // Update animator parameters based on state and movement
        animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
        animator.SetBool("InCombat", currentState == EnemyState.Attacking || currentState == EnemyState.TakingCover);
        animator.SetBool("IsAlerted", currentState != EnemyState.Patrolling);
    }
    
    public void PlayHurtSound()
    {
        if (hurtSounds.Length > 0)
        {
            int index = Random.Range(0, hurtSounds.Length);
            audioSource.PlayOneShot(hurtSounds[index]);
        }
    }
    
    private void Die()
    {
        // Update state
        currentState = EnemyState.Dead;
        
        // Stop movement
        navMeshAgent.isStopped = true;
        navMeshAgent.enabled = false;
        
        // Play death animation
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        
        // Play death sound
        if (deathSounds.Length > 0)
        {
            int index = Random.Range(0, deathSounds.Length);
            audioSource.PlayOneShot(deathSounds[index]);
        }
        
        // Disable colliders
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        // Optional: Add physics to ragdoll if implemented
        
        // Destroy after delay or keep as corpse
        Destroy(gameObject, 10f);
    }
    
    #endregion
    
    // Visual debugging
    private void OnDrawGizmosSelected()
    {
        // Draw sight radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireFrame(transform.position, sightRadius);
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireFrame(transform.position, attackRange);
        
        // Draw field of view
        Gizmos.color = Color.blue;
        float halfFOV = fieldOfView * 0.5f;
        Vector3 leftRayDirection = Quaternion.Euler(0, -halfFOV, 0) * transform.forward;
        Vector3 rightRayDirection = Quaternion.Euler(0, halfFOV, 0) * transform.forward;
        Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, leftRayDirection * sightRadius);
        Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, rightRayDirection * sightRadius);
    }
}

[System.Serializable]
public class EnemyData : ScriptableObject
{
    [Header("General")]
    public string enemyName;
    public float maxHealth = 100f;
    
    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float turnSpeed = 120f;
    public float acceleration = 8f;
    
    [Header("Attack")]
    public enum AttackType { Melee, Ranged }
    public AttackType attackType = AttackType.Ranged;
    public float attackDamage = 10f;
    public float attackRate = 2f; // Time between attacks
    
    [Header("Ranged Attack")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;
    public float attackInaccuracy = 0.1f; // Higher = less accurate
    
    [Header("Melee Attack")]
    public float meleeRange = 2f;
}
