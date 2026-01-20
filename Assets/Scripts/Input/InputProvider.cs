public interface IInputProvider
{
    void UpdateInput();

    bool GetFireOn();

    bool GetFireOff();

    bool GetWaterOn();

    bool GetWaterOff();

    UnityEngine.Vector2 GetJoystickDirection();

    float GetPanMovementIntensity();

    bool IsActive();
}
