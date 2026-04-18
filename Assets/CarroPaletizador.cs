using System.Collections;
using UnityEngine;

public class CarroPaletizador : MonoBehaviour
{
    [Header("Puntos de la escena")]
    public Transform puntoInicio;
    public Transform puntoGiro;
    public Transform punto_1_1;
    public Transform punto_1_2;
    public Transform punto_2_1;
    public Transform punto_2_2;

    [Header("Brazo paletizador (hijo del carro)")]
    public Ventosa ventosa;

    [Header("Velocidad")]
    public float velocidadMovimiento = 2f;
    public float velocidadRotacion = 90f;

    [Header("Tolerancia")]
    public float tolPos = 0.02f;
    public float tolAng = 0.5f;

    // ───────────────────────────────────────────────────────────

    private void Start()
    {
        StartCoroutine(SecuenciaCompleta());
    }

    private IEnumerator SecuenciaCompleta()
    {
        // ── CARRO 1 ────────────────────────────────────────────
        // Punto 1.1 → 2 drones
        yield return EsperarAgarreYNavegar(punto_1_1);   // espera agarre, viaja
        yield return SoltarYEsperar();                   // suelta, espera liberación

        yield return EsperarAgarreYSoltar();             // 2do dron: ya está en punto, solo espera y suelta

        // Punto 1.2 → 2 drones
        yield return EsperarAgarreYNavegar(punto_1_2);
        yield return SoltarYEsperar();

        yield return EsperarAgarreYSoltar();

        // ── CARRO 2 ────────────────────────────────────────────
        yield return EsperarAgarreYNavegar(punto_2_1);
        yield return SoltarYEsperar();

        yield return EsperarAgarreYSoltar();

        yield return EsperarAgarreYNavegar(punto_2_2);
        yield return SoltarYEsperar();

        yield return EsperarAgarreYSoltar();

        // ── Regreso ────────────────────────────────────────────
        yield return IrA(puntoInicio);
        Debug.Log("[Carro] Paletizado completo.");
    }

    // ── Espera que el brazo agarre → viaja a Giro → viaja a destino ──

    private IEnumerator EsperarAgarreYNavegar(Transform destino)
    {
        // 1. Esperar agarre del dron
        yield return new WaitUntil(() => ventosa.TieneObjeto);
        Debug.Log($"[Carro] Dron agarrado → navegando a {destino.name}");

        // 2. Ir a Giro (rota hacia el destino)
        yield return IrA(puntoGiro);

        // 3. Ir al punto de entrega
        yield return IrA(destino);
    }

    // ── Suelta y espera confirmación ─────────────────────────────────

    private IEnumerator SoltarYEsperar()
    {
        ventosa.LiberarObjeto();
        yield return new WaitUntil(() => !ventosa.TieneObjeto);
        yield return new WaitForSeconds(0.3f);
        Debug.Log("[Carro] Dron depositado.");
    }

    // ── 2do dron: ya está en posición, solo espera agarre y suelta ───

    private IEnumerator EsperarAgarreYSoltar()
    {
        yield return new WaitUntil(() => ventosa.TieneObjeto);
        Debug.Log("[Carro] 2do dron agarrado → soltando en posición actual.");
        yield return SoltarYEsperar();
    }

    // ── Movimiento: traslada → rota ──────────────────────────────────

    private IEnumerator IrA(Transform destino)
    {
        // Posición objetivo (solo X y Z, manteniendo Y actual)
        Vector3 posObjetivo = new Vector3(destino.position.x,
                                           transform.position.y,
                                           destino.position.z);

        // Calcular la dirección hacia el destino (solo en el plano XZ)
        Vector3 direccionDestino = destino.position - transform.position;
        direccionDestino.y = 0; // Ignorar diferencia en Y

        // Calcular el ángulo de rotación necesario (solo Y)
        float anguloObjetivo = Quaternion.LookRotation(direccionDestino).eulerAngles.y;

        // Si el destino está exactamente en la misma posición, mantener rotación actual
        if (direccionDestino.magnitude < 0.01f)
            anguloObjetivo = transform.eulerAngles.y;

        Quaternion rotObjetivo = Quaternion.Euler(0f, anguloObjetivo, 0f);

        // 1. Moverse hasta la posición exacta
        while (Vector3.Distance(transform.position, posObjetivo) > tolPos)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, posObjetivo,
                velocidadMovimiento * Time.deltaTime);
            yield return null;
        }
        transform.position = posObjetivo;

        // 2. Rotar sobre su propio eje Y en el lugar
        while (Quaternion.Angle(transform.rotation, rotObjetivo) > tolAng)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, rotObjetivo,
                velocidadRotacion * Time.deltaTime);
            yield return null;
        }
        transform.rotation = rotObjetivo;
    }

    // ── Gizmos ───────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Transform[] rutaC1 = { puntoInicio, puntoGiro, punto_1_1, puntoGiro, punto_1_2 };
        Transform[] rutaC2 = { punto_1_2, puntoGiro, punto_2_1, puntoGiro, punto_2_2, puntoInicio };

        Gizmos.color = Color.cyan;
        for (int i = 0; i < rutaC1.Length - 1; i++)
            if (rutaC1[i] && rutaC1[i + 1])
                Gizmos.DrawLine(rutaC1[i].position, rutaC1[i + 1].position);

        Gizmos.color = Color.yellow;
        for (int i = 0; i < rutaC2.Length - 1; i++)
            if (rutaC2[i] && rutaC2[i + 1])
                Gizmos.DrawLine(rutaC2[i].position, rutaC2[i + 1].position);

        foreach (var t in new[] { puntoInicio, puntoGiro, punto_1_1, punto_1_2, punto_2_1, punto_2_2 })
        {
            if (t == null) continue;
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(t.position, 0.07f);
        }
    }
}