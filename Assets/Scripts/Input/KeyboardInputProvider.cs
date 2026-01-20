using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardInputProvider : MonoBehaviour, IInputProvider
{
    [Header("Keyboard Input Settings")]
    [Tooltip("Key to turn fire ON")]
    public Key fireOnKey = Key.F;

    [Tooltip("Key to turn fire OFF")]
    public Key fireOffKey = Key.G;

    [Tooltip("Key to turn water ON")]
    public Key waterOnKey = Key.W;

    [Tooltip("Key to turn water OFF")]
    public Key waterOffKey = Key.S;

    [Header("Joystick Simulation")]
    [Tooltip("Use WASD for joystick (alternative to arrow keys)")]
    public bool useWASD = true;

    [Tooltip("Joystick movement speed (how fast selection moves)")]
    public float joystickSensitivity = 1.0f;

    [Header("Pan Movement (FSR Simulation)")]
    [Tooltip("Key to hold for pan movement (simulates FSR pressure)")]
    public Key panMoveKey = Key.Space;

    [Tooltip("Maximum movement intensity when key is held (0.0 to 1.0)")]
    [Range(0.0f, 1.0f)]
    public float maxPanIntensity = 1.0f;

    [Tooltip("How quickly intensity increases when key is held (per second)")]
    public float panIntensityIncreaseRate = 2.0f;

    [Tooltip("How quickly intensity decreases when key is released (per second)")]
    public float panIntensityDecreaseRate = 3.0f;

    private Vector2 currentJoystickDirection = Vector2.zero;
    private float currentPanIntensity = 0.0f;
    private bool isActive = true;
    private Keyboard keyboard;

    private void Awake()
    {
        keyboard = Keyboard.current;
    }

    private void OnEnable()
    {
        if (keyboard == null)
        {
            keyboard = Keyboard.current;
        }
    }

    public bool IsActive()
    {
        if (keyboard == null)
        {
            keyboard = Keyboard.current;
        }
        return isActive && keyboard != null;
    }

    public void UpdateInput()
    {
        if (keyboard == null)
        {
            keyboard = Keyboard.current;
            if (keyboard == null) return;
        }

        UpdateJoystickDirection();
        
        UpdatePanMovementIntensity();
    }

    public bool GetFireOn()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current[fireOnKey].wasPressedThisFrame;
    }

    public bool GetFireOff()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current[fireOffKey].wasPressedThisFrame;
    }

    public bool GetWaterOn()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current[waterOnKey].wasPressedThisFrame;
    }

    public bool GetWaterOff()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current[waterOffKey].wasPressedThisFrame;
    }

    public Vector2 GetJoystickDirection()
    {
        return currentJoystickDirection;
    }

    public float GetPanMovementIntensity()
    {
        return currentPanIntensity;
    }

    private void UpdateJoystickDirection()
    {
        if (keyboard == null)
        {
            currentJoystickDirection = Vector2.zero;
            return;
        }

        Vector2 direction = Vector2.zero;

        if (useWASD)
        {
            if (keyboard[Key.A].isPressed) direction.x -= 1f;
            if (keyboard[Key.D].isPressed) direction.x += 1f;
            if (keyboard[Key.W].isPressed) direction.y += 1f;
            if (keyboard[Key.S].isPressed) direction.y -= 1f;
        }
        else
        {
            if (keyboard[Key.LeftArrow].isPressed) direction.x -= 1f;
            if (keyboard[Key.RightArrow].isPressed) direction.x += 1f;
            if (keyboard[Key.UpArrow].isPressed) direction.y += 1f;
            if (keyboard[Key.DownArrow].isPressed) direction.y -= 1f;
        }

        if (direction.magnitude > 0.1f)
        {
            direction = direction.normalized * joystickSensitivity;
        }
        else
        {
            direction = Vector2.zero;
        }

        currentJoystickDirection = direction;
    }

    private void UpdatePanMovementIntensity()
    {
        if (keyboard == null)
        {
            currentPanIntensity = 0.0f;
            return;
        }

        bool isKeyHeld = keyboard[panMoveKey].isPressed;

        if (isKeyHeld)
        {
            currentPanIntensity += panIntensityIncreaseRate * Time.deltaTime;
            currentPanIntensity = Mathf.Clamp01(currentPanIntensity) * maxPanIntensity;
        }
        else
        {
            currentPanIntensity -= panIntensityDecreaseRate * Time.deltaTime;
            currentPanIntensity = Mathf.Max(0.0f, currentPanIntensity);
        }
    }

    public void SetActive(bool active)
    {
        isActive = active;
    }
}
