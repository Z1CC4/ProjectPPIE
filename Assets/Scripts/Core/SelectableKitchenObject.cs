using UnityEngine;

public class SelectableKitchenObject : MonoBehaviour
{
    [Header("Selection Settings")]
    public string displayName = "";

    public int selectionPriority = 0;

    [Header("Visual Feedback")]
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
