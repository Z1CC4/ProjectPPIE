using UnityEngine;

/// <summary>
/// Visual controller for fire effects.
/// This script reacts to KitchenState fire changes and updates visual effects
/// (particle systems, lights, etc.). It contains NO input logic.
/// </summary>
public class FireVisualController : MonoBehaviour
{
    [Header("Fire Visual Effects")]
    [Tooltip("Particle system for fire effect (e.g., flames)")]
    public ParticleSystem fireParticles;

    [Tooltip("Light component for fire glow (optional)")]
    public Light fireLight;

    [Tooltip("GameObject to enable/disable when fire is on (e.g., fire mesh or VFX prefab like VFX_Fire_Floor_02_Simple)")]
    public GameObject fireMesh;

    [Header("Settings")]
    [Tooltip("Intensity of fire light when on")]
    public float fireLightIntensity = 2.0f;

    private float originalLightIntensity = 0f;
    private KitchenState kitchenState;

    private void Start()
    {
        // Get reference to KitchenState
        kitchenState = KitchenState.Instance;
        if (kitchenState == null)
        {
            Debug.LogError("FireVisualController: KitchenState not found!");
            enabled = false;
            return;
        }

        // Store original light intensity
        if (fireLight != null)
        {
            originalLightIntensity = fireLight.intensity;
        }

        // Subscribe to fire state changes
        kitchenState.OnFireStateChanged += HandleFireStateChanged;

        // Set initial state
        HandleFireStateChanged(kitchenState.IsFireOn);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (kitchenState != null)
        {
            kitchenState.OnFireStateChanged -= HandleFireStateChanged;
        }
    }

    /// <summary>
    /// Handles fire state changes and updates visual effects.
    /// </summary>
    private void HandleFireStateChanged(bool isFireOn)
    {
        // Update particle system
        if (fireParticles != null)
        {
            if (isFireOn)
            {
                if (!fireParticles.isPlaying)
                {
                    fireParticles.Play();
                }
            }
            else
            {
                if (fireParticles.isPlaying)
                {
                    fireParticles.Stop();
                }
            }
        }

        // Update light
        if (fireLight != null)
        {
            fireLight.intensity = isFireOn ? fireLightIntensity : 0f;
            fireLight.enabled = isFireOn;
        }

        // Update fire mesh or VFX GameObject (enable/disable the entire prefab)
        if (fireMesh != null)
        {
            fireMesh.SetActive(isFireOn);
            
            // If it's a Visual Effect (VFX Graph), also start/stop it
            var visualEffect = fireMesh.GetComponent<UnityEngine.VFX.VisualEffect>();
            if (visualEffect != null)
            {
                if (isFireOn)
                {
                    visualEffect.Play();
                }
                else
                {
                    visualEffect.Stop();
                }
            }
        }
    }
}
