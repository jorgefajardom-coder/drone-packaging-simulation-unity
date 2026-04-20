using System.Collections;
using UnityEngine;

public class CarroPaletizador : MonoBehaviour
{
    [Header("Puntos de la escena")]
    public Transform puntoInicio;
    public Transform puntoGiro;
    public Transform punto1_1;

    [Header("Brazo paletizador (hijo del carro)")]
    public Ventosa ventosa;

    [Header("Raíz del ArticulationBody del brazo")]
    public ArticulationBody articulacionRaiz;

    [Header("Velocidad")]
    public float velocidadMovimiento = 2f;

    [Header("Tolerancia")]
    public float tolPos = 0.02f;

    [Header("Rotación")]
    public float duracionGiro = 1f;

    // Referencia al pivot real del carro (Punto_inicio)
    private Transform pivotReal;

    // Offset entre el carro y su pivot real (calculado en Start)
    private Vector3 offsetCarroAPivot;

    // Offset fijo entre carro y brazo (posición relativa)
    private Vector3 offsetBrazoLocal;

    // Referencia al Transform del brazo
    private Transform brazoTransform;

    // Rotación acumulada del carro a lo largo de la secuencia
    private float rotacionAcumulada = 0f;

    private void Start()
    {
        pivotReal = puntoInicio;
        offsetCarroAPivot = transform.position - pivotReal.position;

        brazoTransform = articulacionRaiz.transform;
        offsetBrazoLocal = brazoTransform.localPosition;

        Debug.Log($"[Carro] Mi posición: {transform.position}");
        Debug.Log($"[Carro] Pivot real (Punto_inicio): {pivotReal.position}");
        Debug.Log($"[Carro] Offset carro->pivot: {offsetCarroAPivot}");
        Debug.Log($"[Carro] Offset LOCAL del brazo: {offsetBrazoLocal}");

        StartCoroutine(Secuencia());
    }

    private IEnumerator Secuencia()
    {
        Debug.Log("[Carro] Esperando que el brazo agarre el dron...");
        yield return new WaitUntil(() => ventosa.TieneObjeto);
        Debug.Log("[Carro] Dron agarrado. Trasladando a puntoGiro...");

        // Tramo 1: traslado a puntoGiro usando offset respecto a puntoInicio
        yield return TrasladarA(puntoGiro, puntoInicio);
        Debug.Log("[Carro] Llegó a puntoGiro.");

        // Tramo 2: rotación -90° sobre puntoGiro
        Debug.Log("[Carro] Girando -90 grados sobre puntoGiro...");
        yield return GirarCarroSobrePunto(-90f);
        Debug.Log("[Carro] Giro completado.");

        // Tramo 3: traslado a punto1_1 con offset rotado según la rotación acumulada
        Debug.Log("[Carro] Trasladando a punto1_1...");
        yield return TrasladarConPivotRotado(punto1_1, puntoInicio);
        Debug.Log("[Carro] Llegó a punto1_1.");
    }

    private IEnumerator TrasladarA(Transform destino, Transform pivoteDelTramo)
    {
        // Recalcular el offset al inicio de este tramo
        Vector3 offsetLocal = transform.position - pivoteDelTramo.position;

        Vector3 posObjetivo = new Vector3(
            destino.position.x + offsetLocal.x,
            transform.position.y,
            destino.position.z + offsetLocal.z);

        Debug.Log($"[Carro] Pivote del tramo: {pivoteDelTramo.name} en {pivoteDelTramo.position}");
        Debug.Log($"[Carro] Offset recalculado: {offsetLocal}");
        Debug.Log($"[Carro] Objetivo: {posObjetivo}");

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

        Debug.Log($"[Carro] Llegó a {destino.name}. Carro en: {transform.position}");
    }

    private IEnumerator TrasladarConPivotRotado(Transform destino, Transform pivoteOriginal)
    {
        // Offset original guardado en Start
        Vector3 offsetOriginal = offsetCarroAPivot;

        // Rotar el offset según la rotación acumulada del carro
        Quaternion rotacion = Quaternion.Euler(0, rotacionAcumulada, 0);
        Vector3 offsetRotado = rotacion * offsetOriginal;

        Vector3 posObjetivo = new Vector3(
            destino.position.x + offsetRotado.x,
            transform.position.y,
            destino.position.z + offsetRotado.z);

        Debug.Log($"[Carro] Rotación acumulada: {rotacionAcumulada}°");
        Debug.Log($"[Carro] Offset original: {offsetOriginal}");
        Debug.Log($"[Carro] Offset rotado: {offsetRotado}");
        Debug.Log($"[Carro] Objetivo: {posObjetivo}");

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

        Debug.Log($"[Carro] Llegó a {destino.name}. Posición: {transform.position}");
    }

    private IEnumerator GirarCarroSobrePunto(float anguloObjetivo)
    {
        // Usar puntoGiro como pivote de la rotación (gira sobre su eje)
        Vector3 puntoPivote = puntoGiro.position;

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

        // Asegurar posición y rotación final exacta
        Vector3 offsetFinal = rotacionFinalCarro * Quaternion.Inverse(rotacionInicialCarro) * offsetCarroAlPivote;
        transform.position = puntoPivote + offsetFinal;
        transform.rotation = rotacionFinalCarro;

        ActualizarPosicionBrazo();

        // Acumular la rotación para usarla en tramos posteriores
        rotacionAcumulada += anguloObjetivo;

        Debug.Log($"[Carro] Giró {anguloObjetivo}°. Rotación acumulada: {rotacionAcumulada}°");
        Debug.Log($"[Carro] Posición carro: {transform.position}, Rotación: {transform.eulerAngles}");
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

    // Métodos públicos para girar desde otros scripts
    public void Girar90Grados()
    {
        StartCoroutine(GirarCarroSobrePunto(90f));
    }

    public void GirarNegativo90Grados()
    {
        StartCoroutine(GirarCarroSobrePunto(-90f));
    }

    // Visualización en editor
    private void OnDrawGizmos()
    {
        if (puntoInicio != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(puntoInicio.position, 0.2f);

            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, puntoInicio.position);
            }
        }

        if (puntoGiro != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(puntoGiro.position, 0.2f);
        }

        if (punto1_1 != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(punto1_1.position, 0.2f);
        }
    }
}