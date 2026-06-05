using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class CodesysTcpClient : MonoBehaviour
{
    [Header("Conexión")]
    public string codesysIP = "127.0.0.1";
    public int codesysPort = 8888;
    public float reconnectInterval = 3f;

    [Header("Estado")]
    public bool isConnected = false;

    [HideInInspector] public byte TCP_COMANDOS_VENTOSAS = 0x00;
    [HideInInspector] public byte TCP_COMANDOS_LEDS = 0x00;

    [HideInInspector] public byte salidas_plc1 = 0x00;
    [HideInInspector] public byte salidas_plc2 = 0x00;
    [HideInInspector] public byte entradas_plc1 = 0x00;

    [HideInInspector] public bool VENTOSA_OMEGA_ON = false;
    [HideInInspector] public bool VENTOSA_OMEGA_OFF = false;
    [HideInInspector] public bool VENTOSA_PALET_ON = false;
    [HideInInspector] public bool VENTOSA_PALET_OFF = false;

    [HideInInspector] public bool LED1 = false;
    [HideInInspector] public bool LED2 = false;
    [HideInInspector] public bool LED3 = false;
    [HideInInspector] public bool LED4 = false;
    [HideInInspector] public bool LED5 = false;
    [HideInInspector] public bool LED6 = false;
    [HideInInspector] public bool LED7 = false;
    [HideInInspector] public bool LED8 = false;

    [HideInInspector] public bool NEUMATICA_ON = false;
    [HideInInspector] public bool NEUMATICA_OFF = false;
    [HideInInspector] public bool SISTEMA_ON = false;
    [HideInInspector] public bool STOP_EMERGENCIA_ACTIVO = false;

    public event Action<string> OnLogMessage;
    public event Action OnSistemaStart;
    public event Action OnSistemaStop;

    private TcpClient _client;
    private NetworkStream _stream;
    private Thread _receiveThread;
    private Thread _sendThread;

    private volatile bool _running = false;
    private float _reconnectTimer = 0f;
    private int _connectionGeneration = 0;

    private const byte HEADER_TX = 0xAA;
    private const byte HEADER_RX = 0xBB;
    private const int RX_PACKET_SIZE = 5;

    private readonly object _lockBytes = new object();
    private readonly object _lockState = new object();

    private bool _lastSistemaOn = false;

    void Start()
    {
        ConnectToCodesys();
    }

    void Update()
    {
        if (!isConnected)
        {
            _reconnectTimer += Time.deltaTime;

            if (_reconnectTimer >= reconnectInterval)
            {
                _reconnectTimer = 0f;
                ConnectToCodesys();
            }
        }

        bool sistemaActual;

        lock (_lockState)
        {
            sistemaActual = SISTEMA_ON;
        }

        if (sistemaActual && !_lastSistemaOn)
        {
            OnSistemaStart?.Invoke();
            Log("[PLC] START detectado desde CODESYS");
        }

        if (!sistemaActual && _lastSistemaOn)
        {
            OnSistemaStop?.Invoke();
            Log("[PLC] STOP / EMERGENCIA detectado desde CODESYS");
        }

        _lastSistemaOn = sistemaActual;
    }

    void ConnectToCodesys()
    {
        try
        {
            CleanupConnection();

            int myGeneration = ++_connectionGeneration;

            _client = new TcpClient();

            IAsyncResult result = _client.BeginConnect(codesysIP, codesysPort, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));

            if (!success || !_client.Connected)
            {
                try { _client.Close(); } catch { }

                isConnected = false;
                Log("[TCP] Timeout de conexión");
                return;
            }

            _client.EndConnect(result);
            _client.NoDelay = true;

            _stream = _client.GetStream();

            _running = true;
            isConnected = true;

            _receiveThread = new Thread(() => ReceiveLoop(myGeneration));
            _receiveThread.IsBackground = true;
            _receiveThread.Start();

            _sendThread = new Thread(() => SendLoop(myGeneration));
            _sendThread.IsBackground = true;
            _sendThread.Start();

            Log($"[TCP] Conectado a CODESYS {codesysIP}:{codesysPort}");
        }
        catch (Exception e)
        {
            isConnected = false;
            Log($"[TCP] Error de conexión: {e.Message}");
        }
    }

    void SendLoop(int generation)
    {
        while (_running && _stream != null && generation == _connectionGeneration)
        {
            try
            {
                byte v;
                byte l;

                lock (_lockBytes)
                {
                    v = TCP_COMANDOS_VENTOSAS;
                    l = TCP_COMANDOS_LEDS;
                }

                byte[] packet = new byte[] { HEADER_TX, v, l };

                _stream.Write(packet, 0, packet.Length);
                _stream.Flush();
            }
            catch (Exception e)
            {
                Log($"[TCP] Error de envío: {e.Message}");
                HandleDisconnect(generation);
                break;
            }

            Thread.Sleep(50);
        }
    }

    void ReceiveLoop(int generation)
    {
        byte[] payload = new byte[RX_PACKET_SIZE - 1];

        while (_running && _stream != null && generation == _connectionGeneration)
        {
            try
            {
                int header = _stream.ReadByte();

                if (header < 0)
                {
                    HandleDisconnect(generation);
                    break;
                }

                if ((byte)header != HEADER_RX)
                {
                    continue;
                }

                int bytesRead = 0;

                while (bytesRead < payload.Length)
                {
                    int r = _stream.Read(payload, bytesRead, payload.Length - bytesRead);

                    if (r <= 0)
                    {
                        HandleDisconnect(generation);
                        return;
                    }

                    bytesRead += r;
                }

                ParseReceivedPacket(payload);
            }
            catch (Exception e)
            {
                if (_running)
                {
                    Log($"[TCP] Error de recepción: {e.Message}");
                }

                HandleDisconnect(generation);
                break;
            }
        }
    }

    void ParseReceivedPacket(byte[] data)
    {
        lock (_lockState)
        {
            salidas_plc1 = data[0];
            salidas_plc2 = data[1];
            entradas_plc1 = data[2];

            VENTOSA_OMEGA_ON = (salidas_plc1 & 0x01) != 0;
            VENTOSA_OMEGA_OFF = (salidas_plc1 & 0x02) != 0;

            VENTOSA_PALET_ON = (salidas_plc1 & 0x04) != 0;
            VENTOSA_PALET_OFF = (salidas_plc1 & 0x08) != 0;

            LED7 = (salidas_plc1 & 0x10) != 0;
            LED8 = (salidas_plc1 & 0x20) != 0;
            LED5 = (salidas_plc1 & 0x40) != 0;
            LED6 = (salidas_plc1 & 0x80) != 0;

            LED2 = (salidas_plc2 & 0x01) != 0;
            LED1 = (salidas_plc2 & 0x02) != 0;
            LED4 = (salidas_plc2 & 0x04) != 0;
            LED3 = (salidas_plc2 & 0x08) != 0;

            NEUMATICA_OFF = (salidas_plc2 & 0x10) != 0;
            NEUMATICA_ON = (salidas_plc2 & 0x20) != 0;

            STOP_EMERGENCIA_ACTIVO = (salidas_plc2 & 0x40) != 0;

            SISTEMA_ON = data[3] != 0;
        }
    }

    public void SetVentosaOmega(bool on)
    {
        lock (_lockBytes)
        {
            if (on)
                TCP_COMANDOS_VENTOSAS |= 0x01;
            else
                TCP_COMANDOS_VENTOSAS &= 0xFE;
        }
    }

    public void SetVentosaPaletizador(bool on)
    {
        lock (_lockBytes)
        {
            if (on)
                TCP_COMANDOS_VENTOSAS |= 0x02;
            else
                TCP_COMANDOS_VENTOSAS &= 0xFD;
        }
    }

    public void SetLed(int ledIndex, bool on)
    {
        if (ledIndex < 1 || ledIndex > 8)
            return;

        byte mask = (byte)(1 << (ledIndex - 1));

        lock (_lockBytes)
        {
            if (on)
                TCP_COMANDOS_LEDS |= mask;
            else
                TCP_COMANDOS_LEDS &= (byte)~mask;
        }
    }

    public void SetAllLeds(bool on)
    {
        lock (_lockBytes)
        {
            TCP_COMANDOS_LEDS = on ? (byte)0xFF : (byte)0x00;
        }
    }

    public void ResetComandosUnity()
    {
        lock (_lockBytes)
        {
            TCP_COMANDOS_VENTOSAS = 0x00;
            TCP_COMANDOS_LEDS = 0x00;
        }
    }

    void HandleDisconnect(int generation)
    {
        if (generation != _connectionGeneration)
            return;

        if (!isConnected)
            return;

        isConnected = false;
        _running = false;

        Log("[TCP] Desconectado de CODESYS");
    }

    void CleanupConnection()
    {
        _running = false;
        isConnected = false;

        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }

        _stream = null;
        _client = null;

        if (_receiveThread != null && _receiveThread.IsAlive)
        {
            try { _receiveThread.Join(300); } catch { }
        }

        if (_sendThread != null && _sendThread.IsAlive)
        {
            try { _sendThread.Join(300); } catch { }
        }

        _receiveThread = null;
        _sendThread = null;
    }

    void Log(string msg)
    {
        Debug.Log(msg);
        OnLogMessage?.Invoke(msg);
    }

    void OnApplicationQuit()
    {
        CleanupConnection();
    }

    void OnDestroy()
    {
        CleanupConnection();
    }
}