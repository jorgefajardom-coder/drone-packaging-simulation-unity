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

    [Header("Configuración de liberación")]
    public float alturaLiberacion = 0.02f;
    public LayerMask capaBanda = 1;

    [Header("Rotación fija del objeto al agarrar")]
    public Vector3 rotacionFijaAlAgarrar = new Vector3(90f, 0f, 0f); // pon aquí la rotación exacta del prefab PCB

    private GameObject objectInside;
    private GameObject grabbedObject;
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

    [Header("Opciones de reproducción / persistencia")]
    public bool autoStartOnPlay = false;
    public string saveFileName = "poses_ventosa.json";

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
                Debug.Log($"⏰ Esperando {tiempoEsperaInicial}s antes de iniciar secuencia...");
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
        {
            MantenerObjetoAgarrado();
        }
    }

    public void NotifyObjectInside(GameObject obj)
    {
        objectInside = obj;
        Debug.Log($"[Ventosa] Objeto detectado en la ventosa → {obj.name}");

        if (suctionActive && grabbedObject == null)
        {
            AgarrarObjetoConSuccion();
        }
    }

    public void NotifyObjectExit(GameObject obj)
    {
        if (objectInside == obj)
        {
            objectInside = null;
            Debug.Log($"[Ventosa] Objeto salió del área de succión → {obj.name}");
        }
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
        {
            AgarrarObjetoConSuccion();
        }

        if (!suctionActive && grabbedObject != null)
        {
            LiberarObjeto();
        }
    }

    void AgarrarObjetoConSuccion()
    {
        if (objectInside == null) return;

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

        // ✅ Hacer hijo manteniendo posición mundial
        grabbedObject.transform.SetParent(gripPoint, true);

        // ✅ Forzar siempre la rotación exacta del prefab PCB
        grabbedObject.transform.rotation = Quaternion.Euler(rotacionFijaAlAgarrar);

        // ✅ Guardar offset DESPUÉS de aplicar rotación correcta
        grabLocalOffset = grabbedObject.transform.localPosition;
        grabLocalRotOffset = grabbedObject.transform.localRotation;

        Debug.Log($"✅ SUCCIÓN EXITOSA: {grabbedObject.name} | Rot: {grabbedObject.transform.eulerAngles}");
    }

    void MantenerObjetoAgarrado()
    {
        if (grabbedObject != null && gripPoint != null)
        {
            grabbedObject.transform.localPosition = grabLocalOffset;
            grabbedObject.transform.localRotation = grabLocalRotOffset;
        }
    }

    void LiberarObjeto()
    {
        if (grabbedObject == null) return;

        Debug.Log($"🔵 DESACTIVANDO SUCCIÓN: {grabbedObject.name}");

        PosicionarSobreBanda();

        grabbedObject.transform.SetParent(originalParent);
        grabbedObject.transform.localScale = originalScale;

        if (originalRigidbody != null)
        {
            originalRigidbody.isKinematic = wasKinematic;
            originalRigidbody.useGravity = usedGravity;
            originalRigidbody.velocity = Vector3.zero;
            originalRigidbody.angularVelocity = Vector3.zero;
            originalRigidbody.WakeUp();
        }

        // ✅ LÍNEA NUEVA: notifica al script de ensamble que fue liberada
        grabbedObject.GetComponent<Ensamble>()?.NotificarLiberad();

        grabbedObject = null;
        objectInside = null;
        originalParent = null;
        originalRigidbody = null;

        Debug.Log("✅ OBJETO LIBERADO - SUCCIÓN DESACTIVADA");
    }

    void PosicionarSobreBanda()
    {
        if (grabbedObject == null) return;

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

                // ✅ Al liberar también forzar la rotación correcta del prefab
                grabbedObject.transform.rotation = Quaternion.Euler(rotacionFijaAlAgarrar);

                Debug.Log($"📐 Objeto posicionado sobre la banda a altura: {alturaObjeto + alturaLiberacion}");
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
        Debug.Log(suctionActive ? "🔄 SUCCIÓN ACTIVADA" : "🔄 SUCCIÓN DESACTIVADA");

        if (!suctionActive && grabbedObject != null)
        {
            LiberarObjeto();
        }
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
        Debug.Log("Reproduciendo secuencia... total pasos: " + poses.Count);
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
            Debug.Log($"⏰ Esperando {tiempoEsperaInicial}s antes de iniciar secuencia...");
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
            Debug.Log("Secuencia terminada.");
            return;
        }

        // ⛔ Si está en proceso de liberación, no avanzar ni mover
        if (liberandoObjeto) return;

        VentosaPose p = poses[currentPoseIndex];

        SmoothX(Waist, p.waist);
        SmoothZ(Arm01, p.arm01);
        SmoothZ(Arm02, p.arm02);
        SmoothX(Arm03, p.arm03);
        SmoothZ(GripperAssembly, p.gripperAssembly);

        if (suctionActive != p.suctionActive)
        {
            suctionActive = p.suctionActive;
            Debug.Log(suctionActive ? "🔄 SUCCIÓN ACTIVADA (secuencia)" : "🔄 SUCCIÓN DESACTIVADA (secuencia)");
        }

        if (p.suctionActive && grabbedObject == null && objectInside != null)
        {
            AgarrarObjetoConSuccion();
        }
        else if (!p.suctionActive && grabbedObject != null)
        {
            StartCoroutine(LiberarEnSecuencia());
        }

        if (Llegamos(p))
        {
            currentPoseIndex++;
        }
    }

    IEnumerator LiberarEnSecuencia()
    {
        if (grabbedObject == null) yield break;

        liberandoObjeto = true; // 🔒 Congela la secuencia

        Debug.Log($"🔵 SECUENCIA: Liberando objeto {grabbedObject.name}");
        yield return StartCoroutine(BajarABanda());
        LiberarObjeto();

        liberandoObjeto = false; // 🔓 Reanuda la secuencia
    }

    IEnumerator BajarABanda()
    {
        Debug.Log("📥 Bajando brazo cerca de la banda para liberación...");
        yield return new WaitForSeconds(0.2f);
    }

    bool Llegamos(VentosaPose p)
    {
        float tolerancia = 1f;

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
{
    Directory.CreateDirectory(folder);
}

string fileName = saveFileName;
if (!fileName.EndsWith(".json"))
    fileName += ".json";

string path = Path.Combine(folder, fileName);

File.WriteAllText(path, json);

UnityEditor.AssetDatabase.Refresh();

Debug.Log("GUARDADO EN UNITY: " + path);
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
            {
                Debug.Log("No se encontró archivo de poses en: " + fullPath);
                return;
            }

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
                        suctionActive = pose.suctionActive
                    };
                    clones.Add(copia);
                }

                poses = clones;
            }
            else
            {
                poses = new List<VentosaPose>();
            }

            Debug.Log("Poses cargadas (" + poses.Count + ") desde: " + fullPath);
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
}