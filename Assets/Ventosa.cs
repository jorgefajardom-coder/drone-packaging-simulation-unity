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

    [Header("Secuencia de depósito por nombre (para Paletizador)")]
    [Tooltip("Si está activo, el Paletizador deposita en cajas según la lista 'ordenCajas', ignorando puntoDestinoDron")]
    public bool usarSecuenciaDeCajas = false;

    [Tooltip("Orden de números de caja a llenar. Ej: [1,3,2,4,5,7,6,8] buscará CajaPrefab(Clone1), luego Clone3, etc.")]
    public int[] ordenCajas;

    [Tooltip("Prefijo base del nombre de las cajas. Ej: 'CajaPrefab(Clone' → buscará 'CajaPrefab(Clone1)', 'CajaPrefab(Clone2)'...")]
    public string prefijoNombreCaja = "CajaPrefab(Clone";

    [Tooltip("Sufijo final del nombre. Por defecto ')' para cerrar el paréntesis")]
    public string sufijoNombreCaja = ")";

    [Tooltip("Nombre exacto del hijo dentro de cada caja que marca el punto de depósito")]
    public string nombrePuntoDeposito = "PuntoDepositoDron";

    [Tooltip("Si está activo, el dron se vuelve hijo de la caja al soltarlo (viaja con el carro)")]
    public bool emparentarACaja = false;

    public bool dronDepositado = false;

    // Contador interno de drones soltados en esta corrida
    private int dronesSoltados = 0;

    [Tooltip("Delay en segundos entre el emparentado del dron y el inicio del cierre de la tapa. Útil para dar tiempo a que el brazo se retire.")]
    public float delayCierreTapa = 1f;

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

    public bool secuenciaTerminada = false;

    [Header("Caída simulada al soltar dron")]
    [Tooltip("Altura inicial sobre el puntoDestinoDron desde la que empieza la caída visual")]
    public float alturaCaidaSimulada = 0.15f;

    [Tooltip("Duración de la caída visual en segundos")]
    public float duracionCaidaSimulada = 0.5f;

    [Header("Sincronización externa con el cajón móvil (para Paletizador)")]
    [Tooltip("Si está activo, el brazo esperará en la pose de liberación hasta que 'permisoParaSoltar' sea true")]
    public bool esperarPermisoParaSoltar = false;

    [Tooltip("Bandera que habilita la liberación. La setea externamente el CarroPaletizador cuando llega al punto destino.")]
    public bool permisoParaSoltar = false;

    [Header("Pose inicial (home)")]
    public float homeWaist = 0f;
    public float homeArm01 = 0f;
    public float homeArm02 = 0f;
    public float homeArm03 = 0f;
    public float homeGripperAssembly = 0f;

    void Awake()
    {
        LoadFromFile();
    }

    void Start()
    {
        // Aplicar home inmediatamente
        if (Waist) { var d = Waist.xDrive; d.target = homeWaist; Waist.xDrive = d; }
        if (Arm01) { var d = Arm01.zDrive; d.target = homeArm01; Arm01.zDrive = d; }
        if (Arm02) { var d = Arm02.zDrive; d.target = homeArm02; Arm02.zDrive = d; }
        if (Arm03) { var d = Arm03.xDrive; d.target = homeArm03; Arm03.xDrive = d; }
        if (GripperAssembly) { var d = GripperAssembly.zDrive; d.target = homeGripperAssembly; GripperAssembly.zDrive = d; }

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

        // CORRECTO:
        if (!jugandoSecuencia)
            MoverManual();
        else
        {
            ReproducirSecuencia();
        }

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

        Debug.Log($"🎯 [{gameObject.name}] LiberarObjeto → usarSecuencia={usarSecuenciaDeCajas}, dronesSoltados={dronesSoltados}, emparentar={emparentarACaja}");

        string nombreObjeto = grabbedObject.name;
        bool esDronCompleto = grabbedObject.name.Contains("Dron") ||
                              grabbedObject.name.Contains("BasePrefab");

        // Decidir destino: secuencia de cajas por nombre (Paletizador) o punto fijo (Omega)
        Transform destinoFinal = puntoDestinoDron;
        Transform cajaDestino = null;

        if (esDronCompleto && usarSecuenciaDeCajas)
        {
            if (ordenCajas == null || ordenCajas.Length == 0)
            {
                Debug.LogError($"❌ [{gameObject.name}] usarSecuenciaDeCajas activo pero ordenCajas está vacío.");
            }
            // DESPUÉS:
            else
            {
                int indice = dronesSoltados % ordenCajas.Length; // ← vuelve a 0 después del 7
                int numeroCaja = ordenCajas[indice];
                string nombreCaja = prefijoNombreCaja + numeroCaja + sufijoNombreCaja;

                GameObject cajaObj = GameObject.Find(nombreCaja);
                if (cajaObj == null)
                {
                    Debug.LogError($"❌ [{gameObject.name}] No se encontró la caja '{nombreCaja}' en la escena.");
                }
                else
                {
                    Transform punto = cajaObj.transform.Find(nombrePuntoDeposito);
                    if (punto == null)
                    {
                        Debug.LogError($"❌ [{gameObject.name}] No se encontró '{nombrePuntoDeposito}' dentro de '{nombreCaja}'.");
                    }
                    else
                    {
                        destinoFinal = punto;
                        cajaDestino = cajaObj.transform;
                        Debug.Log($"✔ [{gameObject.name}] Depositando dron #{dronesSoltados + 1} en {nombreCaja} (punto: {punto.name})");
                    }
                }

                dronesSoltados++;
            }
        }

        if (esDronCompleto && destinoFinal != null)
        {
            grabbedObject.transform.position = destinoFinal.position;
            grabbedObject.transform.rotation = destinoFinal.rotation;
            dronDepositado = true;
            Debug.Log($"🎯 [{gameObject.name}] Dron colocado en {destinoFinal.name}");
        }

        // Reparentado: a la caja si es Paletizador, o al padre original si es Omega/otro
        if (esDronCompleto && emparentarACaja && cajaDestino != null)
        {
            grabbedObject.transform.SetParent(cajaDestino, true);
            Debug.Log($"🔗 Dron emparentado a: {cajaDestino.name}");

            // Disparar el cierre de la tapa con delay configurable
            CerradorTapa cerrador = cajaDestino.GetComponent<CerradorTapa>();
            if (cerrador != null)
            {
                Debug.Log($"🔒 [{gameObject.name}] Cierre de tapa programado en {delayCierreTapa}s para {cajaDestino.name}");
                StartCoroutine(CerrarTapaConDelay(cerrador, delayCierreTapa));
            }
            else
            {
                Debug.LogWarning($"⚠ [{gameObject.name}] {cajaDestino.name} no tiene componente CerradorTapa.");
            }
        }
        else
        {
            grabbedObject.transform.SetParent(originalParent);
            grabbedObject.transform.localScale = originalScale;
        }

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
        secuenciaTerminada = false;
        esperandoPose = false;
        timerPose = 0f;
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

    public void ResetCompleto()
    {
        jugandoSecuencia = false;
        secuenciaTerminada = false;
        esperandoPose = false;
        timerPose = 0f;
        currentPoseIndex = 0;
        esperandoInicio = false;
        tiempoEsperaInicialTimer = 0f;
        grabbedObject = null;
        objectInside = null;
        liberandoObjeto = false;
        suctionActive = false;
        dronDepositado = false;
    }

    void ReproducirSecuencia()
    {
        if (currentPoseIndex >= poses.Count)
        {
            jugandoSecuencia = false;
            secuenciaTerminada = true;
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
                // Si está configurado para esperar permiso externo, no suelta hasta recibirlo
                if (esperarPermisoParaSoltar && !permisoParaSoltar)
                {
                    // Se queda en esta pose esperando al cajón
                    return;
                }

                suctionActive = false;
                StartCoroutine(LiberarEnSecuencia());

                // Consumir el permiso para que el próximo ciclo requiera uno nuevo
                permisoParaSoltar = false;

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

    IEnumerator CerrarTapaConDelay(CerradorTapa cerrador, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (cerrador != null)
        {
            Debug.Log($"🔒 [{gameObject.name}] Ejecutando cierre de tapa ahora.");
            cerrador.CerrarTapa();
        }
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

    string GetStreamingAssetsPath()
    {
        string fileName = saveFileName;
        if (string.IsNullOrEmpty(fileName))
            fileName = "poses_ventosa.json";
        if (!fileName.EndsWith(".json"))
            fileName += ".json";
        return Path.Combine(Application.streamingAssetsPath, fileName);
    }

    void EnsureUpToDateFromStreamingAssets(string destPath)
    {
        string streamingPath = GetStreamingAssetsPath();
        if (!File.Exists(streamingPath)) return;

        bool shouldCopy = !File.Exists(destPath);
        if (!shouldCopy)
        {
            try
            {
                VentosaPoseContainer existing = JsonUtility.FromJson<VentosaPoseContainer>(File.ReadAllText(destPath));
                VentosaPoseContainer reference = JsonUtility.FromJson<VentosaPoseContainer>(File.ReadAllText(streamingPath));
                int existingCount = existing?.poses?.Count ?? 0;
                int referenceCount = reference?.poses?.Count ?? 0;
                if (referenceCount > existingCount) shouldCopy = true;
            }
            catch { shouldCopy = true; }
        }

        if (shouldCopy)
        {
            string dir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.Copy(streamingPath, destPath, true);
            Debug.Log($"[{gameObject.name}] Poses actualizadas desde StreamingAssets → {destPath}");
        }
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
            string jsonFileName = saveFileName;
            if (!jsonFileName.EndsWith(".json"))
                jsonFileName += ".json";

            string folder = Path.Combine(Application.dataPath, "JSON_Generados");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, jsonFileName), json);

            string streamingFolder = Path.Combine(Application.dataPath, "StreamingAssets");
            if (!Directory.Exists(streamingFolder))
                Directory.CreateDirectory(streamingFolder);
            File.WriteAllText(Path.Combine(streamingFolder, jsonFileName), json);

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
            EnsureUpToDateFromStreamingAssets(fullPath);
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

    public void ReiniciarContadorDrones()
    {
        dronesSoltados = 0;
        Debug.Log($"[{gameObject.name}] Contador de drones reiniciado.");
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