using System;
using UnityEngine;

/// <summary>
/// Central game state manager for the kitchen.
/// This script stores the current state of fire, water, and selected objects.
/// It does NOT depend on any specific input device - it only exposes public methods
/// that can be called by any input provider.
/// </summary>
public class KitchenState : MonoBehaviour
{
    // Singleton pattern for easy access
    public static KitchenState Instance { get; private set; }

    // Events for state changes (visual systems can subscribe to these)
    public event Action<bool> OnFireStateChanged;
    public event Action<bool> OnWaterStateChanged;
    public event Action<SelectableKitchenObject> OnSelectionChanged;
    public event Action<float> OnPanMovementIntensityChanged; // Intensity from 0.0 to 1.0

    // Current state
    [SerializeField] private bool isFireOn = false;
    [SerializeField] private bool isWaterOn = false;
    [SerializeField] private SelectableKitchenObject selectedObject = null;
    [SerializeField] private float panMovementIntensity = 0.0f; // 0.0 to 1.0

    // Public properties (read-only from outside)
    public bool IsFireOn => isFireOn;
    public bool IsWaterOn => isWaterOn;
    public SelectableKitchenObject SelectedObject => selectedObject;
    public float PanMovementIntensity => panMovementIntensity;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple KitchenState instances found! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Sets the fire state. Called by input providers.
    /// </summary>
    public void SetFire(bool isOn)
    {
        if (isFireOn != isOn)
        {
            isFireOn = isOn;
            OnFireStateChanged?.Invoke(isFireOn);
        }
    }

    /// <summary>
    /// Sets the water state. Called by input providers.
    /// </summary>
    public void SetWater(bool isOn)
    {
        if (isWaterOn != isOn)
        {
            isWaterOn = isOn;
            OnWaterStateChanged?.Invoke(isWaterOn);
        }
    }

    /// <summary>
    /// Moves the selection based on joystick direction.
    /// Called by input providers with a normalized Vector2 direction.
    /// </summary>
    public void MoveSelection(Vector2 direction)
    {
        if (direction.magnitude < 0.1f) return; // Dead zone

        // Find the nearest selectable object in the given direction
        SelectableKitchenObject nearest = FindNearestInDirection(direction);
        
        if (nearest != null && nearest != selectedObject)
        {
            SetSelectedObject(nearest);
        }
    }

    /// <summary>
    /// Sets the selected object directly (used internally and by selection system).
    /// </summary>
    public void SetSelectedObject(SelectableKitchenObject obj)
    {
        if (selectedObject != obj)
        {
            selectedObject = obj;
            OnSelectionChanged?.Invoke(selectedObject);
        }
    }

    /// <summary>
    /// Finds the nearest selectable object in the given direction.
    /// Uses a simple distance-based approach with direction preference.
    /// </summary>
    private SelectableKitchenObject FindNearestInDirection(Vector2 direction)
    {
        SelectableKitchenObject[] allSelectables = FindObjectsByType<SelectableKitchenObject>(FindObjectsSortMode.None);
        
        if (allSelectables.Length == 0) return null;

        Vector3 currentPos = selectedObject != null 
            ? selectedObject.transform.position 
            : Vector3.zero;

        SelectableKitchenObject bestCandidate = null;
        float bestScore = float.NegativeInfinity;

        foreach (SelectableKitchenObject obj in allSelectables)
        {
            if (obj == selectedObject) continue;

            Vector3 toObject = obj.transform.position - currentPos;
            Vector2 toObject2D = new Vector2(toObject.x, toObject.z); // Use XZ plane for top-down view

            if (toObject2D.magnitude < 0.1f) continue; // Too close

            // Normalize direction vector
            Vector2 normalizedDir = toObject2D.normalized;

            // Calculate dot product (how aligned with direction)
            float alignment = Vector2.Dot(direction.normalized, normalizedDir);

            // Prefer objects in the direction we're moving
            // Also consider distance (closer objects get slight bonus)
            float distance = toObject2D.magnitude;
            float score = alignment - (distance * 0.1f); // Alignment minus distance penalty

            if (alignment > 0.3f && score > bestScore) // Must be at least somewhat in direction
            {
                bestScore = score;
                bestCandidate = obj;
            }
        }

        return bestCandidate;
    }

    /// <summary>
    /// Sets the pan movement intensity. Called by input providers.
    /// </summary>
    /// <param name="intensity">Movement intensity from 0.0 (no movement) to 1.0 (maximum speed)</param>
    public void SetPanMovementIntensity(float intensity)
    {
        intensity = Mathf.Clamp01(intensity); // Ensure it's between 0 and 1
        
        if (Mathf.Abs(panMovementIntensity - intensity) > 0.01f) // Only update if changed significantly
        {
            panMovementIntensity = intensity;
            OnPanMovementIntensityChanged?.Invoke(panMovementIntensity);
        }
    }
}
