using UnityEngine;

/// <summary>
/// Manages which input provider is currently active.
/// This script bridges input providers to the game logic (KitchenState).
/// It automatically switches between keyboard and ESP32 input when available.
/// </summary>
public class InputManager : MonoBehaviour
{
    [Header("Input Providers")]
    [Tooltip("Keyboard input provider (always available)")]
    [SerializeField] private KeyboardInputProvider keyboardInput;

    [Tooltip("ESP32 input provider (optional, for future use)")]
    [SerializeField] private ESP32InputProvider esp32Input;

    [Header("Settings")]
    [Tooltip("Priority: if ESP32 is connected, use it instead of keyboard")]
    public bool preferESP32WhenAvailable = true;

    private IInputProvider activeInputProvider;
    private KitchenState kitchenState;

    private void Awake()
    {
        // Find or get KitchenState
        InitializeKitchenState();
    }

    private void Start()
    {
        // Try again in Start if not found in Awake (in case of initialization order issues)
        if (kitchenState == null)
        {
            InitializeKitchenState();
        }
    }

    private void InitializeKitchenState()
    {
        // Try to get singleton instance first
        kitchenState = KitchenState.Instance;
        
        // If not found, try to find it in the scene
        if (kitchenState == null)
        {
            kitchenState = FindFirstObjectByType<KitchenState>();
        }
        
        if (kitchenState == null)
        {
            Debug.LogError("KitchenState not found! Please add KitchenState component to a GameObject in the scene.");
            enabled = false;
            return;
        }

        // Initialize keyboard input if not assigned
        if (keyboardInput == null)
        {
            keyboardInput = FindFirstObjectByType<KeyboardInputProvider>();
            if (keyboardInput == null)
            {
                Debug.LogError("KeyboardInputProvider not found! Please add it to a GameObject.");
                enabled = false;
                return;
            }
        }

        // ESP32 input is optional (will be null until implemented)
        if (esp32Input == null)
        {
            esp32Input = FindFirstObjectByType<ESP32InputProvider>();
        }

        // Set initial active provider
        SelectActiveInputProvider();
    }

    private void Update()
    {
        // Check if we should switch input providers (e.g., ESP32 just connected)
        // Only check occasionally to avoid performance issues
        if (preferESP32WhenAvailable && Time.frameCount % 60 == 0) // Check every 60 frames (about once per second)
        {
            SelectActiveInputProvider();
        }

        // Update active input provider
        if (activeInputProvider == null)
        {
            if (Time.frameCount % 120 == 0) // Log occasionally to avoid spam
            {
                Debug.LogWarning("InputManager: activeInputProvider is NULL!");
            }
            return;
        }
        
        if (!activeInputProvider.IsActive())
        {
            if (Time.frameCount % 120 == 0) // Log occasionally to avoid spam
            {
                Debug.LogWarning($"InputManager: activeInputProvider ({activeInputProvider.GetType().Name}) is not active!");
            }
            return;
        }
        
        activeInputProvider.UpdateInput();

        // Process fire input
        if (activeInputProvider.GetFireOn())
        {
            kitchenState.SetFire(true);
        }
        if (activeInputProvider.GetFireOff())
        {
            kitchenState.SetFire(false);
        }

        // Process water input
        if (activeInputProvider.GetWaterOn())
        {
            kitchenState.SetWater(true);
        }
        if (activeInputProvider.GetWaterOff())
        {
            kitchenState.SetWater(false);
        }

        // Process joystick input
        Vector2 joystickDir = activeInputProvider.GetJoystickDirection();
        if (joystickDir.magnitude > 0.1f)
        {
            kitchenState.MoveSelection(joystickDir);
        }

        // Process pan movement input (FSR)
        float panIntensity = activeInputProvider.GetPanMovementIntensity();
        kitchenState.SetPanMovementIntensity(panIntensity);
    }

    /// <summary>
    /// Selects which input provider should be active based on availability.
    /// </summary>
    private void SelectActiveInputProvider()
    {
        if (preferESP32WhenAvailable && esp32Input != null && esp32Input.IsActive())
        {
            // ESP32 is available and preferred
            activeInputProvider = esp32Input;
            if (keyboardInput != null)
            {
                keyboardInput.SetActive(false); // Disable keyboard when ESP32 is active
            }
        }
        else
        {
            // Use keyboard as fallback
            activeInputProvider = keyboardInput;
            if (keyboardInput != null)
            {
                keyboardInput.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Manually set the active input provider (for testing or special cases).
    /// </summary>
    public void SetActiveInputProvider(IInputProvider provider)
    {
        activeInputProvider = provider;
    }

    /// <summary>
    /// Get the currently active input provider.
    /// </summary>
    public IInputProvider GetActiveInputProvider()
    {
        return activeInputProvider;
    }
}
