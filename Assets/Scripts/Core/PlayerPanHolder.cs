using UnityEngine;

/// <summary>
/// Manages when the player is holding a pan and can interact with the stove.
/// This script should be attached to the player GameObject (First Person Controller).
/// It handles picking up/dropping the pan and making it follow the player's hand.
/// </summary>
public class PlayerPanHolder : MonoBehaviour
{
    [Header("Pan Settings")]
    [Tooltip("The pan GameObject that the player can hold")]
    public GameObject panObject;

    [Tooltip("Transform representing the player's hand position (where pan should be held)")]
    public Transform handTransform;

    [Tooltip("Offset from hand position for pan placement")]
    public Vector3 panOffset = new Vector3(0.0f, 0.0f, 0.0f);

    [Header("Interaction Settings")]
    [Tooltip("Maximum distance to pick up or interact with pan")]
    public float interactionDistance = 2.0f;

    [Tooltip("Key to pick up/drop pan (when using keyboard)")]
    public KeyCode pickupKey = KeyCode.E;

    private bool isHoldingPan = false;
    private PanMovementController panMovementController;
    private Vector3 panInitialPosition;
    private Quaternion panInitialRotation;
    private Transform panInitialParent;

    private void Start()
    {
        if (panObject != null)
        {
            // Store initial pan state
            panInitialPosition = panObject.transform.position;
            panInitialRotation = panObject.transform.rotation;
            panInitialParent = panObject.transform.parent;

            // Get pan movement controller (if exists)
            panMovementController = panObject.GetComponent<PanMovementController>();
        }

        // If handTransform is not assigned, try to find a child object named "Hand" or use this transform
        if (handTransform == null)
        {
            Transform hand = transform.Find("Hand");
            if (hand == null)
            {
                // Create a child GameObject for hand position
                GameObject handObj = new GameObject("Hand");
                handObj.transform.SetParent(transform);
                handObj.transform.localPosition = new Vector3(0.5f, 0.0f, 0.8f); // Default hand position
                handTransform = handObj.transform;
            }
            else
            {
                handTransform = hand;
            }
        }
    }

    private void Update()
    {
        // Handle keyboard input for pickup/drop (for testing)
        #if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.E].wasPressedThisFrame)
            {
                TogglePanHold();
            }
        }
        #else
        if (Input.GetKeyDown(pickupKey))
        {
            TogglePanHold();
        }
        #endif

        // If holding pan, update its position to follow hand
        if (isHoldingPan && panObject != null && handTransform != null)
        {
            UpdatePanPosition();
        }
    }

    /// <summary>
    /// Toggles between picking up and dropping the pan.
    /// </summary>
    public void TogglePanHold()
    {
        if (isHoldingPan)
        {
            DropPan();
        }
        else
        {
            PickUpPan();
        }
    }

    /// <summary>
    /// Picks up the pan and makes it follow the player's hand.
    /// </summary>
    public void PickUpPan()
    {
        if (panObject == null || handTransform == null)
            return;

        // Check distance (if pan is too far, don't pick up)
        float distance = Vector3.Distance(transform.position, panObject.transform.position);
        if (distance > interactionDistance)
            return;

        isHoldingPan = true;

        // Disable pan movement controller (player is controlling it now)
        if (panMovementController != null)
        {
            panMovementController.enabled = false;
        }

        // Parent pan to hand (or just update position each frame)
        // For smoother control, we'll update position manually in Update()
        if (panObject.GetComponent<Rigidbody>() != null)
        {
            panObject.GetComponent<Rigidbody>().isKinematic = true; // Disable physics
        }
    }

    /// <summary>
    /// Drops the pan back to its initial position.
    /// </summary>
    public void DropPan()
    {
        if (panObject == null)
            return;

        isHoldingPan = false;

        // Re-enable pan movement controller (if exists)
        if (panMovementController != null)
        {
            panMovementController.enabled = true;
        }

        // Reset pan to initial position
        panObject.transform.position = panInitialPosition;
        panObject.transform.rotation = panInitialRotation;
        panObject.transform.SetParent(panInitialParent);

        // Re-enable physics if it had a Rigidbody
        if (panObject.GetComponent<Rigidbody>() != null)
        {
            panObject.GetComponent<Rigidbody>().isKinematic = false;
        }
    }

    /// <summary>
    /// Updates the pan's position to follow the player's hand.
    /// </summary>
    private void UpdatePanPosition()
    {
        if (panObject == null || handTransform == null)
            return;

        panObject.transform.position = handTransform.position + panOffset;
        // Optionally: make pan rotate to match hand rotation
        // panObject.transform.rotation = handTransform.rotation;
    }

    /// <summary>
    /// Checks if the player is currently holding the pan.
    /// </summary>
    public bool IsHoldingPan()
    {
        return isHoldingPan;
    }

    /// <summary>
    /// Places the pan on the stove (called when player interacts with stove).
    /// </summary>
    public void PlacePanOnStove(Transform stoveTransform)
    {
        if (!isHoldingPan || panObject == null || stoveTransform == null)
            return;

        // Drop pan
        isHoldingPan = false;

        // Position pan on stove
        panObject.transform.position = stoveTransform.position + Vector3.up * 0.1f; // Slightly above stove
        panObject.transform.rotation = Quaternion.identity;

        // Re-enable pan movement controller
        if (panMovementController != null)
        {
            panMovementController.enabled = true;
            panMovementController.stovePosition = stoveTransform;
        }
    }
}
