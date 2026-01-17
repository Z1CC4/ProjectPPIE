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

        // Different approach (no proxies): cast against EVERYTHING (or configured layers),
        // then pick the closest hit that actually contains an IInteractable.
        // This avoids "dead" colliders (cabinet body, counter, etc.) blocking interaction.
        RaycastHit[] hits;
        if (sphereCastRadius > 0f)
        {
            hits = Physics.SphereCastAll(origin, sphereCastRadius, dir, maxDistance, ~0, queryTriggers);
        }
        else
        {
            hits = Physics.RaycastAll(origin, dir, maxDistance, ~0, queryTriggers);
        }

        if (drawDebug)
        {
            Debug.DrawRay(origin, dir * maxDistance, hits != null && hits.Length > 0 ? debugColorHit : debugColorMiss);
        }

        if (hits == null || hits.Length == 0)
        {
            _current = null;
            _currentTransform = null;
            return;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int h = 0; h < hits.Length; h++)
        {
            var col = hits[h].collider;
            if (col == null) continue;

            // Only accept objects on interactableLayers (this keeps your project-controlled whitelist),
            // but we still "see through" non-interactable colliders that are not on those layers.
            if (((1 << col.gameObject.layer) & interactableLayers.value) == 0) continue;

            if (TryGetInteractable(col, out var ia, out var iaT))
            {
                _current = ia;
                _currentTransform = iaT;
                return;
            }
        }

        _current = null;
        _currentTransform = null;
    }

    private static bool TryGetInteractable(Collider col, out IInteractable interactable, out Transform interactableTransform)
    {
        interactable = null;
        interactableTransform = null;
        if (col == null) return false;

        // GetComponentInParent<IInteractable>() is not available directly for interfaces, so we scan components.
        var components = col.GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] is IInteractable ia)
            {
                if (!ia.CanInteract()) return false;
                interactable = ia;
                interactableTransform = components[i].transform;
                return true;
            }
        }
        return false;
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

