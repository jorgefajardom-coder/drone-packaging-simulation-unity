using System.Collections;
using UnityEngine;

public class CerradorTapa : MonoBehaviour
{
    [Header("Referencias")]
    public Transform tapa;

    [Header("Rotación de la animación")]
    public Vector3 rotacionAbierta = new Vector3(80f, 0f, 0f);
    public Vector3 rotacionCerrada = Vector3.zero;

    [Header("Tiempo y suavizado")]
    public float duracionCierre = 1f;
    public AnimationCurve curva = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Estado")]
    public bool tapaCerrada = false;

    private bool animando = false;

    void Start()
    {
        if (tapa != null)
            tapa.localRotation = Quaternion.Euler(rotacionAbierta);
    }

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

        // ← AGREGAR: destruir el dron dentro de la caja para liberar memoria
        yield return new WaitForSeconds(0.5f); // pequeña pausa para q la tapa se vea cerrada

        Transform dron = transform.Find("BasePrefab(Clone)");
        if (dron != null)
        {
            Destroy(dron.gameObject);
        }
    }
}