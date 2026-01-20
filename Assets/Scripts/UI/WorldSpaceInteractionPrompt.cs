using UnityEngine;
using UnityEngine.UI;

public class WorldSpaceInteractionPrompt : MonoBehaviour
{
    private const string _legacyBuiltInFontPath = "LegacyRuntime.ttf";

    [Header("References")]
    public PlayerInteractor playerInteractor;

    public Camera viewCamera;

    [Header("Text")]
    public string prefix = "JOY:";

    public string separator = " ";

    [Header("Placement")]
    public Vector3 worldOffset = new Vector3(0f, 1.6f, 0f);

    public float worldScale = 0.01f;

    public bool billboardToCamera = true;

    public bool flipFacing = false;

    [Header("Anti-clipping")]
    public bool avoidClipping = true;

    public LayerMask obstructionLayers = ~0;

    public float obstructionSurfaceOffset = 0.05f;

    public float overlapRadius = 0.06f;

    public int maxPushOutIterations = 4;

    [Header("Visibility")]
    public bool hideWhenNoPrompt = true;

    public int sortingOrder = 1000;

    private Canvas _canvas;
    private RectTransform _rootRect;
    private Text _text;

    private void Awake()
    {
        if (playerInteractor == null)
        {
            playerInteractor = FindFirstObjectByType<PlayerInteractor>();
        }

        if (viewCamera == null) viewCamera = Camera.main;

        BuildWorldSpaceText();
    }

    private void LateUpdate()
    {
        if (viewCamera == null) viewCamera = Camera.main;

        string prompt = playerInteractor != null ? playerInteractor.GetCurrentPrompt() : null;
        Transform target = playerInteractor != null ? playerInteractor.CurrentTransform : null;

        bool hasPrompt = !string.IsNullOrWhiteSpace(prompt) && target != null;

        if (_canvas != null && hideWhenNoPrompt)
        {
            _canvas.enabled = hasPrompt;
        }

        if (!hasPrompt) return;

        string display = string.IsNullOrEmpty(prefix)
            ? prompt
            : $"{prefix}{separator}{prompt}";

        if (_text != null)
        {
            _text.text = display;
            if (_text.font == null)
            {
                _text.font = GetFallbackFont();
            }
        }

        if (_rootRect != null)
        {
            Transform t = _rootRect.transform;
            Vector3 desiredPos = target.position + worldOffset;
            if (avoidClipping && viewCamera != null)
            {
                desiredPos = AdjustForObstructions(viewCamera, desiredPos);
            }

            t.position = desiredPos;
            t.localScale = Vector3.one * Mathf.Max(0.0001f, worldScale);

            if (billboardToCamera && viewCamera != null)
            {
                Vector3 camPos = viewCamera.transform.position;
                Vector3 toCam = camPos - t.position;
                if (toCam.sqrMagnitude > 0.0001f)
                {
                    t.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
                    if (flipFacing)
                    {
                        t.rotation = t.rotation * Quaternion.Euler(0f, 180f, 0f);
                    }
                }
            }
        }
    }

    private Vector3 AdjustForObstructions(Camera cam, Vector3 desiredWorldPos)
    {
        Vector3 camPos = cam.transform.position;
        Vector3 dir = desiredWorldPos - camPos;
        float dist = dir.magnitude;
        if (dist < 0.0001f) return desiredWorldPos;
        dir /= dist;

        if (Physics.Raycast(camPos, dir, out RaycastHit hit, dist, obstructionLayers, QueryTriggerInteraction.Ignore))
        {
            desiredWorldPos = hit.point + hit.normal * Mathf.Max(0.0f, obstructionSurfaceOffset);
        }

        float radius = Mathf.Max(0.0f, overlapRadius);
        if (radius > 0f)
        {
            for (int i = 0; i < Mathf.Max(0, maxPushOutIterations); i++)
            {
                var overlaps = Physics.OverlapSphere(desiredWorldPos, radius, obstructionLayers, QueryTriggerInteraction.Ignore);
                if (overlaps == null || overlaps.Length == 0) break;

                desiredWorldPos -= dir * (radius + Mathf.Max(0.0f, obstructionSurfaceOffset));
            }
        }

        return desiredWorldPos;
    }

    private void BuildWorldSpaceText()
    {
        var root = new GameObject("InteractionPrompt3D");
        root.transform.SetParent(transform, false);
        _rootRect = root.AddComponent<RectTransform>();
        _rootRect.sizeDelta = new Vector2(600f, 140f);

        _canvas = root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = viewCamera;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = sortingOrder;

        root.AddComponent<CanvasScaler>();

        var raycaster = root.AddComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(root.transform, false);

        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        _text = textGO.AddComponent<Text>();
        _text.raycastTarget = false;
        _text.font = GetFallbackFont();
        _text.fontSize = 52;
        _text.alignment = TextAnchor.MiddleCenter;
        _text.horizontalOverflow = HorizontalWrapMode.Overflow;
        _text.verticalOverflow = VerticalWrapMode.Overflow;
        _text.color = Color.white;

        var outline = textGO.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        if (_canvas != null && hideWhenNoPrompt)
        {
            _canvas.enabled = false;
        }
    }

    private static Font GetFallbackFont()
    {
        try
        {
            var f = Resources.GetBuiltinResource<Font>(_legacyBuiltInFontPath);
            if (f != null) return f;
        }
        catch { }
        return null;
    }
}

