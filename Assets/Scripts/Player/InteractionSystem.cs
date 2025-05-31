using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InteractionSystem : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float interactionCooldown = 0.5f;
    
    [Header("UI")]
    [SerializeField] private UIManager uiManager;
    
    // Input action reference
    private InputAction interactAction;
    private PlayerInput playerInput;
    
    // State variables
    private IInteractable currentInteractable;
    private float lastInteractionTime;
    
    private void Awake()
    {
        // Get player input component
        playerInput = GetComponent<PlayerInput>();
        
        // If camera transform is not set, try to find it
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cameraTransform = cam.transform;
            }
        }
        
        // Find UI manager if not set
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }
    }
    
    private void Start()
    {
        // Get the interact action
        interactAction = playerInput.actions["Interact"];
    }
    
    private void Update()
    {
        // Check for interactables in range
        CheckForInteractable();
        
        // Handle interaction input
        if (interactAction.WasPressedThisFrame() && currentInteractable != null && Time.time > lastInteractionTime + interactionCooldown)
        {
            Interact();
        }
    }
    
    private void CheckForInteractable()
    {
        // Cast a ray from the camera
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;
        
        // Check if we hit an interactable
        if (Physics.Raycast(ray, out hit, interactionRange, interactableMask))
        {
            // Try to get an interactable component
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            
            if (interactable != null)
            {
                // We found an interactable
                if (currentInteractable != interactable)
                {
                    // We're looking at a new interactable
                    currentInteractable = interactable;
                    
                    // Show interaction prompt
                    if (uiManager != null)
                    {
                        uiManager.ShowInteractionPrompt(interactable.GetInteractionPrompt());
                    }
                }
                
                return;
            }
        }
        
        // If we get here, we're not looking at an interactable
        if (currentInteractable != null)
        {
            // Clear current interactable
            currentInteractable = null;
            
            // Hide interaction prompt
            if (uiManager != null)
            {
                uiManager.HideInteractionPrompt();
            }
        }
    }
    
    private void Interact()
    {
        // Interact with the current interactable
        currentInteractable.Interact(gameObject);
        
        // Set cooldown
        lastInteractionTime = Time.time;
    }
}

// Interface for interactable objects
public interface IInteractable
{
    void Interact(GameObject interactor);
    string GetInteractionPrompt();
}

// Basic interactable object implementation
public class InteractableObject : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionPrompt = "Press E to interact";
    [SerializeField] private AudioClip interactionSound;
    [SerializeField] private bool oneTimeUse = false;
    [SerializeField] private bool isEnabled = true;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onInteract;
    [SerializeField] private UnityEvent<GameObject> onInteractWithGameObject;
    
    private AudioSource audioSource;
    private bool hasBeenUsed = false;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    
    public virtual void Interact(GameObject interactor)
    {
        if (!isEnabled || (oneTimeUse && hasBeenUsed))
            return;
        
        // Play interaction sound
        if (interactionSound != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(interactionSound);
            }
            else
            {
                AudioSource.PlayClipAtPoint(interactionSound, transform.position);
            }
        }
        
        // Invoke events
        onInteract?.Invoke();
        onInteractWithGameObject?.Invoke(interactor);
        
        // Mark as used if one-time use
        if (oneTimeUse)
        {
            hasBeenUsed = true;
        }
    }
    
    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }
    
    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
    }
    
    public void ResetUse()
    {
        hasBeenUsed = false;
    }
}

