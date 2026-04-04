using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject prefab;
    public Transform puntoEnsamble; // ← asigna el PuntoEnsamble desde el Inspector

    public GameObject Spawn()
    {
        GameObject pieza = Instantiate(prefab, transform.position, transform.rotation);

        // ✅ PCB
        Ensamble encaje = pieza.GetComponent<Ensamble>();
        if (encaje != null)
            encaje.puntoEnsamble = puntoEnsamble;

        // ✅ Motor 
        EnsambleGri encajeGri = pieza.GetComponent<EnsambleGri>();
        if (encajeGri != null)
            encajeGri.baseParent = GameObject.Find("BasePrefab(Clone)").transform;

        return pieza;
    }
}