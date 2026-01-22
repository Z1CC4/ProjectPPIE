using UnityEngine;
using extOSC;

public class ESP32OSCSender : MonoBehaviour
{
    public string espIP = "192.168.4.1";
    public int espPort = 6666;
    public float heartbeatInterval = 0.5f;

    private OSCTransmitter transmitter;
    private float lastSendTime;

    void Start()
    {
        transmitter = gameObject.AddComponent<OSCTransmitter>();
        transmitter.RemoteHost = espIP;
        transmitter.RemotePort = espPort;

        SendGameName();
    }

    void Update()
    {
        if (!ESP32OSCReceiver.Instance.IsConnected())
            return;

        if (Time.time - lastSendTime >= heartbeatInterval)
        {
            SendHeartbeat();
            lastSendTime = Time.time;
        }
    }

    void SendGameName()
    {
        var msg = new OSCMessage("/game");
        msg.AddValue(OSCValue.String("Andrea"));
        transmitter.Send(msg);
    }

    void SendHeartbeat()
    {
        var msg = new OSCMessage("/heartbeat");
        transmitter.Send(msg);
    }
}