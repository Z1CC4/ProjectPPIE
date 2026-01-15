using UnityEngine;

/// <summary>
/// Controls the movement animation of a pan on the stove/fire.
/// The pan moves in a circular or back-and-forth pattern based on movement intensity.
/// The intensity comes from FSR sensor input (0.0 = no movement, 1.0 = maximum speed).
/// </summary>
public class PanMovementController : MonoBehaviour
{
    [Header("Pan Settings")]
    [Tooltip("Reference to the pan GameObject that will be animated")]
    public Transform panTransform;

    [Tooltip("The stove/fire position (pan will move relative to this)")]
    public Transform stovePosition;

    [Header("Movement Settings")]
    [Tooltip("Maximum movement radius (how far the pan can move from center)")]
    public float movementRadius = 0.15f;

    [Tooltip("Maximum movement speed (rotations per second at intensity = 1.0)")]
    public float maxSpeed = 2.0f;

    [Tooltip("Movement pattern: Circular or BackAndForth")]
    public MovementPattern pattern = MovementPattern.Circular;

    [Header("Back and Forth Settings")]
    [Tooltip("Direction for back-and-forth movement (X = left/right, Z = forward/back)")]
    public Vector3 backAndForthDirection = Vector3.right; // Moves along X axis by default

    [Header("Animation Smoothing")]
    [Tooltip("How quickly the pan accelerates/decelerates to target speed")]
    public float accelerationRate = 5.0f;

    [Tooltip("Minimum intensity threshold to start moving (dead zone)")]
    [Range(0.0f, 0.1f)]
    public float minIntensityThreshold = 0.05f;

    private float currentAngle = 0.0f; // Current angle for circular movement
    private float currentSpeed = 0.0f; // Current angular speed
    private float targetIntensity = 0.0f;
    private Vector3 initialLocalPosition;
    private KitchenState kitchenState;
    private float backAndForthPosition = 0.0f; // Current position in back-and-forth movement (0.0 to 1.0)

    public enum MovementPattern
    {
        Circular,       // Pan moves in circles on the stove
        BackAndForth    // Pan moves back and forth
    }

    private void Awake()
    {
        // If panTransform is not assigned, use this GameObject's transform
        if (panTransform == null)
        {
            panTransform = transform;
        }

        // Store initial position relative to stove (or use current position if no stove)
        if (stovePosition != null)
        {
            initialLocalPosition = panTransform.position - stovePosition.position;
        }
        else
        {
            initialLocalPosition = panTransform.localPosition;
        }
    }

    private void Start()
    {
        // Subscribe to KitchenState pan movement intensity changes
        kitchenState = KitchenState.Instance;
        if (kitchenState != null)
        {
            kitchenState.OnPanMovementIntensityChanged += HandleIntensityChanged;
            // Initialize with current intensity
            targetIntensity = kitchenState.PanMovementIntensity;
        }
        else
        {
            Debug.LogError("PanMovementController: KitchenState not found! Pan movement will not work.");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (kitchenState != null)
        {
            kitchenState.OnPanMovementIntensityChanged -= HandleIntensityChanged;
        }
    }

    private void Update()
    {
        if (panTransform == null || kitchenState == null)
            return;

        // Smoothly interpolate current speed toward target speed
        float targetSpeed = CalculateTargetSpeed(targetIntensity);
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, accelerationRate * Time.deltaTime);

        // Update pan position based on movement pattern
        if (currentSpeed > 0.01f) // Only move if speed is significant
        {
            UpdatePanPosition();
        }
    }

    /// <summary>
    /// Handles intensity changes from KitchenState.
    /// </summary>
    private void HandleIntensityChanged(float intensity)
    {
        targetIntensity = intensity;
    }

    /// <summary>
    /// Calculates the target angular speed based on intensity.
    /// </summary>
    private float CalculateTargetSpeed(float intensity)
    {
        if (intensity < minIntensityThreshold)
            return 0.0f;

        // Scale speed based on intensity (0.0 = stopped, 1.0 = max speed)
        return intensity * maxSpeed * 360f; // Convert to degrees per second
    }

    /// <summary>
    /// Updates the pan's position based on the selected movement pattern.
    /// </summary>
    private void UpdatePanPosition()
    {
        if (pattern == MovementPattern.Circular)
        {
            UpdateCircularMovement();
        }
        else if (pattern == MovementPattern.BackAndForth)
        {
            UpdateBackAndForthMovement();
        }
    }

    /// <summary>
    /// Updates position for circular movement pattern.
    /// </summary>
    private void UpdateCircularMovement()
    {
        // Update angle based on speed
        currentAngle += currentSpeed * Time.deltaTime;

        // Calculate offset from center
        float angleRad = currentAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(angleRad) * movementRadius,
            0.0f,
            Mathf.Sin(angleRad) * movementRadius
        );

        // Apply position
        if (stovePosition != null)
        {
            panTransform.position = stovePosition.position + initialLocalPosition + offset;
        }
        else
        {
            panTransform.localPosition = initialLocalPosition + offset;
        }
    }

    /// <summary>
    /// Updates position for back-and-forth movement pattern.
    /// Uses linear movement: moves forward to max distance, then reverses direction.
    /// </summary>
    private void UpdateBackAndForthMovement()
    {
        // Calculate movement distance per second based on speed
        // Convert angular speed (degrees/sec) to linear movement speed
        // At max speed (360 deg/sec), we want to complete one full cycle (forward + back) per second
        float linearSpeed = (currentSpeed / 360f) * 2f; // 2x because forward + back = 2x movementRadius
        
        // Update position using PingPong for linear back-and-forth movement
        // PingPong goes from 0 to length and back, creating linear movement
        backAndForthPosition += linearSpeed * Time.deltaTime;
        float pingPongValue = Mathf.PingPong(backAndForthPosition, movementRadius * 2f);
        
        // Convert to offset from center (-movementRadius to +movementRadius)
        float offsetDistance = pingPongValue - movementRadius;
        
        // Apply offset in the specified direction
        Vector3 normalizedDirection = backAndForthDirection.normalized;
        Vector3 offset = normalizedDirection * offsetDistance;

        // Apply position
        if (stovePosition != null)
        {
            panTransform.position = stovePosition.position + initialLocalPosition + offset;
        }
        else
        {
            panTransform.localPosition = initialLocalPosition + offset;
        }
    }

    /// <summary>
    /// Resets the pan to its initial position.
    /// </summary>
    public void ResetPosition()
    {
        currentAngle = 0.0f;
        currentSpeed = 0.0f;
        backAndForthPosition = 0.0f;
        
        if (stovePosition != null)
        {
            panTransform.position = stovePosition.position + initialLocalPosition;
        }
        else
        {
            panTransform.localPosition = initialLocalPosition;
        }
    }
}
