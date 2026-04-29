using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class CodesysTcpClient : MonoBehaviour
{
    [Header("Conexión")]
    public string ip = "127.0.0.1";
    public int port = 8888;
    public float reconnectTime = 3f;

    [Header("Estado")]
    public bool isConnected = false;

    // ENVÍO
    public byte TCP_COMANDOS_VENTOSAS = 0x00;
    public byte TCP_COMANDOS_LEDS = 0x00;

    // RECEPCIÓN
    public byte salidas_plc1;
    public byte salidas_plc2;
    public byte entradas_plc1;

    public bool SISTEMA_ON;

    // EVENTO PARA HMI
    public event Action<string> OnLogMessage;

    private TcpClient client;
    private NetworkStream stream;
    private Thread recvThread;
    private Thread sendThread;
    private bool running = false;

    private float reconnectTimer = 0f;

    private readonly object _lock = new object();

    // ===================== UNITY =====================

    void Start()
    {
        Connect();
    }

    void Update()
    {
        if (!isConnected)
        {
            reconnectTimer += Time.deltaTime;

            if (reconnectTimer >= reconnectTime)
            {
                reconnectTimer = 0f;
                Connect();
            }
        }
    }

    // ===================== LOG =====================

    void Log(string msg)
    {
        Debug.Log(msg);
        OnLogMessage?.Invoke(msg);
    }

    // ===================== CONEXIÓN =====================

    void Connect()
    {
        try
        {
            Cleanup();

            client = new TcpClient();
            client.Connect(ip, port);

            stream = client.GetStream();

            running = true;
            isConnected = true;

            recvThread = new Thread(ReceiveLoop);
            recvThread.IsBackground = true;
            recvThread.Start();

            sendThread = new Thread(SendLoop);
            sendThread.IsBackground = true;
            sendThread.Start();

            Log("[TCP] Conectado");
        }
        catch (Exception e)
        {
            isConnected = false;
            Log("[TCP] Error conexión: " + e.Message);
        }
    }

    void Cleanup()
    {
        running = false;
        isConnected = false;

        try { stream?.Close(); } catch { }
        try { client?.Close(); } catch { }

        try { recvThread?.Join(500); } catch { }
        try { sendThread?.Join(500); } catch { }

        stream = null;
        client = null;
    }

    void Disconnect()
    {
        Cleanup();
        Log("[TCP] Desconectado");
    }

    // ===================== ENVÍO =====================

    void SendLoop()
    {
        while (running)
        {
            try
            {
                byte v, l;

                lock (_lock)
                {
                    v = TCP_COMANDOS_VENTOSAS;
                    l = TCP_COMANDOS_LEDS;
                }

                byte[] pkt = new byte[] { 0xAA, v, l };
                stream.Write(pkt, 0, pkt.Length);
            }
            catch (Exception e)
            {
                Log("[TCP] Error envío: " + e.Message);
                Disconnect();
                break;
            }

            Thread.Sleep(50);
        }
    }

    // ===================== RECEPCIÓN =====================

    void ReceiveLoop()
    {
        byte[] buffer = new byte[4];

        while (running)
        {
            try
            {
                int header = stream.ReadByte();

                if (header < 0)
                {
                    Disconnect();
                    break;
                }

                if (header == 0xBB)
                {
                    int read = 0;

                    while (read < 4)
                    {
                        int r = stream.Read(buffer, read, 4 - read);

                        if (r <= 0)
                        {
                            Disconnect();
                            return;
                        }

                        read += r;
                    }

                    lock (_lock)
                    {
                        salidas_plc1 = buffer[0];
                        salidas_plc2 = buffer[1];
                        entradas_plc1 = buffer[2];
                        SISTEMA_ON = buffer[3] != 0;
                    }
                }
            }
            catch (Exception e)
            {
                if (running)
                    Log("[TCP] Error recepción: " + e.Message);

                Disconnect();
                break;
            }
        }
    }

    // ===================== API =====================

    public void SetVentosaOmega(bool on)
    {
        lock (_lock)
        {
            if (on) TCP_COMANDOS_VENTOSAS |= 0x01;
            else TCP_COMANDOS_VENTOSAS &= 0xFE;
        }
    }

    public void SetVentosaPaletizador(bool on)
    {
        lock (_lock)
        {
            if (on) TCP_COMANDOS_VENTOSAS |= 0x02;
            else TCP_COMANDOS_VENTOSAS &= 0xFD;
        }
    }

    public void SetLed(int index, bool on)
    {
        if (index < 1 || index > 8) return;

        byte mask = (byte)(1 << (index - 1));

        lock (_lock)
        {
            if (on) TCP_COMANDOS_LEDS |= mask;
            else TCP_COMANDOS_LEDS &= (byte)~mask;
        }
    }

    public void SetAllLeds(bool on)
    {
        lock (_lock)
        {
            TCP_COMANDOS_LEDS = on ? (byte)0xFF : (byte)0x00;
        }
    }

    // ===================== UNITY =====================

    void OnApplicationQuit()
    {
        Disconnect();
    }

    void OnDestroy()
    {
        Disconnect();
    }
}