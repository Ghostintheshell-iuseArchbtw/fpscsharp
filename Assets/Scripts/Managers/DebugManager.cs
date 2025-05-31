using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
using FPS.Player;
using FPS.Weapons;
using FPS.Survival;
using FPS.Enemy;

namespace FPS.Managers
{
    public class DebugManager : MonoBehaviour
    {
        public static DebugManager Instance { get; private set; }
        
        [Header("Debug UI")]
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private TextMeshProUGUI debugText;
        [SerializeField] private TextMeshProUGUI performanceText;
        [SerializeField] private Toggle debugToggle;
        [SerializeField] private Button godModeButton;
        [SerializeField] private Button infiniteAmmoButton;
        [SerializeField] private Button addHealthButton;
        [SerializeField] private Button skipLevelButton;
        [SerializeField] private Slider timeScaleSlider;
        [SerializeField] private TextMeshProUGUI timeScaleText;
        
        [Header("Performance Monitoring")]
        [SerializeField] private bool showFPS = true;
        [SerializeField] private bool showMemoryUsage = true;
        [SerializeField] private bool showPlayerStats = true;
        [SerializeField] private bool showEnemyCount = true;
        [SerializeField] private float updateInterval = 0.5f;
        
        // Performance tracking
        private float deltaTime = 0.0f;
        private float memoryUsage = 0.0f;
        private int frameCount = 0;
        private float lastUpdateTime = 0.0f;
        
        // References
        private PlayerHealth playerHealth;
        private PlayerController playerController;
        private WeaponController weaponController;
        private SurvivalSystem survivalSystem;
        
        // Debug states
        private bool debugMode = false;
        private bool godMode = false;
        private bool infiniteAmmo = false;
        
        // Console commands
        private Dictionary<string, System.Action<string[]>> consoleCommands;
        private List<string> consoleHistory = new List<string>();
        private StringBuilder debugInfo = new StringBuilder();
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeConsoleCommands();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Find player references
            FindPlayerReferences();
            
            // Initialize UI
            if (debugPanel != null)
            {
                debugPanel.SetActive(debugMode);
            }
            
            SetupDebugUI();
            
            // Load debug preferences
            debugMode = PlayerPrefs.GetInt("DebugMode", 0) == 1;
            UpdateDebugUI();
        }
        
        private void Update()
        {
            // Toggle debug mode with F1
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ToggleDebugMode();
            }
            
            // Toggle god mode with F2
            if (Input.GetKeyDown(KeyCode.F2))
            {
                ToggleGodMode();
            }
            
            // Toggle infinite ammo with F3
            if (Input.GetKeyDown(KeyCode.F3))
            {
                ToggleInfiniteAmmo();
            }
            
            // Update performance metrics
            UpdatePerformanceMetrics();
            
            // Update debug info
            if (debugMode)
            {
                UpdateDebugInfo();
            }
            
            // Apply god mode
            if (godMode && playerHealth != null)
            {
                playerHealth.SetInvulnerable(true);
            }
            
