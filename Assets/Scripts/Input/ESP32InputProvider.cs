using UnityEngine;

/// <summary>
/// ESP32 Input Provider.
///
/// Reads ESP32 inputs via OSC (recommended) or via keyboard simulation.
/// This provider feeds KitchenState through InputManager:
/// - Fire: one-frame ON/OFF events (edge-triggered)
/// - Water: one-frame ON/OFF events (edge-triggered)
/// - Joystick: Vector2 for selection movement
/// - FSR: 0..1 intensity
///
/// NOTE: This script does NOT implement FPS camera look.
/// </summary>
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

    [Header("Debug")]
    [Tooltip("Simulate ESP32 input using keyboard (for testing without hardware)")]
    public bool simulateWithKeyboard = false;

    // Current state (one-frame events for water/fire)
    private bool isFireOn;
    private bool isFireOff;
    private bool isWaterOn;
    private bool isWaterOff;
    private Vector2 joystickDirection;
    private float panMovementIntensity;

    private bool isActive;

    // Edge detection for OSC stateful sensors.
    private bool prevFireState;
    private bool prevWaterState;

    private void Awake()
    {
        // Do not override Inspector defaults here.
        // Fire inversion depends on how the sensor is wired (active-high vs active-low).
    }

    private void Start()
    {
        if (simulateWithKeyboard)
        {
            Debug.LogWarning("ESP32InputProvider: Running in simulation mode (keyboard). Real ESP32 connection is not active.");
            isActive = true;
        }
    }

    #region IInputProvider

    public void UpdateInput()
    {
        // Reset one-time flags (they will be re-set during this UpdateInput call if transitions occur).
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

        // Serial mode not implemented.
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
        // Fire: F on / G off
        if (Input.GetKeyDown(KeyCode.F)) isFireOn = true;
        if (Input.GetKeyDown(KeyCode.G)) isFireOff = true;

        // Water: T on / Y off (avoid WASD to prevent conflicts with FPS controllers)
        if (Input.GetKeyDown(KeyCode.T)) isWaterOn = true;
        if (Input.GetKeyDown(KeyCode.Y)) isWaterOff = true;

        // Joystick: arrows for selection (no WASD here to avoid conflicts with FPS controllers)
        Vector2 dir = Vector2.zero;
        if (Input.GetKey(KeyCode.LeftArrow)) dir.x -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) dir.x += 1f;
        if (Input.GetKey(KeyCode.UpArrow)) dir.y += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) dir.y -= 1f;
        joystickDirection = dir.magnitude > 0.1f ? dir.normalized : Vector2.zero;

        // FSR simulated with LeftControl hold (avoid Space to prevent conflicts with Jump)
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

        // Water/Fire are treated as stateful booleans; we emit ON/OFF one-frame events on transitions.
        //
        // NOTE: /water can be either digital (0/1) or analog RAW (0..4095).
        // For analog sensors, use waterOnThreshold + optional hysteresis (waterOffThreshold) to avoid flicker.
        bool waterState = GetWaterStateWithThresholds(oscReceiver.water, prevWaterState);

        // NOTE: /fire can also be either digital (0/1) or analog RAW (0..4095), depending on ESP32 firmware/sensor.
        // For analog sensors, use fireOnThreshold + optional hysteresis (fireOffThreshold) to avoid "always on" at idle.
        bool fireState = GetFireStateWithThresholds(oscReceiver.fire, prevFireState);

        isWaterOn = !prevWaterState && waterState;
        isWaterOff = prevWaterState && !waterState;
        prevWaterState = waterState;

        isFireOn = !prevFireState && fireState;
        isFireOff = prevFireState && !fireState;
        prevFireState = fireState;

        // Joystick (-1..1 floats) with deadzone.
        Vector2 joy = new Vector2(oscReceiver.joyX, oscReceiver.joyY);
        joystickDirection = joy.magnitude >= joystickDeadzone ? joy : Vector2.zero;

        // FSR raw -> 0..1
        if (fsrMaxRaw <= 0) fsrMaxRaw = 4095;
        panMovementIntensity = Mathf.Clamp01(oscReceiver.fsr / (float)fsrMaxRaw);
    }

    private bool GetWaterStateWithThresholds(int waterRaw, bool previousState)
    {
        // If ESP32 sends a digital 0/1, treat it as binary regardless of threshold settings.
        // Convention: 1 = ON, 0 = OFF (can be flipped via invertWater).
        if (waterRaw == 0 || waterRaw == 1)
        {
            bool binaryState = waterRaw >= 1;
            if (invertWater) binaryState = !binaryState;
            return binaryState;
        }

        // Optional hysteresis: ON threshold higher than OFF threshold.
        int offTh = waterOffThreshold > 0 ? waterOffThreshold : waterOnThreshold;
        int onTh = waterOnThreshold;

        bool state;
        if (waterOffThreshold > 0)
        {
            // Hysteresis behavior:
            // - if currently OFF, require waterRaw >= onTh to switch ON
            // - if currently ON, require waterRaw <= offTh to switch OFF
            state = previousState ? waterRaw > offTh : waterRaw >= onTh;
        }
        else
        {
            // Simple threshold.
            state = waterRaw >= onTh;
        }

        if (invertWater) state = !state;
        return state;
    }

    private bool GetFireStateWithThresholds(int fireRaw, bool previousState)
    {
        // If ESP32 sends a digital 0/1, treat it as binary regardless of threshold settings.
        // Convention: 1 = ON, 0 = OFF (can be flipped via invertFire, e.g. active-low sensors).
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
            // Hysteresis:
            // - if currently OFF, require fireRaw >= onTh to switch ON
            // - if currently ON, require fireRaw <= offTh to switch OFF
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

    /// <summary>
    /// Initializes serial port connection to ESP32.
    /// This will connect to the ESP32 device and start reading sensor data.
    /// 
    /// ESP32 Hardware Setup:
    /// - Fire Sensor: Connected to digital pin (e.g., GPIO 2)
    /// - Water Sensor: Connected to digital pin (e.g., GPIO 3)
    /// - Joystick: X-axis on analog pin A0, Y-axis on analog pin A1
    /// - FSR Sensor: Connected to analog pin A2 (for pan movement intensity)
    /// 
    /// NOTE: You may need to install a serial port library for Unity, such as:
    /// - System.IO.Ports (requires .NET 4.x or newer)
    /// - Or a Unity-compatible serial port asset from Asset Store
    /// 
    /// TODO: Implement using System.IO.Ports.SerialPort or Unity's SerialPort library.
    /// </summary>
    private void InitializeSerialConnection()
    {
        // TODO: Implementation example:
        // using System.IO.Ports;
        // 
        // try
        // {
        //     serialPort = new SerialPort(serialPort, baudRate);
        //     serialPort.Open();
        //     serialPort.ReadTimeout = 100;
        //     serialPort.NewLine = "\n"; // ESP32 sends commands with newline
        //     isActive = true;
        //     Debug.Log($"ESP32 connected on {serialPort} at {baudRate} baud");
        // }
        // catch (System.Exception e)
        // {
        //     Debug.LogError($"Failed to connect to ESP32 on {serialPort}: {e.Message}");
        //     isActive = false;
        //     enableESP32Input = false; // Disable to prevent retry loops
        // }

        Debug.LogWarning("ESP32InputProvider: Serial connection not yet implemented. " +
                        "Enable 'Simulate With Keyboard' for testing. " +
                        "When ESP32 hardware is ready, implement InitializeSerialConnection() and ReadSerialData().");
    }

    /// <summary>
    /// Reads and parses serial data from ESP32.
    /// This method is called every frame when ESP32 is connected.
    /// 
    /// Expected format (one command per line, terminated with newline):
    /// - "FIRE:1" or "FIRE:0" → From fire sensor hardware (1 = fire detected/ON, 0 = fire OFF)
    /// - "WATER:1" or "WATER:0" → From water sensor hardware (1 = water ON, 0 = water OFF)
    /// - "JOY:0.3,-0.8" → From analog joystick (X,Y normalized values, -1.0 to 1.0)
    /// - "FSR:0.75" → From FSR sensor (pressure intensity, 0.0 to 1.0)
    /// 
    /// Example ESP32 Arduino code structure:
    /// ```cpp
    /// void loop() {
    ///     // Fire sensor (digital) - reads fire sensor hardware
    ///     int fireSensorPin = 2;
    ///     bool fireState = digitalRead(fireSensorPin);
    ///     Serial.print("FIRE:");
    ///     Serial.println(fireState ? "1" : "0");
    ///     
    ///     // Water sensor (digital) - reads water sensor hardware
    ///     int waterSensorPin = 3;
    ///     bool waterState = digitalRead(waterSensorPin);
    ///     Serial.print("WATER:");
    ///     Serial.println(waterState ? "1" : "0");
    ///     
    ///     // Joystick (analog) - reads analog joystick X/Y
    ///     float joyX = (analogRead(A0) / 512.0f) - 1.0f; // Map 0-1023 to -1.0 to 1.0
    ///     float joyY = (analogRead(A1) / 512.0f) - 1.0f;
    ///     Serial.print("JOY:");
    ///     Serial.print(joyX, 2); Serial.print(","); Serial.println(joyY, 2);
    ///     
    ///     // FSR sensor (analog) - reads FSR pressure for pan movement
    ///     float fsrValue = analogRead(A2) / 1023.0f; // Map 0-1023 to 0.0 to 1.0
    ///     Serial.print("FSR:");
    ///     Serial.println(fsrValue, 2);
    ///     
    ///     delay(50); // Send updates every 50ms
    /// }
    /// ```
    /// 
    /// TODO: Implement serial data reading and parsing.
    /// </summary>
    private void ReadSerialData()
    {
        // TODO: Implementation example:
        // if (serialPort != null && serialPort.IsOpen)
        // {
        //     try
        //     {
        //         // Read a line from serial port (ESP32 sends one command per line)
        //         string data = serialPort.ReadLine();
        //         ParseSerialCommand(data);
        //     }
        //     catch (System.TimeoutException)
        //     {
        //         // No data available, this is normal
        //     }
        //     catch (System.Exception e)
        //     {
        //         Debug.LogError($"ESP32InputProvider: Error reading serial data: {e.Message}");
        //         // Optionally: Close connection and disable ESP32 input on error
        //         // CloseSerialConnection();
        //     }
        // }
    }

    /// <summary>
    /// Parses a serial command string from ESP32 and updates internal state.
    /// Commands are parsed and trigger appropriate state changes.
    /// </summary>
    /// <param name="command">Raw command string from ESP32 serial port (e.g., "FIRE:1", "WATER:0", "JOY:0.3,-0.8", "FSR:0.75")</param>
    private void ParseSerialCommand(string command)
    {
        // TODO: Implementation example:
        // command = command.Trim();
        // 
        // // Parse FIRE command (from fire sensor hardware)
        // if (command.StartsWith("FIRE:"))
        // {
        //     string value = command.Substring(5);
        //     bool fireState = value == "1";
        //     if (fireState) 
        //         isFireOn = true;  // Fire sensor detected fire ON
        //     else 
        //         isFireOff = true; // Fire sensor detected fire OFF
        // }
        // // Parse WATER command (from water sensor hardware)
        // else if (command.StartsWith("WATER:"))
        // {
        //     string value = command.Substring(6);
        //     bool waterState = value == "1";
        //     if (waterState) 
        //         isWaterOn = true;  // Water sensor detected water ON
        //     else 
        //         isWaterOff = true; // Water sensor detected water OFF
        // }
        // // Parse JOY command (from analog joystick)
        // else if (command.StartsWith("JOY:"))
        // {
        //     string value = command.Substring(4);
        //     string[] parts = value.Split(',');
        //     if (parts.Length == 2)
        //     {
        //         if (float.TryParse(parts[0], out float x) && float.TryParse(parts[1], out float y))
        //         {
        //             joystickDirection = new Vector2(
        //                 Mathf.Clamp(x, -1.0f, 1.0f), 
        //                 Mathf.Clamp(y, -1.0f, 1.0f)
        //             );
        //         }
        //     }
        // }
        // // Parse FSR command (from Force Sensitive Resistor sensor for pan movement)
        // else if (command.StartsWith("FSR:"))
        // {
        //     string value = command.Substring(4);
        //     if (float.TryParse(value, out float intensity))
        //     {
        //         panMovementIntensity = Mathf.Clamp01(intensity); // Clamp between 0.0 and 1.0
        //     }
        // }
        // else
        // {
        //     Debug.LogWarning($"ESP32InputProvider: Unknown command format: {command}");
        // }
    }

    /// <summary>
    /// Closes serial port connection.
    /// </summary>
    private void CloseSerialConnection()
    {
        // TODO: Implementation:
        // if (serialPort != null && serialPort.IsOpen)
        // {
        //     serialPort.Close();
        //     serialPort.Dispose();
        //     isActive = false;
        // }
    }
}
