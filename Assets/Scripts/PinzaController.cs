using UnityEngine;

public enum EstadoPinza
{
    Abierta,
    Cerrando,
    SujetandoCarga,
    CerradaVacia
}

public class PinzaController : MonoBehaviour
{
    [Header("Articulaciones de los Dedos (Prismatic)")]
    [Tooltip("Cuerpo prismático del dedo izquierdo")]
    [SerializeField] private ArticulationBody dedoIzquierdo;
    [Tooltip("Cuerpo prismático del dedo derecho")]
    [SerializeField] private ArticulationBody dedoDerecho;

    [Header("Parámetros de Apertura")]
    [Tooltip("Apertura máxima de cada dedo en metros")]
    [SerializeField] private float aperturaMaxima = 0.20f;
    [Tooltip("Apertura mínima (dedos juntos)")]
    [SerializeField] private float aperturaMinima = 0.02f;

    [Header("Parámetros Físicos de Apriete")]
    [Tooltip("Rigidez (stiffness) del drive de los dedos")]
    [SerializeField] private float driveStiffness = 30000f;
    [Tooltip("Amortiguamiento (damping) del drive")]
    [SerializeField] private float driveDamping = 4000f;
    [Tooltip("Límite de fuerza de apriete en Newtons")]
    [SerializeField] private float driveForceLimit = 50000f;

    [Header("Detección de Contacto y Agarre")]
    [Tooltip("Zona de agarre ubicada entre los dos dedos")]
    [SerializeField] private Collider sensorAgarre;

    [Header("Telemetría")]
    [SerializeField] private float aperturaTarget = 0.20f;
    [SerializeField] private float aperturaReal = 0.20f;
    [SerializeField] private EstadoPinza estadoActual = EstadoPinza.Abierta;

    private ObjetoCarga cargaEnContacto = null;
    private ObjetoCarga cargaSujeta = null;
    private FixedJoint juntaTemporal = null;

    public float AperturaTarget => aperturaTarget;
    public float AperturaReal => aperturaReal;
    public EstadoPinza Estado => estadoActual;
    public ObjetoCarga CargaSujeta => cargaSujeta;

    private void Awake()
    {
        ConfigurarDrives();
    }

    private void Start()
    {
        SetApertura(aperturaMaxima);
    }

    public void ConfigurarDrives()
    {
        if (dedoIzquierdo != null)
        {
            var drive = dedoIzquierdo.xDrive;
            drive.stiffness = driveStiffness;
            drive.damping = driveDamping;
            drive.forceLimit = driveForceLimit;
            drive.lowerLimit = -aperturaMaxima;
            drive.upperLimit = 0f;
            dedoIzquierdo.xDrive = drive;
        }

        if (dedoDerecho != null)
        {
            var drive = dedoDerecho.xDrive;
            drive.stiffness = driveStiffness;
            drive.damping = driveDamping;
            drive.forceLimit = driveForceLimit;
            drive.lowerLimit = 0f;
            drive.upperLimit = aperturaMaxima;
            dedoDerecho.xDrive = drive;
        }
    }

    public void SetAperturaNormalizada(float normalizada01)
    {
        normalizada01 = Mathf.Clamp01(normalizada01);
        float apertura = Mathf.Lerp(aperturaMinima, aperturaMaxima, normalizada01);
        SetApertura(apertura);
    }

    public void SetApertura(float distanciaMetros)
    {
        aperturaTarget = Mathf.Clamp(distanciaMetros, aperturaMinima, aperturaMaxima);
    }

    private void FixedUpdate()
    {
        AplicarTargetsFisicos();
        ActualizarLecturaReal();
        ActualizarEstadoAgarre();
    }

    private void AplicarTargetsFisicos()
    {
        if (dedoIzquierdo != null)
        {
            var drive = dedoIzquierdo.xDrive;
            drive.target = -aperturaTarget;
            dedoIzquierdo.xDrive = drive;
        }

        if (dedoDerecho != null)
        {
            var drive = dedoDerecho.xDrive;
            drive.target = aperturaTarget;
            dedoDerecho.xDrive = drive;
        }
    }

    private void ActualizarLecturaReal()
    {
        if (dedoIzquierdo != null && dedoDerecho != null)
        {
            float posIzq = Mathf.Abs(dedoIzquierdo.jointPosition[0]);
            float posDer = Mathf.Abs(dedoDerecho.jointPosition[0]);
            aperturaReal = (posIzq + posDer) * 0.5f;
        }
        else if (dedoIzquierdo != null)
        {
            aperturaReal = Mathf.Abs(dedoIzquierdo.jointPosition[0]);
        }
    }

    private void ActualizarEstadoAgarre()
    {
        bool intentandoCerrar = aperturaTarget <= (aperturaMinima + 0.04f);

        if (intentandoCerrar)
        {
            if (cargaEnContacto != null && cargaSujeta == null)
            {
                cargaSujeta = cargaEnContacto;
                cargaSujeta.SetSujeta(true);

                if (juntaTemporal == null && cargaSujeta.Rb != null)
                {
                    juntaTemporal = gameObject.AddComponent<FixedJoint>();
                    juntaTemporal.connectedBody = cargaSujeta.Rb;
                    juntaTemporal.breakForce = 15000f;
                    juntaTemporal.breakTorque = 15000f;
                }
            }

            estadoActual = (cargaSujeta != null) ? EstadoPinza.SujetandoCarga : EstadoPinza.CerradaVacia;
        }
        else
        {
            if (cargaSujeta != null)
            {
                LiberarCarga();
            }

            estadoActual = EstadoPinza.Abierta;
        }
    }

    private void LiberarCarga()
    {
        if (juntaTemporal != null)
        {
            Destroy(juntaTemporal);
            juntaTemporal = null;
        }

        if (cargaSujeta != null)
        {
            cargaSujeta.SetSujeta(false);
            cargaSujeta = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ObjetoCarga carga = other.GetComponent<ObjetoCarga>();
        if (carga != null)
        {
            cargaEnContacto = carga;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        ObjetoCarga carga = other.GetComponent<ObjetoCarga>();
        if (carga != null)
        {
            cargaEnContacto = carga;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ObjetoCarga carga = other.GetComponent<ObjetoCarga>();
        if (carga != null && carga == cargaEnContacto)
        {
            cargaEnContacto = null;
        }
    }

    public void ResetearPinza()
    {
        LiberarCarga();
        cargaEnContacto = null;
        SetApertura(aperturaMaxima);
    }
}