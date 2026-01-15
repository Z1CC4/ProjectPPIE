using UnityEngine;

/// <summary>
/// Component that marks a kitchen object as selectable.
/// Add this component to any kitchen object (sink, stove, counter, etc.)
/// that should be selectable using the joystick.
/// 
/// The object will automatically register itself with the selection system.
/// </summary>
public class SelectableKitchenObject : MonoBehaviour
{
    [Header("Selection Settings")]
    [Tooltip("Display name for this object (shown in debug logs)")]
    public string displayName = "";

    [Tooltip("Priority for selection (higher = selected first when multiple objects are in same direction)")]
    public int selectionPriority = 0;

    [Header("Visual Feedback")]
    [Tooltip("Optional: GameObject to highlight when selected (e.g., outline, glow effect)")]
    public GameObject highlightObject;

    private bool isSelected = false;

    private void Awake()
    {
        // Use object name if display name not set
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = gameObject.name;
        }
    }

    private void Start()
    {
        // Subscribe to selection changes
        if (KitchenState.Instance != null)
        {
            KitchenState.Instance.OnSelectionChanged += HandleSelectionChanged;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from selection changes
        if (KitchenState.Instance != null)
        {
            KitchenState.Instance.OnSelectionChanged -= HandleSelectionChanged;
        }
    }

    /// <summary>
    /// Called when selection changes. Updates visual feedback.
    /// </summary>
    private void HandleSelectionChanged(SelectableKitchenObject selected)
    {
        bool wasSelected = isSelected;
        isSelected = (selected == this);

        // Update visual feedback if state changed
        if (wasSelected != isSelected)
        {
            UpdateVisualFeedback();
        }
    }

    /// <summary>
    /// Updates visual feedback based on selection state.
    /// </summary>
    private void UpdateVisualFeedback()
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(isSelected);
        }

        // You can add more visual feedback here:
        // - Change material
        // - Enable/disable particle effects
        // - Play animation
        // - etc.
    }

    /// <summary>
    /// Gets the world position of this selectable object (used for selection calculations).
    /// </summary>
    public Vector3 GetSelectionPosition()
    {
        return transform.position;
    }

    /// <summary>
    /// Called when this object is selected (can be overridden for custom behavior).
    /// </summary>
    public virtual void OnSelected()
    {
        Debug.Log($"Selected: {displayName}");
    }

    /// <summary>
    /// Called when this object is deselected (can be overridden for custom behavior).
    /// </summary>
    public virtual void OnDeselected()
    {
        Debug.Log($"Deselected: {displayName}");
    }
}
