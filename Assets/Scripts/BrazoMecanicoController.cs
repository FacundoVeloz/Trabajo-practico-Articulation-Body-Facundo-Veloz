using UnityEngine;

public class BrazoMecanicoController : MonoBehaviour
{
    [Header("Articulaciones Principales")]
    [Tooltip("Articulación Revolute de la base (rotación horizontal)")]
    [SerializeField] private ArticulationBody torretaBase;

    [Tooltip("Articulación Revolute del brazo principal / hombro (elevación vertical)")]
    [SerializeField] private ArticulationBody brazoPrincipal;

    [Tooltip("Articulación Revolute del antebrazo / codo (flexión vertical)")]
    [SerializeField] private ArticulationBody antebrazo;

    [Tooltip("Articulación Spherical pasiva de la muñeca (responde a gravedad y contacto)")]
    [SerializeField] private ArticulationBody munecaPasiva;

    [Tooltip("Controlador del efector final (pinza prismática)")]
    [SerializeField] private PinzaController pinzaController;

    [Header("Límites de Movimiento (Grados)")]
    [SerializeField] private float limiteBaseMin = -180f;
    [SerializeField] private float limiteBaseMax = 180f;
    [SerializeField] private float limiteBrazoMin = -40f;
    [SerializeField] private float limiteBrazoMax = 75f;
    [SerializeField] private float limiteAntebrazoMin = -90f;
    [SerializeField] private float limiteAntebrazoMax = 90f;

    [Header("Parámetros Dinámicos de los Drives")]
    [SerializeField] private float baseStiffness = 120000f;
    [SerializeField] private float baseDamping = 20000f;
    [SerializeField] private float baseForceLimit = 250000f;

    [SerializeField] private float brazoStiffness = 150000f;
    [SerializeField] private float brazoDamping = 25000f;
    [SerializeField] private float brazoForceLimit = 300000f;

    [SerializeField] private float antebrazoStiffness = 100000f;
    [SerializeField] private float antebrazoDamping = 18000f;
    [SerializeField] private float antebrazoForceLimit = 200000f;

    [Header("Velocidad de Movimiento (Grados/seg)")]
    [SerializeField] private float velocidadGiroBase = 45f;
    [SerializeField] private float velocidadBrazo = 40f;
    [SerializeField] private float velocidadAntebrazo = 45f;

    private float targetBaseDeseado = 0f;
    private float targetBrazoDeseado = 0f;
    private float targetAntebrazoDeseado = 0f;
    private float targetPinzaDeseado = 1f;

    private float targetBaseActual = 0f;
    private float targetBrazoActual = 0f;
    private float targetAntebrazoActual = 0f;

    public float TargetBase => targetBaseDeseado;
    public float TargetBrazo => targetBrazoDeseado;
    public float TargetAntebrazo => targetAntebrazoDeseado;
    public float TargetPinza => targetPinzaDeseado;

    public ArticulationBody TorretaBase => torretaBase;
    public ArticulationBody BrazoPrincipal => brazoPrincipal;
    public ArticulationBody Antebrazo => antebrazo;
    public ArticulationBody MunecaPasiva => munecaPasiva;
    public PinzaController Pinza => pinzaController;

    private void Awake()
    {
        InicializarDrives();
    }

