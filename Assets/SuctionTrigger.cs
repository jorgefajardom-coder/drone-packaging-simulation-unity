using UnityEngine;

public class SuctionTrigger : MonoBehaviour
{
    public Ventosa mainScript;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pickable"))
        {
            Debug.Log($"🔵 SuctionTrigger: Objeto {other.name} entró en el área de succión");
            mainScript.NotifyObjectInside(other.gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Pickable"))
        {
            Debug.Log($"🔵 SuctionTrigger: Objeto {other.name} salió del área de succión");
            mainScript.NotifyObjectExit(other.gameObject);
        }
    }
}