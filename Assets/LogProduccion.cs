using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class LogProduccion : MonoBehaviour
{
    [Header("Control de exportación")]
    [Tooltip("Si está activo, esta corrida SÍ se va a guardar en CSV al terminar.")]
    public bool guardarEstaCorrida = false;

    [Tooltip("Carpeta destino (al lado de Assets/). Se crea si no existe.")]
    public string carpetaDestino = "LogsProduccion";

    [Tooltip("Prefijo del archivo CSV. Se le agrega timestamp automáticamente.")]
    public string prefijoArchivo = "produccion";

    [Header("Estado (solo lectura)")]
    [SerializeField] private int dronesRegistrados = 0;
    [SerializeField] private int cajasRegistradas = 0;

    private struct EventoLog
    {
        public string fecha;
        public string hora;
        public string tipo;       // "DRON", "CAJA", "BRAZO_ACTIVO", "BRAZO_INACTIVO"
        public string brazo;      // "Alpha", "Beta", "Omega", "Paletizador" o ""
        public int numero;        // dron o caja (0 si es brazo)
        public float tiempoSegundos;
    }

    private List<EventoLog> eventos = new List<EventoLog>();

    // Cronómetros activos por brazo (para calcular duración al desactivarse)
    private Dictionary<string, float> tiempoInicioBrazo = new Dictionary<string, float>();

    // Acumuladores totales por brazo
    private Dictionary<string, float> tiempoTotalBrazo = new Dictionary<string, float>();
    private Dictionary<string, int> activacionesBrazo = new Dictionary<string, int>();

    // ────────────────────────────────────────────────
    //   API pública
    // ────────────────────────────────────────────────

    public void RegistrarDron(int numeroDron, float tiempoEnsamblaje)
    {
        DateTime ahora = DateTime.Now;
        eventos.Add(new EventoLog
        {
            fecha = ahora.ToString("yyyy-MM-dd"),
            hora = ahora.ToString("HH:mm:ss"),
            tipo = "DRON",
            brazo = "",
            numero = numeroDron,
            tiempoSegundos = tiempoEnsamblaje
        });
        dronesRegistrados++;
    }

    public void RegistrarCaja(int numeroCaja)
    {
        DateTime ahora = DateTime.Now;
        eventos.Add(new EventoLog
        {
            fecha = ahora.ToString("yyyy-MM-dd"),
            hora = ahora.ToString("HH:mm:ss"),
            tipo = "CAJA",
            brazo = "",
            numero = numeroCaja,
            tiempoSegundos = 0f
        });
        cajasRegistradas++;
    }

    public void RegistrarBrazoActivo(string nombreBrazo)
    {
        DateTime ahora = DateTime.Now;
        tiempoInicioBrazo[nombreBrazo] = Time.time;

        eventos.Add(new EventoLog
        {
            fecha = ahora.ToString("yyyy-MM-dd"),
            hora = ahora.ToString("HH:mm:ss"),
            tipo = "BRAZO_ACTIVO",
            brazo = nombreBrazo,
            numero = 0,
            tiempoSegundos = 0f
        });
    }

    public void RegistrarBrazoInactivo(string nombreBrazo)
    {
        DateTime ahora = DateTime.Now;
        float duracion = 0f;

        if (tiempoInicioBrazo.ContainsKey(nombreBrazo))
        {
            duracion = Time.time - tiempoInicioBrazo[nombreBrazo];
            tiempoInicioBrazo.Remove(nombreBrazo);

            // Acumular en totales
            if (!tiempoTotalBrazo.ContainsKey(nombreBrazo))
                tiempoTotalBrazo[nombreBrazo] = 0f;
            tiempoTotalBrazo[nombreBrazo] += duracion;

            if (!activacionesBrazo.ContainsKey(nombreBrazo))
                activacionesBrazo[nombreBrazo] = 0;
            activacionesBrazo[nombreBrazo]++;
        }

        eventos.Add(new EventoLog
        {
            fecha = ahora.ToString("yyyy-MM-dd"),
            hora = ahora.ToString("HH:mm:ss"),
            tipo = "BRAZO_INACTIVO",
            brazo = nombreBrazo,
            numero = 0,
            tiempoSegundos = duracion
        });
    }

    public void GuardarCSV()
    {
        if (!guardarEstaCorrida)
        {
            Debug.Log($"[LogProduccion] 'guardarEstaCorrida' desactivado — corrida descartada ({dronesRegistrados} drones, {cajasRegistradas} cajas no exportadas).");
            return;
        }

        if (eventos.Count == 0)
        {
            Debug.LogWarning("[LogProduccion] No hay eventos para exportar.");
            return;
        }

        try
        {
            string carpeta = Path.Combine(Application.dataPath, "..", carpetaDestino);
            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string nombreArchivo = $"{prefijoArchivo}_{timestamp}.csv";
            string rutaCompleta = Path.Combine(carpeta, nombreArchivo);

            StringBuilder sb = new StringBuilder();

            // Encabezado
            sb.AppendLine("Fecha,Hora,Tipo,Brazo,Numero,Tiempo_Segundos");

            // Filas en orden cronológico
            foreach (var ev in eventos)
            {
                string numero = (ev.tipo == "DRON" || ev.tipo == "CAJA") ? ev.numero.ToString() : "";
                string tiempo = "";
                if (ev.tipo == "DRON" || ev.tipo == "BRAZO_INACTIVO")
                    tiempo = ev.tiempoSegundos.ToString("F2");

                sb.AppendLine($"{ev.fecha},{ev.hora},{ev.tipo},{ev.brazo},{numero},{tiempo}");
            }

            sb.AppendLine();

            // Resumen general
            sb.AppendLine("RESUMEN");
            sb.AppendLine($"Drones ensamblados,{dronesRegistrados}");
            sb.AppendLine($"Cajas paletizadas,{cajasRegistradas}");

            // Estadísticas de tiempo por dron
            if (dronesRegistrados > 0)
            {
                float totalDron = 0f;
                float minDron = float.MaxValue;
                float maxDron = 0f;
                foreach (var ev in eventos)
                {
                    if (ev.tipo == "DRON")
                    {
                        totalDron += ev.tiempoSegundos;
                        if (ev.tiempoSegundos < minDron) minDron = ev.tiempoSegundos;
                        if (ev.tiempoSegundos > maxDron) maxDron = ev.tiempoSegundos;
                    }
                }
                sb.AppendLine($"Tiempo promedio dron (s),{(totalDron / dronesRegistrados):F2}");
                sb.AppendLine($"Tiempo minimo dron (s),{minDron:F2}");
                sb.AppendLine($"Tiempo maximo dron (s),{maxDron:F2}");
            }

            sb.AppendLine();

            // Tiempo total acumulado por brazo
            sb.AppendLine("TIEMPO TOTAL POR BRAZO (segundos)");
            foreach (var kv in tiempoTotalBrazo)
                sb.AppendLine($"{kv.Key},{kv.Value:F2}");

            sb.AppendLine();

            // Promedio por activación (≈ tiempo por dron)
            sb.AppendLine("TIEMPO PROMEDIO POR ACTIVACION (segundos)");
            foreach (var kv in tiempoTotalBrazo)
            {
                int activaciones = activacionesBrazo.ContainsKey(kv.Key) ? activacionesBrazo[kv.Key] : 1;
                float promedio = activaciones > 0 ? kv.Value / activaciones : 0f;
                sb.AppendLine($"{kv.Key},{promedio:F2},(de {activaciones} activaciones)");
            }

            File.WriteAllText(rutaCompleta, sb.ToString(), Encoding.UTF8);

            Debug.Log($"[LogProduccion] 📊 CSV guardado: {rutaCompleta}");
            Debug.Log($"[LogProduccion]    Drones: {dronesRegistrados} | Cajas: {cajasRegistradas}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LogProduccion] Error guardando CSV: {ex.Message}");
        }
    }

    [ContextMenu("Forzar guardado del CSV ahora")]
    public void ForzarGuardado()
    {
        bool flagOriginal = guardarEstaCorrida;
        guardarEstaCorrida = true;
        GuardarCSV();
        guardarEstaCorrida = flagOriginal;
    }

    [ContextMenu("Limpiar eventos en memoria")]
    public void LimpiarEventos()
    {
        eventos.Clear();
        tiempoInicioBrazo.Clear();
        tiempoTotalBrazo.Clear();
        activacionesBrazo.Clear();
        dronesRegistrados = 0;
        cajasRegistradas = 0;
        Debug.Log("[LogProduccion] Eventos en memoria limpiados.");
    }
}