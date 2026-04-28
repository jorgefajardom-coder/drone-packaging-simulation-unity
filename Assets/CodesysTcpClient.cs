using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class CodesysTcpClient : MonoBehaviour
{
    [Header("Conexión")]
    public string codesysIP = "127.0.0.1";
    public int codesysPort = 8888;
    public float reconnectInterval = 3f;

    [Header("Estado (solo lectura)")]
    public bool isConnected = false;

    // Bytes que Unity ENVÍA a CODESYS
    [HideInInspector] public byte TCP_COMANDOS_VENTOSAS = 0x00;
    [HideInInspector] public byte TCP_COMANDOS_LEDS = 0x00;

    // Bytes que Unity RECIBE desde CODESYS
    [HideInInspector] public byte salidas_plc1 = 0x00;
    [HideInInspector] public byte salidas_plc2 = 0x00;
    [HideInInspector] public byte entradas_plc1 = 0x00;

    // Flags de estado derivados de salidas_plc
    [HideInInspector] public bool VENTOSA_OMEGA_ON = false;
    [HideInInspector] public bool VENTOSA_OMEGA_OFF = false;
    [HideInInspector] public bool VENTOSA_PALET_ON = false;
    [HideInInspector] public bool VENTOSA_PALET_OFF = false;
    [HideInInspector] public bool LED1, LED2, LED3, LED4;
    [HideInInspector] public bool LED5, LED6, LED7, LED8;
    [HideInInspector] public bool NEUMATICA_ON = false;
    [HideInInspector] public bool NEUMATICA_OFF = false;
    [HideInInspector] public bool SISTEMA_ON = false;

    // Evento para log de UI
    public event Action<string> OnLogMessage;

    private TcpClient _client;
    private NetworkStream _stream;
    private Thread _receiveThread;
    private Thread _sendThread;
    private bool _running = false;
    private float _reconnectTimer = 0f;

    private byte _lastVentosas = 0xFF;
    private byte _lastLeds = 0xFF;

    // Protocolo: paquete de 3 bytes enviado a CODESYS
    // [0] = 0xAA (header)
    // [1] = TCP_COMANDOS_VENTOSAS
    // [2] = TCP_COMANDOS_LEDS
    private const byte HEADER_TX = 0xAA;

    // Protocolo: paquete de 4 bytes recibido desde CODESYS
    // [0] = 0xBB (header)
    // [1] = salidas_plc1
    // [2] = salidas_plc2
    // [3] = entradas_plc1
    private const byte HEADER_RX = 0xBB;
    private const int RX_PACKET_SIZE = 4;

    // Agrega este campo privado:
    private readonly object _lockBytes = new object();

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
    }

    void ConnectToCodesys()
    {
        try
        {
            CleanupConnection();
            _lastVentosas = 0xFF;  // ← NUEVO: forzar reenvío al reconectar
            _lastLeds = 0xFF;

            _client = new TcpClient();
            _client.Connect(codesysIP, codesysPort);
            _stream = _client.GetStream();
            _running = true;
            isConnected = true;

            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            _receiveThread.Start();

            _sendThread = new Thread(SendLoop) { IsBackground = true };
            _sendThread.Start();

            Log($"[TCP] Conectado a CODESYS {codesysIP}:{codesysPort}");
        }
        catch (Exception e)
        {
            isConnected = false;
            Log($"[TCP] Error de conexión: {e.Message}");
        }
    }

    void SendLoop()
    {
        while (_running && _stream != null)
        {
            try
            {
                byte v, l;
                lock (_lockBytes)
                {
                    v = TCP_COMANDOS_VENTOSAS;
                    l = TCP_COMANDOS_LEDS;
                }

                if (v != _lastVentosas || l != _lastLeds)
                {
                    byte[] packet = new byte[] { HEADER_TX, v, l };
                    _stream.Write(packet, 0, packet.Length);
                    _lastVentosas = v;
                    _lastLeds = l;
                }
            }
            catch (Exception e)
            {
                Log($"[TCP] Error de envío: {e.Message}");
                HandleDisconnect();
                break;
            }
            Thread.Sleep(50);
        }
    }

    void ReceiveLoop()
    {
        byte[] buffer = new byte[RX_PACKET_SIZE];
        int bytesRead = 0;

        while (_running && _stream != null)
        {
            try
            {
                int b = _stream.ReadByte();
                if (b < 0) { HandleDisconnect(); break; }

                if ((byte)b == HEADER_RX)
                {
                    bytesRead = 0;
                    while (bytesRead < RX_PACKET_SIZE - 1)
                    {
                        int r = _stream.Read(buffer, bytesRead, RX_PACKET_SIZE - 1 - bytesRead);
                        if (r <= 0) { HandleDisconnect(); return; }
                        bytesRead += r;
                    }
                    ParseReceivedPacket(buffer);
                }
            }
            catch (Exception e)
            {
                if (_running) Log($"[TCP] Error de recepción: {e.Message}");
                HandleDisconnect();
                break;
            }
        }
    }

    void ParseReceivedPacket(byte[] data)
    {
        salidas_plc1 = data[0];
        salidas_plc2 = data[1];
        entradas_plc1 = data[2];

        // salidas_plc1 desglose
        VENTOSA_OMEGA_ON = (salidas_plc1 & 0x01) != 0;
        VENTOSA_OMEGA_OFF = (salidas_plc1 & 0x02) != 0;
        VENTOSA_PALET_ON = (salidas_plc1 & 0x04) != 0;
        VENTOSA_PALET_OFF = (salidas_plc1 & 0x08) != 0;
        LED7 = (salidas_plc1 & 0x10) != 0;
        LED8 = (salidas_plc1 & 0x20) != 0;
        LED5 = (salidas_plc1 & 0x40) != 0;
        LED6 = (salidas_plc1 & 0x80) != 0;

        // salidas_plc2 desglose
        LED2 = (salidas_plc2 & 0x01) != 0;
        LED1 = (salidas_plc2 & 0x02) != 0;
        LED4 = (salidas_plc2 & 0x04) != 0;
        LED3 = (salidas_plc2 & 0x08) != 0;
        NEUMATICA_OFF = (salidas_plc2 & 0x10) != 0;
        NEUMATICA_ON = (salidas_plc2 & 0x20) != 0;

        // entradas_plc1: SISTEMA_ON se refleja en bit START lógico
        SISTEMA_ON = (entradas_plc1 & 0x04) != 0; // bit2 = START enclavado
    }

    // ── API pública ─────────────────────────────────────────────────────────

    public void SetVentosaOmega(bool on)
    {
        lock (_lockBytes)
        {
            if (on) TCP_COMANDOS_VENTOSAS |= 0x01;
            else TCP_COMANDOS_VENTOSAS &= 0xFE;
        }
    }

    public void SetVentosaPaletizador(bool on)
    {
        lock (_lockBytes)
        {
            if (on) TCP_COMANDOS_VENTOSAS |= 0x02;
            else TCP_COMANDOS_VENTOSAS &= 0xFD;
        }
    }

    public void SetLed(int ledIndex, bool on)
    {
        if (ledIndex < 1 || ledIndex > 8) return;
        byte mask = (byte)(1 << (ledIndex - 1));
        if (on) TCP_COMANDOS_LEDS |= mask;
        else TCP_COMANDOS_LEDS &= (byte)~mask;
    }

    public void SetAllLeds(bool on)
    {
        TCP_COMANDOS_LEDS = on ? (byte)0xFF : (byte)0x00;
    }

    // ── Internos ─────────────────────────────────────────────────────────────

    void HandleDisconnect()
    {
        if (!isConnected) return;
        isConnected = false;
        _running = false;
        Log("[TCP] Desconectado de CODESYS");
    }

    void CleanupConnection()
    {
        _running = false;
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }
        _stream = null;
        _client = null;
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