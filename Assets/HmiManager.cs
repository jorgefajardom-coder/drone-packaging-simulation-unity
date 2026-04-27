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
    public TMP_Text txtCajaContador;   // "X / 8"
    public TMP_Text txtDroneActual;    // "Dron: X / 8"

    // ── PLC bytes (imágenes de bits opcionales) ───────────────────────────────
    [Header("PLC Bits — Entradas (índice 0=B7 … 7=B0, opcional)")]
    public Image[] imgEntBits = new Image[8];
    [Header("PLC Bits — Salidas plc1 (índice 0=B7 … 7=B0, opcional)")]
    public Image[] imgSalBits = new Image[8];

    // ── Botones Unity → CoDeSys ───────────────────────────────────────────────
    [Header("Botones Ventosas")]
    public Button btnOmegaOn;
    public Button btnOmegaOff;
    public Button btnPaletOn;
    public Button btnPaletOff;

    [Header("Botones LEDs")]
    public Button btnAllLedsOn;
    public Button btnAllLedsOff;
    public Button[] btnLeds = new Button[8];   // toggle individual por caja

    // ── Métricas ──────────────────────────────────────────────────────────────
    [Header("Métricas de ciclo")]
    public TMP_Text txtTiempoCiclo;     // tiempo ensamblaje dron actual
    public TMP_Text txtCarro;           // "Carro: A / B"

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

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        // Auto-buscar si no están asignados
        if (tcp == null) tcp = FindObjectOfType<CodesysTcpClient>();
        if (produccion == null) produccion = FindObjectOfType<Produccion>();

        if (tcp != null)
            tcp.OnLogMessage += AddLog;

        WireButtons();
    }

    void WireButtons()
    {
        // Ventosas
        btnOmegaOn?.onClick.AddListener(() => { tcp?.SetVentosaOmega(true); AddLog("Ventosa OMEGA → ON"); });
        btnOmegaOff?.onClick.AddListener(() => { tcp?.SetVentosaOmega(false); AddLog("Ventosa OMEGA → OFF"); });
        btnPaletOn?.onClick.AddListener(() => { tcp?.SetVentosaPaletizador(true); AddLog("Ventosa PALET → ON"); });
        btnPaletOff?.onClick.AddListener(() => { tcp?.SetVentosaPaletizador(false); AddLog("Ventosa PALET → OFF"); });

        // LEDs globales
        btnAllLedsOn?.onClick.AddListener(() => { tcp?.SetAllLeds(true); AddLog("Todos LEDs → ON"); });
        btnAllLedsOff?.onClick.AddListener(() => { tcp?.SetAllLeds(false); AddLog("Todos LEDs → OFF"); });

        // LEDs individuales (toggle)
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
        RefreshBrazos();
        RefreshCajas();
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

        SetColor(imgSistema, tcp.SISTEMA_ON);
        SetColor(imgNeumatica, tcp.NEUMATICA_ON);

        if (txtSistema != null)
            txtSistema.text = tcp.SISTEMA_ON ? "SISTEMA ON" : "SISTEMA OFF";
    }

    // ── Estado de brazos (desde Produccion) ───────────────────────────────────
    void RefreshBrazos()
    {
        if (produccion == null) return;

        // OMEGA
        bool omegaOn = produccion.OmegaActivo;
        SetColor(imgOmegaEstado, omegaOn ? colorOn : colorOff);
        SetText(txtOmegaEstado, omegaOn ? "ACTIVO" : "IDLE", omegaOn ? colorOn : colorOff);
        SetText(txtOmegaAccion, produccion.OmegaAccion);
        SetText(txtOmegaPneu, tcp.NEUMATICA_ON ? "ON" : "OFF",
                                tcp.NEUMATICA_ON ? colorOn : colorOff);

        // PALETIZADOR
        bool paletOn = produccion.PaletActivo;
        SetColor(imgPaletEstado, paletOn ? colorOn : colorOff);
        SetText(txtPaletEstado, paletOn ? "ACTIVO" : "ESPERA", paletOn ? colorOn : colorOff);
        SetText(txtPaletAccion, produccion.PaletAccion);
        SetText(txtPaletPneu, tcp.NEUMATICA_ON ? "ON" : "OFF",
                                tcp.NEUMATICA_ON ? colorOn : colorOff);
        SetText(txtPaletCarro, "CARRO: " + produccion.CarroActualTag);
    }

    // ── Cajas paletizadas (LEDs) ───────────────────────────────────────────────
    void RefreshCajas()
    {
        // Los LEDs vienen de salidas_plc del PLC (fuente de verdad)
        bool[] leds = {
            tcp.LED1, tcp.LED2, tcp.LED3, tcp.LED4,
            tcp.LED5, tcp.LED6, tcp.LED7, tcp.LED8
        };

        int cajasDone = 0;
        for (int i = 0; i < imgLeds.Length; i++)
        {
            SetColor(imgLeds[i], leds[i] ? colorOn : colorOff);
            if (leds[i]) cajasDone++;
        }

        // Fallback: si TCP no está conectado, usar contador de Produccion
        if (!tcp.isConnected && produccion != null)
            cajasDone = produccion.droneActual;

        SetText(txtCajaContador, $"{cajasDone} / 8");
        SetText(txtDroneActual, produccion != null ? $"Dron: {produccion.droneActual} / {produccion.dronesAProducir}" : "---");
    }

    // ── Bits PLC (opcional, si el Canvas los tiene) ───────────────────────────
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

    // ── Métricas de ciclo ─────────────────────────────────────────────────────
    void RefreshMetricas()
    {
        if (produccion == null) return;

        float t = produccion.TiempoCicloActual;
        int min = Mathf.FloorToInt(t / 60f);
        int seg = Mathf.FloorToInt(t % 60f);
        SetText(txtTiempoCiclo, $"{min:00}:{seg:00}");
        SetText(txtCarro, "Carro: " + produccion.CarroActualTag);
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
    }
}