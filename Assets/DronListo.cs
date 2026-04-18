using UnityEngine;

// Adjuntar al GameObject BasePrefab
public class DronListo : MonoBehaviour
{
    [Tooltip("Se activa cuando el dron está listo para ser levantado por Omega")]
    public bool dronesListo = false;

    // Llamar desde el Orquestador cuando se llega a la etapa de levantamiento
    public void PrepararParaLevantamiento()
    {
        dronesListo = true;

        // Recorre TODOS los hijos y desactiva su física individual
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            if (rb.gameObject == this.gameObject) continue; // saltar la base
            rb.isKinematic = true;
            rb.useGravity = false;
            Debug.Log($"🔒 Pieza bloqueada para levantamiento: {rb.gameObject.name}");
        }

        Debug.Log("🚁 Dron listo para ser levantado como una unidad.");
    }

    // Llamar al soltar el dron en el área de paletizado
    public void SoltarDron()
    {
        dronesListo = false;

        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            if (rb.gameObject == this.gameObject) continue;
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        Debug.Log("📦 Dron soltado, física restaurada.");
    }
}