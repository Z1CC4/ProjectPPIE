using UnityEngine;

/// <summary>
/// Visual controller for object selection (cursor/highlight).
/// This script reacts to KitchenState selection changes and displays
/// a visual indicator (cursor, highlight, etc.) on the selected object.
/// It contains NO input logic.
/// </summary>
public class SelectionVisualController : MonoBehaviour
{
    [Header("Selection Visual")]
    [Tooltip("Cursor/indicator GameObject that follows the selected object")]
    public GameObject selectionCursor;

    [Tooltip("Offset from object position for cursor placement")]
    public Vector3 cursorOffset = new Vector3(0, 1, 0);

    [Tooltip("Smooth movement speed for cursor (0 = instant)")]
    public float cursorMoveSpeed = 10f;

    [Header("Settings")]
    [Tooltip("Hide cursor when no object is selected")]
    public bool hideWhenNoSelection = true;

    private KitchenState kitchenState;
    private SelectableKitchenObject currentSelected;
    private Vector3 targetPosition;

    private void Start()
    {
        // Get reference to KitchenState
        kitchenState = KitchenState.Instance;
        if (kitchenState == null)
        {
            Debug.LogError("SelectionVisualController: KitchenState not found!");
            enabled = false;
            return;
        }

        // Subscribe to selection changes
        kitchenState.OnSelectionChanged += HandleSelectionChanged;

        // Set initial state
        HandleSelectionChanged(kitchenState.SelectedObject);

        // Hide cursor initially if needed
        if (selectionCursor != null && hideWhenNoSelection)
        {
            selectionCursor.SetActive(false);
        }
    }

    private void Update()
    {
        // Smoothly move cursor to target position
        if (selectionCursor != null && currentSelected != null)
        {
            targetPosition = currentSelected.GetSelectionPosition() + cursorOffset;

            if (cursorMoveSpeed > 0)
            {
                selectionCursor.transform.position = Vector3.Lerp(
                    selectionCursor.transform.position,
                    targetPosition,
                    Time.deltaTime * cursorMoveSpeed
                );
            }
            else
            {
                selectionCursor.transform.position = targetPosition;
            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (kitchenState != null)
        {
            kitchenState.OnSelectionChanged -= HandleSelectionChanged;
        }
    }

    /// <summary>
    /// Handles selection changes and updates visual cursor.
    /// </summary>
    private void HandleSelectionChanged(SelectableKitchenObject selected)
    {
        currentSelected = selected;

        if (selectionCursor != null)
        {
            if (selected != null)
            {
                // Show cursor and move to selected object
                selectionCursor.SetActive(true);
                targetPosition = selected.GetSelectionPosition() + cursorOffset;
                
                if (cursorMoveSpeed <= 0)
                {
                    // Instant positioning if no smooth movement
                    selectionCursor.transform.position = targetPosition;
                }
            }
            else
            {
                // Hide cursor if no selection
                if (hideWhenNoSelection)
                {
                    selectionCursor.SetActive(false);
                }
            }
        }
    }
}
