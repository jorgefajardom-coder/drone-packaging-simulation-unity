using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject prefab;
    public Transform puntoEnsamble;
    public Transform baseParent; // ← Produccion asigna esto antes de llamar Spawn()

    public GameObject Spawn()
    {
        GameObject pieza = Instantiate(prefab, transform.position, transform.rotation);

        Ensamble encaje = pieza.GetComponent<Ensamble>();
        if (encaje != null)
        {
            encaje.puntoEnsamble = puntoEnsamble;
            if (baseParent != null)
                encaje.AsignarBase(baseParent); // ← asignar base correcta
        }

        EnsambleGri encajeGri = pieza.GetComponent<EnsambleGri>();
        if (encajeGri != null)
        {
            if (baseParent != null)
                encajeGri.baseParent = baseParent;
            else
                encajeGri.baseParent = GameObject.Find("BasePrefab(Clone)").transform;
        }

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