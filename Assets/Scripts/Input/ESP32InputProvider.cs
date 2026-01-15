using UnityEngine;
using System.Collections;

/// <summary>
/// ESP32 Input Provider - PLACEHOLDER for future implementation.
/// 
/// This script will handle serial communication with the ESP32 device.
/// 
/// ESP32 Hardware Sensors:
/// - Fire Sensor (Digital): Detects if fire is ON or OFF
/// - Water Sensor (Digital): Detects if water is ON or OFF  
/// - Joystick (Analog): 2-axis analog joystick for selection movement
/// - FSR Sensor (Analog): Force Sensitive Resistor for pan movement intensity (0.0 to 1.0)
/// 
/// Expected serial commands from ESP32:
/// - "FIRE:1" or "FIRE:0" → Fire sensor state (from fire sensor hardware)
/// - "WATER:1" or "WATER:0" → Water sensor state (from water sensor hardware)
/// - "JOY:0.3,-0.8" → Joystick direction (X,Y values from analog joystick)
/// - "FSR:0.75" → Pan movement intensity (0.0 to 1.0 from FSR sensor)
/// 
/// TODO: Implement serial port communication when ESP32 hardware is available.
/// </summary>
public class ESP32InputProvider : MonoBehaviour, IInputProvider
{
    [Header("ESP32 Connection Settings")]
    [Tooltip("Serial port name (e.g., COM3 on Windows, /dev/ttyUSB0 on Linux)")]
    public string serialPort = "COM3";

    [Tooltip("Baud rate for serial communication")]
    public int baudRate = 9600;

    [Tooltip("Enable ESP32 input (set to false to disable even if connected)")]
    public bool enableESP32Input = false;

    [Header("Debug")]
    [Tooltip("Simulate ESP32 input using keyboard (for testing without hardware)")]
    public bool simulateWithKeyboard = false;

    // Current state
    private bool isFireOn = false;
    private bool isFireOff = false;
    private bool isWaterOn = false;
    private bool isWaterOff = false;
    private Vector2 joystickDirection = Vector2.zero;
    private float panMovementIntensity = 0.0f; // 0.0 to 1.0 (from FSR sensor)
    private bool isActive = false;

    // TODO: Add serial port field when implementing
    // private SerialPort serialPort;

    private void Start()
    {
        if (enableESP32Input && !simulateWithKeyboard)
        {
            InitializeSerialConnection();
        }
        else if (simulateWithKeyboard)
        {
            Debug.LogWarning("ESP32InputProvider: Running in simulation mode (using keyboard). " +
                           "This is for testing only. Real ESP32 connection is not active.");
            isActive = true; // Allow simulation mode
        }
    }

    private void Update()
    {
        if (simulateWithKeyboard)
        {
            // Simulate ESP32 input using keyboard for testing
            UpdateSimulatedInput();
        }
        else if (enableESP32Input)
        {
            // TODO: Read from serial port
            // ReadSerialData();
        }

        // Reset one-time flags
        isFireOn = false;
        isFireOff = false;
        isWaterOn = false;
        isWaterOff = false;
    }

    private void OnDestroy()
    {
        // TODO: Close serial port connection
        // CloseSerialConnection();
    }

    #region IInputProvider Implementation

    public void UpdateInput()
    {
        // Input is updated in Update() method
    }

    public bool GetFireOn()
    {
        return isFireOn;
    }

    public bool GetFireOff()
    {
        return isFireOff;
    }

    public bool GetWaterOn()
    {
        return isWaterOn;
    }

    public bool GetWaterOff()
    {
        return isWaterOff;
    }

    public Vector2 GetJoystickDirection()
    {
        return joystickDirection;
    }

    public float GetPanMovementIntensity()
    {
        return panMovementIntensity;
    }

    public bool IsActive()
    {
        return isActive && enableESP32Input;
    }

    #endregion

    #region Serial Communication (Placeholder)

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

    #endregion

    #region Simulation Mode (For Testing)

    /// <summary>
    /// Simulates ESP32 input using keyboard (for testing without hardware).
    /// This simulates how the ESP32 would send serial commands, but using keyboard input.
    /// Uses old Input System for compatibility (this is only for simulation/testing).
    /// </summary>
    private void UpdateSimulatedInput()
    {
        // Simulate fire sensor (from ESP32 fire sensor hardware)
        // In real hardware: ESP32 reads digital pin and sends "FIRE:1" or "FIRE:0"
        if (Input.GetKeyDown(KeyCode.F))
        {
            isFireOn = true;  // Simulates ESP32 sending "FIRE:1"
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            isFireOff = true; // Simulates ESP32 sending "FIRE:0"
        }

        // Simulate water sensor (from ESP32 water sensor hardware)
        // In real hardware: ESP32 reads digital pin and sends "WATER:1" or "WATER:0"
        if (Input.GetKeyDown(KeyCode.W))
        {
            isWaterOn = true;  // Simulates ESP32 sending "WATER:1"
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            isWaterOff = true; // Simulates ESP32 sending "WATER:0"
        }

        // Simulate analog joystick (from ESP32 analog joystick)
        // In real hardware: ESP32 reads analog pins and sends "JOY:x,y"
        Vector2 dir = Vector2.zero;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) dir.x -= 1f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) dir.x += 1f;
        if (Input.GetKey(KeyCode.UpArrow)) dir.y += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) dir.y -= 1f;

        if (dir.magnitude > 0.1f)
        {
            joystickDirection = dir.normalized;
        }
        else
        {
            joystickDirection = Vector2.zero;
        }

        // Simulate FSR sensor (from ESP32 FSR sensor for pan movement)
        // In real hardware: ESP32 reads analog pin and sends "FSR:0.75" (0.0 to 1.0)
        if (Input.GetKey(KeyCode.Space))
        {
            // Simulate increasing pressure: hold Space to increase intensity
            panMovementIntensity += 2.0f * Time.deltaTime;
            panMovementIntensity = Mathf.Clamp01(panMovementIntensity);
        }
        else
        {
            // Simulate releasing pressure: intensity decreases when Space is released
            panMovementIntensity -= 3.0f * Time.deltaTime;
            panMovementIntensity = Mathf.Max(0.0f, panMovementIntensity);
        }
    }

    #endregion
}
