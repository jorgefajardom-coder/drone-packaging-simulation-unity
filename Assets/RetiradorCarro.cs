using System.Collections;
using UnityEngine;

public class RetiradorCarro : MonoBehaviour
{
    [Header("Cajas que este carro debe adoptar")]
    [Tooltip("Números de caja que este carro debe arrastrar al retirarse. Ej: [1,2,3,4] para carro 1, [5,6,7,8] para carro 2")]
    public int[] cajasAsignadas;

    [Tooltip("Número de la última caja de este carro. Se comprueba su CerradorTapa.tapaCerrada antes de adoptar.")]
    public int numeroCajaFinal = 4;

    [Header("Nomenclatura de cajas")]
    public string prefijoCaja = "CajaPrefab(Clone";
    public string sufijoCaja = ")";

    [Header("Estado")]
    [Tooltip("Solo lectura. Indica si ya se adoptaron las cajas.")]
    public bool cajasAdoptadas = false;

    /// <summary>
    /// Intenta adoptar las cajas asignadas. Solo procede si la última caja tiene la tapa cerrada.
    /// Puede llamarse desde el script de movimiento del carro cuando llegue al punto de retiro.
    /// </summary>
    public void IntentarAdoptarCajas()
    {
        if (cajasAdoptadas)
        {
            Debug.LogWarning($"[{gameObject.name}] Las cajas ya fueron adoptadas.");
            return;
        }

        // Verificar que la caja final tenga la tapa cerrada
        string nombreCajaFinal = prefijoCaja + numeroCajaFinal + sufijoCaja;
        GameObject cajaFinal = GameObject.Find(nombreCajaFinal);

        if (cajaFinal == null)
        {
            Debug.LogError($"❌ [{gameObject.name}] No se encontró la caja final '{nombreCajaFinal}'.");
            return;
        }

        CerradorTapa cerrador = cajaFinal.GetComponent<CerradorTapa>();
        if (cerrador == null)
        {
            Debug.LogError($"❌ [{gameObject.name}] '{nombreCajaFinal}' no tiene CerradorTapa.");
            return;
        }

        if (!cerrador.tapaCerrada)
        {
            Debug.Log($"⏳ [{gameObject.name}] Esperando cierre de tapa de '{nombreCajaFinal}'. Reintentando...");
            StartCoroutine(EsperarYAdoptar(cerrador));
            return;
        }

        // Todo listo: adoptar
        AdoptarCajas();
    }

    IEnumerator EsperarYAdoptar(CerradorTapa cerrador)
    {
        // Espera hasta que la tapa esté cerrada
        yield return new WaitUntil(() => cerrador.tapaCerrada);

        // Un pequeño colchón para que termine bien la animación visual antes de emparentar
        yield return new WaitForSeconds(0.3f);

        AdoptarCajas();
    }

    [ContextMenu("Adoptar cajas (forzar)")]
    public void AdoptarCajas()
    {
        foreach (int n in cajasAsignadas)
        {
            string nombre = prefijoCaja + n + sufijoCaja;
            GameObject caja = GameObject.Find(nombre);
            if (caja != null)
            {
                caja.transform.SetParent(transform, true);
                Debug.Log($"📦 {nombre} adoptada por {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"⚠ No se encontró '{nombre}' para adoptar.");
            }
        }

        cajasAdoptadas = true;
        Debug.Log($"✔ [{gameObject.name}] Todas las cajas adoptadas.");
    }
}