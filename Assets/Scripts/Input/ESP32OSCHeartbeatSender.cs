using System.Collections;
using UnityEngine;
using extOSC;

public class ESP32OSCHeartbeatSender : MonoBehaviour
{
    [Header("ESP32 Destination")]
    public string esp32Ip = "192.168.4.1";
    public int esp32Port = 6666;

    [Header("Heartbeat")]
    [Tooltip("Seconds between heartbeats (e.g. 0.5 = every 500ms).")]
    public float intervalSeconds = 0.5f;

    [Tooltip("If true, starts sending immediately on play.")]
    public bool autoStart = true;

    [Header("Optional")]
    [Tooltip("If assigned, heartbeat can be gated on receiver activity.")]
    public ESP32OSCReceiver receiver;

    [Tooltip("If true, only sends heartbeats while receiving OSC recently.")]
    public bool onlyWhenReceiving = false;

    private OSCTransmitter transmitter;
    private OSCMessage heartbeatMessage;
    private Coroutine loop;

    void Awake()
    {
        transmitter = GetComponent<OSCTransmitter>();
        if (transmitter == null)
        {
            transmitter = gameObject.AddComponent<OSCTransmitter>();
        }

        if (receiver == null)
        {
            receiver = ESP32OSCReceiver.Instance != null
                ? ESP32OSCReceiver.Instance
                : FindFirstObjectByType<ESP32OSCReceiver>();
        }

        heartbeatMessage = new OSCMessage("/heartbeat");
    }

    void OnEnable()
    {
        if (autoStart)
        {
            StartHeartbeat();
        }
    }

    void OnDisable()
    {
        StopHeartbeat();
    }

    public void StartHeartbeat()
    {
        transmitter.RemoteHost = esp32Ip;
        transmitter.RemotePort = esp32Port;

        if (loop == null)
        {
            loop = StartCoroutine(HeartbeatLoop());
        }
    }

    public void StopHeartbeat()
    {
        if (loop != null)
        {
            StopCoroutine(loop);
            loop = null;
        }
    }

    private IEnumerator HeartbeatLoop()
    {
        while (true)
        {
            if (!onlyWhenReceiving || (receiver != null && receiver.IsConnected()))
            {
                transmitter.Send(heartbeatMessage);
            }

            yield return new WaitForSeconds(intervalSeconds);
        }
    }
}

