using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Requiere: CodesysTcpClient.cs + Produccion.cs accesibles en escena
// Unity 2021.3.45f LTS | TextMeshPro

public class HmiManager : MonoBehaviour
{
    // ── Referencias core ────────────────────────────────────────────────────
    [Header("Referencias")]
    public CodesysTcpClient tcp;
    public Produccion produccion;

    // ── Estado general ───────────────────────────────────────────────────────
    [Header("Estado General")]
    public Image imgSistema;
    public Image imgNeumatica;
    public Image imgTcpEstado;
    public TMP_Text txtConexion;
    public TMP_Text txtConexion2;
    public TMP_Text txtSistema;

    // ── Brazos ───────────────────────────────────────────────────────────────
    [Header("Brazo Omega")]
    public Image imgOmegaEstado;
    public TMP_Text txtOmegaEstado;
    public TMP_Text txtOmegaAccion;
    public TMP_Text txtOmegaPneu;

    [Header("Brazo Paletizador")]
    public Image imgPaletEstado;
    public TMP_Text txtPaletEstado;
    public TMP_Text txtPaletAccion;
    public TMP_Text txtPaletPneu;
    public TMP_Text txtPaletCarro;

    // ── Cajas / LEDs ─────────────────────────────────────────────────────────
    [Header("LEDs de Cajas (índice 0 = LED1 = Caja 1)")]
    public Image[] imgLeds = new Image[8];
    public TMP_Text txtCajaContador;
    public TMP_Text txtDroneActual;

    // ── PLC bytes ─────────────────────────────────────────────────────────────
    [Header("PLC Bits — Entradas (índice 0=B7 … 7=B0, opcional)")]
    public Image[] imgEntBits = new Image[8];
    [Header("PLC Bits — Salidas plc1 (índice 0=B7 … 7=B0, opcional)")]
    public Image[] imgSalBits = new Image[8];

    // ── Botones ───────────────────────────────────────────────────────────────
    [Header("Botones Ventosas")]
    public Button btnOmegaOn;
    public Button btnOmegaOff;
    public Button btnPaletOn;
    public Button btnPaletOff;

    [Header("Botones LEDs")]
    public Button btnAllLedsOn;
    public Button btnAllLedsOff;
    public Button[] btnLeds = new Button[8];

    // ── Métricas ──────────────────────────────────────────────────────────────
    [Header("Métricas de ciclo")]
    public TMP_Text txtTiempoCiclo;
    public TMP_Text txtTiempoTotal;
    public TMP_Text txtCarro;

    // ── Log ───────────────────────────────────────────────────────────────────
    [Header("Log TCP")]
    public TMP_Text txtLog;

    // ── Colores ───────────────────────────────────────────────────────────────
    [Header("Colores")]
    public Color colorOn = new Color(0.00f, 0.90f, 0.46f);
    public Color colorOff = new Color(0.07f, 0.11f, 0.15f);
    public Color colorWarn = new Color(1.00f, 0.60f, 0.00f);
    public Color colorAct = new Color(0.00f, 0.90f, 1.00f);

    // ── Interno ───────────────────────────────────────────────────────────────
    private readonly Queue<string> _log = new Queue<string>();
    private const int MAX_LOG = 10;

    // Debounce ventosas
    private bool _prevOmegaActivo = false;
    private bool _prevPaletActivo = false;
    private float _omegaTimer = 0f;
    private float _paletTimer = 0f;
    private const float DEBOUNCE = 0.1f;

    // LEDs — se controlan por droneActual % 8
    private bool[] _ledActivado = new bool[8];
    private int _prevDroneActual = -1;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (tcp == null) tcp = FindObjectOfType<CodesysTcpClient>();
        if (produccion == null) produccion = FindObjectOfType<Produccion>();

        if (tcp != null)
            tcp.OnLogMessage += AddLog;

        if (produccion != null && HasOnLogTiempo())
            SubscribeOnLogTiempo();