    public void InicializarDrives()
    {
        if (torretaBase != null)
        {
            var drive = torretaBase.xDrive;
            drive.stiffness = baseStiffness;
            drive.damping = baseDamping;
            drive.forceLimit = baseForceLimit;
            drive.lowerLimit = limiteBaseMin;
            drive.upperLimit = limiteBaseMax;
            torretaBase.xDrive = drive;
        }

        if (brazoPrincipal != null)
        {
            var drive = brazoPrincipal.xDrive;
            drive.stiffness = brazoStiffness;
            drive.damping = brazoDamping;
            drive.forceLimit = brazoForceLimit;
            drive.lowerLimit = limiteBrazoMin;
            drive.upperLimit = limiteBrazoMax;
            brazoPrincipal.xDrive = drive;
        }

        if (antebrazo != null)
        {
            var drive = antebrazo.xDrive;
            drive.stiffness = antebrazoStiffness;
            drive.damping = antebrazoDamping;
            drive.forceLimit = antebrazoForceLimit;
            drive.lowerLimit = limiteAntebrazoMin;
            drive.upperLimit = limiteAntebrazoMax;
            antebrazo.xDrive = drive;
        }

        if (munecaPasiva != null)
        {
            var xDrive = munecaPasiva.xDrive;
            xDrive.stiffness = 50f;
            xDrive.damping = 15f;
            xDrive.forceLimit = 500f;
            munecaPasiva.xDrive = xDrive;

            var yDrive = munecaPasiva.yDrive;
            yDrive.stiffness = 50f;
            yDrive.damping = 15f;
            yDrive.forceLimit = 500f;
            munecaPasiva.yDrive = yDrive;

            var zDrive = munecaPasiva.zDrive;
            zDrive.stiffness = 50f;
            zDrive.damping = 15f;
            zDrive.forceLimit = 500f;
            munecaPasiva.zDrive = zDrive;
        }
    }

    #region Setters de Control (Llamados desde UI o Input)

    public void SetTargetBase(float anguloGrados)
    {
        targetBaseDeseado = Mathf.Clamp(anguloGrados, limiteBaseMin, limiteBaseMax);
    }

    public void SetTargetBrazo(float anguloGrados)
    {
        targetBrazoDeseado = Mathf.Clamp(anguloGrados, limiteBrazoMin, limiteBrazoMax);
    }

    public void SetTargetAntebrazo(float anguloGrados)
    {
        targetAntebrazoDeseado = Mathf.Clamp(anguloGrados, limiteAntebrazoMin, limiteAntebrazoMax);
    }

    public void SetTargetPinza(float normalizada01)
    {
        targetPinzaDeseado = Mathf.Clamp01(normalizada01);
        if (pinzaController != null)
        {
            pinzaController.SetAperturaNormalizada(targetPinzaDeseado);
        }
    }

    #endregion

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        targetBaseActual = Mathf.MoveTowards(targetBaseActual, targetBaseDeseado, velocidadGiroBase * dt);
        targetBrazoActual = Mathf.MoveTowards(targetBrazoActual, targetBrazoDeseado, velocidadBrazo * dt);
        targetAntebrazoActual = Mathf.MoveTowards(targetAntebrazoActual, targetAntebrazoDeseado, velocidadAntebrazo * dt);

        if (torretaBase != null)
        {
            var drive = torretaBase.xDrive;
            drive.target = targetBaseActual;
            torretaBase.xDrive = drive;
        }

        if (brazoPrincipal != null)
        {
            var drive = brazoPrincipal.xDrive;
            drive.target = targetBrazoActual;
            brazoPrincipal.xDrive = drive;
        }

        if (antebrazo != null)
        {
            var drive = antebrazo.xDrive;
            drive.target = targetAntebrazoActual;
            antebrazo.xDrive = drive;
        }
    }

    public void ResetMecanismo()
    {
        targetBaseDeseado = 0f;
        targetBrazoDeseado = 0f;
        targetAntebrazoDeseado = 0f;
        targetPinzaDeseado = 1f;

        targetBaseActual = 0f;
        targetBrazoActual = 0f;
        targetAntebrazoActual = 0f;

        if (torretaBase != null)
        {
            var drive = torretaBase.xDrive;
            drive.target = 0f;
            torretaBase.xDrive = drive;
            torretaBase.jointVelocity = new ArticulationReducedSpace(0f);
        }

        if (brazoPrincipal != null)
        {
            var drive = brazoPrincipal.xDrive;
            drive.target = 0f;
            brazoPrincipal.xDrive = drive;
            brazoPrincipal.jointVelocity = new ArticulationReducedSpace(0f);
        }

        if (antebrazo != null)
        {
            var drive = antebrazo.xDrive;
            drive.target = 0f;
            antebrazo.xDrive = drive;
            antebrazo.jointVelocity = new ArticulationReducedSpace(0f);
        }

        if (munecaPasiva != null)
        {
            munecaPasiva.jointVelocity = new ArticulationReducedSpace(0f, 0f, 0f);
        }

        if (pinzaController != null)
        {
            pinzaController.ResetearPinza();
        }
    }
}