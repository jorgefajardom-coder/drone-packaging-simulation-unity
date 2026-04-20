using UnityEngine;

public class CentrarBase : MonoBehaviour
{
    [Header("Punto destino")]
    public Transform puntoDestino;

    [Tooltip("Rotación final. Si la base debe quedar horizontal prueba X=-90")]
    public Vector3 rotacionFija = new Vector3(-90f, 0f, 0f);

    [Header("Estado (solo lectura)")]
    [SerializeField] private bool cayendo = false;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (!cayendo || rb == null) return;

        // Mantener XZ fijo al destino, Y libre para gravedad
        rb.MovePosition(new Vector3(
            puntoDestino.position.x,
            rb.position.y,
            puntoDestino.position.z
        ));

        rb.MoveRotation(Quaternion.Euler(rotacionFija));
    }

    public void IniciarCentrado()
    {
        if (puntoDestino == null)
        {
            Debug.LogWarning("[CentrarBase] Asigna el puntoDestino.");
            return;
        }

        // Aplicar posición XZ y rotación ANTES de activar física
        transform.position = new Vector3(
            puntoDestino.position.x,
            transform.position.y,
            puntoDestino.position.z
        );
        transform.rotation = Quaternion.Euler(rotacionFija);

        // Activar física solo en Y, la rotación se maneja con MoveRotation
        rb.constraints = RigidbodyConstraints.None;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        cayendo = true;
        Debug.Log("[CentrarBase] Base cayendo hacia destino.");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!cayendo) return;
        cayendo = false;

        // Fijar posición y rotación exactas al aterrizar
        transform.position = new Vector3(
            puntoDestino.position.x,
            transform.position.y,
            puntoDestino.position.z
        );
        transform.rotation = Quaternion.Euler(rotacionFija);

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        Debug.Log("[CentrarBase] Base posada en destino.");
    }

    void OnDrawGizmos()
    {
        if (puntoDestino == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(puntoDestino.position, 0.05f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, puntoDestino.position);
    }
}