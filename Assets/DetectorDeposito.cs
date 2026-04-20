using UnityEngine;

public class DetectorDeposito : MonoBehaviour
{
    public Transform PuntoActivo { get; private set; }
    public Transform CajaActiva { get; private set; }

    void OnTriggerEnter(Collider other)
    {
        // Limpiar el nombre de (Clone) y comparar
        string cleanName = other.name;
        if (cleanName.Contains("(Clone)"))
            cleanName = cleanName.Replace("(Clone)", "");

        Debug.Log($"🔍 Algo entró: '{other.name}' -> limpio: '{cleanName}'");

        if (cleanName == "PuntoDepositoDron")
        {
            PuntoActivo = other.transform;
            CajaActiva = other.transform.parent;
            Debug.Log($"✅ Punto activo: {PuntoActivo.name} en {CajaActiva.name}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"🔍 Algo salió: '{other.name}'");
    }

    public void LimpiarPuntoActivo()
    {
        PuntoActivo = null;
        CajaActiva = null;
    }
}