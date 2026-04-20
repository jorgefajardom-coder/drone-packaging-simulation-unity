using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Ventosa : MonoBehaviour
{
    [Header("Articulaciones principales")]
    public ArticulationBody Waist;
    public ArticulationBody Arm01;
    public ArticulationBody Arm02;
    public ArticulationBody Arm03;
    public ArticulationBody GripperAssembly;

    [Header("Ángulos que controlas por código")]
    public float waistTarget = 0f;
    public float arm01Target = 0f;
    public float arm02Target = 0f;
    public float arm03Target = 0f;
    public float gripAssemblyTarget = 0f;

    [Header("Control de succión")]
    public bool suctionActive = false;
    public float suctionForce = 10f;

    [Header("Velocidad de movimiento (°/s)")]
    public float speed = 60f;

    [Header("Tiempo de espera inicial (segundos)")]
    public float tiempoEsperaInicial = 0f;

    [Header("Posiciones guardadas (se persisten en archivo)")]
    public List<VentosaPose> poses = new List<VentosaPose>();

    [Header("Agarre por Trigger")]
    public Transform gripPoint;
    public Collider suctionTrigger;

    [Header("Punto de destino para soltar")]
    public Transform puntoDestinoDron;

    [Header("Búsqueda dinámica del destino (para Paletizador)")]
    [Tooltip("Detector de zona de depósito. Si está asignado, se usa su PuntoActivo en vez de puntoDestinoDron")]
    public DetectorDeposito detectorDeposito;

    [Tooltip("Si está activo, el dron se vuelve hijo de la caja al soltarlo (viaja con el carro)")]
    public bool emparentarACaja = false;

    [Header("Configuración de liberación")]
    public float alturaLiberacion = 0.02f;
    public LayerMask capaBanda = 1;

    [Header("Rotación fija del objeto al agarrar")]
    public Vector3 rotacionFijaAlAgarrar = new Vector3(90f, 0f, 0f);

    private GameObject objectInside;
    private GameObject grabbedObject;
    public bool TieneObjeto => grabbedObject != null;
    private Vector3 originalScale;
    private Transform originalParent;
    private Rigidbody originalRigidbody;
    private bool wasKinematic;
    private bool usedGravity;
    private bool liberandoObjeto = false;

    private Vector3 grabLocalOffset;
    private Quaternion grabLocalRotOffset;

    private int currentPoseIndex = 0;
    public bool jugandoSecuencia = false;
    private bool esperandoInicio = false;
    private float tiempoEsperaInicialTimer = 0f;

    private bool esperandoPose = false;
    private float timerPose = 0f;

    [Header("Opciones de reproducción / persistencia")]
    public bool autoStartOnPlay = false;
    public string saveFileName = "poses_ventosa.json";

    [Header("Caída simulada al soltar dron")]
    [Tooltip("Altura inicial sobre el puntoDestinoDron desde la que empieza la caída visual")]
    public float alturaCaidaSimulada = 0.15f;

    [Tooltip("Duración de la caída visual en segundos")]
    public float duracionCaidaSimulada = 0.5f;

    void Awake()
    {
        LoadFromFile();
    }

    void Start()
    {
        if (autoStartOnPlay && poses.Count > 0)
        {
            if (tiempoEsperaInicial > 0)
            {
                esperandoInicio = true;
                tiempoEsperaInicialTimer = 0f;
            }
            else
            {
                IniciarSecuencia();
            }
        }
    }

    void OnApplicationQuit()
    {
        SaveToFile();
    }

    void Update()
    {
        if (esperandoInicio)
        {
            tiempoEsperaInicialTimer += Time.deltaTime;
            if (tiempoEsperaInicialTimer >= tiempoEsperaInicial)
            {
                esperandoInicio = false;
                IniciarSecuencia();
            }
            return;
        }

        if (!jugandoSecuencia)
            MoverManual();
        else
            ReproducirSecuencia();

        ProcesarSuccion();

        if (grabbedObject != null && suctionActive)
            MantenerObjetoAgarrado();
    }

    public void NotifyObjectInside(GameObject obj)
    {
        // Filtro: ignorar piezas ya ensambladas
        Ensamble ens = obj.GetComponent<Ensamble>();
        if (ens != null && ens.yaEnsamblado)
            return;

        objectInside = obj;

        if (suctionActive && grabbedObject == null)
            AgarrarObjetoConSuccion();
    }

    public void NotifyObjectExit(GameObject obj)
    {
        if (objectInside == obj)
            objectInside = null;
    }

    void MoverManual()
    {
        if (Waist) SmoothX(Waist, waistTarget);
        if (Arm01) SmoothZ(Arm01, arm01Target);
        if (Arm02) SmoothZ(Arm02, arm02Target);
        if (Arm03) SmoothX(Arm03, arm03Target);
        if (GripperAssembly) SmoothZ(GripperAssembly, gripAssemblyTarget);
    }

    void ProcesarSuccion()
    {
        if (jugandoSecuencia) return;

        if (suctionActive && grabbedObject == null && objectInside != null)
            AgarrarObjetoConSuccion();

        if (!suctionActive && grabbedObject != null)
            LiberarObjeto();
    }

    void AgarrarObjetoConSuccion()
    {
        if (objectInside == null) return;

        // Doble seguro contra re-agarre de piezas ensambladas
        Ensamble ens = objectInside.GetComponent<Ensamble>();
        if (ens != null && ens.yaEnsamblado)
        {
            Debug.LogWarning($"⚠️ Intento de re-agarrar pieza ya ensamblada: {objectInside.name}. Ignorado.");
            objectInside = null;
            return;
        }

        grabbedObject = objectInside;

        originalParent = grabbedObject.transform.parent;
        originalScale = grabbedObject.transform.localScale;
        originalRigidbody = grabbedObject.GetComponent<Rigidbody>();

        if (originalRigidbody != null)
        {
            wasKinematic = originalRigidbody.isKinematic;
            usedGravity = originalRigidbody.useGravity;
            originalRigidbody.isKinematic = true;
            originalRigidbody.useGravity = false;
        }

        // Hacer hijo manteniendo posición mundial
        grabbedObject.transform.SetParent(gripPoint, true);

        // Forzar rotación mundo específica según el objeto agarrado
        Ensamble ensambleScript = grabbedObject.GetComponent<Ensamble>();
        if (ensambleScript != null && ensambleScript.rotacionAlAgarrar != Vector3.zero)
            grabbedObject.transform.rotation = Quaternion.Euler(ensambleScript.rotacionAlAgarrar);

        // Guardar offset DESPUÉS de aplicar rotación correcta
        grabLocalOffset = grabbedObject.transform.localPosition;
        grabLocalRotOffset = grabbedObject.transform.localRotation;

        Debug.Log($"✔ Succión: {grabbedObject.name}");
    }

    void MantenerObjetoAgarrado()
    {
        if (grabbedObject != null && gripPoint != null)
        {
            grabbedObject.transform.localPosition = grabLocalOffset;
            grabbedObject.transform.localRotation = grabLocalRotOffset;
        }
    }

    public void LiberarObjeto()
    {
        if (grabbedObject == null) return;
        Debug.Log($"🎯 [{gameObject.name}] LiberarObjeto → detector={detectorDeposito != null}, PuntoActivo={detectorDeposito?.PuntoActivo?.name ?? "NULL"}, emparentar={emparentarACaja}");

        string nombreObjeto = grabbedObject.name;
        bool esDronCompleto = grabbedObject.name.Contains("Dron") ||
                              grabbedObject.name.Contains("BasePrefab");

        // Decidir destino: detector dinámico (Paletizador) o punto fijo (Omega)
        Transform destinoFinal = puntoDestinoDron;
        Transform cajaDestino = null;

        if (esDronCompleto && detectorDeposito != null && detectorDeposito.PuntoActivo != null)
        {
            destinoFinal = detectorDeposito.PuntoActivo;
            cajaDestino = detectorDeposito.CajaActiva;
            Debug.Log($"✔ [{gameObject.name}] Usando punto activo del detector: {destinoFinal.name} en {cajaDestino?.name}");
        }

        if (esDronCompleto && destinoFinal != null)
        {
            grabbedObject.transform.position = destinoFinal.position;
            grabbedObject.transform.rotation = destinoFinal.rotation;
        }

        // Reparentado: a la caja si es Paletizador, o al padre original si es Omega/otro
        if (esDronCompleto && emparentarACaja && cajaDestino != null)
        {
            grabbedObject.transform.SetParent(cajaDestino, true);
            Debug.Log($"🔗 Dron emparentado a: {cajaDestino.name}");
        }
        else
        {
            grabbedObject.transform.SetParent(originalParent);
        }

        grabbedObject.transform.localScale = originalScale;

        if (originalRigidbody != null)
        {
            if (esDronCompleto)
            {
                // Bloqueado totalmente, nada lo mueve
                originalRigidbody.isKinematic = true;
                originalRigidbody.useGravity = false;
                originalRigidbody.constraints = RigidbodyConstraints.FreezeAll;
                originalRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }
            else
            {
                originalRigidbody.isKinematic = wasKinematic;
                originalRigidbody.useGravity = usedGravity;
                originalRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
            originalRigidbody.velocity = Vector3.zero;
            originalRigidbody.angularVelocity = Vector3.zero;
            originalRigidbody.WakeUp();
        }

        grabbedObject.GetComponent<Ensamble>()?.NotificarLiberad();
        grabbedObject = null;
        objectInside = null;
        originalParent = null;
        originalRigidbody = null;

        Debug.Log($"🔵 Liberado: {nombreObjeto}");
    }

    void PosicionarSobreBanda()
    {
        if (grabbedObject == null) return;

        // Si es pieza que se congela, no reposicionar
        Ensamble ensambleScript = grabbedObject.GetComponent<Ensamble>();
        if (ensambleScript != null && ensambleScript.congelarAlLiberar)
            return;

        RaycastHit hit;
        Vector3 origenRaycast = grabbedObject.transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(origenRaycast, Vector3.down, out hit, 1f, capaBanda))
        {
            Collider objCollider = grabbedObject.GetComponent<Collider>();
            if (objCollider != null)
            {
                float alturaObjeto = objCollider.bounds.extents.y;
                Vector3 posicionSobreBanda = hit.point + Vector3.up * (alturaObjeto + alturaLiberacion);
                grabbedObject.transform.position = posicionSobreBanda;
            }
        }
        else
        {
            Debug.LogWarning("No se encontró la banda transportadora. Liberando en posición actual.");
        }
    }

    [ContextMenu("Test Liberar Objeto")]
    public void TestLiberarObjeto()
    {
        if (grabbedObject != null)
            LiberarObjeto();
        else
            Debug.Log("No hay objeto agarrado para liberar");
    }

    [ContextMenu("Activar/Desactivar Succión")]
    public void ToggleSuccion()
    {
        suctionActive = !suctionActive;

        if (!suctionActive && grabbedObject != null)
            LiberarObjeto();
    }

    [ContextMenu("Guardar posición actual (Inspector)")]
    public void GuardarPosicion()
    {
        VentosaPose p = new VentosaPose
        {
            waist = waistTarget,
            arm01 = arm01Target,
            arm02 = arm02Target,
            arm03 = arm03Target,
            gripperAssembly = gripAssemblyTarget,
            suctionActive = suctionActive
        };

        poses.Add(p);
        Debug.Log("Pose guardada (#" + poses.Count + ")");
        SaveToFile();
    }

    [ContextMenu("Guardar posición con LIBERAR objeto")]
    public void GuardarPosicionLiberar()
    {
        VentosaPose p = new VentosaPose
        {
            waist = waistTarget,
            arm01 = arm01Target,
            arm02 = arm02Target,
            arm03 = arm03Target,
            gripperAssembly = gripAssemblyTarget,
            suctionActive = false
        };

        poses.Add(p);
        Debug.Log("Pose GUARDAR Y LIBERAR objeto (#" + poses.Count + ")");
        SaveToFile();
    }

    [ContextMenu("Borrar todas las poses")]
    public void ClearPoses()
    {
        poses.Clear();
        Debug.Log("Poses borradas.");
        SaveToFile();
    }

    public void BorrarPasoDefinitivo(int index)
    {
        if (index < 0 || index >= poses.Count)
        {
            Debug.LogWarning("Índice fuera de rango: " + index);
            return;
        }

        poses.RemoveAt(index);
        SaveToFile();
        Debug.Log("✔ Paso #" + index + " eliminado DEFINITIVAMENTE.");
    }

    [ContextMenu("Iniciar secuencia")]
    public void IniciarSecuencia()
    {
        if (poses.Count == 0)
        {
            Debug.LogWarning("No hay poses guardadas.");
            return;
        }

        currentPoseIndex = 0;
        jugandoSecuencia = true;
        Debug.Log($"▶ [{gameObject.name}] Secuencia iniciada ({poses.Count} pasos)");
    }

    [ContextMenu("Iniciar secuencia con espera")]
    public void IniciarSecuenciaConEspera()
    {
        if (poses.Count == 0)
        {
            Debug.LogWarning("No hay poses guardadas.");
            return;
        }

        if (tiempoEsperaInicial > 0)
        {
            esperandoInicio = true;
            tiempoEsperaInicialTimer = 0f;
        }
        else
        {
            IniciarSecuencia();
        }
    }

    void ReproducirSecuencia()
    {
        if (currentPoseIndex >= poses.Count)
        {
            jugandoSecuencia = false;
            Debug.Log($"⏹ [{gameObject.name}] Secuencia terminada");
            return;
        }

        // Si está en proceso de liberación, no avanzar ni mover
        if (liberandoObjeto) return;

        // Espera del delay entre poses
        if (esperandoPose)
        {
            timerPose += Time.deltaTime;
            if (timerPose >= poses[currentPoseIndex - 1].delay)
                esperandoPose = false;
            return;
        }

        VentosaPose p = poses[currentPoseIndex];

        SmoothX(Waist, p.waist);
        SmoothZ(Arm01, p.arm01);
        SmoothZ(Arm02, p.arm02);
        SmoothX(Arm03, p.arm03);
        SmoothZ(GripperAssembly, p.gripperAssembly);

        // Caso AGARRAR: la succión debe activarse al llegar a la pose
        if (p.suctionActive && grabbedObject == null)
        {
            if (Llegamos(p))
            {
                suctionActive = true;
                if (objectInside != null)
                    AgarrarObjetoConSuccion();

                // No avanzar hasta confirmar agarre real
                if (grabbedObject == null) return;

                currentPoseIndex++;
                if (p.delay > 0f)
                {
                    esperandoPose = true;
                    timerPose = 0f;
                }
            }
            return;
        }

        // Caso LIBERAR: la succión debe desactivarse SOLO al llegar a la pose final
        if (!p.suctionActive && grabbedObject != null)
        {
            if (Llegamos(p))
            {
                suctionActive = false;
                StartCoroutine(LiberarEnSecuencia());
                currentPoseIndex++;
                if (p.delay > 0f)
                {
                    esperandoPose = true;
                    timerPose = 0f;
                }
            }
            return;
        }

        // Caso MOVIMIENTO normal (sin cambio de succión)
        if (suctionActive != p.suctionActive)
            suctionActive = p.suctionActive;

        if (Llegamos(p))
        {
            currentPoseIndex++;
            if (p.delay > 0f)
            {
                esperandoPose = true;
                timerPose = 0f;
            }
        }
    }

    IEnumerator LiberarEnSecuencia()
    {
        if (grabbedObject == null) yield break;

        liberandoObjeto = true;

        // Verificar si es pieza que se congela
        Ensamble ensambleScript = grabbedObject.GetComponent<Ensamble>();
        bool esPiezaQueSeCongela = ensambleScript != null && ensambleScript.congelarAlLiberar;

        if (!esPiezaQueSeCongela)
            yield return StartCoroutine(BajarABanda());
        else
            yield return new WaitForSeconds(0.1f);

        LiberarObjeto();

        liberandoObjeto = false;
    }

    IEnumerator BajarABanda()
    {
        yield return new WaitForSeconds(0.2f);
    }

    bool Llegamos(VentosaPose p)
    {
        float tolerancia = 2f;

        bool bWaist = Waist == null || Mathf.Abs(Waist.xDrive.target - p.waist) < tolerancia;
        bool bArm01 = Arm01 == null || Mathf.Abs(Arm01.zDrive.target - p.arm01) < tolerancia;
        bool bArm02 = Arm02 == null || Mathf.Abs(Arm02.zDrive.target - p.arm02) < tolerancia;
        bool bArm03 = Arm03 == null || Mathf.Abs(Arm03.xDrive.target - p.arm03) < tolerancia;
        bool bGripper = GripperAssembly == null || Mathf.Abs(GripperAssembly.zDrive.target - p.gripperAssembly) < tolerancia;

        return bWaist && bArm01 && bArm02 && bArm03 && bGripper;
    }

    void SmoothX(ArticulationBody joint, float target)
    {
        if (joint == null) return;
        var drive = joint.xDrive;
        float step = speed * Time.deltaTime;
        drive.target = Mathf.MoveTowards(drive.target, target, step);
        joint.xDrive = drive;
    }

    void SmoothZ(ArticulationBody joint, float target)
    {
        if (joint == null) return;
        var drive = joint.zDrive;
        float step = speed * Time.deltaTime;
        drive.target = Mathf.MoveTowards(drive.target, target, step);
        joint.zDrive = drive;
    }

    [System.Serializable]
    class VentosaPoseContainer
    {
        public List<VentosaPose> poses;
    }

    public string GetFullSavePath()
    {
        return Path.Combine(Application.persistentDataPath, saveFileName);
    }

    [ContextMenu("Guardar poses a archivo JSON")]
    public void SaveToFile()
    {
        try
        {
            VentosaPoseContainer c = new VentosaPoseContainer();
            c.poses = poses;

            string json = JsonUtility.ToJson(c, true);
            string fullPath = GetFullSavePath();
            File.WriteAllText(fullPath, json);
            Debug.Log("Poses guardadas en: " + fullPath);

#if UNITY_EDITOR
            string folder = Path.Combine(Application.dataPath, "JSON_Generados");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fileName = saveFileName;
            if (!fileName.EndsWith(".json"))
                fileName += ".json";

            string path = Path.Combine(folder, fileName);

            File.WriteAllText(path, json);
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error guardando poses: " + ex.Message);
        }
    }

    [ContextMenu("Cargar poses desde archivo JSON")]
    public void LoadFromFile()
    {
        try
        {
            string fullPath = GetFullSavePath();
            if (!File.Exists(fullPath))
                return;

            string json = File.ReadAllText(fullPath);
            VentosaPoseContainer c = JsonUtility.FromJson<VentosaPoseContainer>(json);

            if (c != null && c.poses != null)
            {
                List<VentosaPose> clones = new List<VentosaPose>();
                foreach (var pose in c.poses)
                {
                    VentosaPose copia = new VentosaPose
                    {
                        waist = pose.waist,
                        arm01 = pose.arm01,
                        arm02 = pose.arm02,
                        arm03 = pose.arm03,
                        gripperAssembly = pose.gripperAssembly,
                        suctionActive = pose.suctionActive,
                        delay = pose.delay,
                    };
                    clones.Add(copia);
                }

                poses = clones;
            }
            else
            {
                poses = new List<VentosaPose>();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error cargando poses: " + ex.Message);
        }
    }

    void OnDrawGizmos()
    {
        if (gripPoint != null)
        {
            Gizmos.color = suctionActive ? Color.green : Color.red;
            Gizmos.DrawWireSphere(gripPoint.position, 0.05f);
            Gizmos.DrawLine(gripPoint.position, gripPoint.position + gripPoint.forward * 0.1f);
        }

        if (suctionTrigger != null)
        {
            Gizmos.color = objectInside != null ? Color.blue : Color.yellow;
            Gizmos.DrawWireCube(suctionTrigger.bounds.center, suctionTrigger.bounds.size);
        }

        if (grabbedObject != null)
        {
            Vector3 origenRaycast = grabbedObject.transform.position + Vector3.up * 0.5f;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(origenRaycast, origenRaycast + Vector3.down * 1f);
        }
    }
}

[System.Serializable]
public class VentosaPose
{
    public float waist;
    public float arm01;
    public float arm02;
    public float arm03;
    public float gripperAssembly;
    public bool suctionActive;
    public float delay = 0f;
}