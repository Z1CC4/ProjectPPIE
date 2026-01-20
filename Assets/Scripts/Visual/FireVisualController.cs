using UnityEngine;

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
        kitchenState = KitchenState.Instance;
        if (kitchenState == null)
        {
            Debug.LogError("FireVisualController: KitchenState not found!");
            enabled = false;
            return;
        }

        if (fireLight != null)
        {
            originalLightIntensity = fireLight.intensity;
        }

        kitchenState.OnFireStateChanged += HandleFireStateChanged;

        HandleFireStateChanged(kitchenState.IsFireOn);
    }

    private void OnDestroy()
    {
        if (kitchenState != null)
        {
            kitchenState.OnFireStateChanged -= HandleFireStateChanged;
        }
    }

    private void HandleFireStateChanged(bool isFireOn)
    {
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

        if (fireLight != null)
        {
            fireLight.intensity = isFireOn ? fireLightIntensity : 0f;
            fireLight.enabled = isFireOn;
        }

        if (fireMesh != null)
        {
            fireMesh.SetActive(isFireOn);
            
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
