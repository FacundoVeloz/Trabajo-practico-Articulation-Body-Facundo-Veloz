#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class SimuladorSetupMenu
{
    [MenuItem("Simulador/Reconstruir y Configurar Escena Completa")]
    public static void ConfigurarEscenaCompleta()
    {
        EditorUtility.DisplayProgressBar("Configurando Simulador", "Buscando materiales...", 0.1f);

        // 1. Obtener materiales existentes
        Material matMetalOscuro = AssetDatabase.LoadAssetAtPath<Material>("Assets/YughuesFreeMetalMaterials/Materials/M_YFMeM_46.mat");
        Material matJuntas = AssetDatabase.LoadAssetAtPath<Material>("Assets/YughuesFreeMetalMaterials/Materials/M_YFMeM_49.mat");
        Material matAzulIndustrial = AssetDatabase.LoadAssetAtPath<Material>("Assets/YughuesFreeMetalMaterials/Materials/M_YFMeM_16.mat");
        Material matPinza = AssetDatabase.LoadAssetAtPath<Material>("Assets/YughuesFreeMetalMaterials/Materials/M_YFMeM_21.mat");

        if (matMetalOscuro == null) matMetalOscuro = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
        if (matJuntas == null) matJuntas = matMetalOscuro;
        if (matAzulIndustrial == null) matAzulIndustrial = matMetalOscuro;
        if (matPinza == null) matPinza = matMetalOscuro;

        // Fricción alta para la pinza y carga
        PhysicsMaterial matFisicoFriccion = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/PhysicMaterial_AltaFriccion.physicMaterial");
        if (matFisicoFriccion == null)
        {
            matFisicoFriccion = new PhysicsMaterial("FriccionIndustrial")
            {
                dynamicFriction = 0.9f,
                staticFriction = 0.95f,
                frictionCombine = PhysicsMaterialCombine.Maximum,
                bounciness = 0.0f
            };
            AssetDatabase.CreateAsset(matFisicoFriccion, "Assets/PhysicMaterial_AltaFriccion.physicMaterial");
        }

        // 2. Limpiar o reutilizar BrazoMecanico
        EditorUtility.DisplayProgressBar("Configurando Simulador", "Construyendo jerarquía de articulaciones...", 0.3f);
        GameObject brazoRoot = GameObject.Find("BrazoMecanico");
        if (brazoRoot != null)
        {
            Undo.DestroyObjectImmediate(brazoRoot);
        }

        brazoRoot = new GameObject("BrazoMecanico");
        brazoRoot.transform.position = new Vector3(0f, 0f, 0f);
        brazoRoot.transform.rotation = Quaternion.identity;
        brazoRoot.transform.localScale = Vector3.one;
        Undo.RegisterCreatedObjectUndo(brazoRoot, "Crear BrazoMecanico");

        // [1] Base Fija (Raíz ArticulationBody)
        GameObject goBaseFija = new GameObject("BaseFija");
        goBaseFija.transform.SetParent(brazoRoot.transform, false);
        goBaseFija.transform.localPosition = Vector3.zero;
        goBaseFija.transform.localRotation = Quaternion.identity;
        goBaseFija.transform.localScale = Vector3.one;

        ArticulationBody abBaseFija = goBaseFija.AddComponent<ArticulationBody>();
        abBaseFija.immovable = true;
        abBaseFija.jointType = ArticulationJointType.FixedJoint;
        abBaseFija.mass = 500f;

        GameObject visualBaseFija = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visualBaseFija.name = "Visual_BaseFija";
        visualBaseFija.transform.SetParent(goBaseFija.transform, false);
        visualBaseFija.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        visualBaseFija.transform.localScale = new Vector3(4.5f, 0.2f, 4.5f);
        visualBaseFija.GetComponent<MeshRenderer>().sharedMaterial = matMetalOscuro;
        Object.DestroyImmediate(visualBaseFija.GetComponent<Collider>());

        BoxCollider colBaseFija = goBaseFija.AddComponent<BoxCollider>();
        colBaseFija.center = new Vector3(0f, 0.2f, 0f);
        colBaseFija.size = new Vector3(4.5f, 0.4f, 4.5f);

        // [2] Torreta Base (Revolute en Eje Y)
        GameObject goTorreta = new GameObject("TorretaBase");
        goTorreta.transform.SetParent(goBaseFija.transform, false);
        goTorreta.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        goTorreta.transform.localRotation = Quaternion.identity;
        goTorreta.transform.localScale = Vector3.one;

        ArticulationBody abTorreta = goTorreta.AddComponent<ArticulationBody>();
        abTorreta.immovable = false;
        abTorreta.jointType = ArticulationJointType.RevoluteJoint;
        abTorreta.anchorRotation = Quaternion.Euler(0f, 0f, 90f); // Rota alrededor de Y
        abTorreta.parentAnchorRotation = Quaternion.Euler(0f, 0f, 90f);
        abTorreta.mass = 80f;

        var driveTorreta = abTorreta.xDrive;
        driveTorreta.stiffness = 120000f;
        driveTorreta.damping = 20000f;
        driveTorreta.forceLimit = 250000f;
        driveTorreta.lowerLimit = -180f;
        driveTorreta.upperLimit = 180f;
        abTorreta.xDrive = driveTorreta;

        GameObject visualTorreta = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visualTorreta.name = "Visual_Torreta";
        visualTorreta.transform.SetParent(goTorreta.transform, false);
        visualTorreta.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        visualTorreta.transform.localScale = new Vector3(3.2f, 0.6f, 3.2f);
        visualTorreta.GetComponent<MeshRenderer>().sharedMaterial = matMetalOscuro;
        Object.DestroyImmediate(visualTorreta.GetComponent<Collider>());

        // [3] Brazo Principal / Hombro (Revolute en Eje X)
        GameObject goBrazo = new GameObject("BrazoPrincipal");
        goBrazo.transform.SetParent(goTorreta.transform, false);
        goBrazo.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        goBrazo.transform.localRotation = Quaternion.identity;
        goBrazo.transform.localScale = Vector3.one;

        ArticulationBody abBrazo = goBrazo.AddComponent<ArticulationBody>();
        abBrazo.immovable = false;
        abBrazo.jointType = ArticulationJointType.RevoluteJoint;
        abBrazo.anchorRotation = Quaternion.identity; // Rota sobre X (pitch elevación)
        abBrazo.parentAnchorRotation = Quaternion.identity;
        abBrazo.mass = 35f;

        var driveBrazo = abBrazo.xDrive;
        driveBrazo.stiffness = 150000f;
        driveBrazo.damping = 25000f;
        driveBrazo.forceLimit = 300000f;
        driveBrazo.lowerLimit = -40f;
        driveBrazo.upperLimit = 75f;
        abBrazo.xDrive = driveBrazo;

        // Visual rótula esférica hombro
        GameObject visualHombroRotula = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visualHombroRotula.name = "Visual_HombroRotula";
        visualHombroRotula.transform.SetParent(goBrazo.transform, false);
        visualHombroRotula.transform.localPosition = Vector3.zero;
        visualHombroRotula.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
        visualHombroRotula.GetComponent<MeshRenderer>().sharedMaterial = matJuntas;
        Object.DestroyImmediate(visualHombroRotula.GetComponent<Collider>());

        // Visual viga brazo azul
        GameObject visualBrazoViga = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visualBrazoViga.name = "Visual_BrazoViga";
        visualBrazoViga.transform.SetParent(goBrazo.transform, false);
        visualBrazoViga.transform.localPosition = new Vector3(0f, 1.75f, 0f);
        visualBrazoViga.transform.localScale = new Vector3(0.9f, 1.75f, 0.9f);
        visualBrazoViga.GetComponent<MeshRenderer>().sharedMaterial = matAzulIndustrial;
        Object.DestroyImmediate(visualBrazoViga.GetComponent<Collider>());

        CapsuleCollider colBrazo = goBrazo.AddComponent<CapsuleCollider>();
        colBrazo.center = new Vector3(0f, 1.75f, 0f);
        colBrazo.radius = 0.5f;
        colBrazo.height = 3.5f;

        // [4] Antebrazo / Codo (Revolute en Eje X)
        GameObject goAntebrazo = new GameObject("Antebrazo");
        goAntebrazo.transform.SetParent(goBrazo.transform, false);
        goAntebrazo.transform.localPosition = new Vector3(0f, 3.5f, 0f);
        goAntebrazo.transform.localRotation = Quaternion.identity;
        goAntebrazo.transform.localScale = Vector3.one;

        ArticulationBody abAntebrazo = goAntebrazo.AddComponent<ArticulationBody>();
        abAntebrazo.immovable = false;
        abAntebrazo.jointType = ArticulationJointType.RevoluteJoint;
        abAntebrazo.anchorRotation = Quaternion.identity;
        abAntebrazo.parentAnchorRotation = Quaternion.identity;
        abAntebrazo.mass = 20f;

        var driveAntebrazo = abAntebrazo.xDrive;
        driveAntebrazo.stiffness = 100000f;
        driveAntebrazo.damping = 18000f;
        driveAntebrazo.forceLimit = 200000f;
        driveAntebrazo.lowerLimit = -90f;
        driveAntebrazo.upperLimit = 90f;
        abAntebrazo.xDrive = driveAntebrazo;

        // Visual rótula codo
        GameObject visualCodoRotula = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visualCodoRotula.name = "Visual_CodoRotula";
        visualCodoRotula.transform.SetParent(goAntebrazo.transform, false);
        visualCodoRotula.transform.localPosition = Vector3.zero;
        visualCodoRotula.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
        visualCodoRotula.GetComponent<MeshRenderer>().sharedMaterial = matJuntas;
        Object.DestroyImmediate(visualCodoRotula.GetComponent<Collider>());

        // Visual viga antebrazo azul
        GameObject visualAntebrazoViga = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visualAntebrazoViga.name = "Visual_AntebrazoViga";
        visualAntebrazoViga.transform.SetParent(goAntebrazo.transform, false);
        visualAntebrazoViga.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        visualAntebrazoViga.transform.localScale = new Vector3(0.75f, 1.5f, 0.75f);
        visualAntebrazoViga.GetComponent<MeshRenderer>().sharedMaterial = matAzulIndustrial;
        Object.DestroyImmediate(visualAntebrazoViga.GetComponent<Collider>());

        CapsuleCollider colAntebrazo = goAntebrazo.AddComponent<CapsuleCollider>();
        colAntebrazo.center = new Vector3(0f, 1.5f, 0f);
        colAntebrazo.radius = 0.42f;
        colAntebrazo.height = 3.0f;

        // [5] Muñeca Pasiva (Spherical pasiva)
        GameObject goMuneca = new GameObject("MunecaPasiva");
        goMuneca.transform.SetParent(goAntebrazo.transform, false);
        goMuneca.transform.localPosition = new Vector3(0f, 3.0f, 0f);
        goMuneca.transform.localRotation = Quaternion.identity;
        goMuneca.transform.localScale = Vector3.one;

        ArticulationBody abMuneca = goMuneca.AddComponent<ArticulationBody>();
        abMuneca.immovable = false;
        abMuneca.jointType = ArticulationJointType.SphericalJoint;
        abMuneca.mass = 6f;

        // Articulación pasiva suave: baja rigidez para reaccionar a inercia y gravedad
        var driveMuneca = new ArticulationDrive { stiffness = 60f, damping = 18f, forceLimit = 800f, lowerLimit = -30f, upperLimit = 30f };
        abMuneca.xDrive = driveMuneca;
        abMuneca.yDrive = driveMuneca;
        abMuneca.zDrive = driveMuneca;

        GameObject visualMunecaRotula = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visualMunecaRotula.name = "Visual_MunecaRotula";
        visualMunecaRotula.transform.SetParent(goMuneca.transform, false);
        visualMunecaRotula.transform.localPosition = Vector3.zero;
        visualMunecaRotula.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        visualMunecaRotula.GetComponent<MeshRenderer>().sharedMaterial = matJuntas;
        Object.DestroyImmediate(visualMunecaRotula.GetComponent<Collider>());

        SphereCollider colMuneca = goMuneca.AddComponent<SphereCollider>();
        colMuneca.radius = 0.55f;

        // [6] Soporte de la Pinza (Fixed Joint)
        GameObject goSoportePinza = new GameObject("SoportePinza");
        goSoportePinza.transform.SetParent(goMuneca.transform, false);
        goSoportePinza.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        goSoportePinza.transform.localRotation = Quaternion.identity;
        goSoportePinza.transform.localScale = Vector3.one;

        ArticulationBody abSoporte = goSoportePinza.AddComponent<ArticulationBody>();
        abSoporte.immovable = false;
        abSoporte.jointType = ArticulationJointType.FixedJoint;
        abSoporte.mass = 4f;

        GameObject visualSoporte = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualSoporte.name = "Visual_SoportePinza";
        visualSoporte.transform.SetParent(goSoportePinza.transform, false);
        visualSoporte.transform.localPosition = Vector3.zero;
        visualSoporte.transform.localScale = new Vector3(0.5f, 0.25f, 1.2f);
        visualSoporte.GetComponent<MeshRenderer>().sharedMaterial = matMetalOscuro;
        Object.DestroyImmediate(visualSoporte.GetComponent<Collider>());

        BoxCollider colSoporte = goSoportePinza.AddComponent<BoxCollider>();
        colSoporte.size = new Vector3(0.5f, 0.25f, 1.2f);

        // PinzaController en SoportePinza
        PinzaController pinzaCtrl = goSoportePinza.AddComponent<PinzaController>();

        // Sensor Trigger de agarre entre los dedos
        GameObject goSensor = new GameObject("SensorAgarre");
        goSensor.transform.SetParent(goSoportePinza.transform, false);
        goSensor.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        BoxCollider colSensor = goSensor.AddComponent<BoxCollider>();
        colSensor.isTrigger = true;
        colSensor.size = new Vector3(0.8f, 0.8f, 0.6f);

        // [7] Dedo Izquierdo (Prismatic en Z)
        GameObject goDedoIzq = new GameObject("DedoIzquierdo");
        goDedoIzq.transform.SetParent(goSoportePinza.transform, false);
        goDedoIzq.transform.localPosition = new Vector3(0f, 0.2f, -0.35f);
        goDedoIzq.transform.localRotation = Quaternion.identity;
        goDedoIzq.transform.localScale = Vector3.one;

        ArticulationBody abDedoIzq = goDedoIzq.AddComponent<ArticulationBody>();
        abDedoIzq.immovable = false;
        abDedoIzq.jointType = ArticulationJointType.PrismaticJoint;
        abDedoIzq.anchorRotation = Quaternion.Euler(0f, 90f, 0f);
        abDedoIzq.parentAnchorRotation = Quaternion.Euler(0f, 90f, 0f);
        abDedoIzq.mass = 1.5f;

        var driveDedoIzq = abDedoIzq.xDrive;
        driveDedoIzq.stiffness = 30000f;
        driveDedoIzq.damping = 4000f;
        driveDedoIzq.forceLimit = 50000f;
        driveDedoIzq.lowerLimit = -0.20f;
        driveDedoIzq.upperLimit = 0.0f;
        abDedoIzq.xDrive = driveDedoIzq;

        GameObject visualDedoIzq = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualDedoIzq.name = "Visual_DedoIzq";
        visualDedoIzq.transform.SetParent(goDedoIzq.transform, false);
        visualDedoIzq.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        visualDedoIzq.transform.localScale = new Vector3(0.2f, 0.9f, 0.12f);
        visualDedoIzq.GetComponent<MeshRenderer>().sharedMaterial = matPinza;
        Object.DestroyImmediate(visualDedoIzq.GetComponent<Collider>());

        BoxCollider colDedoIzq = goDedoIzq.AddComponent<BoxCollider>();
        colDedoIzq.center = new Vector3(0f, 0.45f, 0f);
        colDedoIzq.size = new Vector3(0.2f, 0.9f, 0.12f);
        colDedoIzq.sharedMaterial = matFisicoFriccion;

        // [8] Dedo Derecho (Prismatic en Z)
        GameObject goDedoDer = new GameObject("DedoDerecho");
        goDedoDer.transform.SetParent(goSoportePinza.transform, false);
        goDedoDer.transform.localPosition = new Vector3(0f, 0.2f, 0.35f);
        goDedoDer.transform.localRotation = Quaternion.identity;
        goDedoDer.transform.localScale = Vector3.one;

        ArticulationBody abDedoDer = goDedoDer.AddComponent<ArticulationBody>();
        abDedoDer.immovable = false;
        abDedoDer.jointType = ArticulationJointType.PrismaticJoint;
        abDedoDer.anchorRotation = Quaternion.Euler(0f, 90f, 0f);
        abDedoDer.parentAnchorRotation = Quaternion.Euler(0f, 90f, 0f);
        abDedoDer.mass = 1.5f;

        var driveDedoDer = abDedoDer.xDrive;
        driveDedoDer.stiffness = 30000f;
        driveDedoDer.damping = 4000f;
        driveDedoDer.forceLimit = 50000f;
        driveDedoDer.lowerLimit = 0.0f;
        driveDedoDer.upperLimit = 0.20f;
        abDedoDer.xDrive = driveDedoDer;

        GameObject visualDedoDer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualDedoDer.name = "Visual_DedoDer";
        visualDedoDer.transform.SetParent(goDedoDer.transform, false);
        visualDedoDer.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        visualDedoDer.transform.localScale = new Vector3(0.2f, 0.9f, 0.12f);
        visualDedoDer.GetComponent<MeshRenderer>().sharedMaterial = matPinza;
        Object.DestroyImmediate(visualDedoDer.GetComponent<Collider>());

        BoxCollider colDedoDer = goDedoDer.AddComponent<BoxCollider>();
        colDedoDer.center = new Vector3(0f, 0.45f, 0f);
        colDedoDer.size = new Vector3(0.2f, 0.9f, 0.12f);
        colDedoDer.sharedMaterial = matFisicoFriccion;

        // Vincular componentes en PinzaController
        SerializedObject soPinza = new SerializedObject(pinzaCtrl);
        soPinza.FindProperty("dedoIzquierdo").objectReferenceValue = abDedoIzq;
        soPinza.FindProperty("dedoDerecho").objectReferenceValue = abDedoDer;
        soPinza.FindProperty("sensorAgarre").objectReferenceValue = colSensor;
        soPinza.ApplyModifiedPropertiesWithoutUndo();

        // Controladores centrales en BrazoMecanico
        BrazoMecanicoController controller = brazoRoot.AddComponent<BrazoMecanicoController>();
        ArticulationTelemetry telemetry = brazoRoot.AddComponent<ArticulationTelemetry>();
        ReiniciarSimulacion reiniciador = brazoRoot.AddComponent<ReiniciarSimulacion>();

        SerializedObject soCtrl = new SerializedObject(controller);
        soCtrl.FindProperty("torretaBase").objectReferenceValue = abTorreta;
        soCtrl.FindProperty("brazoPrincipal").objectReferenceValue = abBrazo;
        soCtrl.FindProperty("antebrazo").objectReferenceValue = abAntebrazo;
        soCtrl.FindProperty("munecaPasiva").objectReferenceValue = abMuneca;
        soCtrl.FindProperty("pinzaController").objectReferenceValue = pinzaCtrl;
        soCtrl.ApplyModifiedPropertiesWithoutUndo();

        // 3. Crear Mesas de Trabajo y Carga
        EditorUtility.DisplayProgressBar("Configurando Simulador", "Construyendo plataformas de recogida y destino...", 0.6f);

        // Mesa Recogida
        GameObject mesaRecogida = GameObject.Find("MesaRecogida");
        if (mesaRecogida != null) Object.DestroyImmediate(mesaRecogida);
        mesaRecogida = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mesaRecogida.name = "MesaRecogida";
        mesaRecogida.transform.position = new Vector3(-3.2f, 0.5f, 3.2f);
        mesaRecogida.transform.localScale = new Vector3(2.2f, 1.0f, 2.2f);
        mesaRecogida.GetComponent<MeshRenderer>().sharedMaterial = matMetalOscuro;

        // Zona Recogida (Trigger)
        GameObject goZonaRecogida = new GameObject("ZonaRecogida");
        goZonaRecogida.transform.SetParent(mesaRecogida.transform, false);
        goZonaRecogida.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        ZonaObjetivo zoRecogida = goZonaRecogida.AddComponent<ZonaObjetivo>();
        BoxCollider colZonaRecogida = goZonaRecogida.AddComponent<BoxCollider>();
        colZonaRecogida.isTrigger = true;
        colZonaRecogida.size = new Vector3(1.0f, 1.0f, 1.0f);
        SerializedObject soZR = new SerializedObject(zoRecogida);
        soZR.FindProperty("tipoZona").enumValueIndex = (int)TipoZona.Recogida;
        soZR.FindProperty("nombreZona").stringValue = "Plataforma de Recogida";
        soZR.ApplyModifiedPropertiesWithoutUndo();

        // Mesa Destino
        GameObject mesaDestino = GameObject.Find("MesaDestino");
        if (mesaDestino != null) Object.DestroyImmediate(mesaDestino);
        mesaDestino = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mesaDestino.name = "MesaDestino";
        mesaDestino.transform.position = new Vector3(3.2f, 0.5f, 3.2f);
        mesaDestino.transform.localScale = new Vector3(2.2f, 1.0f, 2.2f);
        mesaDestino.GetComponent<MeshRenderer>().sharedMaterial = matMetalOscuro;

        // Zona Destino (Trigger)
        GameObject goZonaDestino = new GameObject("ZonaDestino");
        goZonaDestino.transform.SetParent(mesaDestino.transform, false);
        goZonaDestino.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        ZonaObjetivo zoDestino = goZonaDestino.AddComponent<ZonaObjetivo>();
        BoxCollider colZonaDestino = goZonaDestino.AddComponent<BoxCollider>();
        colZonaDestino.isTrigger = true;
        colZonaDestino.size = new Vector3(1.0f, 1.0f, 1.0f);
        SerializedObject soZD = new SerializedObject(zoDestino);
        soZD.FindProperty("tipoZona").enumValueIndex = (int)TipoZona.Destino;
        soZD.FindProperty("nombreZona").stringValue = "Plataforma de Destino";
        soZD.ApplyModifiedPropertiesWithoutUndo();

        // Carga Industrial Física
        GameObject carga = GameObject.Find("CargaIndustrial");
        if (carga != null) Object.DestroyImmediate(carga);
        carga = GameObject.CreatePrimitive(PrimitiveType.Cube);
        carga.name = "CargaIndustrial";
        carga.transform.position = new Vector3(-3.2f, 1.3f, 3.2f); // Sobre la mesa de recogida
        carga.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
        carga.GetComponent<MeshRenderer>().sharedMaterial = matJuntas;
        BoxCollider colCarga = carga.GetComponent<BoxCollider>();
        colCarga.sharedMaterial = matFisicoFriccion;

        Rigidbody rbCarga = carga.AddComponent<Rigidbody>();
        rbCarga.mass = 5f;
        rbCarga.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rbCarga.interpolation = RigidbodyInterpolation.Interpolate;

        ObjetoCarga objCarga = carga.AddComponent<ObjetoCarga>();

        // 4. Sistema de Objetivos
        EditorUtility.DisplayProgressBar("Configurando Simulador", "Configurando sistema de objetivos...", 0.8f);
        GameObject goObjetivos = GameObject.Find("SistemaObjetivos");
        if (goObjetivos == null) goObjetivos = new GameObject("SistemaObjetivos");
        SistemaObjetivos sisObjetivos = goObjetivos.GetComponent<SistemaObjetivos>();
        if (sisObjetivos == null) sisObjetivos = goObjetivos.AddComponent<SistemaObjetivos>();

        SerializedObject soObj = new SerializedObject(sisObjetivos);
        soObj.FindProperty("zonaRecogida").objectReferenceValue = zoRecogida;
        soObj.FindProperty("zonaDestino").objectReferenceValue = zoDestino;
        soObj.FindProperty("cargaPrincipal").objectReferenceValue = objCarga;
        soObj.FindProperty("controller").objectReferenceValue = controller;
        soObj.ApplyModifiedPropertiesWithoutUndo();

        // Vincular ReiniciarSimulacion
        SerializedObject soReinicio = new SerializedObject(reiniciador);
        soReinicio.FindProperty("controller").objectReferenceValue = controller;
        soReinicio.FindProperty("sistemaObjetivos").objectReferenceValue = sisObjetivos;
        soReinicio.ApplyModifiedPropertiesWithoutUndo();

        // 5. Configurar Cámara Orbital
        Camera cam = Camera.main;
        if (cam != null)
        {
            CamaraOrbital camOrb = cam.GetComponent<CamaraOrbital>();
            if (camOrb == null) camOrb = cam.gameObject.AddComponent<CamaraOrbital>();
            SerializedObject soCam = new SerializedObject(camOrb);
            soCam.FindProperty("puntoEnfoque").objectReferenceValue = brazoRoot.transform;
            soCam.ApplyModifiedPropertiesWithoutUndo();
        }

        // 6. Construir Canvas UI
        ConstruirUI(brazoRoot, controller, telemetry, sisObjetivos, reiniciador);

        // Guardar Prefab y Escena
        PrefabUtility.SaveAsPrefabAssetAndConnect(brazoRoot, "Assets/Prefab/BrazoMecanico.prefab", InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        EditorUtility.ClearProgressBar();
        Debug.Log("<color=green><b>[Simulador]</b> ¡Escena completa y árbol de 8 ArticulationBody configurados con éxito!</color>");
    }

    private static void ConstruirUI(GameObject brazoRoot, BrazoMecanicoController controller, ArticulationTelemetry telemetry, SistemaObjetivos sisObjetivos, ReiniciarSimulacion reiniciador)
    {
        GameObject goCanvas = GameObject.Find("CanvasSimulador");
        if (goCanvas != null) Object.DestroyImmediate(goCanvas);

        goCanvas = new GameObject("CanvasSimulador", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = goCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = goCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Asegurar EventSystem
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        Font fontDefault = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fontDefault == null) fontDefault = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Panel Superior: Título y Objetivos
        GameObject panelTop = CrearPanel(goCanvas.transform, "PanelSuperior", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1000f, 100f), new Color(0.08f, 0.1f, 0.14f, 0.85f));
        Text txtTitulo = CrearTexto(panelTop.transform, "TxtTitulo", "SIMULADOR DE BRAZO MECÁNICO INDUSTRIAL", fontDefault, 24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, 20f), new Vector2(900f, 35f));
        Text txtInstrucciones = CrearTexto(panelTop.transform, "TxtInstrucciones", "Paso 1: Posicioná la pinza sobre la carga y cerrala para sujetarla.", fontDefault, 17, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.9f, 0.9f, 0.4f), new Vector2(0f, -20f), new Vector2(900f, 30f));

        // Panel Izquierdo: Controles
        GameObject panelIzq = CrearPanel(goCanvas.transform, "PanelControles", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(200f, -20f), new Vector2(360f, 560f), new Color(0.08f, 0.1f, 0.14f, 0.85f));
        CrearTexto(panelIzq.transform, "HeaderControles", "CONTROLES DE ARTICULACIÓN", fontDefault, 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.cyan, new Vector2(0f, 250f), new Vector2(320f, 30f));

        // Slider Base
        Slider sBase = CrearSliderConEtiqueta(panelIzq.transform, "SliderBase", "Rotación Base (Torreta)", fontDefault, -180f, 180f, 0f, 180f, out Text lblBase);
        Slider sBrazo = CrearSliderConEtiqueta(panelIzq.transform, "SliderBrazo", "Brazo Principal (Hombro)", fontDefault, -40f, 75f, 0f, 100f, out Text lblBrazo);
        Slider sAntebrazo = CrearSliderConEtiqueta(panelIzq.transform, "SliderAntebrazo", "Antebrazo (Codo)", fontDefault, -90f, 90f, 0f, 20f, out Text lblAntebrazo);
        Slider sPinza = CrearSliderConEtiqueta(panelIzq.transform, "SliderPinza", "Apertura Pinza", fontDefault, 0f, 1f, 1f, -60f, out Text lblPinza);

        // Botones de Reinicio
        Button btnResetMec = CrearBoton(panelIzq.transform, "BtnResetMecanismo", "Reiniciar Mecanismo", fontDefault, new Vector2(0f, -160f), new Vector2(280f, 40f), new Color(0.2f, 0.45f, 0.7f));
        Button btnResetSim = CrearBoton(panelIzq.transform, "BtnResetSimulacion", "Reiniciar Simulación Completa", fontDefault, new Vector2(0f, -215f), new Vector2(280f, 40f), new Color(0.7f, 0.3f, 0.2f));

        // Panel Derecho: Telemetría
        GameObject panelDer = CrearPanel(goCanvas.transform, "PanelTelemetria", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-200f, -20f), new Vector2(360f, 560f), new Color(0.08f, 0.1f, 0.14f, 0.85f));
        CrearTexto(panelDer.transform, "HeaderTelemetria", "TELEMETRÍA EN TIEMPO REAL", fontDefault, 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.green, new Vector2(0f, 250f), new Vector2(320f, 30f));

        Text txtEstadoOp = CrearTexto(panelDer.transform, "TxtEstadoOperacion", "ESTADO: EsperandoRecogida", fontDefault, 15, FontStyle.Bold, TextAnchor.MiddleCenter, Color.yellow, new Vector2(0f, 205f), new Vector2(320f, 35f));

        Text tBase = CrearTexto(panelDer.transform, "TxtTelemetriaBase", "Base:\nTarget: 0.0° | Real: 0.0°", fontDefault, 14, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, 140f), new Vector2(300f, 40f));
        Text tBrazo = CrearTexto(panelDer.transform, "TxtTelemetriaBrazo", "Hombro:\nTarget: 0.0° | Real: 0.0°", fontDefault, 14, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, 75f), new Vector2(300f, 40f));
        Text tAntebrazo = CrearTexto(panelDer.transform, "TxtTelemetriaAntebrazo", "Codo:\nTarget: 0.0° | Real: 0.0°", fontDefault, 14, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, 10f), new Vector2(300f, 40f));
        Text tPinza = CrearTexto(panelDer.transform, "TxtTelemetriaPinza", "Pinza:\nTarget: 20.0cm | Real: 20.0cm", fontDefault, 14, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, -55f), new Vector2(300f, 40f));
        Text tMuneca = CrearTexto(panelDer.transform, "TxtTelemetriaMuneca", "Muñeca Pasiva:\nDesviación: 0.0° [Física Libre]", fontDefault, 14, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.7f, 0.8f, 1f), new Vector2(0f, -120f), new Vector2(300f, 40f));

        // Componente BrazoMecanicoUI
        BrazoMecanicoUI ui = goCanvas.AddComponent<BrazoMecanicoUI>();
        SerializedObject soUI = new SerializedObject(ui);
        soUI.FindProperty("controller").objectReferenceValue = controller;
        soUI.FindProperty("telemetry").objectReferenceValue = telemetry;
        soUI.FindProperty("sistemaObjetivos").objectReferenceValue = sisObjetivos;
        soUI.FindProperty("reiniciador").objectReferenceValue = reiniciador;

        soUI.FindProperty("sliderBase").objectReferenceValue = sBase;
        soUI.FindProperty("sliderBrazo").objectReferenceValue = sBrazo;
        soUI.FindProperty("sliderAntebrazo").objectReferenceValue = sAntebrazo;
        soUI.FindProperty("sliderPinza").objectReferenceValue = sPinza;

        soUI.FindProperty("lblValorSliderBase").objectReferenceValue = lblBase;
        soUI.FindProperty("lblValorSliderBrazo").objectReferenceValue = lblBrazo;
        soUI.FindProperty("lblValorSliderAntebrazo").objectReferenceValue = lblAntebrazo;
        soUI.FindProperty("lblValorSliderPinza").objectReferenceValue = lblPinza;

        soUI.FindProperty("btnResetMecanismo").objectReferenceValue = btnResetMec;
        soUI.FindProperty("btnResetSimulacion").objectReferenceValue = btnResetSim;

        soUI.FindProperty("txtTelemetriaBase").objectReferenceValue = tBase;
        soUI.FindProperty("txtTelemetriaBrazo").objectReferenceValue = tBrazo;
        soUI.FindProperty("txtTelemetriaAntebrazo").objectReferenceValue = tAntebrazo;
        soUI.FindProperty("txtTelemetriaPinza").objectReferenceValue = tPinza;
        soUI.FindProperty("txtTelemetriaMuneca").objectReferenceValue = tMuneca;

        soUI.FindProperty("txtInstrucciones").objectReferenceValue = txtInstrucciones;
        soUI.FindProperty("txtEstadoOperacion").objectReferenceValue = txtEstadoOp;
        soUI.ApplyModifiedPropertiesWithoutUndo();

        // Vincular UI a ReiniciarSimulacion
        SerializedObject soReinicio = new SerializedObject(reiniciador);
        soReinicio.FindProperty("ui").objectReferenceValue = ui;
        soReinicio.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CrearPanel(Transform parent, string nombre, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static Text CrearTexto(Transform parent, string nombre, string texto, Font font, int tamano, FontStyle estilo, TextAnchor alineacion, Color color, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Text t = go.GetComponent<Text>();
        t.font = font;
        t.text = texto;
        t.fontSize = tamano;
        t.fontStyle = estilo;
        t.alignment = alineacion;
        t.color = color;
        return t;
    }

    private static Slider CrearSliderConEtiqueta(Transform parent, string nombre, string titulo, Font font, float min, float max, float val, float posY, out Text lblValor)
    {
        GameObject cont = new GameObject(nombre + "_Contenedor", typeof(RectTransform));
        cont.transform.SetParent(parent, false);
        RectTransform rtCont = cont.GetComponent<RectTransform>();
        rtCont.anchoredPosition = new Vector2(0f, posY);
        rtCont.sizeDelta = new Vector2(310f, 65f);

        CrearTexto(cont.transform, "Titulo", titulo, font, 13, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white, new Vector2(-50f, 18f), new Vector2(200f, 24f));
        lblValor = CrearTexto(cont.transform, "Valor", $"{val:F1}", font, 13, FontStyle.Normal, TextAnchor.MiddleRight, Color.cyan, new Vector2(100f, 18f), new Vector2(90f, 24f));

        // Slider GameObject
        GameObject goSlider = new GameObject(nombre, typeof(RectTransform), typeof(Slider));
        goSlider.transform.SetParent(cont.transform, false);
        RectTransform rtSlider = goSlider.GetComponent<RectTransform>();
        rtSlider.anchoredPosition = new Vector2(0f, -12f);
        rtSlider.sizeDelta = new Vector2(290f, 20f);

        // Background
        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(goSlider.transform, false);
        RectTransform rtBg = bg.GetComponent<RectTransform>();
        rtBg.anchorMin = Vector2.zero;
        rtBg.anchorMax = Vector2.one;
        rtBg.sizeDelta = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.2f, 0.22f, 0.28f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(goSlider.transform, false);
        RectTransform rtFA = fillArea.GetComponent<RectTransform>();
        rtFA.anchorMin = new Vector2(0f, 0.25f);
        rtFA.anchorMax = new Vector2(1f, 0.75f);
        rtFA.sizeDelta = new Vector2(-15f, 0f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform rtFill = fill.GetComponent<RectTransform>();
        rtFill.sizeDelta = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.15f, 0.65f, 0.95f);

        // Handle Slide Area
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(goSlider.transform, false);
        RectTransform rtHA = handleArea.GetComponent<RectTransform>();
        rtHA.anchorMin = Vector2.zero;
        rtHA.anchorMax = Vector2.one;
        rtHA.sizeDelta = new Vector2(-15f, 0f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform rtHandle = handle.GetComponent<RectTransform>();
        rtHandle.sizeDelta = new Vector2(20f, 24f);
        handle.GetComponent<Image>().color = Color.white;

        Slider slider = goSlider.GetComponent<Slider>();
        slider.fillRect = rtFill;
        slider.handleRect = rtHandle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = val;

        return slider;
    }

    private static Button CrearBoton(Transform parent, string nombre, string texto, Font font, Vector2 pos, Vector2 size, Color colorNormal)
    {
        GameObject goBtn = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
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

        CrearTexto(goBtn.transform, "Text", texto, font, 14, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, Vector2.zero, size);

        return btn;
    }
}
#endif
