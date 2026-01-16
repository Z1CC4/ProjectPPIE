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

    [Header("JOY Reset View (optional)")]
    [Tooltip("If true, pressing JOY will reset the view to the baseline rotations captured at Start().")]
    public bool resetViewOnButtonPress = true;

    [Tooltip("If assigned, yaw reset will apply to this transform. If null, will use this GameObject's transform.")]
    public Transform playerYawRoot;

    [Tooltip("If assigned, pitch reset will apply to this target's localRotation X. If null, will try to use FirstPersonController.CinemachineCameraTarget.")]
    public Transform cameraPitchTarget;

    private Vector2 _smoothed;
    private Vector2 _smoothVelocity;
    private Vector2 _lastInjected;
    private int _prevButton;
    private bool _hasBaseline;
    private Quaternion _baselineYaw;
    private Quaternion _baselinePitchLocal;

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

        if (playerYawRoot == null)
        {
            playerYawRoot = transform;
        }

        if (cameraPitchTarget == null)
        {
            // Auto-wire to StarterAssets FirstPersonController target if present.
            var fpc = GetComponent<StarterAssets.FirstPersonController>();
            if (fpc != null && fpc.CinemachineCameraTarget != null)
            {
                cameraPitchTarget = fpc.CinemachineCameraTarget.transform;
            }
        }

        CaptureBaseline();
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
        if (!enableJoystickLook) return;

        if (starterInputs == null)
        {
            starterInputs = GetComponent<StarterAssetsInputs>();
            if (starterInputs == null) return;
        }

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

        // Remove previous frame injection first so we don't accumulate if StarterAssets doesn't overwrite look every frame.
        starterInputs.look -= _lastInjected;
        _lastInjected = Vector2.zero;

        if (requireOSCConnected && !oscReceiver.IsConnected(oscTimeoutSeconds))
        {
            // When disconnected, leave only the "base" look input (mouse, etc.).
            return;
        }

        // Handle button edge (JOY press) before applying look, so reset is immediate.
        if (resetViewOnButtonPress)
        {
            int btn = oscReceiver.joyButton;
            bool wasPressed = _prevButton == pressedValue;
            bool isPressed = btn == pressedValue;
            if (!wasPressed && isPressed)
            {
                ResetViewToBaseline();
            }
            _prevButton = btn;
        }

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

    private void CaptureBaseline()
    {
        if (playerYawRoot == null || cameraPitchTarget == null) return;
        _baselineYaw = playerYawRoot.rotation;
        _baselinePitchLocal = cameraPitchTarget.localRotation;
        _hasBaseline = true;
    }

    private void ResetViewToBaseline()
    {
        if (!_hasBaseline)
        {
            CaptureBaseline();
            if (!_hasBaseline) return;
        }

        // Remove any current injection so we don't fight the reset.
        if (starterInputs != null)
        {
            starterInputs.look -= _lastInjected;
        }
        _lastInjected = Vector2.zero;

        // Reset target rotations.
        if (playerYawRoot != null) playerYawRoot.rotation = _baselineYaw;
        if (cameraPitchTarget != null) cameraPitchTarget.localRotation = _baselinePitchLocal;

        // Reset smoothing state.
        _smoothed = Vector2.zero;
        _smoothVelocity = Vector2.zero;
    }
}

