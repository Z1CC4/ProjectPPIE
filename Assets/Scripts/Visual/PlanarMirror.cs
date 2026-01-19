using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Planar mirror for URP (Unity 6 / URP 17):
/// - Renders a reflection camera into a RenderTexture
/// - Applies it to the mirror renderer via MaterialPropertyBlock
/// - Derives the mirror plane from the mesh (more robust for imported prefabs)
/// </summary>
public class PlanarMirror : MonoBehaviour
{
    public enum PlaneDerivation
    {
        FromMeshPlane,
        FromRendererBounds
    }

    public enum ReflectionBehavior
    {
        /// <summary>
        /// Far: freeze a static reflection (stable interior).
        /// Near: render a true planar reflection using the player camera (so you can see yourself).
        /// </summary>
        StaticWhenFar_DynamicWhenNear,
        StaticOnly,
        DynamicOnly
    }

    [Header("Mirror Target")]
    [Tooltip("Renderer that will receive the reflection texture. If null, uses Renderer on this GameObject.")]
    public Renderer mirrorRenderer;

    [Header("Camera Source")]
    [Tooltip("If set, reflections render only when this camera is rendering.")]
    public Camera sourceCameraOverride;

    [Tooltip("If true and no override is set, reflections render only for Camera.main.")]
    public bool mainCameraOnly = true;

    [Header("Mirror Plane")]
    [Tooltip("How the mirror plane is computed.")]
    public PlaneDerivation planeDerivation = PlaneDerivation.FromMeshPlane;

    [Tooltip("Extra offset along plane normal for clipping / z-fight control.")]
    public float clipPlaneOffset = 0.07f;

    [Tooltip("If enabled, uses oblique near-plane clipping to remove objects behind the mirror plane.")]
    public bool useObliqueClipping = true;

    [Header("Behavior")]
    [Tooltip("Controls how the mirror behaves at distance. Recommended: StaticWhenFar_DynamicWhenNear.")]
    public ReflectionBehavior behavior = ReflectionBehavior.StaticWhenFar_DynamicWhenNear;

    [Tooltip("Distance (in meters) at which the mirror switches to dynamic planar reflection (so you can see yourself).")]
    public float dynamicDistance = 4.0f;

    [Tooltip("Only enable dynamic reflection when the camera is on the front side of the mirror plane.")]
    public bool dynamicOnlyWhenInFront = true;

    [Tooltip("Optional reference transform used to generate the static reflection viewpoint. If null, a default point in front of the mirror is used.")]
    public Transform staticReference;

    [Tooltip("Default distance in front of the mirror used for the static reference viewpoint (when staticReference is null).")]
    public float staticReferenceDistance = 1.5f;

    [Tooltip("Layers rendered in the STATIC (far) mode. Use this to exclude the Player layer if you only want to see yourself when near.")]
    public LayerMask staticReflectionMask = ~0;

    [Header("Reflection Rendering")]
    [Tooltip("Layers rendered by the reflection camera.")]
    public LayerMask reflectionMask = ~0;

    [Tooltip("Override reflection camera far clip plane (useful to avoid reflecting 'outside' when the reflected camera ends up behind walls).")]
    public bool overrideFarClipPlane = true;

    [Tooltip("Far clip plane used by the reflection camera when overrideFarClipPlane is enabled.")]
    public float reflectionFarClipPlane = 35f;

    [Tooltip("If enabled, overrides the reflection camera clear flags (e.g., to avoid rendering the skybox as 'outside world').")]
    public bool overrideClearFlags = true;

    [Tooltip("Clear flags for the reflection camera when overrideClearFlags is enabled.")]
    public CameraClearFlags reflectionClearFlags = CameraClearFlags.SolidColor;

    [Tooltip("Background color for the reflection camera when using SolidColor clear flags.")]
    public Color reflectionBackgroundColor = Color.black;

    [Tooltip("Texture resolution (square).")]
    public int textureSize = 1024;

    [Tooltip("MSAA samples for the reflection texture. 1 disables MSAA.")]
    public int msaaSamples = 1;

    [Header("Material Output")]
    [Tooltip("Tint multiplied with reflection. Keep white for a clean mirror.")]
    public Color reflectionTint = Color.white;

    [Range(0f, 2f)]
    public float reflectionIntensity = 1f;

    [Header("Performance")]
    public bool onlyWhenVisible = true;
    public bool useOcclusionCulling = true;

