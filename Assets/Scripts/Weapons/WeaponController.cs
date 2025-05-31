using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private WeaponData currentWeapon;
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private Transform cameraTransform;
    
    [Header("Shooting")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private LayerMask shootableMask;
    [SerializeField] private GameObject bulletHolePrefab;
    [SerializeField] private GameObject bloodSplatPrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private float bulletHoleLifetime = 30f;
    
    [Header("Melee Settings")]
    [SerializeField] private float meleeSwingSpeed = 1.0f;
    [SerializeField] private float throwingForce = 20f;
    [SerializeField] private GameObject meleeSwingEffectPrefab;
    
    [Header("Weapon Sway")]
    [SerializeField] private float swayAmount = 0.02f;
    [SerializeField] private float swaySpeed = 5f;
    [SerializeField] private float returnSpeed = 50f;
    [SerializeField] private float aimSwayMultiplier = 0.3f;
    
    [Header("Recoil")]
    [SerializeField] private float verticalRecoil = 1f;
    [SerializeField] private float horizontalRecoil = 0.5f;
    [SerializeField] private float recoilDuration = 0.1f;
    [SerializeField] private float aimRecoilMultiplier = 0.7f;
    
    // Components
    private Animator weaponAnimator;
    private AudioSource audioSource;
    private PlayerInput playerInput;
    private GameObject currentWeaponModel;
    
    // Input actions
    private InputAction shootAction;
    private InputAction reloadAction;
    private InputAction aimAction;
    private InputAction switchWeaponAction;
    
    // State variables
    private bool isReloading = false;
    private bool isAiming = false;
    private float currentAmmo;
    private int totalAmmo = 150; // Total reserve ammo
    private float timeSinceLastShot = 0f;
    private Vector3 weaponInitialPosition;
    private Quaternion weaponInitialRotation;
    private Vector3 targetWeaponRotation;
    private Vector3 targetWeaponPosition;
    
    // UI reference
    private UIManager uiManager;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        playerInput = GetComponent<PlayerInput>();
        
        // Setup input actions
        shootAction = playerInput.actions["Shoot"];
        reloadAction = playerInput.actions["Reload"];
        aimAction = playerInput.actions["Aim"];
        switchWeaponAction = playerInput.actions["SwitchWeapon"];
    }
    
    private void Start()
    {
        if (currentWeapon != null && weaponHolder != null)
        {
            EquipWeapon(currentWeapon);
        }
        
        // Find UI Manager
        uiManager = FindObjectOfType<UIManager>();
    }
    
    public string GetWeaponName()
    {
        return currentWeapon != null ? currentWeapon.weaponName : "None";
    }
    
    public Sprite GetWeaponIcon()
    {
        return currentWeapon != null ? currentWeapon.weaponIcon : null;
    }
    
    public int GetCurrentAmmo()
    {
        return Mathf.RoundToInt(currentAmmo);
    }
    
    public int GetReserveAmmo()
    {
        return totalAmmo;
    }
    
    public WeaponData GetCurrentWeapon()
    {
        return currentWeapon;
    }
    
    public WeaponData GetWeaponData()
    {
        return currentWeapon;
    }
    
    public bool IsReloading()
    {
        return isReloading;
    }
    
    public void SetAmmo(int current, int reserve)
    {
        currentAmmo = Mathf.Min(current, currentWeapon.magazineSize);
        totalAmmo = reserve;
    }
    
    public void AddAmmo(int amount)
    {
        totalAmmo = Mathf.Min(totalAmmo + amount, currentWeapon.reserveAmmoMax);
    }
    
    public void ClearWeapon()
    {
        // Clear the current weapon
        if (currentWeaponModel != null)
        {
            Destroy(currentWeaponModel);
        }
        
        currentWeapon = null;
        currentAmmo = 0;
        totalAmmo = 0;
    }
    
    private void Update()
    {
        if (currentWeapon == null) return;
        
        // Update timers
        timeSinceLastShot += Time.deltaTime;
        
        // Handle weapon input
        HandleShooting();
        HandleReload();
        HandleAiming();
        HandleWeaponSwitching();
        
        // Update weapon positioning (sway)
        UpdateWeaponSway();
    }
    
    private void EquipWeapon(WeaponData weapon)
    {
        // Destroy current weapon if one exists
        if (currentWeaponModel != null)
        {
            Destroy(currentWeaponModel);
        }
        
        currentWeapon = weapon;
        
        // Instantiate new weapon model
        currentWeaponModel = Instantiate(weapon.weaponPrefab, weaponHolder);
        currentWeaponModel.transform.localPosition = weapon.positionOffset;
        currentWeaponModel.transform.localRotation = Quaternion.Euler(weapon.rotationOffset);
        
        // Set initial references
        weaponInitialPosition = currentWeaponModel.transform.localPosition;
        weaponInitialRotation = currentWeaponModel.transform.localRotation;
        
        // Get animator if available
        weaponAnimator = currentWeaponModel.GetComponent<Animator>();
        
        // Reset ammo
        currentAmmo = weapon.magazineSize;
        
        // Reset state
        isReloading = false;
        isAiming = false;
    }
    
    private void HandleShooting()
    {
        // Cannot shoot while reloading
        if (isReloading) return;
        
        // Auto or semi-auto firing logic
        bool canShoot = (currentWeapon.isAutomatic && shootAction.IsPressed()) || 
                        (!currentWeapon.isAutomatic && shootAction.WasPressedThisFrame());
        
        if (canShoot && timeSinceLastShot >= currentWeapon.fireRate && currentAmmo > 0)
        {
            // Reset timer
            timeSinceLastShot = 0f;
            
            // Handle different weapon types
            if (currentWeapon.weaponType == WeaponType.Melee)
            {
                MeleeAttack();
            }
            else
            {
                Shoot();
            }
            
            // Apply recoil
            ApplyRecoil();
            
            // Decrement ammo if not melee
            if (currentWeapon.weaponType != WeaponType.Melee)
            {
                currentAmmo--;
            }
            
            // Update UI
            if (uiManager != null)
            {
                uiManager.UpdateAmmoUI(Mathf.RoundToInt(currentAmmo), totalAmmo);
            }
            
            // Auto-reload if out of ammo
            if (currentAmmo <= 0 && currentWeapon.weaponType != WeaponType.Melee)
            {
                StartCoroutine(Reload());
            }
        }
        
        // Handle thrown weapons (alt fire for melee)
        if (currentWeapon.weaponType == WeaponType.Melee && aimAction.WasPressedThisFrame())
        {
            ThrowMeleeWeapon();
        }
    }
    
    private void MeleeAttack()
    {
        // Play swing animation
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("Attack");
        }
        
        // Play sound
        if (currentWeapon.shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(currentWeapon.shootSound);
        }
        
        // Spawn melee projectile (hit detection)
        if (currentWeapon.projectilePrefab != null && firePoint != null)
        {
            // Create projectile for melee hit detection
            GameObject projectileObj = objectPool != null 
                ? objectPool.SpawnFromPool("MeleeProjectile", firePoint.position, firePoint.rotation) 
                : Instantiate(currentWeapon.projectilePrefab, firePoint.position, firePoint.rotation);
            
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                // Configure projectile
                projectile.Initialize(
                    firePoint.position,
                    firePoint.rotation,
                    currentWeapon.damage,
                    0f, // No speed for melee
                    0f, // No gravity for melee
                    gameObject,
                    true, // Is melee
                    false // Not thrown
                );
            }
        }
        
        // Show melee swing effect
        if (meleeSwingEffectPrefab != null)
        {
            GameObject swingEffect = Instantiate(meleeSwingEffectPrefab, firePoint.position, firePoint.rotation);
            swingEffect.transform.SetParent(firePoint);
            Destroy(swingEffect, 0.5f);
        }
    }
    
    private void ThrowMeleeWeapon()
    {
        // Only throw if we have ammo or if melee weapon doesn't use ammo
        if (currentWeapon.weaponType != WeaponType.Melee || currentAmmo <= 0) return;
        
        // Play throw animation
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("Throw");
        }
        
        // Play sound
        if (currentWeapon.altFireSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(currentWeapon.altFireSound);
        }
        
        // Spawn thrown weapon projectile
        if (currentWeapon.projectilePrefab != null && firePoint != null)
        {
            // Create projectile for thrown weapon
            GameObject projectileObj = objectPool != null 
                ? objectPool.SpawnFromPool("ThrownWeapon", firePoint.position, firePoint.rotation) 
                : Instantiate(currentWeapon.projectilePrefab, firePoint.position, firePoint.rotation);
            
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                // Configure projectile
                projectile.Initialize(
                    firePoint.position,
                    firePoint.rotation,
                    currentWeapon.damage * 1.5f, // More damage when thrown
                    throwingForce,
                    1f, // Normal gravity
                    gameObject,
                    true, // Is melee
                    true  // Is thrown
                );
            }
            
            // Hide weapon model temporarily after throwing
            if (currentWeaponModel != null)
            {
                currentWeaponModel.SetActive(false);
                StartCoroutine(ShowWeaponAfterDelay(1f));
            }
            
            // Decrement ammo for thrown weapons
            if (currentWeapon.usesAmmo)
            {
                currentAmmo--;
                
                // Update UI
                if (uiManager != null)
                {
                    uiManager.UpdateAmmoUI(Mathf.RoundToInt(currentAmmo), totalAmmo);
                }
            }
        }
    }
    
    private IEnumerator ShowWeaponAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (currentWeaponModel != null)
        {
            currentWeaponModel.SetActive(true);
        }
    }
        {
            Shoot();
        }
    }
    
    private void Shoot()
    {
        timeSinceLastShot = 0f;
        currentAmmo--;
        
        // Play shoot animation
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("Shoot");
        }
        
        // Play sound
        if (currentWeapon.shootSound != null)
        {
            audioSource.PlayOneShot(currentWeapon.shootSound);
        }
        
        // Show muzzle flash
        if (muzzleFlashPrefab != null && firePoint != null)
        {
            GameObject muzzleFlash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);
            Destroy(muzzleFlash, 0.05f);
        }
        
        // Apply recoil
        ApplyRecoil();
        
        // Perform raycast for hit detection
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, currentWeapon.range, shootableMask))
        {
            // Check if we hit an enemy
            EnemyHealth enemyHealth = hit.transform.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // Apply damage to enemy
                enemyHealth.TakeDamage(currentWeapon.damage, hit.point, -hit.normal);
                
                // Spawn blood effect
                if (bloodSplatPrefab != null)
                {
                    GameObject blood = Instantiate(bloodSplatPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(blood, bulletHoleLifetime);
                }
                
                // Show hit marker in UI
                if (uiManager != null)
                {
                    uiManager.ShowHitMarker();
                }
            }
            else
            {
                // Spawn bullet hole on environment
                if (bulletHolePrefab != null)
                {
                    GameObject bulletHole = Instantiate(bulletHolePrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    bulletHole.transform.position += bulletHole.transform.forward * 0.01f; // Offset to prevent z-fighting
                    Destroy(bulletHole, bulletHoleLifetime);
                }
            }
        }
        
        // Update UI ammo display
        uiManager?.UpdateAmmoDisplay(currentAmmo, totalAmmo);
    }
    
    private void ApplyRecoil()
    {
        // Calculate recoil amounts (with reduction when aiming)
        float recoilMultiplier = isAiming ? aimRecoilMultiplier : 1f;
        float vRecoil = verticalRecoil * recoilMultiplier * Random.Range(0.7f, 1.0f);
        float hRecoil = horizontalRecoil * recoilMultiplier * Random.Range(-1.0f, 1.0f);
        
        // Apply to camera rotation
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Ideally you would have a public method on PlayerController to handle recoil
            // This is a placeholder for integration with your player controller
            StartCoroutine(RecoilCoroutine(vRecoil, hRecoil));
        }
    }
    
    private IEnumerator RecoilCoroutine(float verticalAmount, float horizontalAmount)
    {
        float time = 0;
        while (time < recoilDuration)
        {
            // Apply recoil to camera
            cameraTransform.localRotation *= Quaternion.Euler(-verticalAmount * Time.deltaTime / recoilDuration, 
                                                             horizontalAmount * Time.deltaTime / recoilDuration, 0);
            
            time += Time.deltaTime;
            yield return null;
        }
    }
    
    private void HandleReload()
    {
        if (reloadAction.WasPressedThisFrame() && !isReloading && currentAmmo < currentWeapon.magazineSize)
        {
            StartCoroutine(ReloadWeapon());
        }
    }
    
    private IEnumerator ReloadWeapon()
    {
        isReloading = true;
        
        // Play reload animation
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("Reload");
        }
        
        // Play reload sound
        if (currentWeapon.reloadSound != null)
        {
            audioSource.PlayOneShot(currentWeapon.reloadSound);
        }
        
        // Wait for reload time
        yield return new WaitForSeconds(currentWeapon.reloadTime);
        
        // Calculate ammo to refill
        float ammoNeeded = currentWeapon.magazineSize - currentAmmo;
        float ammoToUse = Mathf.Min(ammoNeeded, totalAmmo);
        
        // Refill ammo from reserves
        currentAmmo += ammoToUse;
        totalAmmo -= Mathf.RoundToInt(ammoToUse);
        
        isReloading = false;
    }
    
    private void HandleAiming()
    {
        // Toggle aiming state
        if (aimAction.WasPressedThisFrame() && !isReloading)
        {
            isAiming = !isAiming;
        }
        
        // Determine target position based on aiming state
        Vector3 targetPosition = isAiming ? 
            weaponInitialPosition + currentWeapon.aimPositionOffset : 
            weaponInitialPosition;
            
        // Smoothly transition to target position
        currentWeaponModel.transform.localPosition = Vector3.Lerp(
            currentWeaponModel.transform.localPosition, 
            targetPosition, 
            Time.deltaTime * currentWeapon.aimSpeed);
            
        // Adjust field of view when aiming
        Camera playerCamera = cameraTransform.GetComponent<Camera>();
        if (playerCamera != null)
        {
            float targetFOV = isAiming ? currentWeapon.aimFOV : 60f; // Default FOV is usually 60
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * currentWeapon.aimSpeed);
        }
    }
    
    private void HandleWeaponSwitching()
    {
        // Implementation would depend on how you're storing available weapons
        float scrollValue = switchWeaponAction.ReadValue<float>();
        
        // Example implementation for switching weapons with number keys
        if (scrollValue != 0)
        {
            // You would implement weapon switching logic here based on your weapon inventory system
        }
    }
    
    private void UpdateWeaponSway()
    {
        if (currentWeaponModel == null) return;
        
        // Get mouse input for sway
        Vector2 lookInput = playerInput.actions["Look"].ReadValue<Vector2>();
        
        // Calculate target rotation
        targetWeaponRotation = new Vector3(
            -lookInput.y * swayAmount, 
            lookInput.x * swayAmount, 
            0f);
            
        // Apply sway reduction when aiming
        if (isAiming)
        {
            targetWeaponRotation *= aimSwayMultiplier;
        }
        
        // Smoothly interpolate current rotation
        currentWeaponModel.transform.localRotation = Quaternion.Slerp(
            currentWeaponModel.transform.localRotation,
            Quaternion.Euler(weaponInitialRotation.eulerAngles + targetWeaponRotation),
            Time.deltaTime * swaySpeed);
            
        // Natural return to center when no input
        if (lookInput.magnitude < 0.1f)
        {
            currentWeaponModel.transform.localRotation = Quaternion.Slerp(
                currentWeaponModel.transform.localRotation,
                weaponInitialRotation,
                Time.deltaTime * returnSpeed);
        }
    }
}

// Store weapon data in ScriptableObject for easy configuration
[CreateAssetMenu(fileName = "New Weapon", menuName = "FPS/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("General")]
    public string weaponName;
    public GameObject weaponPrefab;
    
    [Header("Positioning")]
    public Vector3 positionOffset;
    public Vector3 rotationOffset;
    public Vector3 aimPositionOffset;
    public float aimFOV = 40f;
    public float aimSpeed = 10f;
    
    [Header("Shooting")]
    public float damage = 10f;
    public float range = 100f;
    public float fireRate = 0.1f; // Time between shots
    public bool isAutomatic = false;
    
    [Header("Ammo")]
    public int magazineSize = 30;
    public float reloadTime = 1.5f;
    
    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
}
