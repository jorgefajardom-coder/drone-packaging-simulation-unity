using UnityEngine;

public class EnsambleGri : MonoBehaviour
{
    [Header("Configuración de ensamble")]
    public Transform puntoEnsamble;
    public float offsetHundimiento = 0f;
    public float velocidadEncaje = 5f;
    public float distanciaActivacion = 0.15f;

    private Rigidbody rb;
    private Collider col;
    private bool encajando = false;
    private bool encajado = false;
    private bool fueLiberad = false;
    private Vector3 posicionFinal;
    private Quaternion rotacionObjetivo;
    public Transform baseParent;

    private Collider[] collidersAIgnorar;

    void Start()
    {
        rb = GetComponent<Collider>().attachedRigidbody;
        col = GetComponent<Collider>();
    }

    public void NotificarLiberad(Collider[] ignorar)
    {
        fueLiberad = true;
        collidersAIgnorar = ignorar;

        if (collidersAIgnorar != null)
        {
            foreach (var c in collidersAIgnorar)
            {
                if (c != null)
                    Physics.IgnoreCollision(col, c, true);
            }
        }
    }

    void Update()
    {
        if (fueLiberad && !encajado && !encajando && puntoEnsamble != null)
        {
            float dist = Vector3.Distance(transform.position, puntoEnsamble.position);
            if (dist < distanciaActivacion)
            {
                IniciarEncaje();
            }
        }

        if (encajando && !encajado)
        {
            // Interpola posición hacia el punto de ensamble
            transform.position = Vector3.Lerp(
                transform.position,
                posicionFinal,
                Time.deltaTime * velocidadEncaje
            );

            // ✅ Interpola rotación hacia la rotación del punto de ensamble
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                rotacionObjetivo,
                Time.deltaTime * velocidadEncaje
            );

            if (Vector3.Distance(transform.position, posicionFinal) < 0.001f)
            {
                transform.position = posicionFinal;
                transform.rotation = rotacionObjetivo; // fija la rotación final exacta

                encajado = true;
                encajando = false;

                if (collidersAIgnorar != null)
                {
                    foreach (var c in collidersAIgnorar)
                    {
                        if (c != null)
                            Physics.IgnoreCollision(col, c, false);
                    }
                }

                if (col != null) col.enabled = true;

                if (baseParent != null)
                    transform.SetParent(baseParent);

                Debug.Log($"✅ Motor ensamblado: {gameObject.name}");
            }
        }
    }

    void IniciarEncaje()
    {
        encajando = true;

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (col != null) col.enabled = false;

        // ✅ Usa la rotación del puntoEnsamble como objetivo (no la rotación actual del objeto)
        rotacionObjetivo = puntoEnsamble.rotation;

        posicionFinal = puntoEnsamble.position + new Vector3(0, offsetHundimiento, 0);
        Debug.Log($"🔧 Encajando motor hacia: {posicionFinal}");
    }
}