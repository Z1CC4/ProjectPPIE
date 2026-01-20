using UnityEngine;

public class BedLaySpot : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Dove deve andare il player quando si sdraia (posizione + rotazione).")]
    public Transform layTarget;

    [Tooltip("Dove deve tornare il player quando si alza. Se null, torna alla posizione salvata prima di sdraiarsi.")]
    public Transform exitTarget;

    [Header("Tuning")]
    [Min(0f)]
    [Tooltip("Durata (secondi) della transizione (lerp) verso/da letto.")]
    public float snapDuration = 0.25f;
}

