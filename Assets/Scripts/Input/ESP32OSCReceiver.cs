using UnityEngine;
using extOSC;

public class ESP32OSCReceiver : MonoBehaviour
{
    public int receivePort = 9999;

    public static ESP32OSCReceiver Instance { get; private set; }

    public int fsr;
    public int water;
    public int fire;
    public float joyX;
    public float joyY;
    public int joyButton;
    public float lastMessageTime;
    public int moveLeft;
    public int moveRight;
    public int moveForward;
    public int moveBack;

    private OSCReceiver receiver;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        receiver = gameObject.AddComponent<OSCReceiver>();
        receiver.LocalPort = receivePort;

        receiver.Bind("/fsr", msg =>
        {
            msg.ToInt(out fsr);
            lastMessageTime = Time.time;
        });

        receiver.Bind("/water", msg =>
        {
            msg.ToInt(out water);
            lastMessageTime = Time.time;
        });

        receiver.Bind("/fire", msg =>
        {
            msg.ToInt(out fire);
            lastMessageTime = Time.time;
        });

        receiver.Bind("/joystick", msg =>
        {
            joyX = msg.Values[0].FloatValue;
            joyY = msg.Values[1].FloatValue;
            joyButton = msg.Values[2].IntValue;
            lastMessageTime = Time.time;
        });

        receiver.Bind("/move", msg =>
        {
            moveLeft = msg.Values[0].IntValue;
            moveRight = msg.Values[1].IntValue;
            moveForward = msg.Values[2].IntValue;
            moveBack = msg.Values[3].IntValue;
            lastMessageTime = Time.time;
        });
    }

    public bool IsConnected(float timeout = 1.5f)
    {
        return Time.time - lastMessageTime < timeout;
    }

    void OnDestroy()
    {
        receiver?.Close();
    }
}

