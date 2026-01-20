using UnityEngine;

public class PlayerPanHolder : MonoBehaviour
{
    [Header("Pan Settings")]
    public GameObject panObject;

    [Header("Interaction Settings")]
    public float interactionDistance = 2.0f;

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
            panInitialPosition = panObject.transform.position;
            panInitialRotation = panObject.transform.rotation;
            panInitialParent = panObject.transform.parent;

            panMovementController = panObject.GetComponent<PanMovementController>();
        }
    }

    private void Update()
    {
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

        if (isHoldingPan && panObject != null)
            return;
    }

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

    public void PickUpPan()
    {
        if (panObject == null)
            return;

        float distance = Vector3.Distance(transform.position, panObject.transform.position);
        if (distance > interactionDistance)
            return;

        isHoldingPan = true;

        if (panMovementController != null)
        {
            panMovementController.enabled = false;
        }

        panObject.transform.SetParent(transform, true);

        if (panObject.GetComponent<Rigidbody>() != null)
        {
            panObject.GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    public void DropPan()
    {
        if (panObject == null)
            return;

        isHoldingPan = false;

        if (panMovementController != null)
        {
            panMovementController.enabled = true;
        }

        panObject.transform.position = panInitialPosition;
        panObject.transform.rotation = panInitialRotation;
        panObject.transform.SetParent(panInitialParent);

        if (panObject.GetComponent<Rigidbody>() != null)
        {
            panObject.GetComponent<Rigidbody>().isKinematic = false;
        }
    }

    public bool IsHoldingPan()
    {
        return isHoldingPan;
    }

    public void PlacePanOnStove(Transform stoveTransform)
    {
        if (!isHoldingPan || panObject == null || stoveTransform == null)
            return;

        isHoldingPan = false;

        panObject.transform.SetParent(panInitialParent, true);
        panObject.transform.position = stoveTransform.position + Vector3.up * 0.1f;
        panObject.transform.rotation = Quaternion.identity;

        if (panMovementController != null)
        {
            panMovementController.enabled = true;
            panMovementController.stovePosition = stoveTransform;
        }
    }
}
