using UnityEngine;

public class Ensamble : MonoBehaviour
{
    [Header("Configuración de ensamble")]
    public Transform puntoEnsamble;
    public float offsetHundimiento = -0.05f;
    public float velocidadEncaje = 6f;

    [Header("Modo de activación del encaje")]
    [Tooltip("Activar para piezas que van ENCIMA (Tapa). Desactivar para piezas que caen (PCB).")]
    public bool snapPorProximidad = false;

    [Tooltip("Solo si snapPorProximidad=true. Distancia al puntoEnsamble para activar el snap.")]
    public float distanciaActivacionSnap = 0.15f;

    [Tooltip("Activar para piezas que van ENCIMA: congela gravedad al soltarse para evitar caída brusca.")]
    public bool congelarAlLiberar = false;

    public bool yaEnsamblado = false;

    private Rigidbody rb;
    private Collider col;
    private bool encajando = false;
    private bool encajado = false;
    private bool fueLiberad = false;
    private Vector3 posicionFinal;
    private Quaternion rotacionAlLiberar;
    private Transform baseParent;

    [Header("Rotación al ser agarrado por ventosa")]
    public Vector3 rotacionAlAgarrar = Vector3.zero;

    [Header("Rotación final de ensamble")]
    public Vector3 rotacionFinalEnsamble = new Vector3(-90f, 0f, 180f);

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    // Llamado desde Ventosa.cs al soltar la pieza
    public void NotificarLiberad()
    {
        fueLiberad = true;

        // Modo congelar: la pieza va ENCIMA (tapa), no cae por gravedad — 
        // se congela en el aire y hace snap directo al puntoEnsamble
        if (congelarAlLiberar && rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;

            if (col != null) col.enabled = false;

            posicionFinal = puntoEnsamble.position + puntoEnsamble.up * offsetHundimiento;
            baseParent = GameObject.Find("BasePrefab(Clone)")?.transform;
            encajando = true;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Modos que no usan colisión como disparador
        if (snapPorProximidad || congelarAlLiberar) return;

        if (!encajado && fueLiberad && collision.gameObject.name.Contains("BasePrefab"))
        {
            baseParent = collision.gameObject.transform;
            IniciarEncaje();
        }
    }

    void IniciarEncaje()
    {
        if (encajando || encajado) return;

        encajando = true;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (!snapPorProximidad && col != null)
            col.enabled = false;

        rotacionAlLiberar = transform.rotation;
        posicionFinal = puntoEnsamble.position + puntoEnsamble.up * offsetHundimiento;
    }

    void Update()
    {
        // Modo proximidad: vigilar distancia al punto de ensamble
        if (snapPorProximidad && fueLiberad && !encajando && !encajado && puntoEnsamble != null)
        {
            float dist = Vector3.Distance(transform.position, puntoEnsamble.position);
            if (dist <= distanciaActivacionSnap)
            {
                GameObject baseObj = GameObject.Find("BasePrefab");
                if (baseObj != null)
                    baseParent = baseObj.transform;

                IniciarEncaje();
            }
        }

        // Movimiento de encaje (igual para todos los modos)
        if (encajando && !encajado)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                posicionFinal,
                Time.deltaTime * velocidadEncaje
            );

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.Euler(rotacionFinalEnsamble),
                Time.deltaTime * velocidadEncaje
            );

            if (Vector3.Distance(transform.position, posicionFinal) < 0.001f)
            {
                transform.position = posicionFinal;
                transform.rotation = Quaternion.Euler(rotacionFinalEnsamble);

                encajado = true;
                encajando = false;
                yaEnsamblado = true;

                // Reactivar collider para que la pieza sea sólida de nuevo
                if (col != null) col.enabled = true;

                if (baseParent != null)
                    transform.SetParent(baseParent);

                Debug.Log($"✅ Ensamblada: {gameObject.name}");
            }
        }
    }
}