            // Apply infinite ammo
            if (infiniteAmmo && weaponController != null)
            {
                weaponController.SetInfiniteAmmo(true);
            }
        }
        
        private void FindPlayerReferences()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
                playerController = player.GetComponent<PlayerController>();
                weaponController = player.GetComponent<WeaponController>();
                survivalSystem = player.GetComponent<SurvivalSystem>();
            }
        }
        
        private void SetupDebugUI()
        {
            if (debugToggle != null)
            {
                debugToggle.onValueChanged.AddListener(OnDebugToggleChanged);
            }
            
            if (godModeButton != null)
            {
                godModeButton.onClick.AddListener(ToggleGodMode);
            }
            
            if (infiniteAmmoButton != null)
            {
                infiniteAmmoButton.onClick.AddListener(ToggleInfiniteAmmo);
            }
            
            if (addHealthButton != null)
            {
                addHealthButton.onClick.AddListener(() => AddHealth(50f));
            }
            
            if (skipLevelButton != null)
            {
                skipLevelButton.onClick.AddListener(SkipLevel);
            }
            
            if (timeScaleSlider != null)
            {
                timeScaleSlider.onValueChanged.AddListener(OnTimeScaleChanged);
                timeScaleSlider.value = Time.timeScale;
            }
        }
        
        private void InitializeConsoleCommands()
        {
            consoleCommands = new Dictionary<string, System.Action<string[]>>
            {
                { "god", args => ToggleGodMode() },
                { "ammo", args => ToggleInfiniteAmmo() },
                { "health", args => AddHealth(args.Length > 0 ? float.Parse(args[0]) : 100f) },
                { "damage", args => TakeDamage(args.Length > 0 ? float.Parse(args[0]) : 25f) },
                { "timescale", args => SetTimeScale(args.Length > 0 ? float.Parse(args[0]) : 1f) },
                { "spawn", args => SpawnEnemy(args.Length > 0 ? args[0] : "default") },
                { "teleport", args => TeleportPlayer(args) },
                { "give", args => GiveItem(args.Length > 0 ? args[0] : "weapon") },
                { "kill", args => KillAllEnemies() },
                { "clear", args => ClearConsole() },
                { "help", args => ShowHelp() }
            };
        }
        
        private void UpdatePerformanceMetrics()
        {
            frameCount++;
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                memoryUsage = System.GC.GetTotalMemory(false) / 1024f / 1024f; // MB
                lastUpdateTime = Time.time;
            }
        }
        
        private void UpdateDebugInfo()
        {
            if (debugText == null) return;
            
            debugInfo.Clear();
            
            if (showFPS)
            {
                float fps = 1.0f / deltaTime;
                debugInfo.AppendLine($"FPS: {fps:F1}");
                debugInfo.AppendLine($"Frame Time: {deltaTime * 1000.0f:F1}ms");
            }
            
            if (showMemoryUsage)
            {
                debugInfo.AppendLine($"Memory: {memoryUsage:F1} MB");
            }
            
            if (showPlayerStats && playerHealth != null)
            {
                debugInfo.AppendLine($"Health: {playerHealth.GetCurrentHealth():F0}/{playerHealth.GetMaxHealth():F0}");
                debugInfo.AppendLine($"Armor: {playerHealth.GetCurrentArmor():F0}");
                
                if (survivalSystem != null)
                {
                    debugInfo.AppendLine($"Hunger: {survivalSystem.GetHungerPercentage() * 100:F0}%");
                    debugInfo.AppendLine($"Thirst: {survivalSystem.GetThirstPercentage() * 100:F0}%");
                    debugInfo.AppendLine($"Temperature: {survivalSystem.GetTemperature():F1}°C");
                    debugInfo.AppendLine($"Radiation: {survivalSystem.GetRadiationLevel():F0}");
                }
                
                if (playerController != null)
                {
                    Vector3 pos = playerController.transform.position;
                    debugInfo.AppendLine($"Position: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})");
                    debugInfo.AppendLine($"Velocity: {playerController.GetVelocity().magnitude:F1} m/s");
                }
            }
            
            if (showEnemyCount)
            {
                EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();
                debugInfo.AppendLine($"Enemies: {enemies.Length}");
            }
            
            // Debug states
            debugInfo.AppendLine($"God Mode: {godMode}");
            debugInfo.AppendLine($"Infinite Ammo: {infiniteAmmo}");
            debugInfo.AppendLine($"Time Scale: {Time.timeScale:F2}");
            
            debugText.text = debugInfo.ToString();
        }
        
        #region Public Methods
        
        public void ToggleDebugMode()
        {
            debugMode = !debugMode;
            PlayerPrefs.SetInt("DebugMode", debugMode ? 1 : 0);
            UpdateDebugUI();
            
            Log($"Debug Mode: {(debugMode ? "ON" : "OFF")}");
        }
        
        public void ToggleGodMode()
        {
            godMode = !godMode;
            
            if (playerHealth != null)
            {
                playerHealth.SetInvulnerable(godMode);
            }
            
            Log($"God Mode: {(godMode ? "ON" : "OFF")}");
        }
        
        public void ToggleInfiniteAmmo()
        {
            infiniteAmmo = !infiniteAmmo;
            
            if (weaponController != null)
            {
                weaponController.SetInfiniteAmmo(infiniteAmmo);
            }
            
            Log($"Infinite Ammo: {(infiniteAmmo ? "ON" : "OFF")}");
        }
        
        public void AddHealth(float amount)
        {
            if (playerHealth != null)
            {
                playerHealth.Heal(amount);
                Log($"Added {amount} health");
            }
        }
        
        public void TakeDamage(float amount)
        {
            if (playerHealth != null && !godMode)
            {
                playerHealth.TakeDamage(amount);
                Log($"Took {amount} damage");
            }
        }
        
        public void SetTimeScale(float scale)
        {
            Time.timeScale = Mathf.Clamp(scale, 0f, 5f);
            if (timeScaleSlider != null)
            {
                timeScaleSlider.value = Time.timeScale;
            }
            Log($"Time Scale: {Time.timeScale:F2}");
        }
        
        public void SkipLevel()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadNextLevel();
                Log("Skipped to next level");
            }
        }
        
        public void Log(string message)
        {
            consoleHistory.Add($"[{System.DateTime.Now:HH:mm:ss}] {message}");
            Debug.Log($"[DEBUG] {message}");
            
            // Keep only last 100 messages
            if (consoleHistory.Count > 100)
            {
                consoleHistory.RemoveAt(0);
            }
        }
        
        #endregion
        
        #region Console Commands
        
        private void SpawnEnemy(string enemyType)
        {
            // Implementation depends on your enemy spawning system
            Log($"Spawning enemy: {enemyType}");
        }
        
        private void TeleportPlayer(string[] args)
        {
            if (args.Length >= 3 && playerController != null)
            {
                float x = float.Parse(args[0]);
                float y = float.Parse(args[1]);
                float z = float.Parse(args[2]);
                
                playerController.transform.position = new Vector3(x, y, z);
                Log($"Teleported to ({x}, {y}, {z})");
            }
        }
        
        private void GiveItem(string itemName)
        {
            // Implementation depends on your inventory system
            Log($"Giving item: {itemName}");
        }
        
        private void KillAllEnemies()
        {
            EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();
            foreach (var enemy in enemies)
            {
                enemy.TakeDamage(9999f);
            }
            Log($"Killed {enemies.Length} enemies");
        }
        
        private void ClearConsole()
        {
            consoleHistory.Clear();
            Log("Console cleared");
        }
        
        private void ShowHelp()
        {
            Log("Available commands:");
            foreach (var command in consoleCommands.Keys)
            {
                Log($"  {command}");
            }
        }
        
        #endregion
        
        #region UI Event Handlers
        
        private void UpdateDebugUI()
        {
            if (debugPanel != null)
            {
                debugPanel.SetActive(debugMode);
            }
            
            if (debugToggle != null)
            {
                debugToggle.isOn = debugMode;
            }
        }
        
        private void OnDebugToggleChanged(bool value)
        {
            debugMode = value;
            PlayerPrefs.SetInt("DebugMode", debugMode ? 1 : 0);
            UpdateDebugUI();
        }
        
        private void OnTimeScaleChanged(float value)
        {
            Time.timeScale = value;
            if (timeScaleText != null)
            {
                timeScaleText.text = $"Time Scale: {value:F2}";
            }
        }
        
        #endregion
    }
}
