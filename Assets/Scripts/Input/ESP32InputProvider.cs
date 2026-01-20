using UnityEngine;

public class ESP32InputProvider : MonoBehaviour, IInputProvider
{
    [Header("ESP32 Connection Settings")]
    [Tooltip("Enable ESP32 input (set to false to disable even if connected)")]
    public bool enableESP32Input = false;

    [Header("OSC (Wi-Fi) Settings")]
    [Tooltip("Use OSC input coming from ESP32OSCReceiver (Wi-Fi).")]
    public bool useOSC = true;

    [Tooltip("Receiver component that stores latest OSC values from the ESP32.")]
    public ESP32OSCReceiver oscReceiver;

    [Tooltip("If no OSC messages arrive for this many seconds, consider disconnected.")]
    public float oscTimeoutSeconds = 1.5f;

    [Tooltip("Raw value >= threshold means WATER is ON (if not inverted).")]
    public int waterOnThreshold = 1;

    [Tooltip("Raw value <= threshold means WATER is OFF (hysteresis). If <= 0, hysteresis is disabled and waterOnThreshold is used for both.")]
    public int waterOffThreshold = 0;

    [Tooltip("Raw value >= threshold means FIRE is ON (if not inverted).")]
    public int fireOnThreshold = 1;

    [Tooltip("Raw value <= threshold means FIRE is OFF (hysteresis). If <= 0, hysteresis is disabled and fireOnThreshold is used for both.")]
    public int fireOffThreshold = 0;

    [Tooltip("Invert water logic (useful if sensor reports 0 when active).")]
    public bool invertWater = false;

    [Tooltip("Invert fire logic (useful if sensor reports 0 when active).")]
    public bool invertFire = true;

    [Tooltip("Max raw FSR value from ESP32 ADC (used to normalize to 0..1).")]
    public int fsrMaxRaw = 4095;

    [Tooltip("Joystick deadzone for OSC joystick (-1..1).")]
    [Range(0.0f, 1.0f)]
    public float joystickDeadzone = 0.15f;

    [Tooltip("Simulate ESP32 input using keyboard (for testing without hardware)")]
    public bool simulateWithKeyboard = false;

    private bool isFireOn;
    private bool isFireOff;
    private bool isWaterOn;
    private bool isWaterOff;
    private Vector2 joystickDirection;
    private float panMovementIntensity;

    private bool isActive;

    private bool prevFireState;
    private bool prevWaterState;

    private void Awake()
    {
    }

    private void Start()
    {
        if (simulateWithKeyboard)
        {
            isActive = true;
        }
    }

    #region IInputProvider

    public void UpdateInput()
    {
        isFireOn = false;
        isFireOff = false;
        isWaterOn = false;
        isWaterOff = false;

        if (!enableESP32Input)
        {
            isActive = false;
            joystickDirection = Vector2.zero;
            panMovementIntensity = 0.0f;
            return;
        }

        if (simulateWithKeyboard)
        {
            UpdateSimulatedInput();
            return;
        }

        if (useOSC)
        {
            UpdateFromOSC();
            return;
        }

        isActive = false;
        joystickDirection = Vector2.zero;
        panMovementIntensity = 0.0f;
    }

    public bool GetFireOn() => isFireOn;
    public bool GetFireOff() => isFireOff;
    public bool GetWaterOn() => isWaterOn;
    public bool GetWaterOff() => isWaterOff;
    public Vector2 GetJoystickDirection() => joystickDirection;
    public float GetPanMovementIntensity() => panMovementIntensity;

    public bool IsActive()
    {
        if (!enableESP32Input) return false;

        if (simulateWithKeyboard)
        {
            return isActive;
        }

        if (useOSC)
        {
            if (oscReceiver == null)
            {
                oscReceiver = ESP32OSCReceiver.Instance != null
                    ? ESP32OSCReceiver.Instance
                    : FindFirstObjectByType<ESP32OSCReceiver>();
            }

            bool connected = oscReceiver != null && oscReceiver.IsConnected(oscTimeoutSeconds);
            isActive = connected;
            return connected;
        }

        return isActive;
    }

    #endregion

    #region Simulation Mode (Keyboard)

