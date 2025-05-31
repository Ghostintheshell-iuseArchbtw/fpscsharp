using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private bool activateOnTriggerEnter = true;
    [SerializeField] private GameObject checkpointVisualOn;
    [SerializeField] private GameObject checkpointVisualOff;
    [SerializeField] private AudioClip checkpointSound;
    
    private bool isActivated = false;
    private AudioSource audioSource;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Create audio source if needed
        if (audioSource == null && checkpointSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.volume = 0.7f;
        }
        
        // Update visuals
        UpdateVisuals();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!activateOnTriggerEnter || isActivated) return;
        
        // Check if player entered
        if (other.CompareTag("Player"))
        {
            Activate();
        }
    }
    
    public void Activate()
    {
        if (isActivated) return;
        
        isActivated = true;
        
        // Update visuals
        UpdateVisuals();
        
        // Play sound
        if (audioSource != null && checkpointSound != null)
        {
            audioSource.PlayOneShot(checkpointSound);
        }
        
        // Notify game manager
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.SetCheckpoint(transform);
        }
    }
    
    private void UpdateVisuals()
    {
        if (checkpointVisualOn != null)
        {
            checkpointVisualOn.SetActive(isActivated);
        }
        
        if (checkpointVisualOff != null)
        {
            checkpointVisualOff.SetActive(!isActivated);
        }
    }
    
    // Method to manually deactivate checkpoint (for level resets, etc.)
    public void Deactivate()
    {
        isActivated = false;
        UpdateVisuals();
    }
    
    // Public getter for checkpoint state
    public bool IsActivated => isActivated;
}
            audioSource.PlayOneShot(checkpointSound);
        }
        
        // Register as current checkpoint in GameManager
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.SetCheckpoint(transform);
        }
    }
    
    public void Deactivate()
    {
        if (!isActivated) return;
        
        isActivated = false;
        
        // Update visuals
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        if (checkpointVisualOn != null)
        {
            checkpointVisualOn.SetActive(isActivated);
        }
        
        if (checkpointVisualOff != null)
        {
            checkpointVisualOff.SetActive(!isActivated);
        }
    }
}
