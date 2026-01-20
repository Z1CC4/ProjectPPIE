using UnityEngine;

public class WaterVisualController : MonoBehaviour
{
    [Header("Water Visual Effects")]
    [Tooltip("Particle system for water effect (e.g., water stream)")]
    public ParticleSystem waterParticles;

    [Tooltip("GameObject to enable/disable when water is on (e.g., water mesh)")]
    public GameObject waterMesh;

    [Tooltip("Audio source for water sound (optional)")]
    public AudioSource waterAudioSource;

    [Header("Settings")]
    [Tooltip("Water intensity (0-1). Currently binary, but ready for analog input.")]
    [Range(0f, 1f)]
    public float waterIntensity = 1.0f;

    private KitchenState kitchenState;

    private void Start()
    {
        kitchenState = KitchenState.Instance;
        if (kitchenState == null)
        {
            Debug.LogError("WaterVisualController: KitchenState not found!");
            enabled = false;
            return;
        }

        kitchenState.OnWaterStateChanged += HandleWaterStateChanged;

        HandleWaterStateChanged(kitchenState.IsWaterOn);
    }

    private void OnDestroy()
    {
        if (kitchenState != null)
        {
            kitchenState.OnWaterStateChanged -= HandleWaterStateChanged;
        }
    }

    private void HandleWaterStateChanged(bool isWaterOn)
    {
        if (waterParticles != null)
        {
            if (isWaterOn)
            {
                if (!waterParticles.isPlaying)
                {
                    waterParticles.Play();
                }

                var emission = waterParticles.emission;
                emission.rateOverTime = 100f * waterIntensity;
            }
            else
            {
                if (waterParticles.isPlaying)
                {
                    waterParticles.Stop();
                }
            }
        }

        if (waterMesh != null)
        {
            waterMesh.SetActive(isWaterOn);
        }

        if (waterAudioSource != null)
        {
            if (isWaterOn)
            {
                if (!waterAudioSource.isPlaying)
                {
                    waterAudioSource.Play();
                }
            }
            else
            {
                if (waterAudioSource.isPlaying)
                {
                    waterAudioSource.Stop();
                }
            }
        }
    }

    public void SetWaterIntensity(float intensity)
    {
        waterIntensity = Mathf.Clamp01(intensity);
        
        if (kitchenState != null && kitchenState.IsWaterOn && waterParticles != null)
        {
            var emission = waterParticles.emission;
            emission.rateOverTime = 100f * waterIntensity;
        }
    }
}
