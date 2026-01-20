using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("Input Providers")]
    [SerializeField] private KeyboardInputProvider keyboardInput;

    [SerializeField] private ESP32InputProvider esp32Input;

    [Header("Settings")]
    public bool preferESP32WhenAvailable = true;

    private IInputProvider activeInputProvider;
    private KitchenState kitchenState;

    private void Awake()
    {
        InitializeKitchenState();
    }

    private void Start()
    {
        if (kitchenState == null)
        {
            InitializeKitchenState();
        }
    }

    private void InitializeKitchenState()
    {
        kitchenState = KitchenState.Instance;
        
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

        if (esp32Input == null)
        {
            esp32Input = FindFirstObjectByType<ESP32InputProvider>();
        }

        SelectActiveInputProvider();
    }

    private void Update()
    {
        if (preferESP32WhenAvailable && Time.frameCount % 60 == 0)
        {
            SelectActiveInputProvider();
        }

        if (activeInputProvider == null)
        {
            return;
        }
        
        if (!activeInputProvider.IsActive())
        {
            return;
        }
        
        activeInputProvider.UpdateInput();

        if (activeInputProvider.GetFireOn())
        {
            kitchenState.SetFire(true);
        }
        if (activeInputProvider.GetFireOff())
        {
            kitchenState.SetFire(false);
        }

        if (activeInputProvider.GetWaterOn())
        {
            kitchenState.SetWater(true);
        }
        if (activeInputProvider.GetWaterOff())
        {
            kitchenState.SetWater(false);
        }

        Vector2 joystickDir = activeInputProvider.GetJoystickDirection();
        if (joystickDir.magnitude > 0.1f)
        {
            kitchenState.MoveSelection(joystickDir);
        }

        float panIntensity = activeInputProvider.GetPanMovementIntensity();
        kitchenState.SetPanMovementIntensity(panIntensity);
    }

    private void SelectActiveInputProvider()
    {
        if (preferESP32WhenAvailable && esp32Input != null && esp32Input.IsActive())
        {
            activeInputProvider = esp32Input;
            if (keyboardInput != null)
            {
                keyboardInput.SetActive(false);
            }
        }
        else
        {
            activeInputProvider = keyboardInput;
            if (keyboardInput != null)
            {
                keyboardInput.SetActive(true);
            }
        }
    }

    public void SetActiveInputProvider(IInputProvider provider)
    {
        activeInputProvider = provider;
    }

    public IInputProvider GetActiveInputProvider()
    {
        return activeInputProvider;
    }
}
