using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using FPS.Managers;

namespace FPS.Input
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }
        
        [Header("Input Actions")]
        [SerializeField] private InputActionAsset inputActions;
        
        // Input Action Maps
        private InputActionMap playerActionMap;
        private InputActionMap uiActionMap;
        private InputActionMap menuActionMap;
        
        // Player Actions
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction crouchAction;
        private InputAction sprintAction;
        private InputAction fireAction;
        private InputAction aimAction;
        private InputAction reloadAction;
        private InputAction interactAction;
        private InputAction inventoryAction;
        private InputAction craftingAction;
        private InputAction pauseAction;
        
        // UI Actions
        private InputAction navigateAction;
        private InputAction submitAction;
        private InputAction cancelAction;
        
        // Input Events
        public System.Action<Vector2> OnMove;
        public System.Action<Vector2> OnLook;
        public System.Action OnJump;
        public System.Action OnJumpReleased;
        public System.Action OnCrouch;
        public System.Action OnCrouchReleased;
        public System.Action OnSprint;
        public System.Action OnSprintReleased;
        public System.Action OnFire;
        public System.Action OnFireReleased;
        public System.Action OnAim;
        public System.Action OnAimReleased;
        public System.Action OnReload;
        public System.Action OnInteract;
        public System.Action OnInventory;
        public System.Action OnCrafting;
        public System.Action OnPause;
        
        // Input State
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool isJumping;
        private bool isCrouching;
        private bool isSprinting;
        private bool isFiring;
        private bool isAiming;
        
        // Settings
        private float mouseSensitivity = 1f;
        private bool invertYAxis = false;
        private float gamepadSensitivity = 1f;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeInputSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            LoadInputSettings();
            EnablePlayerInput();
        }
        
        private void InitializeInputSystem()
        {
            if (inputActions == null)
            {
                Debug.LogError("Input Actions asset is not assigned!");
                return;
            }
            
            // Get action maps
            playerActionMap = inputActions.FindActionMap("Player");
            uiActionMap = inputActions.FindActionMap("UI");
            menuActionMap = inputActions.FindActionMap("Menu");
            
            // Get player actions
            moveAction = playerActionMap?.FindAction("Move");
            lookAction = playerActionMap?.FindAction("Look");
            jumpAction = playerActionMap?.FindAction("Jump");
            crouchAction = playerActionMap?.FindAction("Crouch");
            sprintAction = playerActionMap?.FindAction("Sprint");
            fireAction = playerActionMap?.FindAction("Fire");
            aimAction = playerActionMap?.FindAction("Aim");
            reloadAction = playerActionMap?.FindAction("Reload");
            interactAction = playerActionMap?.FindAction("Interact");
            inventoryAction = playerActionMap?.FindAction("Inventory");
            craftingAction = playerActionMap?.FindAction("Crafting");
            pauseAction = playerActionMap?.FindAction("Pause");
            
            // Get UI actions
            navigateAction = uiActionMap?.FindAction("Navigate");
            submitAction = uiActionMap?.FindAction("Submit");
            cancelAction = uiActionMap?.FindAction("Cancel");
            
            SetupInputCallbacks();
        }
        
        private void SetupInputCallbacks()
        {
            // Player input callbacks
            if (moveAction != null)
            {
                moveAction.performed += OnMovePerformed;
                moveAction.canceled += OnMoveCanceled;
            }
            
            if (lookAction != null)
            {
                lookAction.performed += OnLookPerformed;
                lookAction.canceled += OnLookCanceled;
            }
            
            if (jumpAction != null)
            {
                jumpAction.performed += OnJumpPerformed;
                jumpAction.canceled += OnJumpCanceled;
            }
            
            if (crouchAction != null)
            {
                crouchAction.performed += OnCrouchPerformed;
                crouchAction.canceled += OnCrouchCanceled;
            }
            
            if (sprintAction != null)
            {
                sprintAction.performed += OnSprintPerformed;
                sprintAction.canceled += OnSprintCanceled;
            }
            
            if (fireAction != null)
            {
                fireAction.performed += OnFirePerformed;
                fireAction.canceled += OnFireCanceled;
            }
            
            if (aimAction != null)
            {
                aimAction.performed += OnAimPerformed;
                aimAction.canceled += OnAimCanceled;
            }
            
            if (reloadAction != null)
            {
                reloadAction.performed += OnReloadPerformed;
            }
            
            if (interactAction != null)
            {
                interactAction.performed += OnInteractPerformed;
            }
            
            if (inventoryAction != null)
            {
                inventoryAction.performed += OnInventoryPerformed;
            }
            
            if (craftingAction != null)
            {
                craftingAction.performed += OnCraftingPerformed;
            }
            
            if (pauseAction != null)
            {
                pauseAction.performed += OnPausePerformed;
            }
        }
        
        #region Input Callbacks
        
        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
            OnMove?.Invoke(moveInput);
        }
        
        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            moveInput = Vector2.zero;
            OnMove?.Invoke(moveInput);
        }
        
        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            Vector2 rawInput = context.ReadValue<Vector2>();
            
            // Apply sensitivity
            lookInput = new Vector2(
                rawInput.x * mouseSensitivity,
                rawInput.y * (invertYAxis ? -1 : 1) * mouseSensitivity
            );
            
            OnLook?.Invoke(lookInput);
        }
        
        private void OnLookCanceled(InputAction.CallbackContext context)
        {
            lookInput = Vector2.zero;
            OnLook?.Invoke(lookInput);
        }
        
        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            isJumping = true;
            OnJump?.Invoke();
        }
        
        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            isJumping = false;
            OnJumpReleased?.Invoke();
        }
        
        private void OnCrouchPerformed(InputAction.CallbackContext context)
        {
            isCrouching = true;
            OnCrouch?.Invoke();
        }
        
        private void OnCrouchCanceled(InputAction.CallbackContext context)
        {
            isCrouching = false;
            OnCrouchReleased?.Invoke();
        }
        
        private void OnSprintPerformed(InputAction.CallbackContext context)
        {
            isSprinting = true;
            OnSprint?.Invoke();
        }
        
        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            isSprinting = false;
            OnSprintReleased?.Invoke();
        }
        
        private void OnFirePerformed(InputAction.CallbackContext context)
        {
            isFiring = true;
            OnFire?.Invoke();
        }
        
        private void OnFireCanceled(InputAction.CallbackContext context)
        {
            isFiring = false;
            OnFireReleased?.Invoke();
        }
        
        private void OnAimPerformed(InputAction.CallbackContext context)
        {
            isAiming = true;
            OnAim?.Invoke();
        }
        
        private void OnAimCanceled(InputAction.CallbackContext context)
        {
            isAiming = false;
            OnAimReleased?.Invoke();
        }
        
        private void OnReloadPerformed(InputAction.CallbackContext context)
        {
            OnReload?.Invoke();
        }
        
        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            OnInteract?.Invoke();
        }
        
        private void OnInventoryPerformed(InputAction.CallbackContext context)
        {
            OnInventory?.Invoke();
        }
        
        private void OnCraftingPerformed(InputAction.CallbackContext context)
        {
            OnCrafting?.Invoke();
        }
        
        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            OnPause?.Invoke();
        }
        
        #endregion
        
        #region Input State Management
        
        public void EnablePlayerInput()
        {
            playerActionMap?.Enable();
            DisableUIInput();
        }
        
        public void EnableUIInput()
        {
            uiActionMap?.Enable();
            DisablePlayerInput();
        }
        
        public void EnableMenuInput()
        {
            menuActionMap?.Enable();
            DisablePlayerInput();
        }
        
        public void DisablePlayerInput()
        {
            playerActionMap?.Disable();
        }
        
        public void DisableUIInput()
        {
            uiActionMap?.Disable();
        }
        
        public void DisableMenuInput()
        {
            menuActionMap?.Disable();
        }
        
        public void DisableAllInput()
        {
            inputActions?.Disable();
        }
        
        public void EnableAllInput()
        {
            inputActions?.Enable();
        }
        
        #endregion
        
        #region Input Getters
        
        public Vector2 GetMoveInput()
        {
            return moveInput;
        }
        
        public Vector2 GetLookInput()
        {
            return lookInput;
        }
        
        public bool IsJumping()
        {
            return isJumping;
        }
        
        public bool IsCrouching()
        {
            return isCrouching;
        }
        
        public bool IsSprinting()
        {
            return isSprinting;
        }
        
        public bool IsFiring()
        {
            return isFiring;
        }
        
        public bool IsAiming()
        {
            return isAiming;
        }
        
        #endregion
        
        #region Settings
        
        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
            PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);
        }
        
        public void SetInvertYAxis(bool invert)
        {
            invertYAxis = invert;
            PlayerPrefs.SetInt("InvertYAxis", invert ? 1 : 0);
        }
        
        public void SetGamepadSensitivity(float sensitivity)
        {
            gamepadSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
            PlayerPrefs.SetFloat("GamepadSensitivity", gamepadSensitivity);
        }
        
        public float GetMouseSensitivity()
        {
            return mouseSensitivity;
        }
        
        public bool GetInvertYAxis()
        {
            return invertYAxis;
        }
        
        public float GetGamepadSensitivity()
        {
            return gamepadSensitivity;
        }
        
        private void LoadInputSettings()
        {
            mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
            invertYAxis = PlayerPrefs.GetInt("InvertYAxis", 0) == 1;
            gamepadSensitivity = PlayerPrefs.GetFloat("GamepadSensitivity", 1f);
        }
        
        #endregion
        
        #region Key Rebinding
        
        public void StartRebinding(string actionName, System.Action<bool> onComplete = null)
        {
            var action = playerActionMap?.FindAction(actionName);
            if (action == null)
            {
                onComplete?.Invoke(false);
                return;
            }
            
            var rebindOperation = action.PerformInteractiveRebinding()
                .WithControlsExcluding("Mouse")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(operation =>
                {
                    operation.Dispose();
                    onComplete?.Invoke(true);
                    SaveBindingOverrides();
                })
                .OnCancel(operation =>
                {
                    operation.Dispose();
                    onComplete?.Invoke(false);
                });
            
            rebindOperation.Start();
        }
        
        public void ResetBinding(string actionName)
        {
            var action = playerActionMap?.FindAction(actionName);
            if (action != null)
            {
                action.RemoveAllBindingOverrides();
                SaveBindingOverrides();
            }
        }
        
        public void ResetAllBindings()
        {
            foreach (var action in playerActionMap.actions)
            {
                action.RemoveAllBindingOverrides();
            }
            SaveBindingOverrides();
        }
        
        private void SaveBindingOverrides()
        {
            var rebinds = playerActionMap.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString("InputBindings", rebinds);
        }
        
        private void LoadBindingOverrides()
        {
            var rebinds = PlayerPrefs.GetString("InputBindings", string.Empty);
            if (!string.IsNullOrEmpty(rebinds))
            {
                playerActionMap.LoadBindingOverridesFromJson(rebinds);
            }
        }
        
        public string GetBindingDisplayString(string actionName)
        {
            var action = playerActionMap?.FindAction(actionName);
            return action?.GetBindingDisplayString() ?? "";
        }
        
        #endregion
        
        private void OnEnable()
        {
            inputActions?.Enable();
            LoadBindingOverrides();
        }
        
        private void OnDisable()
        {
            inputActions?.Disable();
        }
        
        private void OnDestroy()
        {
            // Clean up callbacks
            if (moveAction != null)
            {
                moveAction.performed -= OnMovePerformed;
                moveAction.canceled -= OnMoveCanceled;
            }
            
            if (lookAction != null)
            {
                lookAction.performed -= OnLookPerformed;
                lookAction.canceled -= OnLookCanceled;
            }
            
            // ... (cleanup other actions similarly)
        }
    }
}
