using UnityEngine;

public class BedLaySpot : MonoBehaviour
{
    [Header("Targets")]
    public Transform layTarget;

    public Transform exitTarget;

    [Header("Tuning")]
    [Min(0f)]
    public float snapDuration = 0.25f;
}