    [Header("Editor")]
    public bool renderInSceneView = false;

    [Header("Debug")]
    public bool logWarnings = true;

    private Camera _reflectionCamera;
    private RenderTexture _reflectionTexture;
    private MaterialPropertyBlock _mpb;
    private bool _hasStaticCapture;

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int TintId = Shader.PropertyToID("_Tint");
    private static readonly int TintColorId = Shader.PropertyToID("_TintColor");

    private void Awake()
    {
        if (mirrorRenderer == null) mirrorRenderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        Cleanup();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if (_reflectionCamera != null)
        {
            DestroyImmediate(_reflectionCamera.gameObject);
            _reflectionCamera = null;
        }

        if (_reflectionTexture != null)
        {
            _reflectionTexture.Release();
            DestroyImmediate(_reflectionTexture);
            _reflectionTexture = null;
        }
    }

    private void EnsureResources()
    {
        if (mirrorRenderer == null)
        {
            if (logWarnings) Debug.LogWarning("PlanarMirror: mirrorRenderer is null.", this);
            return;
        }

        if (_reflectionCamera == null)
        {
            var go = new GameObject($"{name}_ReflectionCamera");
            go.hideFlags = HideFlags.HideAndDontSave;
            _reflectionCamera = go.AddComponent<Camera>();
            _reflectionCamera.enabled = false;

            if (go.GetComponent<UniversalAdditionalCameraData>() == null)
            {
                var data = go.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = false;
                data.requiresColorOption = CameraOverrideOption.Off;
                data.requiresDepthOption = CameraOverrideOption.Off;
            }
        }

        int w = Mathf.Clamp(textureSize, 64, 8192);
        int h = w;
        if (_reflectionTexture == null || _reflectionTexture.width != w || _reflectionTexture.height != h)
        {
            if (_reflectionTexture != null)
            {
                _reflectionTexture.Release();
                DestroyImmediate(_reflectionTexture);
            }

            var rt = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            rt.name = $"{name}_MirrorRT";
            rt.useMipMap = false;
            rt.autoGenerateMips = false;
            rt.antiAliasing = Mathf.Clamp(msaaSamples, 1, 8);
            rt.Create();
            _reflectionTexture = rt;
        }

        mirrorRenderer.GetPropertyBlock(_mpb);
        _mpb.SetTexture(BaseMapId, _reflectionTexture);
        _mpb.SetTexture(MainTexId, _reflectionTexture);
        Color tint = reflectionTint * Mathf.Max(0f, reflectionIntensity);
        _mpb.SetColor(BaseColorId, tint);
        _mpb.SetColor(ColorId, tint);
        _mpb.SetColor(TintId, tint);
        _mpb.SetColor(TintColorId, tint);
        mirrorRenderer.SetPropertyBlock(_mpb);
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == null) return;
        if (!renderInSceneView && camera.cameraType == CameraType.SceneView) return;

        if (!IsAllowedCamera(camera)) return;

        if (_reflectionCamera != null && camera == _reflectionCamera) return;

        EnsureResources();
        if (mirrorRenderer == null || _reflectionCamera == null || _reflectionTexture == null) return;

        if (onlyWhenVisible && !IsMirrorVisibleFrom(camera)) return;

        if (!TryGetMirrorPlane(camera.transform.position, out var planePoint, out var planeNormal))
        {
            if (logWarnings) Debug.LogWarning("PlanarMirror: could not compute mirror plane.", this);
            return;
        }

        float viewerSide = Vector3.Dot(planeNormal, camera.transform.position - planePoint);
        float viewerDist = Vector3.Distance(camera.transform.position, planePoint);

        bool dynamicAllowed = behavior != ReflectionBehavior.StaticOnly;
        bool staticAllowed = behavior != ReflectionBehavior.DynamicOnly;

        bool shouldRenderDynamic = dynamicAllowed &&
                                  (behavior == ReflectionBehavior.DynamicOnly ||
                                   (behavior == ReflectionBehavior.StaticWhenFar_DynamicWhenNear && viewerDist <= Mathf.Max(0.01f, dynamicDistance)));

        if (shouldRenderDynamic && dynamicOnlyWhenInFront && viewerSide <= 0f)
        {
            shouldRenderDynamic = false;
        }

        if (shouldRenderDynamic)
        {
            _hasStaticCapture = false; // force a new static capture next time we go static
            RenderDynamicReflection(context, camera, planePoint, planeNormal);
            return;
        }

