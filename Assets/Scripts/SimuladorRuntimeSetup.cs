using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

/// <summary>
/// Componente de inicialización y aseguramiento en tiempo de ejecución.
/// Detecta si el árbol de ArticulationBody ya fue construido en el Editor.
/// Si YA existe → solo crea lo que falta (UI, carga, zonas, objetivos, cámara).
/// Si NO existe → construye el árbol completo + todo lo demás.
/// 100% compatible con WebGL. Seguro de ejecutar en cada Play Mode.
/// </summary>
public class SimuladorRuntimeSetup : MonoBehaviour
{
    private void Awake()
    {
        VerificarYConfigurar();
    }

    public void VerificarYConfigurar()
    {
        // ── 1. Asegurar EventSystem ──────────────────────────────────────────
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        // ── 2. Materiales y PhysicsMaterial ─────────────────────────────────
        Material matMetalOscuro = CrearMaterialShaderColor("Mat_MetalOscuro", new Color(0.2f, 0.22f, 0.25f), 0.7f, 0.5f);
        Material matJuntas = CrearMaterialShaderColor("Mat_Juntas", new Color(0.72f, 0.45f, 0.2f), 0.8f, 0.6f);
        Material matAzulIndustrial = CrearMaterialShaderColor("Mat_AzulIndustrial", new Color(0.1f, 0.35f, 0.75f), 0.6f, 0.4f);
        Material matPinza = CrearMaterialShaderColor("Mat_Pinza", new Color(0.35f, 0.38f, 0.42f), 0.9f, 0.7f);

        PhysicsMaterial matFriccion = new PhysicsMaterial("FriccionAlta")
        {
            dynamicFriction = 0.9f,
            staticFriction = 0.95f,
            frictionCombine = PhysicsMaterialCombine.Maximum,
            bounciness = 0.0f
        };

        // ── 3. ¿El árbol de articulaciones ya existe? ────────────────────────
        BrazoMecanicoController controller = FindFirstObjectByType<BrazoMecanicoController>();
        bool arbolExiste = controller != null && controller.TorretaBase != null;

        GameObject brazoRoot;

        if (arbolExiste)
        {
            // El árbol fue construido por el Editor → NO recrear nada articulado.
            brazoRoot = controller.gameObject;
            if (brazoRoot.transform.parent != null)
                brazoRoot = brazoRoot.transform.root.gameObject;

            Debug.Log("[SimuladorRuntimeSetup] Árbol de articulaciones detectado en la escena. Se omite la construcción del brazo.");
        }
        else
        {
            // No existe → construir el árbol completo desde cero.
            Debug.Log("[SimuladorRuntimeSetup] Árbol no detectado. Construyendo brazo mecánico en tiempo de ejecución...");

            // Buscar o crear raíz BrazoMecanico
            brazoRoot = GameObject.Find("BrazoMecanico");

            if (brazoRoot != null && controller == null)
            {
                // Existe el GO vacío pero sin controller → limpiar para no dejar jerarquías huérfanas
                Destroy(brazoRoot);
                brazoRoot = null;
            }

            if (brazoRoot == null)
            {
                brazoRoot = new GameObject("BrazoMecanico");
                brazoRoot.transform.position = Vector3.zero;
                brazoRoot.transform.rotation = Quaternion.identity;
                brazoRoot.transform.localScale = Vector3.one;
            }

            ConstruirArbolArticulaciones(
                brazoRoot,
                matMetalOscuro,
                matJuntas,
                matAzulIndustrial,
                matPinza,
                matFriccion,
                out controller
            );
        }

        // ── 4. Wiring de referencias nulas en componentes ya en escena ───────
        ArticulationTelemetry telemetry = FindFirstObjectByType<ArticulationTelemetry>();
        ReiniciarSimulacion reiniciador = FindFirstObjectByType<ReiniciarSimulacion>();

        if (telemetry != null && controller != null)
            SetCampoPrivado(telemetry, "controller", controller);

        // ── 5. Mesas, zonas y carga ──────────────────────────────────────────
        ZonaObjetivo zoRecogida = AsegurarMesaYZona(
            "MesaRecogida", "ZonaRecogida", TipoZona.Recogida,
            new Vector3(-4.5f, 0.5f, 3.2f), matMetalOscuro
        );

        ZonaObjetivo zoDestino = AsegurarMesaYZona(
            "MesaDestino", "ZonaDestino", TipoZona.Destino,
            new Vector3(3.2f, 0.5f, 3.2f), matMetalOscuro
        );

        ObjetoCarga objCarga = AsegurarCargaIndustrial(matJuntas, matFriccion);

        // ── 6. Sistema de Objetivos ──────────────────────────────────────────
        GameObject goObj = GameObject.Find("SistemaObjetivos") ?? new GameObject("SistemaObjetivos");
        SistemaObjetivos sisObjetivos = goObj.GetComponent<SistemaObjetivos>() ?? goObj.AddComponent<SistemaObjetivos>();

        SetCampoPrivado(sisObjetivos, "zonaRecogida", zoRecogida);
        SetCampoPrivado(sisObjetivos, "zonaDestino", zoDestino);
        SetCampoPrivado(sisObjetivos, "cargaPrincipal", objCarga);
        SetCampoPrivado(sisObjetivos, "controller", controller);

        // ── 7. ReiniciarSimulacion ────────────────────────────────────────────
        if (reiniciador == null)
        {
            reiniciador = brazoRoot.AddComponent<ReiniciarSimulacion>();
        }

        SetCampoPrivado(reiniciador, "controller", controller);
        SetCampoPrivado(reiniciador, "sistemaObjetivos", sisObjetivos);
        SetCampoPrivado(reiniciador, "cargasSimuladas", new ObjetoCarga[] { objCarga });

        // ── 8. Cámara Orbital ────────────────────────────────────────────────
        Camera cam = Camera.main;

        if (cam != null)
        {
            CamaraOrbital camOrb = cam.GetComponent<CamaraOrbital>() ?? cam.gameObject.AddComponent<CamaraOrbital>();
            SetCampoPrivado(camOrb, "puntoEnfoque", brazoRoot.transform);
        }

        // ── 9. ArticulationTelemetry (si no estaba en escena) ─────────────────
        if (telemetry == null)
        {
            telemetry = brazoRoot.AddComponent<ArticulationTelemetry>();
            SetCampoPrivado(telemetry, "controller", controller);
        }

        // ── 10. UI ────────────────────────────────────────────────────────────
        ConstruirUIRuntime(brazoRoot, controller, telemetry, sisObjetivos, reiniciador);

        Debug.Log("[SimuladorRuntimeSetup] Configuración completada correctamente.");
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Construcción completa del árbol de 8 ArticulationBody (solo si no existe)
    // ════════════════════════════════════════════════════════════════════════════
    private static void ConstruirArbolArticulaciones(
        GameObject brazoRoot,
        Material matMetalOscuro, Material matJuntas, Material matAzulIndustrial, Material matPinza,
        PhysicsMaterial matFriccion,
        out BrazoMecanicoController controller)
    {
        // [1] Base Fija — RAÍZ (única que puede tener immovable = true)
        GameObject goBaseFija = new GameObject("BaseFija");
        goBaseFija.transform.SetParent(brazoRoot.transform, false);
        goBaseFija.transform.localPosition = Vector3.zero;
        goBaseFija.transform.localRotation = Quaternion.identity;
        goBaseFija.transform.localScale = Vector3.one;

        ArticulationBody abBaseFija = goBaseFija.AddComponent<ArticulationBody>();
        abBaseFija.immovable = true;
        abBaseFija.jointType = ArticulationJointType.FixedJoint;
        abBaseFija.mass = 500f;

        GameObject visBaseFija = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visBaseFija.name = "Visual_BaseFija";
        visBaseFija.transform.SetParent(goBaseFija.transform, false);
        visBaseFija.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        visBaseFija.transform.localScale = new Vector3(4.5f, 0.2f, 4.5f);
        visBaseFija.GetComponent<MeshRenderer>().material = matMetalOscuro;
        Destroy(visBaseFija.GetComponent<Collider>());

        BoxCollider colBaseFija = goBaseFija.AddComponent<BoxCollider>();
        colBaseFija.center = new Vector3(0f, 0.2f, 0f);
        colBaseFija.size = new Vector3(4.5f, 0.4f, 4.5f);

        // [2] Torreta Base — Revolute (giro yaw)
        GameObject goTorreta = new GameObject("TorretaBase");
        goTorreta.transform.SetParent(goBaseFija.transform, false);
        goTorreta.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        goTorreta.transform.localRotation = Quaternion.identity;
        goTorreta.transform.localScale = Vector3.one;

        ArticulationBody abTorreta = goTorreta.AddComponent<ArticulationBody>();
        abTorreta.jointType = ArticulationJointType.RevoluteJoint;
        abTorreta.anchorRotation = Quaternion.Euler(0f, 0f, 90f);
        abTorreta.parentAnchorRotation = Quaternion.Euler(0f, 0f, 90f);
        abTorreta.mass = 80f;

        var dTorreta = abTorreta.xDrive;
        dTorreta.stiffness = 120000f;
        dTorreta.damping = 20000f;
        dTorreta.forceLimit = 250000f;
        dTorreta.lowerLimit = -180f;
        dTorreta.upperLimit = 180f;
        abTorreta.xDrive = dTorreta;

        GameObject visTorreta = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visTorreta.name = "Visual_Torreta";
        visTorreta.transform.SetParent(goTorreta.transform, false);
        visTorreta.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        visTorreta.transform.localScale = new Vector3(3.2f, 0.6f, 3.2f);
        visTorreta.GetComponent<MeshRenderer>().material = matMetalOscuro;
        Destroy(visTorreta.GetComponent<Collider>());

        // [3] Brazo Principal — Revolute (pitch hombro)
        GameObject goBrazo = new GameObject("BrazoPrincipal");
        goBrazo.transform.SetParent(goTorreta.transform, false);
        goBrazo.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        goBrazo.transform.localRotation = Quaternion.identity;
        goBrazo.transform.localScale = Vector3.one;

        ArticulationBody abBrazo = goBrazo.AddComponent<ArticulationBody>();
        abBrazo.jointType = ArticulationJointType.RevoluteJoint;
        abBrazo.anchorRotation = Quaternion.identity;
        abBrazo.parentAnchorRotation = Quaternion.identity;
        abBrazo.mass = 35f;

        var dBrazo = abBrazo.xDrive;
        dBrazo.stiffness = 150000f;
        dBrazo.damping = 25000f;
        dBrazo.forceLimit = 300000f;
        dBrazo.lowerLimit = -40f;
        dBrazo.upperLimit = 75f;
        abBrazo.xDrive = dBrazo;

        GameObject visHombroRot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visHombroRot.transform.SetParent(goBrazo.transform, false);
        visHombroRot.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
        visHombroRot.GetComponent<MeshRenderer>().material = matJuntas;
        Destroy(visHombroRot.GetComponent<Collider>());

        GameObject visBrazoViga = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visBrazoViga.transform.SetParent(goBrazo.transform, false);
        visBrazoViga.transform.localPosition = new Vector3(0f, 1.75f, 0f);
        visBrazoViga.transform.localScale = new Vector3(0.9f, 1.75f, 0.9f);
        visBrazoViga.GetComponent<MeshRenderer>().material = matAzulIndustrial;
        Destroy(visBrazoViga.GetComponent<Collider>());

        CapsuleCollider colBrazo = goBrazo.AddComponent<CapsuleCollider>();
        colBrazo.center = new Vector3(0f, 1.75f, 0f);
        colBrazo.radius = 0.5f;
        colBrazo.height = 3.5f;

        // [4] Antebrazo — Revolute (pitch codo)
        GameObject goAntebrazo = new GameObject("Antebrazo");
        goAntebrazo.transform.SetParent(goBrazo.transform, false);
        goAntebrazo.transform.localPosition = new Vector3(0f, 3.5f, 0f);
        goAntebrazo.transform.localRotation = Quaternion.identity;
        goAntebrazo.transform.localScale = Vector3.one;

        ArticulationBody abAntebrazo = goAntebrazo.AddComponent<ArticulationBody>();
        abAntebrazo.jointType = ArticulationJointType.RevoluteJoint;
        abAntebrazo.anchorRotation = Quaternion.identity;
        abAntebrazo.parentAnchorRotation = Quaternion.identity;
        abAntebrazo.mass = 20f;

        var dAntebrazo = abAntebrazo.xDrive;
        dAntebrazo.stiffness = 100000f;
        dAntebrazo.damping = 18000f;
        dAntebrazo.forceLimit = 200000f;
        dAntebrazo.lowerLimit = -90f;
        dAntebrazo.upperLimit = 90f;
        abAntebrazo.xDrive = dAntebrazo;

        GameObject visCodoRot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visCodoRot.transform.SetParent(goAntebrazo.transform, false);
        visCodoRot.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
        visCodoRot.GetComponent<MeshRenderer>().material = matJuntas;
        Destroy(visCodoRot.GetComponent<Collider>());

        GameObject visAnteViga = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visAnteViga.transform.SetParent(goAntebrazo.transform, false);
        visAnteViga.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        visAnteViga.transform.localScale = new Vector3(0.75f, 1.5f, 0.75f);
        visAnteViga.GetComponent<MeshRenderer>().material = matAzulIndustrial;
        Destroy(visAnteViga.GetComponent<Collider>());

        CapsuleCollider colAntebrazo = goAntebrazo.AddComponent<CapsuleCollider>();
        colAntebrazo.center = new Vector3(0f, 1.5f, 0f);
        colAntebrazo.radius = 0.42f;
        colAntebrazo.height = 3.0f;

        // [5] Muñeca Pasiva — Spherical
        GameObject goMuneca = new GameObject("MunecaPasiva");
        goMuneca.transform.SetParent(goAntebrazo.transform, false);
        goMuneca.transform.localPosition = new Vector3(0f, 3.0f, 0f);
        goMuneca.transform.localRotation = Quaternion.identity;
        goMuneca.transform.localScale = Vector3.one;

        ArticulationBody abMuneca = goMuneca.AddComponent<ArticulationBody>();
        abMuneca.jointType = ArticulationJointType.SphericalJoint;
        abMuneca.mass = 6f;

        var dMuneca = new ArticulationDrive
        {
            stiffness = 60f,
            damping = 18f,
            forceLimit = 800f,
            lowerLimit = -30f,
            upperLimit = 30f
        };

        abMuneca.xDrive = dMuneca;
        abMuneca.yDrive = dMuneca;
        abMuneca.zDrive = dMuneca;

        GameObject visMunecaRot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visMunecaRot.transform.SetParent(goMuneca.transform, false);
        visMunecaRot.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        visMunecaRot.GetComponent<MeshRenderer>().material = matJuntas;
        Destroy(visMunecaRot.GetComponent<Collider>());

        SphereCollider colMuneca = goMuneca.AddComponent<SphereCollider>();
        colMuneca.radius = 0.55f;

        // [6] Soporte Pinza — Fixed
        GameObject goSoportePinza = new GameObject("SoportePinza");
        goSoportePinza.transform.SetParent(goMuneca.transform, false);
        goSoportePinza.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        goSoportePinza.transform.localRotation = Quaternion.identity;
        goSoportePinza.transform.localScale = Vector3.one;

        ArticulationBody abSoporte = goSoportePinza.AddComponent<ArticulationBody>();
        abSoporte.jointType = ArticulationJointType.FixedJoint;
        abSoporte.mass = 4f;

        GameObject visSoporte = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visSoporte.transform.SetParent(goSoportePinza.transform, false);
        visSoporte.transform.localScale = new Vector3(0.5f, 0.25f, 1.2f);
        visSoporte.GetComponent<MeshRenderer>().material = matMetalOscuro;
        Destroy(visSoporte.GetComponent<Collider>());

        BoxCollider colSoporte = goSoportePinza.AddComponent<BoxCollider>();
        colSoporte.size = new Vector3(0.5f, 0.25f, 1.2f);

        PinzaController pinzaCtrl = goSoportePinza.AddComponent<PinzaController>();

        GameObject goSensor = new GameObject("SensorAgarre");
        goSensor.transform.SetParent(goSoportePinza.transform, false);
        goSensor.transform.localPosition = new Vector3(0f, 0.5f, 0f);

        BoxCollider colSensor = goSensor.AddComponent<BoxCollider>();
        colSensor.isTrigger = true;
        colSensor.size = new Vector3(0.8f, 0.8f, 0.6f);

        // [7] Dedo Izquierdo — Prismatic
        GameObject goDedoIzq = new GameObject("DedoIzquierdo");
        goDedoIzq.transform.SetParent(goSoportePinza.transform, false);
        goDedoIzq.transform.localPosition = new Vector3(0f, 0.2f, -0.35f);
        goDedoIzq.transform.localRotation = Quaternion.identity;
        goDedoIzq.transform.localScale = Vector3.one;

        ArticulationBody abDedoIzq = goDedoIzq.AddComponent<ArticulationBody>();
        abDedoIzq.jointType = ArticulationJointType.PrismaticJoint;
        abDedoIzq.anchorRotation = Quaternion.Euler(0f, 90f, 0f);
        abDedoIzq.parentAnchorRotation = Quaternion.Euler(0f, 90f, 0f);
        abDedoIzq.mass = 1.5f;

        var dDedoIzq = abDedoIzq.xDrive;
        dDedoIzq.stiffness = 30000f;
        dDedoIzq.damping = 4000f;
        dDedoIzq.forceLimit = 50000f;
        dDedoIzq.lowerLimit = -0.20f;
        dDedoIzq.upperLimit = 0f;
        abDedoIzq.xDrive = dDedoIzq;

        GameObject visDedoIzq = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visDedoIzq.transform.SetParent(goDedoIzq.transform, false);
        visDedoIzq.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        visDedoIzq.transform.localScale = new Vector3(0.2f, 0.9f, 0.12f);
        visDedoIzq.GetComponent<MeshRenderer>().material = matPinza;
        Destroy(visDedoIzq.GetComponent<Collider>());

        BoxCollider colDedoIzq = goDedoIzq.AddComponent<BoxCollider>();
        colDedoIzq.center = new Vector3(0f, 0.45f, 0f);
        colDedoIzq.size = new Vector3(0.2f, 0.9f, 0.12f);
        colDedoIzq.sharedMaterial = matFriccion;

        // [8] Dedo Derecho — Prismatic
        GameObject goDedoDer = new GameObject("DedoDerecho");
        goDedoDer.transform.SetParent(goSoportePinza.transform, false);
        goDedoDer.transform.localPosition = new Vector3(0f, 0.2f, 0.35f);
        goDedoDer.transform.localRotation = Quaternion.identity;
        goDedoDer.transform.localScale = Vector3.one;

        ArticulationBody abDedoDer = goDedoDer.AddComponent<ArticulationBody>();
        abDedoDer.jointType = ArticulationJointType.PrismaticJoint;
        abDedoDer.anchorRotation = Quaternion.Euler(0f, 90f, 0f);
        abDedoDer.parentAnchorRotation = Quaternion.Euler(0f, 90f, 0f);
        abDedoDer.mass = 1.5f;

        var dDedoDer = abDedoDer.xDrive;
        dDedoDer.stiffness = 30000f;
        dDedoDer.damping = 4000f;
        dDedoDer.forceLimit = 50000f;
        dDedoDer.lowerLimit = 0f;
        dDedoDer.upperLimit = 0.20f;
        abDedoDer.xDrive = dDedoDer;

        GameObject visDedoDer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visDedoDer.transform.SetParent(goDedoDer.transform, false);
        visDedoDer.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        visDedoDer.transform.localScale = new Vector3(0.2f, 0.9f, 0.12f);
        visDedoDer.GetComponent<MeshRenderer>().material = matPinza;
        Destroy(visDedoDer.GetComponent<Collider>());

        BoxCollider colDedoDer = goDedoDer.AddComponent<BoxCollider>();
        colDedoDer.center = new Vector3(0f, 0.45f, 0f);
        colDedoDer.size = new Vector3(0.2f, 0.9f, 0.12f);
        colDedoDer.sharedMaterial = matFriccion;

        // ── Wiring PinzaController ───────────────────────────────────────────
        SetCampoPrivado(pinzaCtrl, "dedoIzquierdo", abDedoIzq);
        SetCampoPrivado(pinzaCtrl, "dedoDerecho", abDedoDer);
        SetCampoPrivado(pinzaCtrl, "sensorAgarre", colSensor);
        pinzaCtrl.ConfigurarDrives();

        // ── Controladores centrales ──────────────────────────────────────────
        controller = brazoRoot.AddComponent<BrazoMecanicoController>();
        SetCampoPrivado(controller, "torretaBase", abTorreta);
        SetCampoPrivado(controller, "brazoPrincipal", abBrazo);
        SetCampoPrivado(controller, "antebrazo", abAntebrazo);
        SetCampoPrivado(controller, "munecaPasiva", abMuneca);
        SetCampoPrivado(controller, "pinzaController", pinzaCtrl);
        controller.InicializarDrives();
    }
    // ════════════════════════════════════════════════════════════════════════════
    // Helpers: mesas, zonas, carga
    // ════════════════════════════════════════════════════════════════════════════

    private static ZonaObjetivo AsegurarMesaYZona(
        string nombreMesa, string nombreZona, TipoZona tipo,
        Vector3 posicion, Material mat)
    {
        GameObject mesa = GameObject.Find(nombreMesa);

        if (mesa == null)
        {
            mesa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesa.name = nombreMesa;
        }

        mesa.transform.position = posicion;
        mesa.transform.localScale = new Vector3(2.2f, 1.0f, 2.2f);

        MeshRenderer mr = mesa.GetComponent<MeshRenderer>();
        if (mr != null) mr.material = mat;

        GameObject goZona = GameObject.Find(nombreZona);

        if (goZona == null)
        {
            goZona = new GameObject(nombreZona);
            goZona.transform.SetParent(mesa.transform, false);
            goZona.transform.localPosition = new Vector3(0f, 0.8f, 0f);

            BoxCollider c = goZona.AddComponent<BoxCollider>();
            c.isTrigger = true;
            c.size = new Vector3(1f, 1f, 1f);
        }

        ZonaObjetivo zo = goZona.GetComponent<ZonaObjetivo>() ?? goZona.AddComponent<ZonaObjetivo>();
        SetCampoPrivado(zo, "tipoZona", tipo);

        return zo;
    }

    private static ObjetoCarga AsegurarCargaIndustrial(Material matJuntas, PhysicsMaterial matFriccion)
    {
        GameObject carga = GameObject.Find("CargaIndustrial");

        if (carga == null)
        {
            // Crear primitiva limpia
            carga = GameObject.CreatePrimitive(PrimitiveType.Cube);
            carga.name = "CargaIndustrial";
        }

        carga.transform.position = new Vector3(-4.5f, 1.3f, 3.2f);
        carga.transform.localScale = new Vector3(0.40f, 0.40f, 0.40f);

        MeshRenderer mr = carga.GetComponent<MeshRenderer>();
        if (mr != null) mr.material = matJuntas;

        // Asegurar BoxCollider
        BoxCollider colCarga = carga.GetComponent<BoxCollider>();
        if (colCarga == null) colCarga = carga.AddComponent<BoxCollider>();
        colCarga.sharedMaterial = matFriccion;

        // Asegurar Rigidbody ANTES que ObjetoCarga (que tiene [RequireComponent(Rigidbody)])
        Rigidbody rbCarga = carga.GetComponent<Rigidbody>();
        if (rbCarga == null) rbCarga = carga.AddComponent<Rigidbody>();

        rbCarga.mass = 5f;
        rbCarga.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rbCarga.interpolation = RigidbodyInterpolation.Interpolate;

        // Asegurar ObjetoCarga
        ObjetoCarga objCarga = carga.GetComponent<ObjetoCarga>() ?? carga.AddComponent<ObjetoCarga>();

        return objCarga;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Construcción de UI en tiempo de ejecución
    // ════════════════════════════════════════════════════════════════════════════

    private static void ConstruirUIRuntime(
        GameObject brazoRoot, BrazoMecanicoController controller,
        ArticulationTelemetry telemetry, SistemaObjetivos sisObjetivos,
        ReiniciarSimulacion reiniciador)
    {
        // Destruir canvas anterior si existe (evita duplicados entre reinicios)
        GameObject goCanvas = GameObject.Find("CanvasSimulador");
        if (goCanvas != null) Destroy(goCanvas);

        goCanvas = new GameObject("CanvasSimulador",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = goCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = goCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Fuente de TextMeshPro
        TMP_FontAsset fontDefault = TMP_Settings.defaultFontAsset;

        if (fontDefault == null)
        {
            fontDefault = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/LiberationSans SDF");
        }

        if (fontDefault == null)
        {
            Debug.LogError("[SimuladorRuntimeSetup] No se encontró una fuente TMP. Importá TMP Essential Resources.");
            return;
        }

        // Panel Superior
        GameObject panelTop = CrearPanel(goCanvas.transform, "PanelSuperior",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -60f), new Vector2(1000f, 100f),
            new Color(0.08f, 0.1f, 0.14f, 0.88f));

        CrearTexto(panelTop.transform, "TxtTitulo",
            "SIMULADOR DE BRAZO MECÁNICO INDUSTRIAL",
            fontDefault, 24, FontStyle.Bold, TextAnchor.MiddleCenter,
            Color.white, new Vector2(0f, 20f), new Vector2(900f, 35f));

        TMP_Text txtInstrucciones = CrearTexto(panelTop.transform, "TxtInstrucciones",
            "Paso 1: Posicioná la pinza sobre la carga y cerrala para sujetarla.",
            fontDefault, 17, FontStyle.Normal, TextAnchor.MiddleCenter,
            new Color(0.95f, 0.95f, 0.45f),
            new Vector2(0f, -20f), new Vector2(900f, 30f));

        // Panel Izquierdo: Controles
        GameObject panelIzq = CrearPanel(goCanvas.transform, "PanelControles",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(200f, -20f), new Vector2(360f, 560f),
            new Color(0.08f, 0.1f, 0.14f, 0.88f));

        CrearTexto(panelIzq.transform, "HeaderControles",
            "CONTROLES DE ARTICULACIÓN",
            fontDefault, 18, FontStyle.Bold, TextAnchor.MiddleCenter,
            Color.cyan, new Vector2(0f, 250f), new Vector2(320f, 30f));

        Slider sBase = CrearSliderConEtiqueta(
            panelIzq.transform, "SliderBase", "Rotación Base (Torreta)",
            fontDefault, -180f, 180f, 0f, 180f, out TMP_Text lblBase);

        Slider sBrazo = CrearSliderConEtiqueta(
            panelIzq.transform, "SliderBrazo", "Brazo Principal (Hombro)",
            fontDefault, -40f, 75f, 0f, 100f, out TMP_Text lblBrazo);

        Slider sAntebrazo = CrearSliderConEtiqueta(
            panelIzq.transform, "SliderAntebrazo", "Antebrazo (Codo)",
            fontDefault, -90f, 90f, 0f, 20f, out TMP_Text lblAntebrazo);

        Slider sPinza = CrearSliderConEtiqueta(
            panelIzq.transform, "SliderPinza", "Apertura Pinza",
            fontDefault, 0f, 1f, 1f, -60f, out TMP_Text lblPinza);

        Button btnResetMec = CrearBoton(
            panelIzq.transform, "BtnResetMecanismo",
            "Reiniciar Mecanismo", fontDefault,
            new Vector2(0f, -160f), new Vector2(280f, 40f),
            new Color(0.2f, 0.45f, 0.7f));

        Button btnResetSim = CrearBoton(
            panelIzq.transform, "BtnResetSimulacion",
            "Reiniciar Simulación Completa", fontDefault,
            new Vector2(0f, -215f), new Vector2(280f, 40f),
            new Color(0.7f, 0.3f, 0.2f));

        // Panel Derecho: Telemetría
        GameObject panelDer = CrearPanel(goCanvas.transform, "PanelTelemetria",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-200f, -20f), new Vector2(360f, 560f),
            new Color(0.08f, 0.1f, 0.14f, 0.88f));

        CrearTexto(panelDer.transform, "HeaderTelemetria",
            "TELEMETRÍA EN TIEMPO REAL",
            fontDefault, 18, FontStyle.Bold, TextAnchor.MiddleCenter,
            Color.green, new Vector2(0f, 250f), new Vector2(320f, 30f));

        TMP_Text txtEstadoOp = CrearTexto(panelDer.transform, "TxtEstadoOperacion",
            "ESTADO: EsperandoRecogida",
            fontDefault, 15, FontStyle.Bold, TextAnchor.MiddleCenter,
            Color.yellow, new Vector2(0f, 205f), new Vector2(320f, 35f));

        TMP_Text tBase = CrearTexto(panelDer.transform, "TxtTelemetriaBase",
            "Base:\nTarget: 0.0° | Real: 0.0°",
            fontDefault, 14, FontStyle.Normal, TextAnchor.MiddleLeft,
            Color.white, new Vector2(0f, 140f), new Vector2(300f, 40f));

        TMP_Text tBrazo = CrearTexto(panelDer.transform, "TxtTelemetriaBrazo",
            "Hombro:\nTarget: 0.0° | Real: 0.0°",
            fontDefault, 14, FontStyle.Normal, TextAnchor.MiddleLeft,
            Color.white, new Vector2(0f, 75f), new Vector2(300f, 40f));

        TMP_Text tAntebrazo = CrearTexto(panelDer.transform, "TxtTelemetriaAntebrazo",
            "Codo:\nTarget: 0.0° | Real: 0.0°",
            fontDefault, 14, FontStyle.Normal, TextAnchor.MiddleLeft,
            Color.white, new Vector2(0f, 10f), new Vector2(300f, 40f));

        TMP_Text tPinza = CrearTexto(panelDer.transform, "TxtTelemetriaPinza",
            "Pinza:\nTarget: 20.0cm | Real: 20.0cm",
            fontDefault, 14, FontStyle.Normal, TextAnchor.MiddleLeft,
            Color.white, new Vector2(0f, -55f), new Vector2(300f, 40f));

        TMP_Text tMuneca = CrearTexto(panelDer.transform, "TxtTelemetriaMuneca",
            "Muñeca Pasiva:\nDesviación: 0.0° [Física Libre]",
            fontDefault, 14, FontStyle.Normal, TextAnchor.MiddleLeft,
            new Color(0.7f, 0.85f, 1f),
            new Vector2(0f, -120f), new Vector2(300f, 40f));

        // BrazoMecanicoUI
        BrazoMecanicoUI ui = goCanvas.AddComponent<BrazoMecanicoUI>();

        SetCampoPrivado(ui, "controller", controller);
        SetCampoPrivado(ui, "telemetry", telemetry);
        SetCampoPrivado(ui, "sistemaObjetivos", sisObjetivos);
        SetCampoPrivado(ui, "reiniciador", reiniciador);

        SetCampoPrivado(ui, "sliderBase", sBase);
        SetCampoPrivado(ui, "sliderBrazo", sBrazo);
        SetCampoPrivado(ui, "sliderAntebrazo", sAntebrazo);
        SetCampoPrivado(ui, "sliderPinza", sPinza);

        SetCampoPrivado(ui, "lblValorSliderBase", lblBase);
        SetCampoPrivado(ui, "lblValorSliderBrazo", lblBrazo);
        SetCampoPrivado(ui, "lblValorSliderAntebrazo", lblAntebrazo);
        SetCampoPrivado(ui, "lblValorSliderPinza", lblPinza);

        SetCampoPrivado(ui, "btnResetMecanismo", btnResetMec);
        SetCampoPrivado(ui, "btnResetSimulacion", btnResetSim);

        SetCampoPrivado(ui, "txtTelemetriaBase", tBase);
        SetCampoPrivado(ui, "txtTelemetriaBrazo", tBrazo);
        SetCampoPrivado(ui, "txtTelemetriaAntebrazo", tAntebrazo);
        SetCampoPrivado(ui, "txtTelemetriaPinza", tPinza);
        SetCampoPrivado(ui, "txtTelemetriaMuneca", tMuneca);

        SetCampoPrivado(ui, "txtInstrucciones", txtInstrucciones);
        SetCampoPrivado(ui, "txtEstadoOperacion", txtEstadoOp);

        SetCampoPrivado(reiniciador, "ui", ui);
    }
    // ════════════════════════════════════════════════════════════════════════════
    // Utilidades de creación de UI
    // ════════════════════════════════════════════════════════════════════════════

    private static Material CrearMaterialShaderColor(
        string nombre, Color color, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        Material mat = new Material(shader)
        {
            name = nombre,
            color = color
        };

        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smoothness);

        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", metallic);

        return mat;
    }

