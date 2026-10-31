using UnityEngine;

public enum FaseOperacion
{
    EsperandoRecogida,
    CargaSujeta,
    Transportando,
    AlineadoEnDestino,
    OperacionCompletada,
    CargaCaida
}

public class SistemaObjetivos : MonoBehaviour
{
    [Header("Referencias de la Escena")]
    [SerializeField] private ZonaObjetivo zonaRecogida;
    [SerializeField] private ZonaObjetivo zonaDestino;
    [SerializeField] private ObjetoCarga cargaPrincipal;
    [SerializeField] private BrazoMecanicoController controller;

    [Header("Estado y Progreso")]
    [SerializeField] private FaseOperacion faseActual = FaseOperacion.EsperandoRecogida;
    [SerializeField][TextArea(2, 4)] private string mensajeEstado = "";

    public FaseOperacion Fase => faseActual;
    public string MensajeEstado => mensajeEstado;

    private void Update()
    {
        EvaluarProgresion();
    }

    private void EvaluarProgresion()
    {
        if (cargaPrincipal == null) return;

        bool sujeta = cargaPrincipal.EstaSujeta;
        bool enRecogida = (zonaRecogida != null && zonaRecogida.CargaPresente);
        bool enDestino = (zonaDestino != null && zonaDestino.CargaPresente);

        if (!sujeta && !enRecogida && !enDestino && cargaPrincipal.transform.position.y < -1f)
        {
            faseActual = FaseOperacion.CargaCaida;
            mensajeEstado = "¡Alerta! Carga caída fuera de zona. Presioná 'Reiniciar Simulación'.";
            return;
        }

        switch (faseActual)
        {
            case FaseOperacion.EsperandoRecogida:
                if (sujeta)
                {
                    faseActual = FaseOperacion.CargaSujeta;
                    mensajeEstado = "Carga sujeta correctamente. Elevala y rotá hacia la zona de destino.";
                }
                else
                {
                    mensajeEstado = "Paso 1: Posicioná la pinza sobre la carga y cerrala para sujetarla.";
                }
                break;

            case FaseOperacion.CargaSujeta:
            case FaseOperacion.Transportando:
                if (!sujeta)
                {
                    if (enDestino)
                    {
                        faseActual = FaseOperacion.OperacionCompletada;
                        mensajeEstado = "¡OPERACIÓN COMPLETADA CON ÉXITO! Carga depositada y liberada.";
                    }
                    else if (enRecogida)
                    {
                        faseActual = FaseOperacion.EsperandoRecogida;
                        mensajeEstado = "Carga liberada nuevamente en zona de recogida.";
                    }
                    else
                    {
                        faseActual = FaseOperacion.CargaCaida;
                        mensajeEstado = "Carga caída durante el traslado. Reintentá la maniobra.";
                    }
                }
                else
                {
                    if (enDestino)
                    {
                        faseActual = FaseOperacion.AlineadoEnDestino;
                        mensajeEstado = "Paso 2: Carga sobre zona de destino. Descendé y abrí la pinza.";
                    }
                    else
                    {
                        faseActual = FaseOperacion.Transportando;
                        mensajeEstado = "Transportando carga... Mové el brazo hacia la zona de destino.";
                    }
                }
                break;

            case FaseOperacion.AlineadoEnDestino:
                if (!sujeta)
                {
                    if (enDestino)
                    {
                        faseActual = FaseOperacion.OperacionCompletada;
                        mensajeEstado = "¡OPERACIÓN COMPLETADA CON ÉXITO! Carga depositada correctamente.";
                    }
                    else
                    {
                        faseActual = FaseOperacion.CargaCaida;
                        mensajeEstado = "La carga cayó fuera del objetivo al abrir la pinza.";
                    }
                }
                else
                {
                    if (!enDestino)
                    {
                        faseActual = FaseOperacion.Transportando;
                        mensajeEstado = "Transportando carga hacia zona de destino...";
                    }
                    else
                    {
                        mensajeEstado = "Paso 2: Abrí la pinza para depositar la carga.";
                    }
                }
                break;

            case FaseOperacion.OperacionCompletada:
                if (sujeta || !enDestino)
                {
                    faseActual = FaseOperacion.Transportando;
                }
                else
                {
                    mensajeEstado = "¡OPERACIÓN COMPLETADA CON ÉXITO!";
                }
                break;

            case FaseOperacion.CargaCaida:
                if (sujeta)
                {
                    faseActual = FaseOperacion.CargaSujeta;
                }
                break;
        }
    }

    public void ResetearObjetivos()
    {
        faseActual = FaseOperacion.EsperandoRecogida;
        mensajeEstado = "Paso 1: Posicioná la pinza sobre la carga y cerrala para sujetarla.";
    }
}