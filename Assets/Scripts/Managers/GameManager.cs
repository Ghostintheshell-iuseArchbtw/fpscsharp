using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using FPS.Managers;
using FPS.UI;

namespace FPS.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("Game State")]
        [SerializeField] private bool isPaused = false;
        [SerializeField] private bool isGameOver = false;
        [SerializeField] private GameState currentGameState = GameState.MainMenu;
        
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
        
        [Header("Performance")]
        [SerializeField] private bool enableAutoSave = true;
        [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
        
        // Game state properties
        public bool IsPaused => isPaused;
        public bool IsGameOver => isGameOver;
        public GameState CurrentGameState => currentGameState;
        
        // Events
        public System.Action<GameState> OnGameStateChanged;
        public System.Action OnGamePaused;
        public System.Action OnGameResumed;
        public System.Action OnPlayerDied;
        public System.Action OnLevelCompleted;        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void InitializeGame()
        {
            // Initialize current lives
            currentLives = respawnLives;
            
            // Set initial game state based on current scene
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene == mainMenuSceneName)
            {
                SetGameState(GameState.MainMenu);
            }
            else
            {
                SetGameState(GameState.Playing);
            }
            
            // Subscribe to scene events
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // Start auto-save coroutine if enabled
            if (enableAutoSave)
            {
                StartCoroutine(AutoSaveCoroutine());
            }
        }
        
        private void Start()
        {
            // Find references if not set
            if (uiManager == null)
            {
                uiManager = FindObjectOfType<UIManager>();
            }
            
            // Find player health if not set
            if (playerHealth == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerHealth = player.GetComponent<PlayerHealth>();
                }
            }
            
            // Subscribe to player health events
            if (playerHealth != null)
            {
                playerHealth.OnDeath.AddListener(HandlePlayerDeath);
            }
        }
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
    }        private void Update()
        {
            // Handle pause input (only during gameplay)
            if (Input.GetKeyDown(KeyCode.Escape) && currentGameState == GameState.Playing && !isGameOver)
            {
                // Check if tutorial is active
                if (TutorialSystem.Instance != null && TutorialSystem.Instance.IsTutorialActive())
                {
                    return; // Don't pause during tutorial
                }
                
                TogglePause();
            }
        }
        
        public void SetGameState(GameState newState)
        {
            if (currentGameState != newState)
            {
                GameState previousState = currentGameState;
                currentGameState = newState;
                
                OnGameStateChanged?.Invoke(newState);
                
                // Handle state transitions
                HandleGameStateTransition(previousState, newState);
            }
        }
        
        private void HandleGameStateTransition(GameState from, GameState to)
        {
            switch (to)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    break;
                    
                case GameState.Playing:
                    Time.timeScale = 1f;
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    isPaused = false;
                    break;
                    
                case GameState.Paused:
                    Time.timeScale = 0f;
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    break;
                    
                case GameState.GameOver:
                    Time.timeScale = 1f;
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    break;
                    
                case GameState.Loading:
                    // Loading state is handled by SceneTransitionManager
                    break;
            }
        }
        
        public void TogglePause()
        {
            if (currentGameState == GameState.Playing)
            {
                isPaused = true;
                SetGameState(GameState.Paused);
                OnGamePaused?.Invoke();
                
                // Update UI
                if (uiManager != null)
                {
                    uiManager.ShowPauseMenu();
                }
            }
            else if (currentGameState == GameState.Paused)
            {
                isPaused = false;
                SetGameState(GameState.Playing);
                OnGameResumed?.Invoke();
                
                // Update UI
                if (uiManager != null)
                {
                    uiManager.HidePauseMenu();
                }
            }
        }        private void HandlePlayerDeath()
        {
            isGameOver = true;
            SetGameState(GameState.GameOver);
            OnPlayerDied?.Invoke();
            
            // Decrement lives
            currentLives--;
            
            // Check if player has lives remaining
            if (currentLives > 0)
            {
                // Show respawn UI
                if (uiManager != null)
                {
                    uiManager.ShowNotification($"Lives remaining: {currentLives}");
                }
                
                // Respawn after delay
                StartCoroutine(RespawnAfterDelay());
            }
            else
            {
                // Game over - no lives left
                if (uiManager != null)
                {
                    uiManager.ShowGameOverScreen();
                }
            }
        }
        
        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            RespawnPlayer();
        }
        
        public void RespawnPlayer()
        {
            if (playerHealth != null)
            {
                Vector3 respawnPosition = GetRespawnPosition();
                playerHealth.Respawn(respawnPosition);
                
                // Grant temporary invulnerability
                StartCoroutine(GrantRespawnProtection());
                
                // Reset game state
                isGameOver = false;
                SetGameState(GameState.Playing);
                
                if (uiManager != null)
                {
                    uiManager.ShowNotification("Respawned!");
                }
            }
        }
        
        private Vector3 GetRespawnPosition()
        {
            if (useCheckpoints && lastCheckpoint != null)
            {
                return lastCheckpoint.position;
            }
            else if (defaultRespawnPoint != null)
            {
                return defaultRespawnPoint.position;
            }
            else if (respawnPoints.Length > 0)
            {
                return respawnPoints[0].position;
            }
            else
            {
                return Vector3.zero;
            }
        }
        
        private IEnumerator GrantRespawnProtection()
        {
            if (playerHealth != null)
            {
                playerHealth.SetInvulnerable(true);
                yield return new WaitForSeconds(respawnProtectionTime);
                playerHealth.SetInvulnerable(false);
            }
        }
        
        private IEnumerator AutoSaveCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(autoSaveInterval);
                
                // Only auto-save during gameplay
                if (currentGameState == GameState.Playing && !isGameOver && SaveSystem.Instance != null)
                {
                    SaveSystem.Instance.SaveGame(0); // Auto-save to slot 0
                    if (uiManager != null)
                    {
                        uiManager.ShowNotification("Game auto-saved");
                    }
                }
            }
        }        public void SetCheckpoint(Transform checkpoint)
        {
            if (checkpoint != null)
            {
                lastCheckpoint = checkpoint;
                
                if (uiManager != null)
                {
                    uiManager.ShowNotification("Checkpoint reached!");
                }
                
                // Auto-save at checkpoint
                if (SaveSystem.Instance != null)
                {
                    SaveSystem.Instance.SaveGame(0);
                }
            }
        }
        
        public void CompleteLevel()
        {
            OnLevelCompleted?.Invoke();
            
            if (uiManager != null)
            {
                uiManager.ShowLevelCompleteScreen();
            }
            
            SetGameState(GameState.LevelComplete);
        }
        
        public void RestartLevel()
        {
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.ReloadCurrentScene();
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
        
        public void LoadNextLevel()
        {
            currentLevelIndex++;
            
            if (currentLevelIndex >= levelSceneNames.Length)
            {
                // Game completed, return to main menu
                LoadMainMenu();
            }
            else
            {
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.LoadScene(levelSceneNames[currentLevelIndex]);
                }
                else
                {
                    SceneManager.LoadScene(levelSceneNames[currentLevelIndex]);
                }
            }
        }
        
        public void LoadMainMenu()
        {
            // Reset game state
            isGameOver = false;
            isPaused = false;
            currentLives = respawnLives;
            
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadMainMenu();
            }
            else
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }
        
        public void QuitGame()
        {
            // Save before quitting
            if (SaveSystem.Instance != null && currentGameState == GameState.Playing)
            {
                SaveSystem.Instance.SaveGame(0);
            }
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        public void NewGame(int difficultyLevel = 0)
        {
            // Reset game state
            currentLives = respawnLives;
            currentLevelIndex = 0;
            isGameOver = false;
            isPaused = false;
            lastCheckpoint = null;
            
            // Set difficulty (implement based on your difficulty system)
            SetDifficulty(difficultyLevel);
            
            // Load first level
            if (levelSceneNames.Length > 0)
            {
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.LoadScene(levelSceneNames[0]);
                }
                else
                {
                    SceneManager.LoadScene(levelSceneNames[0]);
                }
            }
        }
        
        private void SetDifficulty(int level)
        {
            // Implement difficulty settings
            switch (level)
            {
                case 0: // Easy
                    respawnLives = 5;
                    break;
                case 1: // Normal
                    respawnLives = 3;
                    break;
                case 2: // Hard
                    respawnLives = 1;
                    break;
            }
            
            currentLives = respawnLives;
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Reset game state
            isGameOver = false;
            
            // Update current level index
            for (int i = 0; i < levelSceneNames.Length; i++)
            {
                if (scene.name == levelSceneNames[i])
                {
                    currentLevelIndex = i;
                    break;
                }
            }
            
            // Set appropriate game state
            if (scene.name == mainMenuSceneName)
            {
                SetGameState(GameState.MainMenu);
            }
            else
            {
                SetGameState(GameState.Playing);
            }
            
            // Find new references in the loaded scene
            FindSceneReferences();
        }
        
        private void FindSceneReferences()
        {
            // Find UI Manager
            if (uiManager == null)
            {
                uiManager = FindObjectOfType<UIManager>();
            }
            
            // Find player health
            if (playerHealth == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerHealth = player.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.OnDeath.AddListener(HandlePlayerDeath);
                    }
                }
            }
        }
        
        #region Public Methods
        
        public int GetCurrentLives()
        {
            return currentLives;
        }
        
        public int GetCurrentLevelIndex()
        {
            return currentLevelIndex;
        }
        
        public void AddLife()
        {
            currentLives++;
            if (uiManager != null)
            {
                uiManager.ShowNotification($"Extra life! Lives: {currentLives}");
            }
        }
        
        public void SetLives(int lives)
        {
            currentLives = Mathf.Max(0, lives);
        }
        
        #endregion
        
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
    
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        LevelComplete,
        Loading
    }
}
