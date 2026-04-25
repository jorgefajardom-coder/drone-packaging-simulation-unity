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

    private int droneActual = 0;
    private int carroActual = 0;
    private int cajasEnCarroActual = 0;
    private const int CAJAS_POR_CARRO = 4;
    private GameObject baseActual;

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

        // ── Configurar RetiradorCarro de los dos carros iniciales ──
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

        // ── Spawnear las 8 cajas iniciales ──
        for (int i = 0; i < spawnsCaja.Length; i++)
        {
            GameObject caja = spawnsCaja[i].Spawn();
            caja.name = "CajaPrefab(Clone" + (i + 1) + ")";
        }

        StartCoroutine(LoopProduccion());
    }

    IEnumerator LoopProduccion()
    {
        while (droneActual < dronesAProducir)
        {
            // ── Reiniciar estado interno del ciclo de 8 ──
            cajasEnCarroActual = 0;
            brazoAlpha.ResetCompleto();
            brazoBeta.ResetCompleto();
            brazoOmega.ResetCompleto();
            brazoPaletizador.ResetCompleto();
            brazoPaletizador.ReiniciarContadorDrones();

            yield return null;

            // ── Dron 1 del ciclo ──
            StartCoroutine(SecuenciaEnsamblaje());
            brazoAlpha.IniciarSecuenciaConEspera();
            brazoBeta.IniciarSecuenciaConEspera();
            brazoOmega.IniciarSecuenciaConEspera();

            yield return new WaitUntil(() => brazoPaletizador.TieneObjeto);
            Debug.Log($"[Produccion] Paletizador tomó dron {droneActual + 1}");

            StartCoroutine(SecuenciaEnsamblaje());
            brazoAlpha.ResetCompleto();
            brazoBeta.ResetCompleto();
            brazoOmega.ResetCompleto();
            yield return null;
            brazoAlpha.IniciarSecuenciaConEspera();
            brazoBeta.IniciarSecuenciaConEspera();
            brazoOmega.IniciarSecuenciaConEspera();

            yield return new WaitUntil(() => brazoPaletizador.secuenciaTerminada);

            droneActual++;
            cajasEnCarroActual++;
            Debug.Log($"[Produccion] Dron {droneActual} completo.");

            if (cajasEnCarroActual >= CAJAS_POR_CARRO)
            {
                yield return StartCoroutine(SwapCarro());
                cajasEnCarroActual = 0;
            }

            // ── Drones 2..8 del ciclo ──
            while (droneActual < dronesAProducir && cajasEnCarroActual < CAJAS_POR_CARRO * 2)
            {
                brazoPaletizador.ResetCompleto();
                yield return null;

                yield return new WaitUntil(() => brazoOmega.dronDepositado);
                brazoOmega.dronDepositado = false;
                Debug.Log($"[Produccion] Omega depositó dron {droneActual + 1} — activando paletizador.");

                brazoPaletizador.IniciarSecuencia();

                yield return new WaitUntil(() => brazoPaletizador.TieneObjeto);
                Debug.Log($"[Produccion] Paletizador tomó dron {droneActual + 1}.");

                if (droneActual < dronesAProducir - 1)
                {
                    StartCoroutine(SecuenciaEnsamblaje());
                    brazoAlpha.ResetCompleto();
                    brazoBeta.ResetCompleto();
                    brazoOmega.ResetCompleto();
                    yield return null;
                    brazoAlpha.IniciarSecuenciaConEspera();
                    brazoBeta.IniciarSecuenciaConEspera();
                    brazoOmega.IniciarSecuenciaConEspera();
                }

                yield return new WaitUntil(() => brazoPaletizador.secuenciaTerminada);

                droneActual++;
                cajasEnCarroActual++;
                Debug.Log($"[Produccion] Dron {droneActual} completo.");

                if (cajasEnCarroActual >= CAJAS_POR_CARRO)
                {
                    yield return StartCoroutine(SwapCarro());
                    cajasEnCarroActual = 0;
                }
            }
        }

        Debug.Log($"[Produccion] ✅ PRODUCCIÓN COMPLETA: {droneActual} drones.");
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
            yield return new WaitUntil(() => retiradorSaliente.cajasAdoptadas);
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

        // ── Avanzar carroActual ANTES de instanciar el nuevo ──
        carroActual++;

        if (droneActual >= dronesAProducir)
        {
            Debug.Log("[Produccion] Producción completa, no se instancia reemplazo.");
            yield break;
        }

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

        // ── Nuevo carro va al FINAL del array, nunca sobreescribe carros existentes ──
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

        Debug.Log($"[Produccion] {carroNuevo.name} entrando con cajas...");
        yield return StartCoroutine(AnimarCarroEnZ(
            carroNuevo.transform,
            posInicial,
            posSlot,
            duracionEntrada));

        foreach (GameObject caja in cajasNuevas)
            caja.transform.SetParent(null, true);

        Debug.Log($"[Produccion] {carroNuevo.name} en posición, cajas liberadas.");
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