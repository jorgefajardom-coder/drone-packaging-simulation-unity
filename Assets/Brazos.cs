using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Brazos : MonoBehaviour
{
    [Header("Articulaciones principales")]
    public ArticulationBody Waist;
    public ArticulationBody Arm01;
    public ArticulationBody Arm02;
    public ArticulationBody Arm03;
    public ArticulationBody GripperAssembly;
    public ArticulationBody Gear1;
    public ArticulationBody Gear2;

    [Header("Ángulos que controlas por código")]
    public float waistTarget = 0f;
    public float arm01Target = 0f;
    public float arm02Target = 0f;
    public float arm03Target = 0f;
    public float gripAssemblyTarget = 0f;

    [Header("Control del gripper")]
    public float gripperOpenAngle = -20f;
    public float gripperClosedAngle = 8f;
    public bool gripperClosed = false;
    public float delay = 0f;

    [Header("Velocidad de movimiento (°/s)")]
    public float speed = 120f;

    [Header("Tiempo de espera inicial (segundos)")]
    public float tiempoEsperaInicial = 0f;

    [Header("Posiciones guardadas (se persisten en archivo)")]
    public List<RobotPose> poses = new List<RobotPose>();

    [Header("Agarre por Trigger")]
    public Transform gripPoint;
    public Collider gripperTrigger;

    [Header("Centrado de la base al soltar")]
    public CentrarBase scriptCentrarBase;

    private GameObject objectInside;
    private GameObject grabbedObject;

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
    public string saveFileName = "poses_cubo.json";

    void Awake()
    {
        LoadFromFile();
    }

    void Start()
    {
        if (autoStartOnPlay)
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

        ProcesarAgarre();
    }

    public void NotifyObjectInside(GameObject obj)
    {
        objectInside = obj;
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

        float targetGrip = gripperClosed ? gripperClosedAngle : gripperOpenAngle;

        if (Gear1) SmoothX(Gear1, targetGrip);
        if (Gear2) SmoothX(Gear2, -targetGrip);
    }

    void ProcesarAgarre()
    {
        if (jugandoSecuencia) return;

        if (gripperClosed && grabbedObject == null && objectInside != null)
            AgarrarObjeto();

        if (!gripperClosed && grabbedObject != null)
            LiberarObjeto();

        if (grabbedObject != null && gripperClosed)
        {
            grabbedObject.transform.localPosition = grabLocalOffset;
            grabbedObject.transform.localRotation = grabLocalRotOffset;
        }
    }

    void AgarrarObjeto()
    {
        if (objectInside == null) return;

        grabbedObject = objectInside;

        Rigidbody rb = grabbedObject.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Guardar rotación mundial ANTES del parenting
        Quaternion rotacionMundial = grabbedObject.transform.rotation;
        Vector3 posicionMundial = grabbedObject.transform.position;

        grabbedObject.transform.SetParent(gripPoint);

        grabbedObject.transform.position = posicionMundial;
        grabbedObject.transform.rotation = rotacionMundial;

        grabLocalOffset = grabbedObject.transform.localPosition;
        grabLocalRotOffset = grabbedObject.transform.localRotation;

        // Si el objeto agarrado tiene CentrarBase, guardamos la referencia automáticamente
        CentrarBase cb = grabbedObject.GetComponent<CentrarBase>();
        if (cb != null) scriptCentrarBase = cb;

        Debug.Log("✔ Agarrado: " + grabbedObject.name);
    }

    void LiberarObjeto()
    {
        if (scriptCentrarBase != null)
            scriptCentrarBase.IniciarCentrado();

        string nombreObjeto = grabbedObject.name;

        EnsambleGri ensambleGri = grabbedObject.GetComponent<EnsambleGri>();
        if (ensambleGri != null)
        {
            Collider colPCB = GameObject.Find("PCBPrefab(Clone)")?.GetComponent<Collider>();
            Collider colBase = GameObject.Find("BasePrefab(Clone)")?.GetComponent<Collider>();
            ensambleGri.NotificarLiberad(new Collider[] { colPCB, colBase });

            // NO restaurar física para EnsambleGri
            grabbedObject.transform.SetParent(null);
        }
        else
        {
            Ensamble ensamblePCB = grabbedObject.GetComponent<Ensamble>();
            if (ensamblePCB != null)
                ensamblePCB.NotificarLiberad();

            Rigidbody rb = grabbedObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.WakeUp();
            }
            grabbedObject.transform.SetParent(null);
        }

        grabbedObject = null;
        if (objectInside != null) objectInside = null;

        Debug.Log("🔵 Liberado: " + nombreObjeto);
    }

    [ContextMenu("Test Liberar Objeto")]
    public void TestLiberarObjeto()
    {
        if (grabbedObject != null)
            LiberarObjeto();
        else
            Debug.Log("No hay objeto agarrado para liberar");
    }

    [ContextMenu("Guardar posición actual (Inspector)")]
    public void GuardarPosicion()
    {
        RobotPose p = new RobotPose
        {
            waist = waistTarget,
            arm01 = arm01Target,
            arm02 = arm02Target,
            arm03 = arm03Target,
            gripperAssembly = gripAssemblyTarget,
            gripperClosed = gripperClosed,
            gripperOpenAngle = this.gripperOpenAngle,
            gripperClosedAngle = this.gripperClosedAngle,
            delay = 0f,
        };

        poses.Add(p);
        Debug.Log("Pose guardada (#" + poses.Count + ")");
        SaveToFile();
    }

    [ContextMenu("Guardar posición con SOLTAR objeto")]
    public void GuardarPosicionSoltar()
    {
        RobotPose p = new RobotPose
        {
            waist = waistTarget,
            arm01 = arm01Target,
            arm02 = arm02Target,
            arm03 = arm03Target,
            gripperAssembly = gripAssemblyTarget,
            gripperClosed = false,
            gripperOpenAngle = this.gripperOpenAngle,
            gripperClosedAngle = this.gripperClosedAngle,
            delay = 0f,
        };

        poses.Add(p);
        Debug.Log("Pose GUARDAR Y SOLTAR objeto (#" + poses.Count + ")");
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

        if (esperandoPose)
        {
            timerPose += Time.deltaTime;
            if (timerPose >= poses[currentPoseIndex - 1].delay)
                esperandoPose = false;
            return;
        }

        RobotPose p = poses[currentPoseIndex];

        // Mover articulaciones
        SmoothX(Waist, p.waist);
        SmoothZ(Arm01, p.arm01);
        SmoothZ(Arm02, p.arm02);
        SmoothX(Arm03, p.arm03);
        SmoothZ(GripperAssembly, p.gripperAssembly);

        // Mover gripper
        float gripTarget = p.gripperClosed ? p.gripperClosedAngle : p.gripperOpenAngle;
        if (Gear1) SmoothX(Gear1, gripTarget);
        if (Gear2) SmoothX(Gear2, -gripTarget);

        if (p.gripperClosed && grabbedObject == null && objectInside != null)
            AgarrarObjeto();

        if (!p.gripperClosed && grabbedObject != null)
            LiberarObjeto();

        // Mantener pegado si está agarrado
        if (grabbedObject != null && p.gripperClosed)
        {
            grabbedObject.transform.localPosition = grabLocalOffset;
            grabbedObject.transform.localRotation = grabLocalRotOffset;
        }

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

    bool Llegamos(RobotPose p)
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
    class RobotPoseContainer
    {
        public List<RobotPose> poses;
    }

    public string GetFullSavePath()
    {
        string fileName = saveFileName;

        if (string.IsNullOrEmpty(fileName))
            fileName = "poses.json";

        if (!fileName.EndsWith(".json"))
            fileName += ".json";

        return Path.Combine(Application.persistentDataPath, fileName);
    }

    [ContextMenu("Guardar poses a archivo JSON")]
    public void SaveToFile()
    {
        try
        {
            RobotPoseContainer c = new RobotPoseContainer();
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
            RobotPoseContainer c = JsonUtility.FromJson<RobotPoseContainer>(json);

            if (c != null && c.poses != null)
            {
                List<RobotPose> clones = new List<RobotPose>();
                foreach (var pose in c.poses)
                {
                    RobotPose copia = new RobotPose
                    {
                        waist = pose.waist,
                        arm01 = pose.arm01,
                        arm02 = pose.arm02,
                        arm03 = pose.arm03,
                        gripperAssembly = pose.gripperAssembly,
                        gripperClosed = pose.gripperClosed,
                        gripperOpenAngle = pose.gripperOpenAngle,
                        gripperClosedAngle = pose.gripperClosedAngle,
                        delay = pose.delay,
                    };
                    clones.Add(copia);
                }

                poses = clones;
            }
            else
            {
                poses = new List<RobotPose>();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error cargando poses: " + ex.Message);
        }
    }
}

[System.Serializable]
public class RobotPose
{
    public float waist;
    public float arm01;
    public float arm02;
    public float arm03;
    public float gripperAssembly;
    public bool gripperClosed;
    public float gripperOpenAngle = -20f;
    public float gripperClosedAngle = -8f;
    public float delay = 0f;
}