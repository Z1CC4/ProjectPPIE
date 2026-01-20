using System.Collections;
using UnityEngine;
using StarterAssets;

public class PlayerBedInteractor : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Key del joystick da usare (consigliato se non vuoi configurare l'Input Manager).")]
    public KeyCode joyKey = KeyCode.JoystickButton0;

    [Tooltip("Se hai configurato Project Settings > Input Manager con un bottone chiamato così, puoi usarlo. Se vuoto, viene ignorato.")]
    public string joyButtonName = "JOY";

    [Header("ESP32 OSC (progetto TUTTO)")]
    [Tooltip("Se usi l'ESP32 (come per frigo/oggetti), qui puoi leggere il joyButton direttamente.")]
    public ESP32OSCReceiver oscReceiver;

    [Tooltip("ESP32 usa INPUT_PULLUP: 0=pressed, 1=released di default.")]
    public int oscPressedValue = 0;

    [Tooltip("Se true, il JOY via OSC funziona solo quando l'OSC è connesso.")]
    public bool requireOscConnected = true;

    public float oscTimeoutSeconds = 1.5f;

    [Header("Disable while lying")]
    [Tooltip("Componenti da disabilitare quando sei sdraiato (es. StarterAssets FirstPersonController, mouse/gamepad look, ecc.).")]
    public Behaviour[] disableWhileLying;

    [Tooltip("Se presente, viene disabilitato durante lo sdraio/alzata per evitare collisioni strane.")]
    public CharacterController characterController;

    [Tooltip("Se usi un Rigidbody sul player, verrà messo in kinematic durante lo sdraio.")]
    public Rigidbody rigidbodyToFreeze;

    [Header("While lying")]
    [Tooltip("Se true, mentre sei a letto la posizione del player viene tenuta bloccata sul layTarget (impedisce di camminare/cadere), ma la vista resta libera.")]
    public bool lockPositionToLayTarget = true;

    [Tooltip("Se assegnato (Starter Assets), durante lo sdraio blocca SOLO il movimento (move/jump/sprint) lasciando la vista libera.")]
    public StarterAssetsInputs starterAssetsInputs;

    [Header("Optional")]
    [Tooltip("Se assegnato, il player verrà mosso/ruotato su questo transform invece che su 'this.transform'.")]
    public Transform playerRootOverride;

    private BedLaySpot _bedInRange;
    private BedLaySpot _activeBed;
    private bool _isLying;
    private bool _isTransitioning;

    private Vector3 _savedPos;
    private Quaternion _savedRot;
    private bool _savedRbKinematic;
    private int _prevOscBtn;

    private void Reset()
    {
        characterController = GetComponent<CharacterController>();
        rigidbodyToFreeze = GetComponent<Rigidbody>();
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
    }

    private void Start()
    {
        _prevOscBtn = 1 - oscPressedValue;
    }

    private void LateUpdate()
    {
        if (!_isLying || _activeBed == null)
            return;

        if (starterAssetsInputs == null)
        {
            starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        }
        if (starterAssetsInputs != null)
        {
            starterAssetsInputs.move = Vector2.zero;
            starterAssetsInputs.jump = false;
            starterAssetsInputs.sprint = false;
        }

        if (lockPositionToLayTarget && _activeBed.layTarget != null)
        {
            var t = PlayerRoot;
            t.position = _activeBed.layTarget.position;
        }
    }

    private void Update()
    {
        if (_isTransitioning)
            return;

        if (!JoyPressedThisFrame())
            return;

        if (_isLying)
        {
            StartCoroutine(CoGetUp());
        }
        else if (_bedInRange != null)
        {
            StartCoroutine(CoLayDown(_bedInRange));
        }
    }

    public void ToggleBed(BedLaySpot bed)
    {
        if (_isTransitioning)
            return;

        if (_isLying)
        {
            StartCoroutine(CoGetUp());
            return;
        }

        if (bed != null)
        {
            StartCoroutine(CoLayDown(bed));
        }
    }

    private bool JoyPressedThisFrame()
    {
        if (Input.GetKeyDown(joyKey))
            return true;

        if (!string.IsNullOrWhiteSpace(joyButtonName))
        {
            try
            {
                if (Input.GetButtonDown(joyButtonName))
                    return true;
            }
            catch
            {
            }
        }

        if (oscReceiver == null)
        {
            oscReceiver = ESP32OSCReceiver.Instance != null
                ? ESP32OSCReceiver.Instance
                : FindFirstObjectByType<ESP32OSCReceiver>();
        }

        if (oscReceiver != null)
        {
            if (!requireOscConnected || oscReceiver.IsConnected(oscTimeoutSeconds))
            {
                int btnNow = oscReceiver.joyButton;
                bool wasPressed = _prevOscBtn == oscPressedValue;
                bool isPressed = btnNow == oscPressedValue;
                _prevOscBtn = btnNow;

                if (!wasPressed && isPressed)
                    return true;
            }
        }

        return false;
    }

    private Transform PlayerRoot => playerRootOverride != null ? playerRootOverride : transform;

    private IEnumerator CoLayDown(BedLaySpot bed)
    {
        if (bed == null || bed.layTarget == null)
            yield break;

        _isTransitioning = true;
        _activeBed = bed;

        _savedPos = PlayerRoot.position;
        _savedRot = PlayerRoot.rotation;

        SetEnabled(disableWhileLying, false);

        if (rigidbodyToFreeze != null)
        {
            _savedRbKinematic = rigidbodyToFreeze.isKinematic;
            rigidbodyToFreeze.isKinematic = true;
            rigidbodyToFreeze.linearVelocity = Vector3.zero;
            rigidbodyToFreeze.angularVelocity = Vector3.zero;
        }

        yield return CoSnap(PlayerRoot, bed.layTarget.position, bed.layTarget.rotation, Mathf.Max(0f, bed.snapDuration));

        _isLying = true;
        SetEnabled(disableWhileLying, true);
        _isTransitioning = false;
    }

    private IEnumerator CoGetUp()
    {
        if (_activeBed == null)
            yield break;

        _isTransitioning = true;

        SetEnabled(disableWhileLying, false);

        Vector3 targetPos;
        Quaternion targetRot;

        if (_activeBed.exitTarget != null)
        {
            targetPos = _activeBed.exitTarget.position;
            targetRot = _activeBed.exitTarget.rotation;
        }
        else
        {
            targetPos = _savedPos;
            targetRot = _savedRot;
        }

        yield return CoSnap(PlayerRoot, targetPos, targetRot, Mathf.Max(0f, _activeBed.snapDuration));

        if (rigidbodyToFreeze != null)
            rigidbodyToFreeze.isKinematic = _savedRbKinematic;

        SetEnabled(disableWhileLying, true);

        _isLying = false;
        _activeBed = null;
        _isTransitioning = false;
    }

    private static IEnumerator CoSnap(Transform t, Vector3 pos, Quaternion rot, float duration)
    {
        if (duration <= 0f)
        {
            t.SetPositionAndRotation(pos, rot);
            yield break;
        }

        Vector3 startPos = t.position;
        Quaternion startRot = t.rotation;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float a = Mathf.Clamp01(time / duration);
            t.SetPositionAndRotation(Vector3.Lerp(startPos, pos, a), Quaternion.Slerp(startRot, rot, a));
            yield return null;
        }

        t.SetPositionAndRotation(pos, rot);
    }

    private static void SetEnabled(Behaviour[] behaviours, bool enabled)
    {
        if (behaviours == null)
            return;

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null)
                behaviours[i].enabled = enabled;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var bed = other.GetComponentInParent<BedLaySpot>();
        if (bed != null)
            _bedInRange = bed;
    }

    private void OnTriggerExit(Collider other)
    {
        var bed = other.GetComponentInParent<BedLaySpot>();
        if (bed != null && bed == _bedInRange)
            _bedInRange = null;
    }
}

