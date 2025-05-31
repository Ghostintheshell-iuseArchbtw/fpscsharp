using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float airControl = 0.5f;
    
    [Header("Look Settings")]
    [SerializeField] private float lookSensitivity = 1f;
    [SerializeField] private float maxLookAngle = 85f;
    [SerializeField] private bool invertY = false;
    
    [Header("Audio")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private float footstepInterval = 0.5f;
    
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private AudioSource audioSource;
    
    // Components
    private CharacterController characterController;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    
    // State variables
    private Vector3 moveDirection;
    private Vector3 velocity;
    private float verticalLookRotation;
    private float lastFootstepTime;
    private bool isGrounded;
    private bool wasPreviouslyGrounded;
    private bool isSprinting = false;
    
    // Multipliers for survival system effects
    private float movementMultiplier = 1f;
    private float aimMultiplier = 1f;
    
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        audioSource = GetComponent<AudioSource>();
        
        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Setup input actions
        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        jumpAction = playerInput.actions["Jump"];
        sprintAction = playerInput.actions["Sprint"];
    }
    
    private void Update()
    {
        HandleMovement();
        HandleLooking();
        HandleJumping();
        ApplyGravity();
        CheckGrounding();
        PlayFootstepSounds();
    }
    
    private void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        
        Vector3 forward = transform.forward * input.y;
        Vector3 right = transform.right * input.x;
        
        // Check if sprinting
        isSprinting = sprintAction.IsPressed();
        
        // Handle sprint stamina consumption
        if (isSprinting)
        {
            // Try to use stamina for sprinting
            SurvivalSystem survivalSystem = GetComponent<SurvivalSystem>();
            if (survivalSystem != null)
            {
                // Use stamina per second while sprinting
                float staminaPerFrame = 10f * Time.deltaTime;
                isSprinting = survivalSystem.UseStamina(staminaPerFrame);
            }
        }
        
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        
        // Apply survival system movement multiplier
        currentSpeed *= movementMultiplier;
        
        // Apply reduced control in air
        if (!isGrounded)
        {
            currentSpeed *= airControl;
        }
        
        moveDirection = (forward + right).normalized * currentSpeed;
        
        characterController.Move(moveDirection * Time.deltaTime);
    }
    }
    
    private void HandleLooking()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        
        // Apply survival system aim multiplier
        float adjustedSensitivity = lookSensitivity / aimMultiplier;
        
        // Horizontal rotation (player body)
        transform.Rotate(Vector3.up, lookInput.x * adjustedSensitivity);
        
        // Vertical rotation (camera only)
        float verticalLook = lookInput.y * adjustedSensitivity * (invertY ? 1 : -1);
        verticalLookRotation += verticalLook;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(verticalLookRotation, 0, 0);
    }
    
    private void HandleJumping()
    {
        if (jumpAction.WasPressedThisFrame() && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            
            if (jumpSound != null)
            {
                audioSource.PlayOneShot(jumpSound);
            }
        }
    }
    
    private void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small constant to keep player grounded
        }
        
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
    
    private void CheckGrounding()
    {
        wasPreviouslyGrounded = isGrounded;
        isGrounded = characterController.isGrounded;
        
        // Play landing sound
        if (!wasPreviouslyGrounded && isGrounded && landSound != null)
        {
            audioSource.PlayOneShot(landSound);
        }
    }
    
    private void PlayFootstepSounds()
    {
        if (footstepSounds.Length == 0) return;
        
        // Check if moving on ground and enough time has passed since last footstep
        if (isGrounded && moveDirection.magnitude > 0.1f && Time.time - lastFootstepTime > footstepInterval)
        {
            // Adjust interval based on speed (faster when sprinting)
            float adjustedInterval = sprintAction.IsPressed() 
                ? footstepInterval * 0.6f 
                : footstepInterval;
                
            if (Time.time - lastFootstepTime > adjustedInterval)
            {
                lastFootstepTime = Time.time;
                int index = Random.Range(0, footstepSounds.Length);
                audioSource.PlayOneShot(footstepSounds[index]);
            }
        }
    }
    
    // Getters and setters for survival system
    public bool IsSprinting()
    {
        return isSprinting;
    }
    
    // Public methods for survival system to modify movement
    public void SetMovementMultiplier(float multiplier)
    {
        movementMultiplier = multiplier;
    }
    
    public void SetAimMultiplier(float multiplier)
    {
        aimMultiplier = multiplier;
    }
    
    // Method to handle a melee kick or push (called by WeaponController)
    public void PerformMeleeKick()
    {
        // Cast a ray forward to detect objects that can be kicked
        Ray kickRay = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(kickRay, out hit, 2f))
        {
            // Check if we hit a rigidbody
            Rigidbody hitRb = hit.collider.GetComponentInParent<Rigidbody>();
            if (hitRb != null)
            {
                // Apply kick force
                hitRb.AddForceAtPosition(cameraTransform.forward * 5f, hit.point, ForceMode.Impulse);
            }
            
            // Check if we hit a damageable object
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                // Apply small damage
                DamageInfo kickDamage = new DamageInfo(
                    10f,
                    DamageType.Melee,
                    hit.point,
                    cameraTransform.forward,
                    5f,
                    gameObject,
                    gameObject
                );
                
                damageable.TakeDamage(kickDamage);
            }
        }
    }
}
