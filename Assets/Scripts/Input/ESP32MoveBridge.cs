using UnityEngine;
using StarterAssets;

/// <summary>
/// Bridges ESP32 OSC digital movement buttons (/move L R F B) into StarterAssets FirstPersonController movement.
///
/// Expected OSC address:
/// - "/move" with 4 int values: Left, Right, Forward, Back
/// Values are usually 0/1 (or 0=pressed,1=released with INPUT_PULLUP). Configure pressed logic below.
///
/// This component injects into StarterAssetsInputs.move in a non-accumulating way (removes previous injection first),
/// so mouse/keyboard input can still work (optional).
/// </summary>
public class ESP32MoveBridge : MonoBehaviour
{
    [Header("Enable")]
    public bool enableMove = true;

    [Header("Sources")]
    [Tooltip("OSC receiver that holds the latest /move values. If null, will try to auto-find.")]
    public ESP32OSCReceiver oscReceiver;

    [Tooltip("StarterAssets input component to inject move into. If null, will try to use one on the same GameObject.")]
    public StarterAssetsInputs starterInputs;

    [Header("Connection Gate (optional)")]
    public bool requireOSCConnected = true;
    public float oscTimeoutSeconds = 1.5f;

    [Header("Button Mapping")]
    [Tooltip("If true, any non-zero value is treated as pressed.")]
    public bool treatNonZeroAsPressed = true;

    [Tooltip("If treatNonZeroAsPressed is false, this exact value is treated as pressed.")]
    public int pressedValue = 1;

    [Tooltip("Multiplier applied to resulting move vector. 1 = normal. Use -1 to invert both axes.")]
    public float moveSensitivity = 1f;

    [Tooltip("If true, diagonal input is normalized (so speed is not faster on diagonals).")]
    public bool normalizeDiagonal = true;

    private Vector2 _lastInjected;

    private void Reset()
    {
        starterInputs = GetComponent<StarterAssetsInputs>();
    }

    private void OnDisable()
    {
        // Undo our last injection so we don't leave a stuck move value behind.
        if (starterInputs != null)
        {
            starterInputs.move -= _lastInjected;
        }
        _lastInjected = Vector2.zero;
    }

    private void LateUpdate()
    {
        if (!enableMove) return;

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

        // Remove previous injection first so we don't accumulate if StarterAssets overwrites move elsewhere.
        starterInputs.move -= _lastInjected;
        _lastInjected = Vector2.zero;

        if (requireOSCConnected && !oscReceiver.IsConnected(oscTimeoutSeconds))
        {
            // When disconnected, leave only the "base" move input (keyboard, etc.).
            return;
        }

        bool left = IsPressed(oscReceiver.moveLeft);
        bool right = IsPressed(oscReceiver.moveRight);
        bool forward = IsPressed(oscReceiver.moveForward);
        bool back = IsPressed(oscReceiver.moveBack);

        float x = (right ? 1f : 0f) - (left ? 1f : 0f);
        float y = (forward ? 1f : 0f) - (back ? 1f : 0f);

        Vector2 injected = new Vector2(x, y);
        if (normalizeDiagonal && injected.sqrMagnitude > 1.01f)
        {
            injected = injected.normalized;
        }

        injected *= moveSensitivity;

        starterInputs.move += injected;
        _lastInjected = injected;
    }

    private bool IsPressed(int v)
    {
        return treatNonZeroAsPressed ? v != 0 : v == pressedValue;
    }
}

