using UnityEngine;

public enum EstadoCarga
{
    EnZonaRecogida,
    SujetaPorPinza,
    EnTransporte,
    EnZonaDestino,
    CaidaFueraDeZona
}

[RequireComponent(typeof(Rigidbody))]
public class ObjetoCarga : MonoBehaviour
{
    [Header("Configuración Física")]
    [Tooltip("Masa física de la carga en kilogramos.")]
    [SerializeField] private float masa = 5f;

    [Header("Estado Actual")]
    [SerializeField] private EstadoCarga estadoActual = EstadoCarga.EnZonaRecogida;

    private Rigidbody rb;
    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;
    private bool estaSujeta = false;

    public EstadoCarga Estado => estadoActual;
    public bool EstaSujeta => estaSujeta;
    public Rigidbody Rb => rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = masa;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;
    }

    public void SetSujeta(bool sujeta)
    {
        estaSujeta = sujeta;
        if (estaSujeta)
        {
            estadoActual = EstadoCarga.SujetaPorPinza;
        }
        else
        {
            if (estadoActual == EstadoCarga.SujetaPorPinza)
            {
                estadoActual = EstadoCarga.EnTransporte;
            }
        }
    }

    public void ActualizarEstadoZona(EstadoCarga nuevoEstado)
    {
        if (!estaSujeta)
        {
            estadoActual = nuevoEstado;
        }
    }

    public void ResetearPosicion()
    {
        estaSujeta = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = posicionInicial;
        transform.rotation = rotacionInicial;
        estadoActual = EstadoCarga.EnZonaRecogida;
    }
}