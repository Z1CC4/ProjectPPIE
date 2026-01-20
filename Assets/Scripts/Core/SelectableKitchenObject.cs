using UnityEngine;

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
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = gameObject.name;
        }
    }

    private void Start()
    {
        if (KitchenState.Instance != null)
        {
            KitchenState.Instance.OnSelectionChanged += HandleSelectionChanged;
        }
    }

    private void OnDestroy()
    {
        if (KitchenState.Instance != null)
        {
            KitchenState.Instance.OnSelectionChanged -= HandleSelectionChanged;
        }
    }

    private void HandleSelectionChanged(SelectableKitchenObject selected)
    {
        bool wasSelected = isSelected;
        isSelected = (selected == this);

        if (wasSelected != isSelected)
        {
            UpdateVisualFeedback();
        }
    }

    private void UpdateVisualFeedback()
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(isSelected);
        }
    }

    public Vector3 GetSelectionPosition()
    {
        return transform.position;
    }

    public virtual void OnSelected()
    {
    }

    public virtual void OnDeselected()
    {
    }
}