    private void UpdateSimulatedInput()
    {
        if (Input.GetKeyDown(KeyCode.F)) isFireOn = true;
        if (Input.GetKeyDown(KeyCode.G)) isFireOff = true;

        if (Input.GetKeyDown(KeyCode.T)) isWaterOn = true;
        if (Input.GetKeyDown(KeyCode.Y)) isWaterOff = true;

        Vector2 dir = Vector2.zero;
        if (Input.GetKey(KeyCode.LeftArrow)) dir.x -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) dir.x += 1f;
        if (Input.GetKey(KeyCode.UpArrow)) dir.y += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) dir.y -= 1f;
        joystickDirection = dir.magnitude > 0.1f ? dir.normalized : Vector2.zero;

        if (Input.GetKey(KeyCode.LeftControl))
        {
            panMovementIntensity = Mathf.Clamp01(panMovementIntensity + 2.0f * Time.deltaTime);
        }
        else
        {
            panMovementIntensity = Mathf.Max(0.0f, panMovementIntensity - 3.0f * Time.deltaTime);
        }

        isActive = true;
    }

    #endregion

    #region OSC (Wi-Fi)

    private void UpdateFromOSC()
    {
        if (oscReceiver == null)
        {
            oscReceiver = ESP32OSCReceiver.Instance != null
                ? ESP32OSCReceiver.Instance
                : FindFirstObjectByType<ESP32OSCReceiver>();
        }

        if (oscReceiver == null)
        {
            isActive = false;
            joystickDirection = Vector2.zero;
            panMovementIntensity = 0.0f;
            return;
        }

        isActive = oscReceiver.IsConnected(oscTimeoutSeconds);
        if (!isActive)
        {
            joystickDirection = Vector2.zero;
            panMovementIntensity = 0.0f;
            return;
        }

        bool waterState = GetWaterStateWithThresholds(oscReceiver.water, prevWaterState);

        bool fireState = GetFireStateWithThresholds(oscReceiver.fire, prevFireState);

        isWaterOn = !prevWaterState && waterState;
        isWaterOff = prevWaterState && !waterState;
        prevWaterState = waterState;

        isFireOn = !prevFireState && fireState;
        isFireOff = prevFireState && !fireState;
        prevFireState = fireState;

        Vector2 joy = new Vector2(oscReceiver.joyX, oscReceiver.joyY);
        joystickDirection = joy.magnitude >= joystickDeadzone ? joy : Vector2.zero;

        if (fsrMaxRaw <= 0) fsrMaxRaw = 4095;
        panMovementIntensity = Mathf.Clamp01(oscReceiver.fsr / (float)fsrMaxRaw);
    }

    private bool GetWaterStateWithThresholds(int waterRaw, bool previousState)
    {
        if (waterRaw == 0 || waterRaw == 1)
        {
            bool binaryState = waterRaw >= 1;
            if (invertWater) binaryState = !binaryState;
            return binaryState;
        }

        int offTh = waterOffThreshold > 0 ? waterOffThreshold : waterOnThreshold;
        int onTh = waterOnThreshold;

        bool state;
        if (waterOffThreshold > 0)
        {
            state = previousState ? waterRaw > offTh : waterRaw >= onTh;
        }
        else
        {
            state = waterRaw >= onTh;
        }

        if (invertWater) state = !state;
        return state;
    }

    private bool GetFireStateWithThresholds(int fireRaw, bool previousState)
    {
        if (fireRaw == 0 || fireRaw == 1)
        {
            bool binaryState = fireRaw >= 1;
            if (invertFire) binaryState = !binaryState;
            return binaryState;
        }

        int offTh = fireOffThreshold > 0 ? fireOffThreshold : fireOnThreshold;
        int onTh = fireOnThreshold;

        bool state;
        if (fireOffThreshold > 0)
        {
            state = previousState ? fireRaw > offTh : fireRaw >= onTh;
        }
        else
        {
            state = fireRaw >= onTh;
        }

        if (invertFire) state = !state;
        return state;
    }

    #endregion

    private void InitializeSerialConnection()
    {
    }

    private void ReadSerialData()
    {
    }

    private void ParseSerialCommand(string command)
    {
    }

    private void CloseSerialConnection()
    {
    }
}
