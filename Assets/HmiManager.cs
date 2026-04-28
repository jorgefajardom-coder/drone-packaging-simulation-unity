using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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
    public TMP_Text txtConexion2;   // panel TCP secundario
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
    public TMP_Text txtTiempoCiclo;
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
    private const float DEBOUNCE = 0.1f; // 100ms para estabilizar

    // LEDs por tapa cerrada
    private CerradorTapa[] _cerradoresTapa = new CerradorTapa[8];
    private bool[] _ledActivado = new bool[8];

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (tcp == null) tcp = FindObjectOfType<CodesysTcpClient>();
        if (produccion == null) produccion = FindObjectOfType<Produccion>();

        if (tcp != null)
            tcp.OnLogMessage += AddLog;

        WireButtons();
        BuscarCerradoresTapa();

        RetiradorCarro.OnCarroRetirado -= OnCarroRetirado;
    }

    // ── Búsqueda inicial de cerradores ────────────────────────────────────────
    void BuscarCerradoresTapa()
    {
        for (int i = 0; i < 8; i++)
        {
            string nombreCaja = $"CajaPrefab(Clone{i + 1})";
            GameObject caja = GameObject.Find(nombreCaja);
            if (caja != null)
                _cerradoresTapa[i] = caja.GetComponent<CerradorTapa>();
        }
    }

    void OnCarroRetirado(int[] cajasDelCarro)
    {
        // Solo resetear cuando se retira el carro 2 (cajas 5,6,7,8)
        bool esCarro2 = System.Array.IndexOf(cajasDelCarro, 8) >= 0;
        if (!esCarro2) return;

        // Verificar que los 8 LEDs estén activos antes de resetear
        bool todosOn = true;
        for (int i = 0; i < 8; i++)
            if (!_ledActivado[i]) { todosOn = false; break; }

        if (!todosOn) return;

        // Reset CODESYS
        tcp?.SetAllLeds(false);
        AddLog("LEDs CODESYS → RESET (lote 8 completo)");

        // Reset HMI
        for (int i = 0; i < 8; i++)
        {
            _ledActivado[i] = false;
            _cerradoresTapa[i] = null;
            SetColor(imgLeds[i], colorOff);
        }

        // Reset contador 0/8
        SetText(txtCajaContador, "0 / 8");
    }

    // ── Botones ───────────────────────────────────────────────────────────────
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

    // ── Update ────────────────────────────────────────────────────────────────
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

    // ── Ventosas: detección de cambio + envío a CODESYS ──────────────────────
    void RefreshVentosas()
    {
        if (produccion == null || tcp == null) return;

        bool omegaActivo = produccion.OmegaActivo;
        bool paletActivo = produccion.PaletActivo;

        // Debounce Omega
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
        else
        {
            _omegaTimer = 0f;
        }

        // Debounce Paletizador
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
        else
        {
            _paletTimer = 0f;
        }

        // Indicador general de neumática
        SetColor(imgNeumatica, omegaActivo || paletActivo);
    }

    // ── Estado de brazos ─────────────────────────────────────────────────────
    void RefreshBrazos()
    {
        if (produccion == null) return;

        // OMEGA — neumática independiente
        bool omegaOn = produccion.OmegaActivo;
        SetColor(imgOmegaEstado, omegaOn ? colorOn : colorOff);
        SetText(txtOmegaEstado, omegaOn ? "ACTIVO" : "IDLE", omegaOn ? colorOn : colorOff);
        SetText(txtOmegaAccion, produccion.OmegaAccion);
        SetText(txtOmegaPneu, omegaOn ? "ON" : "OFF", omegaOn ? colorOn : colorOff);

        // PALETIZADOR — neumática independiente
        bool paletOn = produccion.PaletActivo;
        SetColor(imgPaletEstado, paletOn ? colorOn : colorOff);
        SetText(txtPaletEstado, paletOn ? "ACTIVO" : "ESPERA", paletOn ? colorOn : colorOff);
        SetText(txtPaletAccion, produccion.PaletAccion);
        SetText(txtPaletPneu, paletOn ? "ON" : "OFF", paletOn ? colorOn : colorOff);
        SetText(txtPaletCarro, "CARRO: " + produccion.CarroActualTag);
    }

    // ── LEDs por tapa cerrada (fuente de verdad: Unity) ───────────────────────
    void RefreshLedsCajas()
    {
        // Reset de flags cuando la caja fue destruida (nuevo ciclo)
        for (int i = 0; i < 8; i++)
        {
            if (_cerradoresTapa[i] == null && _ledActivado[i])
                _ledActivado[i] = false;
        }

        // Re-buscar cerradores de cajas spawneadas tardíamente
        for (int i = 0; i < 8; i++)
        {
            if (_cerradoresTapa[i] == null)
            {
                GameObject caja = GameObject.Find($"CajaPrefab(Clone{i + 1})");
                if (caja != null)
                    _cerradoresTapa[i] = caja.GetComponent<CerradorTapa>();
            }
        }

        int cajasDone = 0;
        for (int i = 0; i < 8; i++)
        {
            bool cerrada = _cerradoresTapa[i] != null && _cerradoresTapa[i].tapaCerrada;

            // Flanco: recién cerrada → encender LED y enviar a CODESYS
            if (cerrada && !_ledActivado[i])
            {
                _ledActivado[i] = true;
                tcp?.SetLed(i + 1, true);
                AddLog($"LED {i + 1} ON → Caja {i + 1} cerrada");
            }

            SetColor(imgLeds[i], cerrada ? colorOn : colorOff);
            if (cerrada) cajasDone++;
        }

        SetText(txtCajaContador, $"{cajasDone} / 8");
        SetText(txtDroneActual, produccion != null
            ? $"Dron: {produccion.droneActual} / {produccion.dronesAProducir}"
            : "---");
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