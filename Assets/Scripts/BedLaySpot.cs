using UnityEngine;

/// <summary>
/// Metti questo componente sul GameObject "letto" (o su un suo child).
/// Serve a definire dove deve essere "snappato" il player quando si sdraia.
/// </summary>
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

    private void OnDrawGizmosSelected()
    {
        if (layTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(layTarget.position, 0.08f);
            Gizmos.DrawLine(layTarget.position, layTarget.position + layTarget.forward * 0.4f);
        }

        if (exitTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(exitTarget.position, 0.08f);
            Gizmos.DrawLine(exitTarget.position, exitTarget.position + exitTarget.forward * 0.4f);
        }
    }
}

