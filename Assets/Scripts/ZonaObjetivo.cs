using UnityEngine;

public enum TipoZona
{
    Recogida,
    Destino
}

[RequireComponent(typeof(Collider))]
public class ZonaObjetivo : MonoBehaviour
{
    [Header("Configuración de Zona")]
    [SerializeField] private TipoZona tipoZona = TipoZona.Destino;
    [SerializeField] private string nombreZona = "Zona de Destino";

    private bool cargaPresente = false;
    private ObjetoCarga cargaActual = null;

    public TipoZona Tipo => tipoZona;
    public string Nombre => nombreZona;
    public bool CargaPresente => cargaPresente;
    public ObjetoCarga CargaActual => cargaActual;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        ObjetoCarga carga = other.GetComponent<ObjetoCarga>();
        if (carga != null)
        {
            cargaPresente = true;
            cargaActual = carga;
            if (!carga.EstaSujeta)
            {
                carga.ActualizarEstadoZona(tipoZona == TipoZona.Destino ? EstadoCarga.EnZonaDestino : EstadoCarga.EnZonaRecogida);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        ObjetoCarga carga = other.GetComponent<ObjetoCarga>();
        if (carga != null)
        {
            cargaPresente = true;
            cargaActual = carga;
            if (!carga.EstaSujeta)
            {
                carga.ActualizarEstadoZona(tipoZona == TipoZona.Destino ? EstadoCarga.EnZonaDestino : EstadoCarga.EnZonaRecogida);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ObjetoCarga carga = other.GetComponent<ObjetoCarga>();
        if (carga != null && carga == cargaActual)
        {
            cargaPresente = false;
            cargaActual = null;
            if (!carga.EstaSujeta)
            {
                carga.ActualizarEstadoZona(EstadoCarga.CaidaFueraDeZona);
            }
        }
    }
}