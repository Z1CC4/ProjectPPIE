using UnityEngine;

/// <summary>
/// Visual controller for water effects.
/// This script reacts to KitchenState water changes and updates visual effects
/// (particle systems, water flow, etc.). It contains NO input logic.
/// </summary>
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
        // Get reference to KitchenState
        kitchenState = KitchenState.Instance;
        if (kitchenState == null)
        {
            Debug.LogError("WaterVisualController: KitchenState not found!");
            enabled = false;
            return;
        }

        // Subscribe to water state changes
        kitchenState.OnWaterStateChanged += HandleWaterStateChanged;

        // Set initial state
        HandleWaterStateChanged(kitchenState.IsWaterOn);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (kitchenState != null)
        {
            kitchenState.OnWaterStateChanged -= HandleWaterStateChanged;
        }
    }

    /// <summary>
    /// Handles water state changes and updates visual effects.
    /// </summary>
    private void HandleWaterStateChanged(bool isWaterOn)
    {
        // Update particle system
        if (waterParticles != null)
        {
            if (isWaterOn)
            {
                if (!waterParticles.isPlaying)
                {
                    waterParticles.Play();
                }

                // Adjust emission rate based on intensity (ready for analog input)
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

        // Update water mesh
        if (waterMesh != null)
        {
            waterMesh.SetActive(isWaterOn);
        }

        // Update audio
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

    /// <summary>
    /// Sets water intensity (for future analog water sensor support).
    /// </summary>
    public void SetWaterIntensity(float intensity)
    {
        waterIntensity = Mathf.Clamp01(intensity);
        
        // Update particle emission if water is on
        if (kitchenState != null && kitchenState.IsWaterOn && waterParticles != null)
        {
            var emission = waterParticles.emission;
            emission.rateOverTime = 100f * waterIntensity;
        }
    }
}
