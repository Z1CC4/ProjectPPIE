using System.Collections;
using UnityEngine;

public class HingedDoorToggleInteractable : MonoBehaviour, IInteractable
{
    [Header("Target")]
    public Transform door;

    [Header("Rotation")]
    public Vector3 closedLocalEuler = Vector3.zero;

    public Vector3 openLocalEuler = new Vector3(0f, 90f, 0f);

    [Header("Timing")]
    public float duration = 0.25f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Prompt")]
    public string promptWhenClosed = "Apri";
    public string promptWhenOpen = "Chiudi";

    [Header("State")]
    [SerializeField] private bool isOpen = false;

    private Coroutine _anim;

    private void Reset()
    {
        door = transform;
    }

    private void Awake()
    {
        if (door == null) door = transform;
        ApplyImmediate();
    }

    public string GetInteractionPrompt()
    {
        return isOpen ? promptWhenOpen : promptWhenClosed;
    }

    public bool CanInteract()
    {
        return _anim == null;
    }

    public void Interact(GameObject interactor)
    {
        Toggle();
    }

    public void Toggle()
    {
        if (_anim != null) return;
        isOpen = !isOpen;
        _anim = StartCoroutine(AnimateTo(isOpen ? openLocalEuler : closedLocalEuler));
    }

    private void ApplyImmediate()
    {
        if (door == null) return;
        door.localRotation = Quaternion.Euler(isOpen ? openLocalEuler : closedLocalEuler);
    }

    private IEnumerator AnimateTo(Vector3 targetEuler)
    {
        if (door == null)
        {
            _anim = null;
            yield break;
        }

        Quaternion from = door.localRotation;
        Quaternion to = Quaternion.Euler(targetEuler);

        if (duration <= 0f)
        {
            door.localRotation = to;
            _anim = null;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            float e = ease != null ? ease.Evaluate(a) : a;
            door.localRotation = Quaternion.Slerp(from, to, e);
            yield return null;
        }

        door.localRotation = to;
        _anim = null;
    }
}

