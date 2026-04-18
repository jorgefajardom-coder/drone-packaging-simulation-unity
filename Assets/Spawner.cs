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

        // ✅ NUEVO: conectar HingeJoint de la tapa al Rigidbody de la caja
        HingeJoint hinge = pieza.GetComponentInChildren<HingeJoint>();
        if (hinge != null)
        {
            Rigidbody rbCaja = pieza.GetComponent<Rigidbody>();
            if (rbCaja != null)
                hinge.connectedBody = rbCaja;
        }

        return pieza;
    }
}