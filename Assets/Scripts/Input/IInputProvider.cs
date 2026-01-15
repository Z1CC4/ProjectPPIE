/// <summary>
/// Interface for all input providers (Keyboard, ESP32, etc.).
/// This allows the game logic to work with any input device without knowing
/// which specific device is being used.
/// </summary>
public interface IInputProvider
{
    /// <summary>
    /// Called every frame to update input state.
    /// </summary>
    void UpdateInput();

    /// <summary>
    /// Returns true if fire should be turned on.
    /// </summary>
    bool GetFireOn();

    /// <summary>
    /// Returns true if fire should be turned off.
    /// </summary>
    bool GetFireOff();

    /// <summary>
    /// Returns true if water should be turned on.
    /// </summary>
    bool GetWaterOn();

    /// <summary>
    /// Returns true if water should be turned off.
    /// </summary>
    bool GetWaterOff();

    /// <summary>
    /// Returns the joystick direction as a normalized Vector2.
    /// X = left/right, Y = forward/back (or up/down in 2D space).
    /// </summary>
    UnityEngine.Vector2 GetJoystickDirection();

    /// <summary>
    /// Returns the pan movement intensity/speed (0.0 to 1.0).
    /// This simulates FSR (Force Sensitive Resistor) pressure from ESP32.
    /// 0.0 = no movement, 1.0 = maximum speed movement.
    /// </summary>
    float GetPanMovementIntensity();

    /// <summary>
    /// Returns true if this input provider is currently active/connected.
    /// </summary>
    bool IsActive();
}
