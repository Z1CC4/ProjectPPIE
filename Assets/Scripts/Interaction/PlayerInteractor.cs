using UnityEngine;

/// <summary>
/// Finds the best interactable in front of the player (raycast from camera)
/// and calls Interact() when requested (e.g., via ESP32 JOYBUTTON).
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast Source")]
    [Tooltip("If null, will use Camera.main.")]
    public Camera viewCamera;

    [Header("Raycast Settings")]
    public float maxDistance = 2.0f;
    public LayerMask interactableLayers = ~0;

    [Tooltip("Small sphere radius to make interaction more forgiving than a thin ray.")]
    public float sphereCastRadius = 0.08f;

    [Tooltip("If your interactable colliders are triggers, set this to Collide.")]
    public QueryTriggerInteraction queryTriggers = QueryTriggerInteraction.Ignore;

    [Header("Debug")]
    public bool drawDebug = false;
    public Color debugColorHit = Color.green;
    public Color debugColorMiss = Color.red;

    private IInteractable _current;
    private Transform _currentTransform;

    public IInteractable Current => _current;
    public Transform CurrentTransform => _currentTransform;

    private void Awake()
    {
        if (viewCamera == null) viewCamera = Camera.main;
    }

    private void Update()
    {
        UpdateCurrentInteractable();
    }

    private void UpdateCurrentInteractable()
    {
        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera == null)
        {
            _current = null;
            _currentTransform = null;
            return;
        }

        Vector3 origin = viewCamera.transform.position;
        Vector3 dir = viewCamera.transform.forward;

        bool hitSomething;
        RaycastHit hit;

        if (sphereCastRadius > 0f)
        {
            hitSomething = Physics.SphereCast(origin, sphereCastRadius, dir, out hit, maxDistance, interactableLayers, queryTriggers);
        }
        else
        {
            hitSomething = Physics.Raycast(origin, dir, out hit, maxDistance, interactableLayers, queryTriggers);
        }

        if (drawDebug)
        {
            Debug.DrawRay(origin, dir * maxDistance, hitSomething ? debugColorHit : debugColorMiss);
        }

        if (!hitSomething)
        {
            _current = null;
            _currentTransform = null;
            return;
        }

        // Search on hit object and parents (door handle collider often sits on a child).
        IInteractable interactable = null;
        Transform interactableTransform = null;

        // GetComponentInParent<IInteractable>() is not available directly for interfaces,
        // so we scan components.
        var components = hit.collider.GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] is IInteractable ia)
            {
                interactable = ia;
                interactableTransform = components[i].transform;
                break;
            }
        }

        _current = interactable;
        _currentTransform = interactableTransform;
    }

    /// <summary>
    /// Call this when the player presses the interaction button.
    /// </summary>
    public void Interact()
    {
        if (_current == null) return;
        if (!_current.CanInteract()) return;
        _current.Interact(gameObject);
    }

    public string GetCurrentPrompt()
    {
        return _current?.GetInteractionPrompt();
    }
}

