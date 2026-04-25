using UnityEngine;

// Adjuntar al GameObject BasePrefab (la base instanciada)
public class DronListo : MonoBehaviour
{
    [Tooltip("Se activa cuando el dron está listo para ser levantado por Omega")]
    public bool dronesListo = false;

    [Tooltip("Cantidad total de piezas que debe tener el dron ensamblado antes de poder levantarse (PCB + 4 motores + tapa + 4 hélices = 10)")]
    public int piezasEsperadas = 10;

    [Tooltip("Referencia al collider/trigger de la ventosa de Omega. Asignar en Inspector.")]
    public Collider ventosaOmega;

    private bool yaSellado = false;

    void Update()
    {
        // Auto-detección: si ya hay suficientes piezas hijas y no está sellado, preparar
        if (!yaSellado && !dronesListo)
        {
            int piezasActuales = ContarPiezasEnsambladas();
            if (piezasActuales >= piezasEsperadas)
                PrepararParaLevantamiento();
        }
    }

    int ContarPiezasEnsambladas()
    {
        // Cuenta los hijos directos con Rigidbody (excluyendo la propia base)
        int count = 0;
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            if (rb.gameObject == this.gameObject) continue;
            count++;
        }
        return count;
    }

    public void PrepararParaLevantamiento()
    {
        if (yaSellado) return;
        yaSellado = true;
        dronesListo = true;

        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            if (rb.gameObject == this.gameObject) continue;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Debug.Log("🚁 Dron completo y sellado. Listo para levantamiento.");
    }

    public void SoltarDron()
    {
        dronesListo = false;
        yaSellado = false;

        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            if (rb.gameObject == this.gameObject) continue;
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        Debug.Log("📦 Dron soltado, física restaurada.");
    }
}