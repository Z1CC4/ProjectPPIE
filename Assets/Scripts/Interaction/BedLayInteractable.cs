using UnityEngine;

/// <summary>
/// Rende il letto compatibile col sistema già presente (PlayerInteractor + IInteractable).
/// Così il JOY che usi per frigo/porte funziona anche per sdraiarsi.
/// </summary>
public class BedLayInteractable : MonoBehaviour, IInteractable
{
    [Header("Setup")]
    public BedLaySpot bedSpot;

    [Header("Prompt")]
    public string prompt = "Sdraiati / Alzati";

    private void Reset()
    {
        bedSpot = GetComponentInParent<BedLaySpot>();
    }

    public string GetInteractionPrompt()
    {
        return prompt;
    }

    public bool CanInteract()
    {
        return bedSpot != null && bedSpot.layTarget != null;
    }

    public void Interact(GameObject interactor)
    {
        if (interactor == null) return;

        var playerBed = interactor.GetComponentInParent<PlayerBedInteractor>();
        if (playerBed == null) playerBed = interactor.GetComponent<PlayerBedInteractor>();
        if (playerBed == null) return;

        playerBed.ToggleBed(bedSpot);
    }
}

