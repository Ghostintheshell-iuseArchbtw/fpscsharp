using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game State")]
    [SerializeField] private bool isPaused = false;
    [SerializeField] private bool isGameOver = false;
    
    [Header("References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayerHealth playerHealth;
    
    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private bool useCheckpoints = true;
    [SerializeField] private Transform[] respawnPoints;
    [SerializeField] private Transform defaultRespawnPoint;
    [SerializeField] private int respawnLives = 3;
    [SerializeField] private float respawnProtectionTime = 2f;
    private int currentLives;
    private Transform lastCheckpoint;
    
    [Header("Level Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string[] levelSceneNames;
    private int currentLevelIndex = 0;
    
    // Game state properties
    public bool IsPaused => isPaused;
    public bool IsGameOver => isGameOver;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Find references if not set
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }
    }
    
    private void Start()
    {
        // Initialize lives
        currentLives = respawnLives;
        
        // Listen for player death
        if (playerHealth != null)
        {
            playerHealth.OnDeath.AddListener(HandlePlayerDeath);
        }
        else
        {
            // Find player when level loads
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        // Set default checkpoint if none is set
        if (lastCheckpoint == null)
        {
            if (defaultRespawnPoint != null)
            {
                lastCheckpoint = defaultRespawnPoint;
            }
            else if (respawnPoints != null && respawnPoints.Length > 0)
            {
                lastCheckpoint = respawnPoints[0];
            }
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find player in the newly loaded scene
        playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnDeath.AddListener(HandlePlayerDeath);
        }
        
        // Find UI manager
        uiManager = FindObjectOfType<UIManager>();
        
        // Reset game state
        isPaused = false;
        isGameOver = false;
        
        // Resume time scale
        Time.timeScale = 1f;
        
        // Update current level index
        for (int i = 0; i < levelSceneNames.Length; i++)
        {
            if (scene.name == levelSceneNames[i])
            {
                currentLevelIndex = i;
                break;
            }
        }
    }
    
    private void Update()
    {
        // Handle pause input
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver)
        {
            TogglePause();
        }
    }
    
    public void TogglePause()
    {
        isPaused = !isPaused;
        
        // Set time scale
        Time.timeScale = isPaused ? 0f : 1f;
        
        // Update UI
        if (uiManager != null)
        {
            if (isPaused)
            {
                uiManager.ShowPauseMenu();
            }
            else
            {
                uiManager.HidePauseMenu();
            }
        }
        
        // Lock/unlock cursor
        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    private void HandlePlayerDeath()
    {
        // Decrement lives
        currentLives--;
        
        // Check if player has lives remaining
        if (currentLives > 0)
        {
            // Show respawn UI
            if (uiManager != null)
            {
                uiManager.ShowRespawnScreen(currentLives);
            }
            
            // Respawn after delay
            StartCoroutine(RespawnAfterDelay());
        }
        else
        {
            // Game over - no lives left
            isGameOver = true;
            
            // Show game over UI
            if (uiManager != null)
            {
                uiManager.ShowGameOverScreen();
            }
            
            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    
    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        
        if (playerHealth != null && lastCheckpoint != null)
        {
            // Respawn player at last checkpoint
            playerHealth.Respawn(lastCheckpoint.position);
            
            // Apply temporary invulnerability
            playerHealth.IsInvulnerable = true;
            StartCoroutine(DisableInvulnerability(respawnProtectionTime));
        }
        else if (playerHealth != null && defaultRespawnPoint != null)
        {
            // Fallback to default respawn if no checkpoint was activated
            playerHealth.Respawn(defaultRespawnPoint.position);
            
            // Apply temporary invulnerability
            playerHealth.IsInvulnerable = true;
            StartCoroutine(DisableInvulnerability(respawnProtectionTime));
        }
    }
    
    private IEnumerator DisableInvulnerability(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (playerHealth != null)
        {
            playerHealth.IsInvulnerable = false;
        }
    }
    
    public void SetCheckpoint(Transform checkpoint)
    {
        if (checkpoint != null)
        {
            lastCheckpoint = checkpoint;
            
            // Optional: Save checkpoint to player prefs or other persistence
            if (uiManager != null)
            {
                uiManager.ShowNotification("Checkpoint reached!");
            }
        }
    }
            
            // Hide respawn UI
            if (uiManager != null)
            {
                uiManager.HideRespawnScreen();
            }
            
            // Grant temporary invulnerability
            StartCoroutine(GrantRespawnProtection());
        }
        else
        {
            // If we can't respawn properly, just restart the level
            RestartLevel();
        }
    }
    
    private IEnumerator GrantRespawnProtection()
    {
        // Enable respawn protection
        if (playerHealth != null)
        {
            playerHealth.IsInvulnerable = true;
        }
        
        yield return new WaitForSeconds(respawnProtectionTime);
        
        // Disable respawn protection
        if (playerHealth != null)
        {
            playerHealth.IsInvulnerable = false;
        }
    }
    
    public void SetCheckpoint(Transform checkpoint)
    {
        if (checkpoint != null)
        {
            lastCheckpoint = checkpoint;
            
            // Show checkpoint notification
            if (uiManager != null)
            {
                uiManager.ShowNotification("Checkpoint reached");
            }
        }
    }
    
    public void RestartLevel()
    {
        // Reset game state
        isGameOver = false;
        
        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void LoadNextLevel()
    {
        currentLevelIndex++;
        
        // Check if we've completed all levels
        if (currentLevelIndex >= levelSceneNames.Length)
        {
            // Game completed, return to main menu
            LoadMainMenu();
        }
        else
        {
            // Load next level
            SceneManager.LoadScene(levelSceneNames[currentLevelIndex]);
        }
    }
    
    public void LoadMainMenu()
    {
        // Reset game state
        isGameOver = false;
        isPaused = false;
        Time.timeScale = 1f;
        
        // Load main menu scene
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
