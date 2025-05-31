using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FPS.Managers;

namespace FPS.UI
{
    public class SaveSlotUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI slotNumberText;
        [SerializeField] private TextMeshProUGUI saveTimeText;
        [SerializeField] private TextMeshProUGUI levelNameText;
        [SerializeField] private TextMeshProUGUI playTimeText;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private GameObject emptySlotIndicator;
        [SerializeField] private GameObject saveDataContainer;
        
        private int slotIndex;
        private System.Action onLoadCallback;
        private System.Action onDeleteCallback;
        
        public void Setup(int slot, SaveData saveData, System.Action onLoad, System.Action onDelete)
        {
            slotIndex = slot;
            onLoadCallback = onLoad;
            onDeleteCallback = onDelete;
            
            // Show save data container, hide empty indicator
            if (saveDataContainer != null) saveDataContainer.SetActive(true);
            if (emptySlotIndicator != null) emptySlotIndicator.SetActive(false);
            
            // Set slot number
            if (slotNumberText != null)
                slotNumberText.text = $"Slot {slot + 1}";
            
            // Set save time
            if (saveTimeText != null)
                saveTimeText.text = saveData.saveTime;
            
            // Set level name
            if (levelNameText != null)
                levelNameText.text = saveData.currentLevel;
            
            // Set play time
            if (playTimeText != null)
            {
                int hours = Mathf.FloorToInt(saveData.playTime / 3600f);
                int minutes = Mathf.FloorToInt((saveData.playTime % 3600f) / 60f);
                playTimeText.text = $"{hours:00}:{minutes:00}";
            }
            
            // Setup buttons
            if (loadButton != null)
            {
                loadButton.onClick.RemoveAllListeners();
                loadButton.onClick.AddListener(() => onLoadCallback?.Invoke());
                loadButton.interactable = true;
            }
            
            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(() => onDeleteCallback?.Invoke());
                deleteButton.interactable = true;
            }
        }
        
        public void SetupEmpty(int slot)
        {
            slotIndex = slot;
            
            // Show empty indicator, hide save data container
            if (emptySlotIndicator != null) emptySlotIndicator.SetActive(true);
            if (saveDataContainer != null) saveDataContainer.SetActive(false);
            
            // Set slot number
            if (slotNumberText != null)
                slotNumberText.text = $"Slot {slot + 1}";
            
            // Disable buttons
            if (loadButton != null)
                loadButton.interactable = false;
            
            if (deleteButton != null)
                deleteButton.interactable = false;
        }
    }
}