    private static void SetCampoPrivado(object target, string campo, object valor)
    {
        if (target == null) return;

        var f = target.GetType().GetField(campo,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public);

        if (f != null)
            f.SetValue(target, valor);
        else
            Debug.LogWarning($"[SimuladorRuntimeSetup] Campo no encontrado: {target.GetType().Name}.{campo}");
    }

    private static GameObject CrearPanel(
        Transform parent, string nombre,
        Vector2 aMin, Vector2 aMax, Vector2 pivot,
        Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(nombre,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        go.GetComponent<Image>().color = color;

        return go;
    }

    // Crear texto usando TextMeshPro
    private static TMP_Text CrearTexto(
        Transform parent, string nombre, string texto,
        TMP_FontAsset font, int tamano,
        FontStyle estilo, TextAnchor alineacion,
        Color color, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(nombre,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));

        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TMP_Text t = go.GetComponent<TMP_Text>();

        t.font = font;
        t.text = texto;
        t.fontSize = tamano;
        t.fontStyle = estilo == FontStyle.Bold
            ? FontStyles.Bold
            : FontStyles.Normal;

        t.alignment = ConvertirAlineacion(alineacion);
        t.color = color;
        t.enableWordWrapping = true;
        t.overflowMode = TextOverflowModes.Overflow;

        return t;
    }

