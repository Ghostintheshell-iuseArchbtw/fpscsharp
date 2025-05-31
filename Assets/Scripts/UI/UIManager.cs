using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using FPS.Player;
using FPS.Survival;
using FPS.Weapons;
using FPS.Managers;

namespace FPS.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("HUD Elements")]
        [SerializeField] private GameObject hudContainer;
        [SerializeField] private Image healthBar;
        [SerializeField] private Image armorBar;
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI armorText;
        [SerializeField] private Image hitMarker;
        [SerializeField] private Image damageVignette;
        [SerializeField] private GameObject enemyHitIndicatorPrefab;
        [SerializeField] private Transform enemyHitIndicatorParent;
        [SerializeField] private Image crosshair;
        [SerializeField] private Image interactionPrompt;
        [SerializeField] private TextMeshProUGUI interactionText;
        [SerializeField] private TextMeshProUGUI objectiveText;
        
        [Header("Survival UI")]
        [SerializeField] private Slider hungerBar;
        [SerializeField] private Slider thirstBar;
        [SerializeField] private Slider temperatureBar;
        [SerializeField] private Slider radiationBar;
        [SerializeField] private Slider staminaBar;
        [SerializeField] private TextMeshProUGUI hungerText;
        [SerializeField] private TextMeshProUGUI thirstText;
        [SerializeField] private TextMeshProUGUI temperatureText;
        [SerializeField] private TextMeshProUGUI radiationText;
        [SerializeField] private TextMeshProUGUI staminaText;
        
        [Header("Inventory UI")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Transform inventorySlotContainer;
        [SerializeField] private GameObject inventorySlotPrefab;
        
        [Header("Crafting UI")]
        [SerializeField] private GameObject craftingPanel;
        [SerializeField] private Transform recipeContainer;
        [SerializeField] private Button craftingButton;
        [SerializeField] private Slider craftingProgressBar;
        [SerializeField] private TextMeshProUGUI craftingProgressText;
        
        [Header("Notification UI")]
        [SerializeField] private GameObject notificationPrefab;
        [SerializeField] private Transform notificationContainer;
        [SerializeField] private float notificationDuration = 3f;
    
    [Header("Menus")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private GameObject mainMenuPanel;
    
    [Header("Animation")]
    [SerializeField] private float hitMarkerDuration = 0.2f;
    [SerializeField] private float damageVignetteFadeSpeed = 2f;        // Reference to player components
        private PlayerHealth playerHealth;
        private WeaponController weaponController;
        private SurvivalSystem survivalSystem;
        private InventoryManager inventoryManager;
        private CraftingManager craftingManager;
        
        // State variables
        private float currentVignetteAlpha = 0f;
        private Coroutine hitMarkerCoroutine;
        
        // UI state
        private bool inventoryOpen = false;
        private bool craftingOpen = false;
    
    private void Start()
    {
        // Find player references
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            weaponController = player.GetComponent<WeaponController>();
            survivalSystem = player.GetComponent<SurvivalSystem>();
            inventoryManager = player.GetComponent<InventoryManager>();
            craftingManager = player.GetComponent<CraftingManager>();
            
            // Subscribe to player events
            if (playerHealth != null)
            {
                playerHealth.OnDamaged.AddListener(OnPlayerDamaged);
                playerHealth.OnHealed.AddListener(OnPlayerHealed);
            }
            
            // Subscribe to survival events
            if (survivalSystem != null)
            {
                survivalSystem.OnHungerChanged.AddListener(UpdateSurvivalUI);
                survivalSystem.OnThirstChanged.AddListener(UpdateSurvivalUI);
                survivalSystem.OnTemperatureChanged.AddListener(UpdateSurvivalUI);
                survivalSystem.OnRadiationChanged.AddListener(UpdateSurvivalUI);
                survivalSystem.OnStaminaChanged.AddListener(UpdateSurvivalUI);
            }
            
            // Subscribe to inventory events
            if (inventoryManager != null)
            {
                inventoryManager.OnInventoryChanged.AddListener(UpdateInventoryUI);
            }
            
            // Subscribe to crafting events
            if (craftingManager != null)
            {
                craftingManager.OnCraftingStarted.AddListener(OnCraftingStarted);
                craftingManager.OnCraftingCompleted.AddListener(OnCraftingCompleted);
                craftingManager.OnCraftingProgress.AddListener(UpdateCraftingProgress);
            }
        }
        
        // Hide menus on start
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
        
        // Show or hide main menu
        if (mainMenuPanel != null)
        {
            bool isMainMenu = SceneManager.GetActiveScene().name == "MainMenu";
            mainMenuPanel.SetActive(isMainMenu);
            hudContainer.SetActive(!isMainMenu);
        }
        
        // Initialize HUD
        UpdateHealthUI();
        UpdateAmmoUI();
        UpdateSurvivalUI();
        
        // Initialize UI panels
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (craftingPanel != null) craftingPanel.SetActive(false);
        
        // Initialize crosshair
        if (crosshair != null)
        {
            crosshair.enabled = true;
        }
        
        // Hide interaction prompt initially
        if (interactionPrompt != null)
        {
            interactionPrompt.enabled = false;
            interactionText.enabled = false;
        }
        
        // Hide hit marker initially
        if (hitMarker != null)
        {
            hitMarker.enabled = false;
        }
        
        // Initialize damage vignette
        if (damageVignette != null)
        {
            Color color = damageVignette.color;
            color.a = 0f;
            damageVignette.color = color;
        }
    }
    
    private void Update()
    {
        // Update HUD elements if we have references
        if (playerHealth != null)
        {
            UpdateHealthUI();
        }
        
        if (weaponController != null)
        {
            UpdateAmmoUI();
        }
        
        if (survivalSystem != null)
        {
            UpdateSurvivalUI();
        }
        
        // Handle input for UI panels
        HandleUIInput();
        
        // Update damage vignette
        if (damageVignette != null)
        {
            // Gradually fade out vignette
            currentVignetteAlpha = Mathf.Max(0, currentVignetteAlpha - Time.deltaTime * damageVignetteFadeSpeed);
            
            // Update vignette alpha
            Color color = damageVignette.color;
            color.a = currentVignetteAlpha;
            damageVignette.color = color;
        }
    }
    
    #region HUD Updates
    
    private void UpdateHealthUI()
    {
        if (playerHealth == null) return;
        
        // Update health bar
        if (healthBar != null)
        {
            healthBar.fillAmount = playerHealth.GetHealthPercentage();
        }
        
        // Update armor bar
        if (armorBar != null)
        {
            armorBar.fillAmount = playerHealth.GetArmorPercentage();
        }
        
        // Update text displays
        if (healthText != null)
        {
            healthText.text = Mathf.Round(playerHealth.GetCurrentHealth()).ToString();
        }
        
        if (armorText != null)
        {
            armorText.text = Mathf.Round(playerHealth.GetCurrentArmor()).ToString();
        }
    }
    
    private void UpdateAmmoUI()
    {
        if (weaponController == null || ammoText == null) return;
        
        // Get ammo info from weapon controller
        int currentAmmo = weaponController.GetCurrentAmmo();
        int totalAmmo = weaponController.GetTotalAmmo();
        
        // Update ammo text
        ammoText.text = $"{currentAmmo} / {totalAmmo}";
    }
    
    public void ShowHitMarker()
    {
        if (hitMarker == null) return;
        
        // Cancel existing coroutine if running
        if (hitMarkerCoroutine != null)
        {
            StopCoroutine(hitMarkerCoroutine);
        }
        
        // Start new hit marker animation
        hitMarkerCoroutine = StartCoroutine(ShowHitMarkerForDuration());
    }
    
    private IEnumerator ShowHitMarkerForDuration()
    {
        // Show hit marker
        hitMarker.enabled = true;
        
        // Wait for duration
        yield return new WaitForSeconds(hitMarkerDuration);
        
        // Hide hit marker
        hitMarker.enabled = false;
        hitMarkerCoroutine = null;
    }
    
    public void ShowEnemyHitIndicator(Vector3 enemyPosition)
    {
        if (enemyHitIndicatorPrefab == null || enemyHitIndicatorParent == null) return;
        
        // Create a hit indicator
        GameObject indicator = Instantiate(enemyHitIndicatorPrefab, enemyHitIndicatorParent);
        
        // Get the direction to the enemy from player
        Vector3 direction = enemyPosition - Camera.main.transform.position;
        
        // Calculate 2D angle on the horizontal plane
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        
        // Set the rotation of the indicator
        indicator.transform.rotation = Quaternion.Euler(0, 0, -angle);
        
        // Destroy after delay
        Destroy(indicator, 1.5f);
    }
    
    public void ShowInteractionPrompt(string text)
    {
        if (interactionPrompt == null || interactionText == null) return;
        
        interactionPrompt.enabled = true;
        interactionText.enabled = true;
        interactionText.text = text;
    }
    
    public void HideInteractionPrompt()
    {
        if (interactionPrompt == null || interactionText == null) return;
        
        interactionPrompt.enabled = false;
        interactionText.enabled = false;
    }
    
    public void SetObjectiveText(string text)
    {
        if (objectiveText == null) return;
        
        objectiveText.text = text;
        
        // Animate objective text appearing
        StartCoroutine(AnimateObjectiveText());
    }
    
    private IEnumerator AnimateObjectiveText()
    {
        // Ensure text is visible
        objectiveText.enabled = true;
        
        // Scale up animation
        objectiveText.transform.localScale = Vector3.zero;
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            objectiveText.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        objectiveText.transform.localScale = Vector3.one;
        
        // Wait a few seconds
        yield return new WaitForSeconds(5f);
        
        // Fade out text
        elapsed = 0f;
        duration = 1f;
        Color startColor = objectiveText.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            objectiveText.color = Color.Lerp(startColor, endColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset color but hide text
        objectiveText.color = startColor;
        objectiveText.enabled = false;
    }
    
    #endregion
    
    #region Menu Management
    
    public void ShowPauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
        
        // Hide HUD elements
        if (hudContainer != null)
        {
            hudContainer.SetActive(false);
        }
    }
    
    public void HidePauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        // Show HUD elements
        if (hudContainer != null)
        {
            hudContainer.SetActive(true);
        }
    }
    
    public void ShowGameOverScreen()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // Hide HUD elements
        if (hudContainer != null)
        {
            hudContainer.SetActive(false);
        }
    }
    
    public void ShowLevelCompleteScreen()
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }
        
        // Hide HUD elements
        if (hudContainer != null)
        {
            hudContainer.SetActive(false);
        }
    }
    
    #endregion
    
    #region Survival UI Updates
    
    private void UpdateSurvivalUI()
    {
        if (survivalSystem == null) return;
        
        // Update hunger
        if (hungerBar != null)
        {
            hungerBar.value = survivalSystem.GetHungerPercentage();
        }
        if (hungerText != null)
        {
            hungerText.text = $"{survivalSystem.GetHungerPercentage() * 100:F0}%";
        }
        
        // Update thirst
        if (thirstBar != null)
        {
            thirstBar.value = survivalSystem.GetThirstPercentage();
        }
        if (thirstText != null)
        {
            thirstText.text = $"{survivalSystem.GetThirstPercentage() * 100:F0}%";
        }
        
        // Update temperature
        if (temperatureBar != null)
        {
            float temp = survivalSystem.GetTemperature();
            temperatureBar.value = (temp + 40f) / 80f; // Normalize -40 to 40 degrees to 0-1
        }
        if (temperatureText != null)
        {
            temperatureText.text = $"{survivalSystem.GetTemperature():F0}°C";
        }
        
        // Update radiation
        if (radiationBar != null)
        {
            radiationBar.value = survivalSystem.GetRadiationLevel() / 100f;
        }
        if (radiationText != null)
        {
            radiationText.text = $"{survivalSystem.GetRadiationLevel():F0}";
        }
        
        // Update stamina
        if (staminaBar != null)
        {
            staminaBar.value = survivalSystem.GetStaminaPercentage();
        }
        if (staminaText != null)
        {
            staminaText.text = $"{survivalSystem.GetStaminaPercentage() * 100:F0}%";
        }
    }
    
    #endregion
    
    #region Inventory and Crafting UI
    
    private void HandleUIInput()
    {
        // Toggle inventory with Tab key
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
        
        // Toggle crafting with C key
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleCrafting();
        }
        
        // Close all panels with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (inventoryOpen || craftingOpen)
            {
                CloseAllPanels();
            }
        }
    }
    
    public void ToggleInventory()
    {
        inventoryOpen = !inventoryOpen;
        
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(inventoryOpen);
        }
        
        if (inventoryOpen)
        {
            UpdateInventoryUI();
            // Close crafting if open
            if (craftingOpen)
            {
                ToggleCrafting();
            }
        }
        
        // Control cursor and time scale
        SetUIMode(inventoryOpen || craftingOpen);
    }
    
    public void ToggleCrafting()
    {
        craftingOpen = !craftingOpen;
        
        if (craftingPanel != null)
        {
            craftingPanel.SetActive(craftingOpen);
        }
        
        if (craftingOpen)
        {
            UpdateCraftingUI();
            // Close inventory if open
            if (inventoryOpen)
            {
                ToggleInventory();
            }
        }
        
        // Control cursor and time scale
        SetUIMode(inventoryOpen || craftingOpen);
    }
    
    private void CloseAllPanels()
    {
        if (inventoryOpen) ToggleInventory();
        if (craftingOpen) ToggleCrafting();
    }
    
    private void SetUIMode(bool uiOpen)
    {
        // Control cursor visibility and lock state
        Cursor.visible = uiOpen;
        Cursor.lockState = uiOpen ? CursorLockMode.None : CursorLockMode.Locked;
        
        // Pause or unpause time (optional - you might want to keep the game running)
        Time.timeScale = uiOpen ? 0f : 1f;
    }
    
    private void UpdateInventoryUI()
    {
        if (inventoryManager == null || inventorySlotContainer == null) return;
        
        // Clear existing slots
        foreach (Transform child in inventorySlotContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create slots for inventory items
        var items = inventoryManager.GetAllItems();
        foreach (var item in items)
        {
            if (inventorySlotPrefab != null)
            {
                GameObject slot = Instantiate(inventorySlotPrefab, inventorySlotContainer);
                // Configure slot with item data (you'll need to implement InventorySlot component)
            }
        }
    }
    
    private void UpdateCraftingUI()
    {
        if (craftingManager == null || recipeContainer == null) return;
        
        // Clear existing recipes
        foreach (Transform child in recipeContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Display available recipes
        var recipes = craftingManager.GetAvailableRecipes();
        foreach (var recipe in recipes)
        {
            // Create recipe UI elements (you'll need to implement RecipeSlot component)
        }
    }
    
    private void OnCraftingStarted(string itemName)
    {
        if (craftingProgressBar != null)
        {
            craftingProgressBar.gameObject.SetActive(true);
            craftingProgressBar.value = 0f;
        }
        
        if (craftingProgressText != null)
        {
            craftingProgressText.text = $"Crafting {itemName}...";
        }
        
        ShowNotification($"Started crafting {itemName}");
    }
    
    private void OnCraftingCompleted(string itemName)
    {
        if (craftingProgressBar != null)
        {
            craftingProgressBar.gameObject.SetActive(false);
        }
        
        if (craftingProgressText != null)
        {
            craftingProgressText.text = "";
        }
        
        ShowNotification($"Crafted {itemName}!");
        UpdateInventoryUI();
    }
    
    private void UpdateCraftingProgress(float progress)
    {
        if (craftingProgressBar != null)
        {
            craftingProgressBar.value = progress;
        }
    }
    
    #endregion
    
    #region Notification System
    
    public void ShowNotification(string message)
    {
        if (notificationPrefab == null || notificationContainer == null) return;
        
        GameObject notification = Instantiate(notificationPrefab, notificationContainer);
        
        // Set notification text
        TextMeshProUGUI notificationText = notification.GetComponentInChildren<TextMeshProUGUI>();
        if (notificationText != null)
        {
            notificationText.text = message;
        }
        
        // Start fade out coroutine
        StartCoroutine(FadeOutNotification(notification));
    }
    
    private IEnumerator FadeOutNotification(GameObject notification)
    {
        // Wait for display duration
        yield return new WaitForSeconds(notificationDuration * 0.8f);
        
        // Fade out
        CanvasGroup canvasGroup = notification.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = notification.AddComponent<CanvasGroup>();
        }
        
        float fadeTime = notificationDuration * 0.2f;
        float elapsed = 0f;
        
        while (elapsed < fadeTime)
        {
            float alpha = 1f - (elapsed / fadeTime);
            canvasGroup.alpha = alpha;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // Destroy notification
        Destroy(notification);
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnPlayerDamaged(float damageAmount)
    {
        // Update health UI
        UpdateHealthUI();
        
        // Show damage vignette
        if (damageVignette != null)
        {
            // Set vignette alpha based on damage and health percentage
            float healthPercent = playerHealth.GetHealthPercentage();
            float damagePercent = damageAmount / playerHealth.GetMaxHealth();
            
            // Higher intensity at low health
            float baseIntensity = Mathf.Lerp(0.3f, 0.7f, 1f - healthPercent);
            
            // Add spike based on current damage
            currentVignetteAlpha = Mathf.Min(0.95f, baseIntensity + damagePercent);
        }
    }
    
    private void OnPlayerHealed(float healAmount)
    {
        // Update health UI
        UpdateHealthUI();
    }
    
    #endregion
    
    #region Button Handlers
    
    public void OnResumeButtonClicked()
    {
        // Unpause the game
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePause();
        }
    }
    
    public void OnRestartButtonClicked()
    {
        // Restart the current level
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartLevel();
        }
    }
    
    public void OnMainMenuButtonClicked()
    {
        // Load main menu
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMainMenu();
        }
    }
    
    public void OnQuitButtonClicked()
    {
        // Quit the game
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
    }
    
    public void OnNextLevelButtonClicked()
    {
        // Load next level
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadNextLevel();
        }
    }
    
    #endregion
}
