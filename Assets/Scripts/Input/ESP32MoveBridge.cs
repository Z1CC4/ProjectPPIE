using UnityEngine;
using StarterAssets;

public class ESP32MoveBridge : MonoBehaviour
{
    [Header("Enable")]
    public bool enableMove = true;

    [Header("Sources")]
    public ESP32OSCReceiver oscReceiver;

    public StarterAssetsInputs starterInputs;

    [Header("Connection Gate (optional)")]
    public bool requireOSCConnected = true;
    public float oscTimeoutSeconds = 1.5f;

    [Header("Button Mapping")]
    public bool treatNonZeroAsPressed = true;

    public int pressedValue = 1;

    public float moveSensitivity = 1f;

    public bool normalizeDiagonal = true;

    private Vector2 _lastInjected;

    private void Reset()
    {
        starterInputs = GetComponent<StarterAssetsInputs>();
    }

    private void OnDisable()
    {
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

        starterInputs.move -= _lastInjected;
        _lastInjected = Vector2.zero;

        if (requireOSCConnected && !oscReceiver.IsConnected(oscTimeoutSeconds))
        {
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

