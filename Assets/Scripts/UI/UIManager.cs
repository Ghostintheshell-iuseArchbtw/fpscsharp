using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

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
    
    [Header("Menus")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private GameObject mainMenuPanel;
    
    [Header("Animation")]
    [SerializeField] private float hitMarkerDuration = 0.2f;
    [SerializeField] private float damageVignetteFadeSpeed = 2f;
    
    // Reference to player components
    private PlayerHealth playerHealth;
    private WeaponController weaponController;
    
    // State variables
    private float currentVignetteAlpha = 0f;
    private Coroutine hitMarkerCoroutine;
    
    private void Start()
    {
        // Find player references
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            weaponController = player.GetComponent<WeaponController>();
            
            // Subscribe to player events
            if (playerHealth != null)
            {
                playerHealth.OnDamaged.AddListener(OnPlayerDamaged);
                playerHealth.OnHealed.AddListener(OnPlayerHealed);
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
