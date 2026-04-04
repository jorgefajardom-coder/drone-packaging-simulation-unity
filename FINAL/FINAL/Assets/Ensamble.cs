using UnityEngine;
public class Ensamble : MonoBehaviour
{
    [Header("Configuración de ensamble")]
    public Transform puntoEnsamble;          // Se asigna desde el Spawner automáticamente
    public float offsetHundimiento = -0.05f; // Cuánto se hunde (negativo = baja)
    public float velocidadEncaje = 3f;       // Qué tan rápido baja al encajar
    private Rigidbody rb;
    private Collider col;
    private bool encajando = false;
    private bool encajado = false;
    private bool fueLiberad = false;
    private Vector3 posicionFinal;
    private Quaternion rotacionAlLiberar;
    private Transform baseParent; // ← esta línea
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }
    // Llamado desde Ventosa.cs al soltar la pieza
    public void NotificarLiberad()
    {
        fueLiberad = true;
        Debug.Log("📦 PCB liberada, lista para encajar.");
    }
    void OnCollisionEnter(Collision collision)
    {
        // Debug temporal para ver con qué choca
        Debug.Log($"💥 Colisión con: '{collision.gameObject.name}' | fueLiberad: {fueLiberad} | encajado: {encajado}");
        if (!encajado && fueLiberad && collision.gameObject.name.Contains("BasePrefab"))
        {
            baseParent = collision.gameObject.transform;
            IniciarEncaje();
        }
    }
    void IniciarEncaje()
    {
        encajando = true;
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        if (col != null) col.enabled = false;
        // ✅ Guarda la rotación exacta en que fue soltada
        rotacionAlLiberar = transform.rotation;
        posicionFinal = puntoEnsamble.position + new Vector3(0, offsetHundimiento, 0);
        Debug.Log($"🔧 Iniciando encaje hacia: {posicionFinal}");
    }
    void Update()
    {
        if (encajando && !encajado)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                posicionFinal,
                Time.deltaTime * velocidadEncaje
            );
            // ✅ Mantiene la rotación con la que fue soltada, sin voltearla
            transform.rotation = rotacionAlLiberar;
            if (Vector3.Distance(transform.position, posicionFinal) < 0.001f)
            {
                transform.position = posicionFinal;
                encajado = true;
                encajando = false;
                if (col != null) col.enabled = true;
                transform.SetParent(baseParent);
                Debug.Log("✅ PCB ensamblada correctamente.");
            }
        }
    }
}