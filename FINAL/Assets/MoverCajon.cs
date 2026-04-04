using UnityEngine;

public class MoverCajon : MonoBehaviour
{
    public Transform[] puntos;
    public float velocidad = 2f;

    private int destinoActual = 0;

    void Update()
    {
        if (destinoActual >= puntos.Length)
            return;

        Transform objetivo = puntos[destinoActual];

        transform.position = Vector3.MoveTowards(
            transform.position,
            objetivo.position,
            velocidad * Time.deltaTime
        );

        transform.LookAt(objetivo);

        if (Vector3.Distance(transform.position, objetivo.position) < 0.1f)
        {
            destinoActual++;
        }
    }
}