using UnityEngine;

public class GripperTrigger : MonoBehaviour
{
    public Brazos mainScript;  // Cambiado de Cubo a Brazos

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pickable"))
        {
            mainScript.NotifyObjectInside(other.gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Pickable"))
        {
            mainScript.NotifyObjectExit(other.gameObject);
        }
    }
}