using UnityEngine;
using System.Collections;

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

    [Header("Carros de paleta (en orden de aparición)")]
    public GameObject[] carros;

    private int droneActual = 0;
    private int carroActual = 0;
    private int cajasEnCarroActual = 0;
    private const int CAJAS_POR_CARRO = 4;
    private GameObject baseActual;

    [Header("Animación de swap de carros")]
    [Tooltip("Distancia en Z que el carro saliente recorre al retirarse")]
    public float distanciaSalidaZ = -3f;

    [Tooltip("Distancia en Z desde la que el carro entrante aparece (relativa a su posición final)")]
    public float distanciaEntradaZ = -3f;

    [Tooltip("Duración en segundos de la animación de salida")]
    public float duracionSalida = 2f;

    [Tooltip("Duración en segundos de la animación de entrada del nuevo carro")]
    public float duracionEntrada = 2f;

    void Start()
    {
        for (int i = 0; i < carros.Length; i++)
            carros[i].SetActive(i < 2);

        for (int i = 0; i < spawnsCaja.Length; i++)
        {
            GameObject caja = spawnsCaja[i].Spawn();
            caja.name = "CajaPrefab(Clone" + (i + 1) + ")";
        }

        StartCoroutine(LoopProduccion());
    }

    IEnumerator LoopProduccion()
    {
        // ── Dron 1: todo arranca junto con autoStartOnPlay y delays originales ──
        StartCoroutine(SecuenciaEnsamblaje());

        // Esperar que paletizador tome el dron 1
        yield return new WaitUntil(() => brazoPaletizador.TieneObjeto);
        Debug.Log("[Produccion] Paletizador tomó dron 1 — arrancando ciclo 2.");

        // Arrancar ensamblaje del dron 2 en paralelo mientras paletizador hace recorrido
        StartCoroutine(SecuenciaEnsamblaje());
        brazoAlpha.ResetCompleto();
        brazoBeta.ResetCompleto();
        brazoOmega.ResetCompleto();
        yield return null;
        brazoAlpha.IniciarSecuenciaConEspera();
        brazoBeta.IniciarSecuenciaConEspera();
        brazoOmega.IniciarSecuenciaConEspera();

        // Esperar que paletizador termine ciclo 1
        yield return new WaitUntil(() => brazoPaletizador.secuenciaTerminada);

        droneActual++;
        cajasEnCarroActual++;
        Debug.Log($"[Produccion] Dron 1 completo.");

        if (cajasEnCarroActual >= CAJAS_POR_CARRO)
        {
            yield return StartCoroutine(SwapCarro());
            cajasEnCarroActual = 0;
        }

        // ── Dron 2 en adelante ──
        while (droneActual < dronesAProducir)
        {
            if (droneActual % 8 == 0)
                brazoPaletizador.ReiniciarContadorDrones();

            Debug.Log($"[Produccion] Esperando que Omega suelte dron {droneActual + 1}...");

            // Resetear paletizador
            brazoPaletizador.ResetCompleto();
            yield return null;

            // Esperar que Omega deposite el dron completo en la mesa
            yield return new WaitUntil(() => brazoOmega.dronDepositado);
            brazoOmega.dronDepositado = false; // resetear para el siguiente ciclo
            Debug.Log($"[Produccion] Omega depositó dron {droneActual + 1} — activando paletizador.");

            // Arrancar paletizador sin delay
            brazoPaletizador.IniciarSecuencia();

            // Esperar que paletizador tome el dron
            yield return new WaitUntil(() => brazoPaletizador.TieneObjeto);
            Debug.Log($"[Produccion] Paletizador tomó dron {droneActual + 1}.");

            // Arrancar siguiente ciclo en paralelo
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

            // Esperar que paletizador termine
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

        // Esperar que el último ciclo de ensamblaje termine
        yield return new WaitUntil(() =>
            brazoAlpha.secuenciaTerminada &&
            brazoBeta.secuenciaTerminada &&
            brazoOmega.secuenciaTerminada
        );

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

        // 1. Adoptar cajas del carro saliente antes de moverlo
        GameObject carroSaliente = carros[carroActual];
        RetiradorCarro retirador = carroSaliente.GetComponent<RetiradorCarro>();
        if (retirador != null)
        {
            Debug.Log($"[Produccion] {carroSaliente.name} adoptando sus cajas antes de retirarse...");
            retirador.IntentarAdoptarCajas();
            yield return new WaitUntil(() => retirador.cajasAdoptadas);
        }
        else
        {
            Debug.LogWarning($"[Produccion] {carroSaliente.name} no tiene RetiradorCarro. Las cajas se quedarán sueltas.");
        }

        // 2. Animar salida del carro (desliza en Z)
        Debug.Log($"[Produccion] {carroSaliente.name} saliendo de escena...");
        yield return StartCoroutine(AnimarCarroEnZ(
            carroSaliente.transform,
            carroSaliente.transform.position,
            carroSaliente.transform.position + new Vector3(0f, 0f, distanciaSalidaZ),
            duracionSalida));

        // 3. Desactivar y avanzar al siguiente
        carroSaliente.SetActive(false);
        Debug.Log($"[Produccion] Carro {carroActual} retirado.");

        carroActual++;

        if (carroActual < carros.Length)
        {
            GameObject carroEntrante = carros[carroActual];
            Vector3 posFinal = carroEntrante.transform.position;
            Vector3 posInicial = posFinal - new Vector3(0f, 0f, distanciaEntradaZ);

            // 4. Posicionar el carro entrante en su punto de entrada (fuera de escena)
            carroEntrante.transform.position = posInicial;
            carroEntrante.SetActive(true);
            Debug.Log($"[Produccion] Carro {carroActual} entrando en escena...");

            // 5. Spawnear las cajas nuevas (viajan con el carro ya que aún no se depositaron drones)
            RetiradorCarro retiradorEntrante = carroEntrante.GetComponent<RetiradorCarro>();
            if (retiradorEntrante == null)
            {
                Debug.LogError($"[Produccion] {carroEntrante.name} no tiene RetiradorCarro. No se pueden spawnear cajas.");
            }
            else
            {
                int[] numerosCaja = retiradorEntrante.cajasAsignadas;

                foreach (int n in numerosCaja)
                {
                    int indexSpawn = n - 1; // caja 1 → spawnsCaja[0], caja 5 → spawnsCaja[4]
                    if (indexSpawn < 0 || indexSpawn >= spawnsCaja.Length)
                    {
                        Debug.LogError($"[Produccion] Índice {indexSpawn} fuera de rango para spawnsCaja.");
                        continue;
                    }

                    GameObject caja = spawnsCaja[indexSpawn].Spawn();
                    caja.name = "CajaPrefab(Clone" + n + ")";

                    // Emparentar al carro entrante para que viajen con él durante la animación
                    caja.transform.SetParent(carroEntrante.transform, true);
                }
            }

            // 6. Animar entrada del carro
            yield return StartCoroutine(AnimarCarroEnZ(
                carroEntrante.transform,
                posInicial,
                posFinal,
                duracionEntrada));

            // 7. Des-emparentar las cajas para que funcione como siempre
            if (retiradorEntrante != null)
            {
                foreach (int n in retiradorEntrante.cajasAsignadas)
                {
                    string nombre = "CajaPrefab(Clone" + n + ")";
                    GameObject caja = GameObject.Find(nombre);
                    if (caja != null)
                        caja.transform.SetParent(null, true);
                }
            }
            Debug.Log($"[Produccion] Carro {carroActual} en posición.");
        }

        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator AnimarCarroEnZ(Transform carro, Vector3 desde, Vector3 hasta, float duracion)
    {
        float tiempo = 0f;
        while (tiempo < duracion)
        {
            float t = tiempo / duracion;
            t = Mathf.SmoothStep(0f, 1f, t); // suavizado ease in/out
            carro.position = Vector3.Lerp(desde, hasta, t);
            tiempo += Time.deltaTime;
            yield return null;
        }
        carro.position = hasta;
    }
}