        WireButtons();
    }

    // Verifica si Produccion tiene el evento OnLogTiempo antes de suscribirse
    bool HasOnLogTiempo()
    {
        var field = typeof(Produccion).GetEvent("OnLogTiempo");
        return field != null;
    }

    void SubscribeOnLogTiempo()
    {
        try { produccion.OnLogTiempo += AddLog; } catch { }
    }

    void WireButtons()
    {
        btnOmegaOn?.onClick.AddListener(() => { tcp?.SetVentosaOmega(true); AddLog("Ventosa OMEGA → ON"); });
        btnOmegaOff?.onClick.AddListener(() => { tcp?.SetVentosaOmega(false); AddLog("Ventosa OMEGA → OFF"); });
        btnPaletOn?.onClick.AddListener(() => { tcp?.SetVentosaPaletizador(true); AddLog("Ventosa PALET → ON"); });
        btnPaletOff?.onClick.AddListener(() => { tcp?.SetVentosaPaletizador(false); AddLog("Ventosa PALET → OFF"); });

        btnAllLedsOn?.onClick.AddListener(() => { tcp?.SetAllLeds(true); AddLog("Todos LEDs → ON"); });
        btnAllLedsOff?.onClick.AddListener(() => { tcp?.SetAllLeds(false); AddLog("Todos LEDs → OFF"); });

        for (int i = 0; i < btnLeds.Length; i++)
        {
            int idx = i + 1;
            btnLeds[i]?.onClick.AddListener(() =>
            {
                if (tcp == null) return;
                bool estado = (tcp.TCP_COMANDOS_LEDS & (1 << (idx - 1))) != 0;
                tcp.SetLed(idx, !estado);
                AddLog($"LED {idx} → {(!estado ? "ON" : "OFF")}");
            });
        }
    }

    void Update()
    {
        if (tcp == null) return;
        RefreshConexion();
        RefreshVentosas();
        RefreshBrazos();
        RefreshLedsCajas();
        RefreshPLC();
        RefreshMetricas();
    }

    // ── Conexión TCP ──────────────────────────────────────────────────────────
    void RefreshConexion()
    {
        bool con = tcp.isConnected;
        SetColor(imgTcpEstado, con ? colorOn : colorWarn);

        if (txtConexion != null)
        {
            txtConexion.text = con ? "● CODESYS CONECTADO" : "○ CODESYS DESCONECTADO";
            txtConexion.color = con ? colorOn : colorWarn;
        }

        if (txtConexion2 != null)
        {
            txtConexion2.text = con ? "● CONECTADO" : "○ DESCONECTADO";
            txtConexion2.color = con ? colorOn : colorWarn;
        }

        bool neumatica = produccion != null && (produccion.OmegaActivo || produccion.PaletActivo);
        SetColor(imgNeumatica, neumatica);
        SetColor(imgSistema, tcp.SISTEMA_ON);

        if (txtSistema != null)
            txtSistema.text = tcp.SISTEMA_ON ? "SISTEMA ON" : "SISTEMA OFF";
    }

    // ── Ventosas ──────────────────────────────────────────────────────────────
    void RefreshVentosas()
    {
        if (produccion == null || tcp == null) return;

        bool omegaActivo = produccion.OmegaActivo;
        bool paletActivo = produccion.PaletActivo;

        if (omegaActivo != _prevOmegaActivo)
        {
            _omegaTimer += Time.deltaTime;
            if (_omegaTimer >= DEBOUNCE)
            {
                tcp.SetVentosaOmega(omegaActivo);
                AddLog($"Ventosa OMEGA → {(omegaActivo ? "ON" : "OFF")}");
                _prevOmegaActivo = omegaActivo;
                _omegaTimer = 0f;
            }
        }
        else { _omegaTimer = 0f; }

        if (paletActivo != _prevPaletActivo)
        {
            _paletTimer += Time.deltaTime;
            if (_paletTimer >= DEBOUNCE)
            {
                tcp.SetVentosaPaletizador(paletActivo);
                AddLog($"Ventosa PALET → {(paletActivo ? "ON" : "OFF")}");
                _prevPaletActivo = paletActivo;
                _paletTimer = 0f;
            }
        }
        else { _paletTimer = 0f; }

        SetColor(imgNeumatica, omegaActivo || paletActivo);
    }

    // ── Brazos ────────────────────────────────────────────────────────────────
    void RefreshBrazos()
    {
        if (produccion == null) return;

        bool omegaOn = produccion.OmegaActivo;
        SetColor(imgOmegaEstado, omegaOn ? colorOn : colorOff);
        SetText(txtOmegaEstado, omegaOn ? "ACTIVO" : "IDLE", omegaOn ? colorOn : colorOff);
        SetText(txtOmegaAccion, produccion.OmegaAccion);
        SetText(txtOmegaPneu, omegaOn ? "ON" : "OFF", omegaOn ? colorOn : colorOff);

        bool paletOn = produccion.PaletActivo;
        SetColor(imgPaletEstado, paletOn ? colorOn : colorOff);
        SetText(txtPaletEstado, paletOn ? "ACTIVO" : "ESPERA", paletOn ? colorOn : colorOff);
        SetText(txtPaletAccion, produccion.PaletAccion);
        SetText(txtPaletPneu, paletOn ? "ON" : "OFF", paletOn ? colorOn : colorOff);
        SetText(txtPaletCarro, "CARRO: " + produccion.CarroActualTag);
    }

    // ── LEDs por ciclo de 8 drones ────────────────────────────────────────────
    void RefreshLedsCajas()
    {
        if (produccion == null) return;

        int droneActual = produccion.droneActual;

        // Sin cambio, no hacer nada
        if (droneActual == _prevDroneActual) return;
        _prevDroneActual = droneActual;

        // Posición dentro del ciclo de 8 (0..7)
        // droneActual=1 → ciclo=1 (LED1 ON), droneActual=8 → ciclo=0 (reset)
        int ciclo = droneActual % 8;

        // Detectar inicio de nuevo ciclo (ciclo==0 y droneActual>0)
        if (ciclo == 0 && droneActual > 0)
        {
            // Asegurar que el LED 8 esté encendido antes de resetear
            if (!_ledActivado[7])
            {
                _ledActivado[7] = true;
                tcp?.SetLed(8, true);
                SetColor(imgLeds[7], colorOn);
                AddLog("LED 8 ON");
            }

            // Pequeña pausa visual — el reset ocurre en el próximo frame
            // para que se vea el LED 8 encendido brevemente
            _pendingReset = true;
            _resetFrame = Time.frameCount + 2;
            return;
        }

        // Encender LEDs hasta el índice actual
        for (int i = 0; i < ciclo; i++)
        {
            if (!_ledActivado[i])
            {
                _ledActivado[i] = true;
                tcp?.SetLed(i + 1, true);
                SetColor(imgLeds[i], colorOn);
                AddLog($"LED {i + 1} ON → Dron {droneActual}");
            }
        }

        SetText(txtCajaContador, $"{ciclo}");
        SetText(txtDroneActual, $"Dron: {droneActual} / {produccion.dronesAProducir}");
    }

    // ── Reset pendiente (se ejecuta 2 frames después para ver LED 8) ──────────
    private bool _pendingReset = false;
    private int _resetFrame = 0;

    void LateUpdate()
    {
        if (!_pendingReset) return;
        if (Time.frameCount < _resetFrame) return;

        _pendingReset = false;

        tcp?.SetAllLeds(false);
        AddLog("LEDs CODESYS → RESET (ciclo 8 completo)");

        for (int i = 0; i < 8; i++)
        {
            _ledActivado[i] = false;
            SetColor(imgLeds[i], colorOff);
        }

        SetText(txtCajaContador, "0");
        SetText(txtDroneActual, $"Dron: {produccion.droneActual} / {produccion.dronesAProducir}");
    }

    // ── Bits PLC ──────────────────────────────────────────────────────────────
    void RefreshPLC()
    {
        PaintBits(imgEntBits, tcp.entradas_plc1);
        PaintBits(imgSalBits, tcp.salidas_plc1);
    }

    void PaintBits(Image[] imgs, byte val)
    {
        for (int i = 0; i < imgs.Length; i++)
        {
            if (imgs[i] == null) continue;
            bool on = ((val >> (7 - i)) & 1) == 1;
            imgs[i].color = on ? colorAct : colorOff;
        }
    }

    // ── Métricas ──────────────────────────────────────────────────────────────
    void RefreshMetricas()
    {
        if (produccion == null) return;

        float t = produccion.TiempoCicloActual;
        int min = Mathf.FloorToInt(t / 60f);
        int seg = Mathf.FloorToInt(t % 60f);
        SetText(txtTiempoCiclo, $"Ciclo: {min:00}:{seg:00}");
        SetText(txtCarro, "Carro: " + produccion.CarroActualTag);

        try
        {
            float tot = produccion.tiempoTotalSimulacion;
            int mTot = Mathf.FloorToInt(tot / 60f);
            int sTot = Mathf.FloorToInt(tot % 60f);
            SetText(txtTiempoTotal, $"Total: {mTot:00}:{sTot:00}");
        }
        catch { }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    void SetColor(Image img, bool on) { if (img) img.color = on ? colorOn : colorOff; }
    void SetColor(Image img, Color c) { if (img) img.color = c; }
    void SetText(TMP_Text t, string s) { if (t) t.text = s; }
    void SetText(TMP_Text t, string s, Color c) { if (t) { t.text = s; t.color = c; } }

    void AddLog(string msg)
    {
        string line = $"[{System.DateTime.Now:HH:mm:ss}] {msg}";
        _log.Enqueue(line);
        if (_log.Count > MAX_LOG) _log.Dequeue();
        if (txtLog != null) txtLog.text = string.Join("\n", _log);
    }

    void OnDestroy()
    {
        if (tcp != null) tcp.OnLogMessage -= AddLog;
        try { if (produccion != null) produccion.OnLogTiempo -= AddLog; } catch { }
    }
}