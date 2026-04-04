using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class OrquestadorDron : MonoBehaviour
{
    [Header("Referencias a los 3 brazos")]
    public Brazos alfa;
    public Brazos beta;
    public Ventosa omega;

    [Header("Archivo maestro")]
    public string archivoMaestro = "ensamblaje_dron.json";

    [Header("Estado (solo lectura en Inspector)")]
    public int etapaActual = 0;
    public string nombreEtapaActual = "";
    public bool ensamblajeFinalizado = false;

    private MaestroEnsamblaje maestro;
    private bool etapaEnCurso = false;
    private bool alfaActivo = false;
    private bool betaActivo = false;
    private bool omegaActivo = false;

    private int frameEspera = 0;
    private const int FRAMES_ESPERA = 0; // espera 3 frames antes de evaluar

    void Start()
    {
        // Resetear estado completamente
        etapaActual = 0;
        etapaEnCurso = false;
        alfaActivo = false;
        betaActivo = false;
        omegaActivo = false;
        ensamblajeFinalizado = false;

        // Limpiar estado previo de todos los brazos
        alfa.jugandoSecuencia = false;
        beta.jugandoSecuencia = false;
        omega.jugandoSecuencia = false;

        CargarMaestro();
        // Verificar orden ANTES de ejecutar
        if (maestro != null)
        {
            for (int i = 0; i < maestro.etapas.Count; i++)
                Debug.Log($"[{i}] {maestro.etapas[i].nombre}");
        }
        if (maestro != null && maestro.etapas.Count > 0)
            EjecutarEtapa(0);
    }

    void Update()
    {
        if (!etapaEnCurso || ensamblajeFinalizado) return;

        // Esperar algunos frames después de lanzar la etapa
        if (frameEspera < FRAMES_ESPERA)
        {
            frameEspera++;
            return;
        }

        // Solo espera los brazos que participan en esta etapa
        bool alfaListo = !alfaActivo || !alfa.jugandoSecuencia;
        bool betaListo = !betaActivo || !beta.jugandoSecuencia;
        bool omegaListo = !omegaActivo || !omega.jugandoSecuencia;

        if (alfaListo && betaListo && omegaListo)
        {
            etapaEnCurso = false;
            etapaActual++;

            if (etapaActual < maestro.etapas.Count)
                EjecutarEtapa(etapaActual);
            else
            {
                ensamblajeFinalizado = true;
                Debug.Log("✅ Ensamblaje del dron COMPLETADO.");
            }
        }
    }

    void CargarMaestro()
    {
        string path = Path.Combine(Application.streamingAssetsPath, archivoMaestro);

        if (!File.Exists(path))
        {
            Debug.LogError("❌ No se encontró el archivo maestro en: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        maestro = JsonUtility.FromJson<MaestroEnsamblaje>(json);
        Debug.Log("✅ Maestro cargado: " + maestro.etapas.Count + " etapas");
    }

    void EjecutarEtapa(int index)
    {
        EtapaEnsamblaje etapa = maestro.etapas[index];
        nombreEtapaActual = etapa.nombre;

        // Resetear flags de participación
        alfaActivo = false;
        betaActivo = false;
        omegaActivo = false;

        Debug.Log($"▶ Etapa {index + 1}/{maestro.etapas.Count}: {etapa.nombre}");

        foreach (var instruccion in etapa.brazos)
        {
            if (string.IsNullOrEmpty(instruccion.archivo)) continue;

            switch (instruccion.brazo)
            {
                case "Alpha":
                    alfa.saveFileName = instruccion.archivo;
                    alfa.LoadFromFile();
                    alfa.IniciarSecuencia();
                    alfaActivo = true;
                    break;

                case "Beta":
                    beta.saveFileName = instruccion.archivo;
                    beta.LoadFromFile();
                    beta.IniciarSecuencia();
                    betaActivo = true;
                    break;

                case "Omega":
                    omega.saveFileName = instruccion.archivo;
                    omega.LoadFromFile();
                    omega.IniciarSecuencia();
                    omegaActivo = true;
                    break;

                default:
                    Debug.LogWarning("⚠ Brazo no reconocido: " + instruccion.brazo);
                    break;
            }
        }

        frameEspera = 0; ;
        etapaEnCurso = true;
    }
}

// ── Clases de datos ──────────────────────────────────────────

[System.Serializable]
public class BrazoInstruccion
{
    public string brazo;
    public string archivo;
}

[System.Serializable]
public class EtapaEnsamblaje
{
    public string nombre;
    public List<BrazoInstruccion> brazos;
}

[System.Serializable]
public class MaestroEnsamblaje
{
    public List<EtapaEnsamblaje> etapas;
}