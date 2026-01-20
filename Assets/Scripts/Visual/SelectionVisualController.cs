using UnityEngine;

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
        kitchenState = KitchenState.Instance;
        if (kitchenState == null)
        {
            Debug.LogError("SelectionVisualController: KitchenState not found!");
            enabled = false;
            return;
        }

        kitchenState.OnSelectionChanged += HandleSelectionChanged;

        HandleSelectionChanged(kitchenState.SelectedObject);

        if (selectionCursor != null && hideWhenNoSelection)
        {
            selectionCursor.SetActive(false);
        }
    }

    private void Update()
    {
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
        if (kitchenState != null)
        {
            kitchenState.OnSelectionChanged -= HandleSelectionChanged;
        }
    }

    private void HandleSelectionChanged(SelectableKitchenObject selected)
    {
        currentSelected = selected;

        if (selectionCursor != null)
        {
            if (selected != null)
            {
                selectionCursor.SetActive(true);
                targetPosition = selected.GetSelectionPosition() + cursorOffset;
                
                if (cursorMoveSpeed <= 0)
                {
                    selectionCursor.transform.position = targetPosition;
                }
            }
            else
            {
                if (hideWhenNoSelection)
                {
                    selectionCursor.SetActive(false);
                }
            }
        }
    }
}