// Specialized interactable for doors
public class InteractableDoor : InteractableObject
{
    [Header("Door Settings")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 2f;
    [SerializeField] private bool isLocked = false;
    [SerializeField] private string keyName = "";
    
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private float currentOpenAmount = 0f;
    
    private void Start()
    {
        // Save the initial rotation as closed position
        closedRotation = transform.rotation;
        
        // Calculate open rotation
        openRotation = Quaternion.Euler(transform.eulerAngles + new Vector3(0, openAngle, 0));
        
        // Set initial state
        currentOpenAmount = isOpen ? 1f : 0f;
        UpdateDoorPosition();
    }
    
    private void Update()
    {
        // Smooth door animation
        if ((isOpen && currentOpenAmount < 1f) || (!isOpen && currentOpenAmount > 0f))
        {
            float targetAmount = isOpen ? 1f : 0f;
            currentOpenAmount = Mathf.MoveTowards(currentOpenAmount, targetAmount, openSpeed * Time.deltaTime);
            UpdateDoorPosition();
        }
    }
    
    private void UpdateDoorPosition()
    {
        // Interpolate between closed and open rotations
        transform.rotation = Quaternion.Slerp(closedRotation, openRotation, currentOpenAmount);
    }
    
    public override void Interact(GameObject interactor)
    {
        if (isLocked)
        {
            // Check if player has the key
            Inventory inventory = interactor.GetComponent<Inventory>();
            if (inventory != null && inventory.HasItem(keyName))
            {
                isLocked = false;
                
                // Play unlock sound
                AudioSource audioSource = GetComponent<AudioSource>();
                if (audioSource != null && audioSource.clip != null)
                {
                    audioSource.Play();
                }
                
                // Toggle door state
                ToggleDoor();
            }
            else
            {
                // Door is locked and player doesn't have the key
                // Play locked sound or display message
                UIManager uiManager = FindObjectOfType<UIManager>();
                if (uiManager != null)
                {
                    uiManager.SetObjectiveText("This door is locked. Find the " + keyName + ".");
                }
            }
        }
        else
        {
            // Door is unlocked, toggle its state
            ToggleDoor();
            base.Interact(interactor);
        }
    }
    
    private void ToggleDoor()
    {
        isOpen = !isOpen;
    }
    
    public void SetLocked(bool locked)
    {
        isLocked = locked;
    }
}

// Specialized interactable for pickups
public class InteractablePickup : InteractableObject
{
    [Header("Pickup Settings")]
    [SerializeField] private string itemName = "Item";
    [SerializeField] private string itemDescription = "A useful item";
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private GameObject itemModel;
    [SerializeField] private bool destroyOnPickup = true;
    
    public override void Interact(GameObject interactor)
    {
        // Try to add to inventory
        Inventory inventory = interactor.GetComponent<Inventory>();
        if (inventory != null)
        {
            bool added = inventory.AddItem(new InventoryItem(itemName, itemDescription, itemIcon));
            
            if (added)
            {
                // Play pickup effects
                base.Interact(interactor);
                
                // Display pickup message
                UIManager uiManager = FindObjectOfType<UIManager>();
                if (uiManager != null)
                {
                    uiManager.SetObjectiveText("Picked up " + itemName);
                }
                
                // Hide item model or destroy gameObject
                if (destroyOnPickup)
                {
                    Destroy(gameObject);
                }
                else if (itemModel != null)
                {
                    itemModel.SetActive(false);
                    SetEnabled(false);
                }
            }
            else
            {
                // Inventory full or item couldn't be added
                UIManager uiManager = FindObjectOfType<UIManager>();
                if (uiManager != null)
                {
                    uiManager.SetObjectiveText("Cannot pick up " + itemName + ". Inventory full.");
                }
            }
        }
    }
}

// Simple inventory system
public class Inventory : MonoBehaviour
{
    [SerializeField] private int maxItems = 10;
    [SerializeField] private InventoryItem[] items;
    
    private void Awake()
    {
        items = new InventoryItem[maxItems];
    }
    
    public bool AddItem(InventoryItem item)
    {
        // Find an empty slot
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                items[i] = item;
                return true;
            }
        }
        
        // No empty slot found
        return false;
    }
    
    public bool RemoveItem(string itemName)
    {
        // Find the item
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].Name == itemName)
            {
                items[i] = null;
                return true;
            }
        }
        
        // Item not found
        return false;
    }
    
    public bool HasItem(string itemName)
    {
        // Check if we have the item
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].Name == itemName)
            {
                return true;
            }
        }
        
        // Item not found
        return false;
    }
    
    public InventoryItem[] GetItems()
    {
        return items;
    }
}

// Simple inventory item class
[System.Serializable]
public class InventoryItem
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Sprite Icon { get; private set; }
    
    public InventoryItem(string name, string description, Sprite icon)
    {
        Name = name;
        Description = description;
        Icon = icon;
    }
}