    // Convertir alineación de Unity UI a TextMeshPro
    private static TextAlignmentOptions ConvertirAlineacion(TextAnchor alineacion)
    {
        switch (alineacion)
        {
            case TextAnchor.MiddleLeft:
                return TextAlignmentOptions.MidlineLeft;

            case TextAnchor.MiddleRight:
                return TextAlignmentOptions.MidlineRight;

            default:
                return TextAlignmentOptions.Midline;
        }
    }

    // Crear slider con etiquetas TextMeshPro
    private static Slider CrearSliderConEtiqueta(
        Transform parent, string nombre, string titulo,
        TMP_FontAsset font, float min, float max,
        float val, float posY, out TMP_Text lblValor)
    {
        GameObject cont = new GameObject(nombre + "_Contenedor", typeof(RectTransform));
        cont.transform.SetParent(parent, false);

        RectTransform rtCont = cont.GetComponent<RectTransform>();
        rtCont.anchoredPosition = new Vector2(0f, posY);
        rtCont.sizeDelta = new Vector2(310f, 65f);

        CrearTexto(cont.transform, "Titulo", titulo, font,
            13, FontStyle.Bold, TextAnchor.MiddleLeft,
            Color.white, new Vector2(-50f, 18f), new Vector2(200f, 24f));

        lblValor = CrearTexto(cont.transform, "Valor", $"{val:F1}", font,
            13, FontStyle.Normal, TextAnchor.MiddleRight,
            Color.cyan, new Vector2(100f, 18f), new Vector2(90f, 24f));

        GameObject goSlider = new GameObject(nombre,
            typeof(RectTransform), typeof(Slider));

        goSlider.transform.SetParent(cont.transform, false);

        RectTransform rtSlider = goSlider.GetComponent<RectTransform>();
        rtSlider.anchoredPosition = new Vector2(0f, -12f);
        rtSlider.sizeDelta = new Vector2(290f, 20f);

        GameObject bg = new GameObject("Background",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        bg.transform.SetParent(goSlider.transform, false);

        RectTransform rtBg = bg.GetComponent<RectTransform>();
        rtBg.anchorMin = Vector2.zero;
        rtBg.anchorMax = Vector2.one;
        rtBg.sizeDelta = Vector2.zero;

        bg.GetComponent<Image>().color = new Color(0.2f, 0.22f, 0.28f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(goSlider.transform, false);

        RectTransform rtFA = fillArea.GetComponent<RectTransform>();
        rtFA.anchorMin = new Vector2(0f, 0.25f);
        rtFA.anchorMax = new Vector2(1f, 0.75f);
        rtFA.sizeDelta = new Vector2(-15f, 0f);

        GameObject fill = new GameObject("Fill",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.15f, 0.65f, 0.95f);

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(goSlider.transform, false);

        RectTransform rtHA = handleArea.GetComponent<RectTransform>();
        rtHA.anchorMin = Vector2.zero;
        rtHA.anchorMax = Vector2.one;
        rtHA.sizeDelta = new Vector2(-15f, 0f);

        GameObject handle = new GameObject("Handle",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        handle.transform.SetParent(handleArea.transform, false);
        handle.GetComponent<RectTransform>().sizeDelta = new Vector2(20f, 24f);
        handle.GetComponent<Image>().color = Color.white;

        Slider slider = goSlider.GetComponent<Slider>();

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = val;

        return slider;
    }

    // Crear botón con texto TextMeshPro
    private static Button CrearBoton(
        Transform parent, string nombre, string texto,
        TMP_FontAsset font, Vector2 pos,
        Vector2 size, Color colorNormal)
    {
        GameObject goBtn = new GameObject(nombre,
            typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button));

        goBtn.transform.SetParent(parent, false);

        RectTransform rt = goBtn.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = goBtn.GetComponent<Image>();
        img.color = colorNormal;

        Button btn = goBtn.GetComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.normalColor = colorNormal;
        cb.highlightedColor = colorNormal * 1.25f;
        cb.pressedColor = colorNormal * 0.8f;
        btn.colors = cb;

        CrearTexto(goBtn.transform, "Text", texto, font,
            14, FontStyle.Bold, TextAnchor.MiddleCenter,
            Color.white, Vector2.zero, size);

        return btn;
    }
}