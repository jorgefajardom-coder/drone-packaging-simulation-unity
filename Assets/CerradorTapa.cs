using System.Collections;
using UnityEngine;

public class CerradorTapa : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform de la tapa que se va a animar (típicamente Tapa_Pivot si tienes un pivote separado)")]
    public Transform tapa;

    [Header("Rotación de la animación")]
    [Tooltip("Rotación local cuando la tapa está ABIERTA (pose inicial)")]
    public Vector3 rotacionAbierta = new Vector3(80f, 0f, 0f);

    [Tooltip("Rotación local cuando la tapa está CERRADA (pose final)")]
    public Vector3 rotacionCerrada = Vector3.zero;

    [Header("Tiempo y suavizado")]
    [Tooltip("Duración total del cierre en segundos")]
    public float duracionCierre = 1f;

    [Tooltip("Curva de animación. Por defecto suaviza inicio y final (ease in/out)")]
    public AnimationCurve curva = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Estado")]
    [Tooltip("Solo lectura — indica si la tapa ya se cerró")]
    public bool tapaCerrada = false;

    private bool animando = false;

    void Start()
    {
        // Asegura que la tapa empiece en la pose abierta
        if (tapa != null)
            tapa.localRotation = Quaternion.Euler(rotacionAbierta);
    }

    /// <summary>
    /// Inicia la animación de cierre. Puede llamarse desde cualquier script.
    /// </summary>
    [ContextMenu("Cerrar tapa (animación)")]
    public void CerrarTapa()
    {
        if (animando)
        {
            Debug.LogWarning($"[{gameObject.name}] Ya hay una animación en curso.");
            return;
        }
        if (tapaCerrada)
        {
            Debug.LogWarning($"[{gameObject.name}] La tapa ya está cerrada.");
            return;
        }
        if (tapa == null)
        {
            Debug.LogError($"[{gameObject.name}] No hay referencia a 'tapa' asignada.");
            return;
        }

        StartCoroutine(AnimarCierre());
    }

    /// <summary>
    /// Reinicia la tapa a la pose abierta (útil para testing o reutilizar la caja).
    /// </summary>
    [ContextMenu("Abrir tapa (reset instantáneo)")]
    public void AbrirTapaInstantaneo()
    {
        if (tapa != null)
        {
            StopAllCoroutines();
            animando = false;
            tapa.localRotation = Quaternion.Euler(rotacionAbierta);
            tapaCerrada = false;
            Debug.Log($"[{gameObject.name}] Tapa reabierta.");
        }
    }

    private IEnumerator AnimarCierre()
    {
        animando = true;

        Quaternion rotInicio = Quaternion.Euler(rotacionAbierta);
        Quaternion rotFin = Quaternion.Euler(rotacionCerrada);

        float tiempo = 0f;
        while (tiempo < duracionCierre)
        {
            float t = tiempo / duracionCierre;
            float tCurva = curva.Evaluate(t);
            tapa.localRotation = Quaternion.Slerp(rotInicio, rotFin, tCurva);

            tiempo += Time.deltaTime;
            yield return null;
        }

        tapa.localRotation = rotFin;
        tapaCerrada = true;
        animando = false;

        Debug.Log($"✔ [{gameObject.name}] Tapa cerrada.");
    }
}