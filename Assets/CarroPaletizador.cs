using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarroPaletizador : MonoBehaviour
{
    public enum PatronMovimiento
    {
        Directo,        // traslado directo (diagonal si X y Z difieren)
        EnL_XLuegoZ     // primero mueve en X, después en Z
    }

    [System.Serializable]
    public class MovimientoPaletizado
    {
        public string nombre;                   // descriptivo, ej: "Dron 1 → Punto1_1"
        public Transform zonaGiro;              // qué zona de giro usar
        public float anguloGiro;                // -90 o 90
        public Transform puntoDestino;          // punto final
        public PatronMovimiento patron;         // cómo moverse al destino
    }

    [Header("Punto de inicio (fijo)")]
    public Transform puntoInicio;

    [Header("Secuencia de movimientos (uno por dron)")]
    public List<MovimientoPaletizado> movimientos;

    [Header("Brazo paletizador (hijo del carro)")]
    public Ventosa ventosa;

    [Header("Raíz del ArticulationBody del brazo")]
    public ArticulationBody articulacionRaiz;

    [Header("Velocidad")]
    public float velocidadMovimiento = 1f;

    [Header("Tolerancia")]
    public float tolPos = 0.02f;

    [Header("Rotación")]
    public float duracionGiro = 0.5f;

    [Header("Duración del traslado suave (segundos)")]
    public float duracionTrasladoFinal = 0.5f;

    [Header("Delay después de soltar (segundos)")]
    public float delayDespuesSoltar = 0.5f;

    [Header("Retirador final (CARRO v1 o v2 que recibe las cajas)")]
    [Tooltip("Arrastra aquí el GameObject del CARRO (v1 o v2) que tiene el componente RetiradorCarro")]
    private RetiradorCarro retiradorCarro;

    [Header("Total de drones a paletizar (sincronizar con Produccion)")]
    public int totalDrones = 0;
    private int dronesAtendidos = 0;

    private Vector3 offsetCarroAPivot;
    private Vector3 offsetBrazoLocal;
    private Transform brazoTransform;
    private float rotacionAcumulada = 0f;

    private void Start()
    {
        // Siempre usar el RetiradorCarro del mismo GameObject
        retiradorCarro = GetComponent<RetiradorCarro>();

        if (retiradorCarro == null)
            retiradorCarro = gameObject.AddComponent<RetiradorCarro>();

        offsetCarroAPivot = transform.position - puntoInicio.position;
        brazoTransform = articulacionRaiz.transform;
        offsetBrazoLocal = brazoTransform.localPosition;
    }

    public void IniciarSecuenciaCarro()
    {
        dronesAtendidos = 0;
        StartCoroutine(EjecutarSecuencia());
    }

    IEnumerator EjecutarSecuencia()
    {
        while (dronesAtendidos < totalDrones)
        {
            int i = dronesAtendidos % movimientos.Count;
            var mov = movimientos[i];

            // 1. Esperar a que el brazo agarre el dron
            yield return new WaitUntil(() => ventosa.TieneObjeto);

            // 2. Traslado a la zona de giro
            yield return TrasladarA(mov.zonaGiro, puntoInicio);

            // 3. Giro
            yield return GirarCarroSobrePunto(mov.anguloGiro, mov.zonaGiro);

            // 4. Traslado al punto destino según el patrón
            if (mov.patron == PatronMovimiento.Directo)
                yield return TrasladarConPivotRotado(mov.puntoDestino, puntoInicio);
            else
                yield return TrasladarEnL(mov.puntoDestino, puntoInicio, xPrimero: true);

            // 4.5. Otorgar permiso al brazo para soltar
            ventosa.permisoParaSoltar = true;

            // 5. Esperar a que suelte el dron
            yield return new WaitUntil(() => !ventosa.TieneObjeto);

            // 5.5. Delay después de soltar
            yield return new WaitForSeconds(delayDespuesSoltar);

            // 6. Regreso a la zona de giro
            if (mov.patron == PatronMovimiento.Directo)
                yield return TrasladarConPivotRotado(mov.zonaGiro, puntoInicio);
            else
                yield return TrasladarEnL(mov.zonaGiro, puntoInicio, xPrimero: false);

            // 7. Des-girar
            yield return GirarCarroSobrePunto(-mov.anguloGiro, mov.zonaGiro);

            // 8. Regreso a puntoInicio
            yield return TrasladarConPivotRotado(puntoInicio, puntoInicio);

            dronesAtendidos++;
            Debug.Log($"[Carro] 📦 Dron {dronesAtendidos}/{totalDrones} paletizado ({mov.nombre})");

            // Adoptar cajas cada vez que se completa un grupo (y no es el último)
            if (dronesAtendidos % movimientos.Count == 0 && dronesAtendidos < totalDrones)
            {
                if (retiradorCarro != null)
                    retiradorCarro.IntentarAdoptarCajas();
            }
        }

        // Adoptar cajas del último grupo al terminar
        if (retiradorCarro != null)
            retiradorCarro.IntentarAdoptarCajas();

        Debug.Log("[Carro] ✅ Secuencia de paletizado completa.");
    }

    // ==================== MÉTODOS DE MOVIMIENTO ====================

    private IEnumerator TrasladarA(Transform destino, Transform pivoteDelTramo)
    {
        Vector3 offsetLocal = transform.position - pivoteDelTramo.position;

        Vector3 posObjetivo = new Vector3(
            destino.position.x + offsetLocal.x,
            transform.position.y,
            destino.position.z + offsetLocal.z);

        while (Vector3.Distance(transform.position, posObjetivo) > tolPos)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, posObjetivo,
                velocidadMovimiento * Time.deltaTime);

            ActualizarPosicionBrazo();
            yield return null;
        }

        transform.position = posObjetivo;
        ActualizarPosicionBrazo();
    }

    private IEnumerator TrasladarConPivotRotado(Transform destino, Transform pivoteOriginal)
    {
        Vector3 offsetOriginal = offsetCarroAPivot;
        Quaternion rotacion = Quaternion.Euler(0, rotacionAcumulada, 0);
        Vector3 offsetRotado = rotacion * offsetOriginal;

        Vector3 posInicial = transform.position;
        Vector3 posObjetivo = new Vector3(
            destino.position.x + offsetRotado.x,
            transform.position.y,
            destino.position.z + offsetRotado.z);

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionTrasladoFinal)
        {
            float t = tiempoTranscurrido / duracionTrasladoFinal;
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(posInicial, posObjetivo, t);
            ActualizarPosicionBrazo();

            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        transform.position = posObjetivo;
        ActualizarPosicionBrazo();
    }

    // Traslado en L: mueve primero en un eje, después en el otro
    private IEnumerator TrasladarEnL(Transform destino, Transform pivoteOriginal, bool xPrimero)
    {
        Vector3 offsetOriginal = offsetCarroAPivot;
        Quaternion rotacion = Quaternion.Euler(0, rotacionAcumulada, 0);
        Vector3 offsetRotado = rotacion * offsetOriginal;

        Vector3 posFinal = new Vector3(
            destino.position.x + offsetRotado.x,
            transform.position.y,
            destino.position.z + offsetRotado.z);

        // Punto intermedio según qué eje mover primero
        Vector3 posIntermedia;
        if (xPrimero)
            posIntermedia = new Vector3(posFinal.x, transform.position.y, transform.position.z);
        else
            posIntermedia = new Vector3(transform.position.x, transform.position.y, posFinal.z);

        // Tramo 1: ir al intermedio
        yield return LerpAPosicion(posIntermedia);

        // Tramo 2: ir al final
        yield return LerpAPosicion(posFinal);
    }

    private IEnumerator LerpAPosicion(Vector3 objetivo)
    {
        Vector3 inicio = transform.position;
        float tiempo = 0f;

        while (tiempo < duracionTrasladoFinal)
        {
            float t = Mathf.SmoothStep(0f, 1f, tiempo / duracionTrasladoFinal);
            transform.position = Vector3.Lerp(inicio, objetivo, t);
            ActualizarPosicionBrazo();
            tiempo += Time.deltaTime;
            yield return null;
        }

        transform.position = objetivo;
        ActualizarPosicionBrazo();
    }

    private IEnumerator GirarCarroSobrePunto(float anguloObjetivo, Transform zonaGiro)
    {
        Vector3 puntoPivote = zonaGiro.position;
        Quaternion rotacionInicialCarro = transform.rotation;
        Quaternion rotacionFinalCarro = rotacionInicialCarro * Quaternion.Euler(0, anguloObjetivo, 0);
        Vector3 offsetCarroAlPivote = transform.position - puntoPivote;

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionGiro)
        {
            float t = tiempoTranscurrido / duracionGiro;
            t = Mathf.SmoothStep(0, 1, t);

            Quaternion rotacionActualCarro = Quaternion.Slerp(rotacionInicialCarro, rotacionFinalCarro, t);
            Vector3 offsetRotado = rotacionActualCarro * Quaternion.Inverse(rotacionInicialCarro) * offsetCarroAlPivote;

            transform.position = puntoPivote + offsetRotado;
            transform.rotation = rotacionActualCarro;

            ActualizarPosicionBrazo();

            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        Vector3 offsetFinal = rotacionFinalCarro * Quaternion.Inverse(rotacionInicialCarro) * offsetCarroAlPivote;
        transform.position = puntoPivote + offsetFinal;
        transform.rotation = rotacionFinalCarro;

        ActualizarPosicionBrazo();

        rotacionAcumulada += anguloObjetivo;
    }

    private void ActualizarPosicionBrazo()
    {
        if (brazoTransform != null)
        {
            brazoTransform.localPosition = offsetBrazoLocal;

            if (articulacionRaiz != null)
            {
                articulacionRaiz.TeleportRoot(
                    brazoTransform.position,
                    brazoTransform.rotation);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (puntoInicio != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(puntoInicio.position, 0.2f);
        }

        if (movimientos != null)
        {
            foreach (var mov in movimientos)
            {
                if (mov.zonaGiro != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(mov.zonaGiro.position, 0.2f);
                }
                if (mov.puntoDestino != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(mov.puntoDestino.position, 0.2f);
                }
            }
        }
    }
}