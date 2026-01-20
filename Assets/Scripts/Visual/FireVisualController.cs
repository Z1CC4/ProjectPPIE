using UnityEngine;

public class FireVisualController : MonoBehaviour
{
    [Header("Fire Visual Effects")]
    public ParticleSystem fireParticles;

    public Light fireLight;

    public GameObject fireMesh;

    [Header("Settings")]
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
