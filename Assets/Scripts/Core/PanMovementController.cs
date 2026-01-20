using UnityEngine;

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
    public Vector3 backAndForthDirection = Vector3.right;

    [Header("Animation Smoothing")]
    [Tooltip("How quickly the pan accelerates/decelerates to target speed")]
    public float accelerationRate = 5.0f;

    [Tooltip("Minimum intensity threshold to start moving (dead zone)")]
    [Range(0.0f, 0.1f)]
    public float minIntensityThreshold = 0.05f;

    private float currentAngle = 0.0f;
    private float currentSpeed = 0.0f;
    private float targetIntensity = 0.0f;
    private Vector3 initialLocalPosition;
    private KitchenState kitchenState;
    private float backAndForthPosition = 0.0f;

    public enum MovementPattern
    {
        Circular,
        BackAndForth
    }

    private void Awake()
    {
        if (panTransform == null)
        {
            panTransform = transform;
        }

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
        kitchenState = KitchenState.Instance;
        if (kitchenState != null)
        {
            kitchenState.OnPanMovementIntensityChanged += HandleIntensityChanged;
            targetIntensity = kitchenState.PanMovementIntensity;
        }
        else
        {
            Debug.LogError("PanMovementController: KitchenState not found! Pan movement will not work.");
        }
    }

    private void OnDestroy()
    {
        if (kitchenState != null)
        {
            kitchenState.OnPanMovementIntensityChanged -= HandleIntensityChanged;
        }
    }

    private void Update()
    {
        if (panTransform == null || kitchenState == null)
            return;

        float targetSpeed = CalculateTargetSpeed(targetIntensity);
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, accelerationRate * Time.deltaTime);

        if (currentSpeed > 0.01f)
        {
            UpdatePanPosition();
        }
    }

    private void HandleIntensityChanged(float intensity)
    {
        targetIntensity = intensity;
    }

    private float CalculateTargetSpeed(float intensity)
    {
        if (intensity < minIntensityThreshold)
            return 0.0f;

        return intensity * maxSpeed * 360f;
    }

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

    private void UpdateCircularMovement()
    {
        currentAngle += currentSpeed * Time.deltaTime;

        float angleRad = currentAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(angleRad) * movementRadius,
            0.0f,
            Mathf.Sin(angleRad) * movementRadius
        );

        if (stovePosition != null)
        {
            panTransform.position = stovePosition.position + initialLocalPosition + offset;
        }
        else
        {
            panTransform.localPosition = initialLocalPosition + offset;
        }
    }

    private void UpdateBackAndForthMovement()
    {
        float linearSpeed = (currentSpeed / 360f) * 2f;
        
        backAndForthPosition += linearSpeed * Time.deltaTime;
        float pingPongValue = Mathf.PingPong(backAndForthPosition, movementRadius * 2f);
        
        float offsetDistance = pingPongValue - movementRadius;
        
        Vector3 normalizedDirection = backAndForthDirection.normalized;
        Vector3 offset = normalizedDirection * offsetDistance;

        if (stovePosition != null)
        {
            panTransform.position = stovePosition.position + initialLocalPosition + offset;
        }
        else
        {
            panTransform.localPosition = initialLocalPosition + offset;
        }
    }

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
