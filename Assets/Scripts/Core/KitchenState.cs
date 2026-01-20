using System;
using UnityEngine;

public class KitchenState : MonoBehaviour
{
    public static KitchenState Instance { get; private set; }

    public event Action<bool> OnFireStateChanged;
    public event Action<bool> OnWaterStateChanged;
    public event Action<SelectableKitchenObject> OnSelectionChanged;
    public event Action<float> OnPanMovementIntensityChanged;

    [SerializeField] private bool isFireOn = false;
    [SerializeField] private bool isWaterOn = false;
    [SerializeField] private SelectableKitchenObject selectedObject = null;
    [SerializeField] private float panMovementIntensity = 0.0f;

    public bool IsFireOn => isFireOn;
    public bool IsWaterOn => isWaterOn;
    public SelectableKitchenObject SelectedObject => selectedObject;
    public float PanMovementIntensity => panMovementIntensity;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple KitchenState instances found! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    public void SetFire(bool isOn)
    {
        if (isFireOn != isOn)
        {
            isFireOn = isOn;
            OnFireStateChanged?.Invoke(isFireOn);
        }
    }

    public void SetWater(bool isOn)
    {
        if (isWaterOn != isOn)
        {
            isWaterOn = isOn;
            OnWaterStateChanged?.Invoke(isWaterOn);
        }
    }

    public void MoveSelection(Vector2 direction)
    {
        if (direction.magnitude < 0.1f) return;

        SelectableKitchenObject nearest = FindNearestInDirection(direction);
        
        if (nearest != null && nearest != selectedObject)
        {
            SetSelectedObject(nearest);
        }
    }

    public void SetSelectedObject(SelectableKitchenObject obj)
    {
        if (selectedObject != obj)
        {
            selectedObject = obj;
            OnSelectionChanged?.Invoke(selectedObject);
        }
    }

    private SelectableKitchenObject FindNearestInDirection(Vector2 direction)
    {
        SelectableKitchenObject[] allSelectables = FindObjectsByType<SelectableKitchenObject>(FindObjectsSortMode.None);
        
        if (allSelectables.Length == 0) return null;

        Vector3 currentPos = selectedObject != null 
            ? selectedObject.transform.position 
            : Vector3.zero;

        SelectableKitchenObject bestCandidate = null;
        float bestScore = float.NegativeInfinity;

        foreach (SelectableKitchenObject obj in allSelectables)
        {
            if (obj == selectedObject) continue;

            Vector3 toObject = obj.transform.position - currentPos;
            Vector2 toObject2D = new Vector2(toObject.x, toObject.z);

            if (toObject2D.magnitude < 0.1f) continue;

            Vector2 normalizedDir = toObject2D.normalized;

            float alignment = Vector2.Dot(direction.normalized, normalizedDir);

            float distance = toObject2D.magnitude;
            float score = alignment - (distance * 0.1f);

            if (alignment > 0.3f && score > bestScore)
            {
                bestScore = score;
                bestCandidate = obj;
            }
        }

        return bestCandidate;
    }

    public void SetPanMovementIntensity(float intensity)
    {
        intensity = Mathf.Clamp01(intensity);
        
        if (Mathf.Abs(panMovementIntensity - intensity) > 0.01f)
        {
            panMovementIntensity = intensity;
            OnPanMovementIntensityChanged?.Invoke(panMovementIntensity);
        }
    }
}
