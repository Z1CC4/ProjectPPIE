using System;
using System.Collections.Generic;
using UnityEngine;

// [ExecuteInEditMode] // Make mirror live-update even when not in play mode
public class MirrorScript : MonoBehaviour
{
    public int MaximumPerPixelLights = 2;

    public int TextureSize = 768;

    public float ClipPlaneOffset = 0.07f;

    public float FarClipPlane = 1000.0f;

    public LayerMask ReflectLayers = -1;

    public bool AddFlareLayer = false;

    public bool NormalIsForward = true;

    public float AspectRatio = 0.0f;

    public bool MirrorRecursion;
}