        if (!staticAllowed) return;

        // Static/frozen: capture once, then keep it.
        if (!_hasStaticCapture)
        {
            RenderStaticReflection(context, camera, planePoint, planeNormal);
            _hasStaticCapture = true;
        }
    }

    private bool IsMirrorVisibleFrom(Camera cam)
    {
        var planes = GeometryUtility.CalculateFrustumPlanes(cam);
        return GeometryUtility.TestPlanesAABB(planes, mirrorRenderer.bounds);
    }

    private void RenderDynamicReflection(ScriptableRenderContext context, Camera sourceCamera, Vector3 planePoint, Vector3 planeNormal)
    {
        ConfigureReflectionCamera(sourceCamera, reflectionMask);

        // Reflection matrix around plane.
        float d = -Vector3.Dot(planeNormal, planePoint) - clipPlaneOffset;
        Vector4 plane = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, d);
        Matrix4x4 refl = CalculateReflectionMatrix(plane);

        Matrix4x4 worldToCamera = sourceCamera.worldToCameraMatrix * refl;
        ApplyViewAndProjection(sourceCamera, worldToCamera);

        if (useObliqueClipping)
        {
            float sideSign = Vector3.Dot(planeNormal, sourceCamera.transform.position - planePoint) >= 0f ? 1f : -1f;
            Vector4 clip = CameraSpacePlane(ref worldToCamera, planePoint, planeNormal, sideSign, clipPlaneOffset);
            _reflectionCamera.projectionMatrix = _reflectionCamera.CalculateObliqueMatrix(clip);
        }

        RenderOnce(context);
    }

    private void RenderStaticReflection(ScriptableRenderContext context, Camera sourceCamera, Vector3 planePoint, Vector3 planeNormal)
    {
        Vector3 viewerPos;
        Quaternion viewerRot;

        if (staticReference != null)
        {
            viewerPos = staticReference.position;
            viewerRot = staticReference.rotation;
        }
        else
        {
            viewerPos = planePoint + planeNormal * Mathf.Max(0.05f, staticReferenceDistance);
            viewerRot = Quaternion.LookRotation(-planeNormal, Vector3.up);
        }

        Matrix4x4 virtualWorldToCamera = Matrix4x4.TRS(viewerPos, viewerRot, Vector3.one).inverse;

        ConfigureReflectionCamera(sourceCamera, staticReflectionMask);

        float d = -Vector3.Dot(planeNormal, planePoint) - clipPlaneOffset;
        Vector4 plane = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, d);
        Matrix4x4 refl = CalculateReflectionMatrix(plane);

        Matrix4x4 worldToCamera = virtualWorldToCamera * refl;
        ApplyViewAndProjection(sourceCamera, worldToCamera);

        if (useObliqueClipping)
        {
            float sideSign = Vector3.Dot(planeNormal, viewerPos - planePoint) >= 0f ? 1f : -1f;
            Vector4 clip = CameraSpacePlane(ref worldToCamera, planePoint, planeNormal, sideSign, clipPlaneOffset);
            _reflectionCamera.projectionMatrix = _reflectionCamera.CalculateObliqueMatrix(clip);
        }

        RenderOnce(context);
    }

    private bool IsAllowedCamera(Camera camera)
    {
        Camera allowed = sourceCameraOverride != null ? sourceCameraOverride : (mainCameraOnly ? Camera.main : null);
        return allowed == null || camera == allowed;
    }

    private void ConfigureReflectionCamera(Camera template, LayerMask mask)
    {
        _reflectionCamera.CopyFrom(template);
        _reflectionCamera.enabled = false;
        _reflectionCamera.useOcclusionCulling = useOcclusionCulling;
        _reflectionCamera.targetTexture = _reflectionTexture;
        _reflectionCamera.cullingMask = mask;

        if (overrideFarClipPlane && reflectionFarClipPlane > 0.01f)
        {
            _reflectionCamera.farClipPlane = reflectionFarClipPlane;
        }

        if (overrideClearFlags)
        {
            _reflectionCamera.clearFlags = reflectionClearFlags;
            if (reflectionClearFlags == CameraClearFlags.SolidColor)
            {
                _reflectionCamera.backgroundColor = reflectionBackgroundColor;
            }
        }
    }

    private void ApplyViewAndProjection(Camera sourceCamera, Matrix4x4 worldToCamera)
    {
        _reflectionCamera.worldToCameraMatrix = worldToCamera;
        _reflectionCamera.projectionMatrix = sourceCamera.projectionMatrix;

        // Keep transform consistent for culling/skybox heuristics.
        Matrix4x4 camToWorld = worldToCamera.inverse;
        Vector3 pos = camToWorld.GetColumn(3);
        Vector3 forward = camToWorld.GetColumn(2);
        Vector3 up = camToWorld.GetColumn(1);
        if (forward.sqrMagnitude > 1e-6f && up.sqrMagnitude > 1e-6f)
        {
            _reflectionCamera.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(forward, up));
        }
    }

    private void RenderOnce(ScriptableRenderContext context)
    {
        bool prevInvert = GL.invertCulling;
        GL.invertCulling = true;
        bool prevEnabled = mirrorRenderer.enabled;
        mirrorRenderer.enabled = false;
        try
        {
            UniversalRenderPipeline.RenderSingleCamera(context, _reflectionCamera);
        }
        finally
        {
            mirrorRenderer.enabled = prevEnabled;
            GL.invertCulling = prevInvert;
        }
    }

    private bool TryGetMirrorPlane(Vector3 viewerPosition, out Vector3 point, out Vector3 normal)
    {
        point = Vector3.zero;
        normal = Vector3.forward;

        if (mirrorRenderer == null) return false;

        if (planeDerivation == PlaneDerivation.FromRendererBounds)
        {
            point = mirrorRenderer.bounds.center;
            normal = (mirrorRenderer.transform.forward).normalized;
            return true;
        }

        // From mesh plane (preferred).
        var mf = mirrorRenderer.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return false;
        var mesh = mf.sharedMesh;
        var tris = mesh.triangles;
        var verts = mesh.vertices;
        if (tris == null || tris.Length < 3 || verts == null || verts.Length < 3) return false;

        int i0 = tris[0], i1 = tris[1], i2 = tris[2];
        if (i0 < 0 || i0 >= verts.Length || i1 < 0 || i1 >= verts.Length || i2 < 0 || i2 >= verts.Length) return false;

        Vector3 v0 = mf.transform.TransformPoint(verts[i0]);
        Vector3 v1 = mf.transform.TransformPoint(verts[i1]);
        Vector3 v2 = mf.transform.TransformPoint(verts[i2]);

        Vector3 n = Vector3.Cross(v1 - v0, v2 - v0);
        if (n.sqrMagnitude < 1e-6f) return false;
        normal = n.normalized;

        // Use mesh bounds center as a stable point on the plane.
        point = mf.transform.TransformPoint(mesh.bounds.center);

        // For clipping we want the normal that faces the camera; reflection matrix is invariant under sign,
        // but clip side selection relies on it.
        if (Vector3.Dot(normal, viewerPosition - point) < 0f)
        {
            normal = -normal;
        }

        return true;
    }

    private static Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
    {
        Matrix4x4 m = Matrix4x4.identity;
        m.m00 = 1F - 2F * plane[0] * plane[0];
        m.m01 = -2F * plane[0] * plane[1];
        m.m02 = -2F * plane[0] * plane[2];
        m.m03 = -2F * plane[3] * plane[0];

        m.m10 = -2F * plane[1] * plane[0];
        m.m11 = 1F - 2F * plane[1] * plane[1];
        m.m12 = -2F * plane[1] * plane[2];
        m.m13 = -2F * plane[3] * plane[1];

        m.m20 = -2F * plane[2] * plane[0];
        m.m21 = -2F * plane[2] * plane[1];
        m.m22 = 1F - 2F * plane[2] * plane[2];
        m.m23 = -2F * plane[3] * plane[2];

        m.m30 = 0F;
        m.m31 = 0F;
        m.m32 = 0F;
        m.m33 = 1F;
        return m;
    }

    private static Vector4 CameraSpacePlane(ref Matrix4x4 worldToCameraMatrix, Vector3 planePoint, Vector3 planeNormal, float sideSign, float clipOffset)
    {
        Vector3 offsetPos = planePoint + planeNormal * clipOffset;
        Vector3 cPos = worldToCameraMatrix.MultiplyPoint(offsetPos);
        Vector3 cNormal = worldToCameraMatrix.MultiplyVector(planeNormal).normalized * sideSign;
        return new Vector4(cNormal.x, cNormal.y, cNormal.z, -Vector3.Dot(cPos, cNormal));
    }

}

