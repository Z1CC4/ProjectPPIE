using UnityEngine;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Bridges ESP32 OSC joystick (/joystick X Y Button) into StarterAssets FirstPersonController look input.
///
/// - Does NOT touch movement keys (WASD), sprint (Shift) or jump (Space).
/// - Injects into StarterAssetsInputs.look in a non-accumulating way (subtracts previous injection first).
/// - Adds optional deadzone + smoothing to reduce drift and jitter at low OSC rates (e.g., 16 Hz).
/// </summary>
public class ESP32JoystickLookBridge : MonoBehaviour
{
    [Header("Enable")]
    public bool enableJoystickLook = true;

    [Header("Sources")]
    [Tooltip("OSC receiver that holds the latest /joystick values. If null, will try to auto-find.")]
    public ESP32OSCReceiver oscReceiver;

    [Tooltip("StarterAssets input component to inject look into. If null, will try to use one on the same GameObject.")]
    public StarterAssetsInputs starterInputs;

#if ENABLE_INPUT_SYSTEM
    [Tooltip("Optional: used to match StarterAssets' 'KeyboardMouse' deltaTime behavior.")]
    public PlayerInput playerInput;
#endif

    [Header("Connection Gate (optional)")]
    public bool requireOSCConnected = true;
    public float oscTimeoutSeconds = 1.5f;

    [Header("Joystick Processing")]
    [Range(0f, 1f)]
    public float deadzone = 0.20f;

    [Tooltip("If true, swap X and Y coming from /joystick (useful if axes are wired/encoded swapped).")]
    public bool swapXY = false;

    [Tooltip("If true, invert X (left/right).")]
    public bool invertX = false;

    [Tooltip("If true, invert Y (useful if pushing up should look up but feels reversed).")]
    public bool invertY = false;

    [Tooltip("Units injected into StarterAssetsInputs.look. Higher = faster camera rotation.")]
    public float lookSensitivity = 250f;

    [Tooltip("Smooth time (seconds). 0 = no smoothing.")]
    [Range(0f, 0.5f)]
    public float smoothingTime = 0.08f;

    [Header("Button (optional)")]
    public bool onlyWhenButtonPressed = false;
    [Tooltip("ESP32 uses INPUT_PULLUP: 0=pressed, 1=released by default.")]
    public int pressedValue = 0;

    [Header("JOY Interact (replaces Reset View)")]
    [Tooltip("If true, a JOY button press will call PlayerInteractor.Interact().")]
    public bool interactOnButtonPress = true;

    [Tooltip("Interactor that will receive the Interact() call. If null, will try to find one in the scene.")]
    public PlayerInteractor interactor;

    private Vector2 _smoothed;
    private Vector2 _smoothVelocity;
    private Vector2 _lastInjected;
    private int _prevButton;

    private void Reset()
    {
        starterInputs = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
        playerInput = GetComponent<PlayerInput>();
#endif
    }

    private void Start()
    {
        _prevButton = 1 - pressedValue; // assume "not pressed" at boot
    }

    private void OnDisable()
    {
        // Undo our last injection so we don't leave a stuck look value behind.
        if (starterInputs != null)
        {
            starterInputs.look -= _lastInjected;
        }
        _lastInjected = Vector2.zero;
    }

    private void Update()
    {
        if (oscReceiver == null)
        {
            oscReceiver = ESP32OSCReceiver.Instance != null
                ? ESP32OSCReceiver.Instance
                : FindFirstObjectByType<ESP32OSCReceiver>();
            if (oscReceiver == null) return;
        }

#if ENABLE_INPUT_SYSTEM
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }
#endif

        if (interactOnButtonPress && interactor == null)
        {
            interactor = FindFirstObjectByType<PlayerInteractor>();
        }

        if (requireOSCConnected && !oscReceiver.IsConnected(oscTimeoutSeconds))
        {
            // When disconnected, undo any previous injection and do nothing.
            if (starterInputs != null)
            {
                starterInputs.look -= _lastInjected;
            }
            _lastInjected = Vector2.zero;
            return;
        }

        // Handle button edge (JOY press) even if look is disabled.
        int btnNow = oscReceiver.joyButton;
        bool wasPressed = _prevButton == pressedValue;
        bool isPressed = btnNow == pressedValue;
        if (!wasPressed && isPressed)
        {
            if (interactOnButtonPress && interactor != null)
            {
                interactor.Interact();
            }
        }
        _prevButton = btnNow;

        if (!enableJoystickLook)
        {
            // If look gets disabled at runtime, remove any leftover injection.
            if (starterInputs != null)
            {
                starterInputs.look -= _lastInjected;
            }
            _lastInjected = Vector2.zero;
            return;
        }

        if (starterInputs == null)
        {
            starterInputs = GetComponent<StarterAssetsInputs>();
            if (starterInputs == null) return;
        }

        // Remove previous frame injection first so we don't accumulate if StarterAssets doesn't overwrite look every frame.
        starterInputs.look -= _lastInjected;
        _lastInjected = Vector2.zero;

        // Raw joystick is already normalized to about [-1..+1].
        Vector2 raw = new Vector2(oscReceiver.joyX, oscReceiver.joyY);
        if (swapXY) raw = new Vector2(raw.y, raw.x);
        if (invertX) raw.x = -raw.x;
        if (invertY) raw.y = -raw.y;

        raw = ApplyDeadzone(raw, deadzone);

        if (onlyWhenButtonPressed && oscReceiver.joyButton != pressedValue)
        {
            raw = Vector2.zero;
        }

        Vector2 target = raw * lookSensitivity;
        Vector2 injected = smoothingTime > 0f
            ? Vector2.SmoothDamp(_smoothed, target, ref _smoothVelocity, smoothingTime)
            : target;

        _smoothed = injected;

        // StarterAssets FirstPersonController special-cases "KeyboardMouse" by NOT multiplying by deltaTime.
        // To keep a consistent feel, we scale by deltaTime for that scheme only.
#if ENABLE_INPUT_SYSTEM
        bool treatAsMouse = playerInput != null && playerInput.currentControlScheme == "KeyboardMouse";
#else
        bool treatAsMouse = true;
#endif
        if (treatAsMouse)
        {
            injected *= Time.deltaTime;
        }

        starterInputs.look += injected;
        _lastInjected = injected;
    }

    private static Vector2 ApplyDeadzone(Vector2 v, float dz)
    {
        if (dz <= 0f) return v;
        return v.magnitude < dz ? Vector2.zero : v;
    }

}

