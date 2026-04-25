using UnityEngine;

public class EnsambleGri : MonoBehaviour
{
    [Header("Configuración de ensamble")]
    public Transform puntoEnsamble;
    public float offsetHundimiento = 0f;
    public float velocidadEncaje = 2f;
    public float distanciaActivacion = 0.15f;

    [Header("Configuración de rotación para hélices")]
    public bool esHelice = false;
    public bool forzarRotacionAbsoluta = true;
    public Vector3 rotacionForzada = new Vector3(-90f, 0f, 0f);

    [Header("Rotación específica por hélice")]
    public bool usarRotacionPorNumero = true;

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
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        string nombreLimpio = LimpiarNombre(gameObject.name);

        if (nombreLimpio.Contains("Helice") || nombreLimpio.Contains("Hélice"))
        {
            esHelice = true;

            if (usarRotacionPorNumero)
                ConfigurarRotacionPorNumero(nombreLimpio);
        }
    }

    string LimpiarNombre(string nombre)
    {
        return nombre.Replace("(Clone)", "").Trim();
    }

    void ConfigurarRotacionPorNumero(string nombreLimpio)
    {
        if (nombreLimpio.Contains("Helice1") || nombreLimpio.Contains("Hélice1"))
            rotacionForzada = new Vector3(90f, 0f, 0f);
        else if (nombreLimpio.Contains("Helice2") || nombreLimpio.Contains("Hélice2"))
            rotacionForzada = new Vector3(-90f, 90f, 0f);
        else if (nombreLimpio.Contains("Helice3") || nombreLimpio.Contains("Hélice3"))
            rotacionForzada = new Vector3(-90f, 180f, 0f);
        else if (nombreLimpio.Contains("Helice4") || nombreLimpio.Contains("Hélice4"))
            rotacionForzada = new Vector3(90f, 270f, 0f);
    }

    public void NotificarLiberad(Collider[] ignorar)
    {
        fueLiberad = true;
        collidersAIgnorar = ignorar;

        // Kinematic inmediato para que no empuje la base ni un solo frame
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

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
                IniciarEncaje();
        }

        if (encajando && !encajado)
        {
            // MoveTowards: velocidad constante, independiente del framerate
            transform.position = Vector3.MoveTowards(
                transform.position,
                posicionFinal,
                Time.deltaTime * velocidadEncaje
            );

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                rotacionObjetivo,
                Time.deltaTime * 180f
            );

            if (Vector3.Distance(transform.position, posicionFinal) < 0.0005f)
            {
                transform.position = posicionFinal;
                transform.rotation = rotacionObjetivo;

                encajado = true;
                encajando = false;

                if (collidersAIgnorar != null)
                    foreach (var c in collidersAIgnorar)
                        if (c != null) Physics.IgnoreCollision(col, c, false);

                if (baseParent != null)
                    transform.SetParent(baseParent);

                Debug.Log($"✅ Ensamblado: {gameObject.name}");
            }
        }
    }

    void IniciarEncaje()
    {
        encajando = true;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (col != null) col.enabled = false;

        if (esHelice && forzarRotacionAbsoluta)
            rotacionObjetivo = Quaternion.Euler(rotacionForzada);
        else
            rotacionObjetivo = puntoEnsamble.rotation;

        posicionFinal = puntoEnsamble.position + Vector3.up * offsetHundimiento;
    }
}