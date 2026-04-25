using UnityEngine;

public class SuctionTrigger : MonoBehaviour
{
    public Ventosa mainScript;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pickable"))
            mainScript.NotifyObjectInside(other.gameObject);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Pickable"))
            mainScript.NotifyObjectExit(other.gameObject);
    }
}