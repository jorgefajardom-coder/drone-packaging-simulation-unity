using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Produccion : MonoBehaviour
{
    [Header("Cantidad de drones a producir")]
    public int dronesAProducir = 8;

    [Header("Spawns de piezas")]
    public Spawner spawnBase;
    public Spawner spawnPCB;
    public Spawner spawnMotor1, spawnMotor2, spawnMotor3, spawnMotor4;
    public Spawner spawnHelice1, spawnHelice2, spawnHelice3, spawnHelice4;
    public Spawner spawnTapa;

    [Header("Cajas (una por punto de spawn)")]
    public Spawner[] spawnsCaja;

    [Header("Brazos de ensamblaje")]
    public Brazos brazoAlpha;
    public Brazos brazoBeta;
    public Ventosa brazoOmega;
    public Ventosa brazoPaletizador;

    [HideInInspector] public int droneActual = 0;
    [HideInInspector] public int carroActual = 0;
    [HideInInspector] public int cajasEnCarroActual = 0;
    [HideInInspector] public float tiempoInicioDronActual;
    [HideInInspector] public float tiempoTotalSimulacion = 0f;
    [HideInInspector] public bool simulacionActiva = false;
    [HideInInspector] public string ultimoLogTiempo = "";

    private const int CAJAS_POR_CARRO = 4;
    private GameObject baseActual;

    public event Action<string> OnLogTiempo;

    private Vector3[] posicionesOriginalesCarros;
    private Quaternion[] rotacionesOriginalesCarros;

    [Header("Animación de swap de carros")]
    public float distanciaSalidaZ = -3f;
    public float distanciaEntradaZ = 3f;
    public float duracionSalida = 2f;
    public float duracionEntrada = 2f;

    [Header("Carros de paleta (en orden de aparición)")]
    public GameObject[] carros;
    public GameObject carroPrefab;

    [Header("TCP")]
    public CodesysTcpClient tcp;

    [Header("Logger de producción")]
    public LogProduccion logger;

    // Propiedades para HMI
    public bool OmegaActivo => brazoOmega != null && brazoOmega.TieneObjeto;
    public bool PaletActivo => brazoPaletizador != null && brazoPaletizador.TieneObjeto;
    public string OmegaAccion => brazoOmega != null && brazoOmega.TieneObjeto ? "HOLDING" :
                                    brazoOmega != null && brazoOmega.secuenciaTerminada ? "IDLE" : "MOVING";
    public string PaletAccion => brazoPaletizador != null && brazoPaletizador.TieneObjeto ? "HOLDING" :
                                    brazoPaletizador != null && brazoPaletizador.secuenciaTerminada ? "IDLE" : "MOVING";
    public string CarroActualTag => carroActual % 2 == 0 ? "A" : "B";
    public float TiempoCicloActual => Time.time - tiempoInicioDronActual;
    public bool SistemaPausado => tcp != null && tcp.isConnected && (tcp.salidas_plc2 & 0x10) != 0;

    void Start()
    {
        posicionesOriginalesCarros = new Vector3[carros.Length];
        rotacionesOriginalesCarros = new Quaternion[carros.Length];
        for (int i = 0; i < carros.Length; i++)
        {
            posicionesOriginalesCarros[i] = carros[i].transform.position;
            rotacionesOriginalesCarros[i] = carros[i].transform.rotation;
            carros[i].SetActive(i < 2);
        }

        RetiradorCarro r0 = carros[0].GetComponent<RetiradorCarro>();
        if (r0 != null)
        {
            r0.cajasAsignadas = new int[] { 1, 2, 3, 4 };
            r0.numeroCajaFinal = 4;
            r0.cajasAdoptadas = false;
        }

        RetiradorCarro r1 = carros[1].GetComponent<RetiradorCarro>();
        if (r1 != null)
        {
            r1.cajasAsignadas = new int[] { 5, 6, 7, 8 };
            r1.numeroCajaFinal = 8;
            r1.cajasAdoptadas = false;
        }

        for (int i = 0; i < spawnsCaja.Length; i++)
        {
            GameObject caja = spawnsCaja[i].Spawn();
            caja.name = "CajaPrefab(Clone" + (i + 1) + ")";
        }

        CarroPaletizador cp = GameObject.FindObjectOfType<CarroPaletizador>();
        if (cp != null)
        {
            cp.totalDrones = dronesAProducir;
            cp.IniciarSecuenciaCarro();
        }

        StartCoroutine(LoopProduccion());
    }

    void Update()
    {
        Time.timeScale = SistemaPausado ? 0f : 1f;
        if (!simulacionActiva) return;
        tiempoTotalSimulacion += Time.deltaTime;
    }

    IEnumerator Esperar(Func<bool> condicion)
    {
        yield return new WaitUntil(() => !SistemaPausado && condicion());
    }

    IEnumerator LoopProduccion()
    {
        simulacionActiva = true;
        tiempoTotalSimulacion = 0f;

        while (droneActual < dronesAProducir)
        {
            cajasEnCarroActual = 0;
            brazoAlpha.ResetCompleto();
            brazoBeta.ResetCompleto();
            brazoOmega.ResetCompleto();
            brazoPaletizador.ResetCompleto();
            brazoPaletizador.ReiniciarContadorDrones();

            yield return null;

            tiempoInicioDronActual = Time.time;

            StartCoroutine(SecuenciaEnsamblaje());
            brazoAlpha.IniciarSecuenciaConEspera();
            brazoBeta.IniciarSecuenciaConEspera();
            brazoOmega.IniciarSecuenciaConEspera();

            yield return StartCoroutine(Esperar(() => brazoPaletizador.TieneObjeto));

            ReportarTiempoDron(droneActual + 1);

            tiempoInicioDronActual = Time.time;

            StartCoroutine(SecuenciaEnsamblaje());
            brazoAlpha.ResetCompleto();
            brazoBeta.ResetCompleto();
            brazoOmega.ResetCompleto();
            yield return null;
            brazoAlpha.IniciarSecuenciaConEspera();
            brazoBeta.IniciarSecuenciaConEspera();
            brazoOmega.IniciarSecuenciaConEspera();

            yield return StartCoroutine(Esperar(() => brazoPaletizador.secuenciaTerminada));

            droneActual++;
            cajasEnCarroActual++;
            Debug.Log($"[Produccion] ✅ Dron {droneActual}/{dronesAProducir} paletizado");

            // Registro al logger CSV
            if (logger != null)
                logger.RegistrarCaja(droneActual);

            if (cajasEnCarroActual >= CAJAS_POR_CARRO)
            {
                yield return StartCoroutine(SwapCarro());
                cajasEnCarroActual = 0;
            }

            while (droneActual < dronesAProducir && cajasEnCarroActual < CAJAS_POR_CARRO * 2)
            {
                brazoPaletizador.ResetCompleto();
                yield return null;

                yield return StartCoroutine(Esperar(() => brazoOmega.dronDepositado));
                brazoOmega.dronDepositado = false;

                brazoPaletizador.IniciarSecuencia();

                yield return StartCoroutine(Esperar(() => brazoPaletizador.TieneObjeto));

                ReportarTiempoDron(droneActual + 1);

                if (droneActual < dronesAProducir - 1)
                {
                    tiempoInicioDronActual = Time.time;

                    StartCoroutine(SecuenciaEnsamblaje());
                    brazoAlpha.ResetCompleto();
                    brazoBeta.ResetCompleto();
                    brazoOmega.ResetCompleto();
                    yield return null;
                    brazoAlpha.IniciarSecuenciaConEspera();
                    brazoBeta.IniciarSecuenciaConEspera();
                    brazoOmega.IniciarSecuenciaConEspera();
                }

                yield return StartCoroutine(Esperar(() => brazoPaletizador.secuenciaTerminada));

                droneActual++;
                cajasEnCarroActual++;
                Debug.Log($"[Produccion] ✅ Dron {droneActual}/{dronesAProducir} paletizado");

                // Registro al logger CSV
                if (logger != null)
                    logger.RegistrarCaja(droneActual);

                if (cajasEnCarroActual >= CAJAS_POR_CARRO)
                {
                    yield return StartCoroutine(SwapCarro());
                    cajasEnCarroActual = 0;
                }
            }
        }

        simulacionActiva = false;
        int mTot = Mathf.FloorToInt(tiempoTotalSimulacion / 60f);
        int sTot = Mathf.FloorToInt(tiempoTotalSimulacion % 60f);
        string msgFinal = $"TOTAL: {mTot:00}:{sTot:00}";
        OnLogTiempo?.Invoke(msgFinal);
        Debug.Log($"[Produccion] 🏁 Tiempo total: {msgFinal}");

        // Guardar CSV de la corrida (si está activado en el Inspector del logger)
        if (logger != null)
            logger.GuardarCSV();
    }

    private void ReportarTiempoDron(int numeroDron)
    {
        float tiempoCiclo = Time.time - tiempoInicioDronActual;
        int minutos = Mathf.FloorToInt(tiempoCiclo / 60f);
        int segundos = Mathf.FloorToInt(tiempoCiclo % 60f);
        string msg = $"Dron {numeroDron}: {minutos:00}:{segundos:00}";
        ultimoLogTiempo = msg;
        OnLogTiempo?.Invoke(msg);
        Debug.Log($"[Produccion] ⏱ {msg} ({tiempoCiclo:F2}s)");

        // Registro al logger CSV
        if (logger != null)
            logger.RegistrarDron(numeroDron, tiempoCiclo);
    }

    IEnumerator SecuenciaEnsamblaje()
    {
        baseActual = spawnBase.Spawn();
        yield return new WaitForSeconds(1);

        spawnPCB.Spawn();
        yield return new WaitForSeconds(1);

        spawnMotor1.baseParent = baseActual.transform;
        spawnMotor2.baseParent = baseActual.transform;
        spawnMotor3.baseParent = baseActual.transform;
        spawnMotor4.baseParent = baseActual.transform;
        spawnHelice1.baseParent = baseActual.transform;
        spawnHelice2.baseParent = baseActual.transform;
        spawnHelice3.baseParent = baseActual.transform;
        spawnHelice4.baseParent = baseActual.transform;
        spawnTapa.baseParent = baseActual.transform;

        spawnMotor1.Spawn();
        spawnMotor2.Spawn();
        yield return new WaitForSeconds(1);

        spawnMotor3.Spawn();
        spawnMotor4.Spawn();
        yield return new WaitForSeconds(1);

        spawnHelice1.Spawn();
        spawnHelice2.Spawn();
        yield return new WaitForSeconds(2);

        spawnHelice3.Spawn();
        spawnHelice4.Spawn();
        yield return new WaitForSeconds(2);

        spawnTapa.Spawn();
        yield return new WaitForSeconds(2);
    }

    IEnumerator SwapCarro()
    {
        yield return new WaitForSeconds(1f);

        GameObject carroSaliente = carros[carroActual];
        RetiradorCarro retiradorSaliente = carroSaliente.GetComponent<RetiradorCarro>();
        if (retiradorSaliente != null)
        {
            retiradorSaliente.IntentarAdoptarCajas();
            yield return StartCoroutine(Esperar(() => retiradorSaliente.cajasAdoptadas));
        }
        else
        {
            Debug.LogWarning($"[Produccion] {carroSaliente.name} no tiene RetiradorCarro.");
        }

        int slotLiberado = carroActual % 2;
        Vector3 posSlot = posicionesOriginalesCarros[slotLiberado];
        Quaternion rotSlot = rotacionesOriginalesCarros[slotLiberado];

        yield return StartCoroutine(AnimarCarroEnZ(
            carroSaliente.transform,
            carroSaliente.transform.position,
            carroSaliente.transform.position + new Vector3(0f, 0f, distanciaSalidaZ),
            duracionSalida));

        int[] cajasDelCarro = slotLiberado == 0
            ? new int[] { 1, 2, 3, 4 }
            : new int[] { 5, 6, 7, 8 };

        foreach (int n in cajasDelCarro)
        {
            GameObject caja = GameObject.Find("CajaPrefab(Clone" + n + ")");
            if (caja != null) Destroy(caja);
        }

        Destroy(carroSaliente);
        carroActual++;

        if (droneActual >= dronesAProducir)
            yield break;

        if (carroPrefab == null)
        {
            Debug.LogError("[Produccion] carroPrefab no asignado.");
            yield break;
        }

        int[] numerosCajaNuevo = slotLiberado == 0
            ? new int[] { 1, 2, 3, 4 }
            : new int[] { 5, 6, 7, 8 };

        Vector3 posInicial = posSlot - new Vector3(0f, 0f, distanciaEntradaZ);
        GameObject carroNuevo = Instantiate(carroPrefab, posInicial, rotSlot);
        carroNuevo.name = "CARRO_slot" + slotLiberado + "_v" + (carroActual + 1);

        System.Array.Resize(ref carros, carros.Length + 1);
        carros[carros.Length - 1] = carroNuevo;

        RetiradorCarro retiradorNuevo = carroNuevo.GetComponent<RetiradorCarro>();
        if (retiradorNuevo != null)
        {
            retiradorNuevo.cajasAsignadas = numerosCajaNuevo;
            retiradorNuevo.numeroCajaFinal = numerosCajaNuevo[numerosCajaNuevo.Length - 1];
            retiradorNuevo.cajasAdoptadas = false;
        }

        List<GameObject> cajasNuevas = new List<GameObject>();
        for (int i = 0; i < numerosCajaNuevo.Length; i++)
        {
            int n = numerosCajaNuevo[i];
            int indexSpawn = n - 1;
            if (indexSpawn < 0 || indexSpawn >= spawnsCaja.Length) continue;

            Vector3 posSpawnMundial = spawnsCaja[indexSpawn].transform.position;
            Vector3 offsetRelativoAlSlot = posSpawnMundial - posSlot;
            Vector3 posInicioCaja = posInicial + offsetRelativoAlSlot;

            GameObject caja = spawnsCaja[indexSpawn].Spawn();
            caja.name = "CajaPrefab(Clone" + n + ")";
            caja.transform.position = posInicioCaja;
            caja.transform.SetParent(carroNuevo.transform, true);

            cajasNuevas.Add(caja);
        }

        Debug.Log($"[Produccion] 🔄 Swap de carro: entra {carroNuevo.name}");

        yield return StartCoroutine(AnimarCarroEnZ(
            carroNuevo.transform,
            posInicial,
            posSlot,
            duracionEntrada));

        foreach (GameObject caja in cajasNuevas)
            caja.transform.SetParent(null, true);

        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator AnimarCarroEnZ(Transform carro, Vector3 desde, Vector3 hasta, float duracion)
    {
        float tiempo = 0f;
        while (tiempo < duracion)
        {
            float t = Mathf.SmoothStep(0f, 1f, tiempo / duracion);
            carro.position = Vector3.Lerp(desde, hasta, t);
            tiempo += Time.deltaTime;
            yield return null;
        }
        carro.position = hasta;
    }
}