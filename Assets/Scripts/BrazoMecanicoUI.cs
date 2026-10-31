
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BrazoMecanicoUI : MonoBehaviour
{
    [Header("Referencias de Control y Físicas")]
    [SerializeField] private BrazoMecanicoController controller;
    [SerializeField] private ArticulationTelemetry telemetry;
    [SerializeField] private SistemaObjetivos sistemaObjetivos;
    [SerializeField] private ReiniciarSimulacion reiniciador;

    [Header("Sliders de Control de Articulaciones")]
    [SerializeField] private Slider sliderBase;
    [SerializeField] private Slider sliderBrazo;
    [SerializeField] private Slider sliderAntebrazo;
    [SerializeField] private Slider sliderPinza;

    [Header("Etiquetas de Valor del Slider")]
    [SerializeField] private TMP_Text lblValorSliderBase;
    [SerializeField] private TMP_Text lblValorSliderBrazo;
    [SerializeField] private TMP_Text lblValorSliderAntebrazo;
    [SerializeField] private TMP_Text lblValorSliderPinza;

    [Header("Botones de Operación")]
    [SerializeField] private Button btnResetMecanismo;
    [SerializeField] private Button btnResetSimulacion;

    [Header("Textos de Telemetría (Target vs Real)")]
    [SerializeField] private TMP_Text txtTelemetriaBase;
    [SerializeField] private TMP_Text txtTelemetriaBrazo;
    [SerializeField] private TMP_Text txtTelemetriaAntebrazo;
    [SerializeField] private TMP_Text txtTelemetriaPinza;
    [SerializeField] private TMP_Text txtTelemetriaMuneca;

    [Header("Panel Superior de Misión")]
    [SerializeField] private TMP_Text txtInstrucciones;
    [SerializeField] private TMP_Text txtEstadoOperacion;

    private void Start()
    {
        VincularEventosUI();
        SincronizarSlidersConController();
    }

    private void VincularEventosUI()
    {
        if (sliderBase != null)
        {
            sliderBase.minValue = -180f;
            sliderBase.maxValue = 180f;
            sliderBase.onValueChanged.AddListener(OnSliderBaseChanged);
        }

        if (sliderBrazo != null)
        {
            sliderBrazo.minValue = -40f;
            sliderBrazo.maxValue = 75f;
            sliderBrazo.onValueChanged.AddListener(OnSliderBrazoChanged);
        }

        if (sliderAntebrazo != null)
        {
            sliderAntebrazo.minValue = -90f;
            sliderAntebrazo.maxValue = 90f;
            sliderAntebrazo.onValueChanged.AddListener(OnSliderAntebrazoChanged);
        }

        if (sliderPinza != null)
        {
            sliderPinza.minValue = 0f;
            sliderPinza.maxValue = 1f;
            sliderPinza.onValueChanged.AddListener(OnSliderPinzaChanged);
        }

        if (btnResetMecanismo != null)
            btnResetMecanismo.onClick.AddListener(OnBtnResetMecanismoClicked);

        if (btnResetSimulacion != null)
            btnResetSimulacion.onClick.AddListener(OnBtnResetSimulacionClicked);
    }

    public void SincronizarSlidersConController()
    {
        if (controller == null) return;

        if (sliderBase != null) sliderBase.value = controller.TargetBase;
        if (sliderBrazo != null) sliderBrazo.value = controller.TargetBrazo;
        if (sliderAntebrazo != null) sliderAntebrazo.value = controller.TargetAntebrazo;
        if (sliderPinza != null) sliderPinza.value = controller.TargetPinza;

        ActualizarEtiquetasSliders();
    }

    private void OnSliderBaseChanged(float val)
    {
        if (controller != null) controller.SetTargetBase(val);
        if (lblValorSliderBase != null) lblValorSliderBase.text = $"{val:F1}°";
    }

    private void OnSliderBrazoChanged(float val)
    {
        if (controller != null) controller.SetTargetBrazo(val);
        if (lblValorSliderBrazo != null) lblValorSliderBrazo.text = $"{val:F1}°";
    }

    private void OnSliderAntebrazoChanged(float val)
    {
        if (controller != null) controller.SetTargetAntebrazo(val);
        if (lblValorSliderAntebrazo != null) lblValorSliderAntebrazo.text = $"{val:F1}°";
    }

    private void OnSliderPinzaChanged(float val)
    {
        if (controller != null)
            controller.SetTargetPinza(val);

        if (lblValorSliderPinza != null)
        {
            lblValorSliderPinza.text = val <= 0.05f
                ? "Cerrada"
                : val >= 0.95f
                    ? "Abierta"
                    : $"{val * 100:F0}%";
        }
    }

    private void OnBtnResetMecanismoClicked()
    {
        if (controller != null)
        {
            controller.ResetMecanismo();
            SincronizarSlidersConController();
        }
    }

    private void OnBtnResetSimulacionClicked()
    {
        if (reiniciador != null)
        {
            reiniciador.EjecutarReinicioCompleto();
            SincronizarSlidersConController();
        }
        else
        {
            OnBtnResetMecanismoClicked();

            if (sistemaObjetivos != null)
                sistemaObjetivos.ResetearObjetivos();
        }
    }

    private void Update()
    {
        ActualizarTelemetriaUI();
        ActualizarMensajesMision();
    }

    private void ActualizarTelemetriaUI()
    {
        if (telemetry == null) return;

        var tBase = telemetry.TelemetriaBase;

        if (txtTelemetriaBase != null)
            txtTelemetriaBase.text = $"{tBase.nombre}\nTarget: {tBase.target:F1}{tBase.unidad} | Real: {tBase.valorReal:F1}{tBase.unidad}";

        var tBrazo = telemetry.TelemetriaHombro;

        if (txtTelemetriaBrazo != null)
            txtTelemetriaBrazo.text = $"{tBrazo.nombre}\nTarget: {tBrazo.target:F1}{tBrazo.unidad} | Real: {tBrazo.valorReal:F1}{tBrazo.unidad}";

        var tCodo = telemetry.TelemetriaCodo;

        if (txtTelemetriaAntebrazo != null)
            txtTelemetriaAntebrazo.text = $"{tCodo.nombre}\nTarget: {tCodo.target:F1}{tCodo.unidad} | Real: {tCodo.valorReal:F1}{tCodo.unidad}";

        var tPinza = telemetry.TelemetriaPinza;

        if (txtTelemetriaPinza != null)
        {
            string estadoPinzaStr = controller != null && controller.Pinza != null
                ? controller.Pinza.Estado.ToString()
                : "";

            txtTelemetriaPinza.text = $"{tPinza.nombre} [{estadoPinzaStr}]\nTarget: {tPinza.target:F1}{tPinza.unidad} | Real: {tPinza.valorReal:F1}{tPinza.unidad}";
        }

        var tMuneca = telemetry.TelemetriaMuneca;

        if (txtTelemetriaMuneca != null)
            txtTelemetriaMuneca.text = $"{tMuneca.nombre} (Pasiva)\nDesviación: {tMuneca.valorReal:F1}° [Física Libre]";
    }

    private void ActualizarEtiquetasSliders()
    {
        if (sliderBase != null && lblValorSliderBase != null)
            lblValorSliderBase.text = $"{sliderBase.value:F1}°";

        if (sliderBrazo != null && lblValorSliderBrazo != null)
            lblValorSliderBrazo.text = $"{sliderBrazo.value:F1}°";

        if (sliderAntebrazo != null && lblValorSliderAntebrazo != null)
            lblValorSliderAntebrazo.text = $"{sliderAntebrazo.value:F1}°";

        if (sliderPinza != null && lblValorSliderPinza != null)
        {
            float valor = sliderPinza.value;

            lblValorSliderPinza.text = valor <= 0.05f
                ? "Cerrada"
                : valor >= 0.95f
                    ? "Cerrada"
                    : $"{valor * 100:F0}%";
        }
    }

    private void ActualizarMensajesMision()
    {
        if (sistemaObjetivos != null)
        {
            if (txtEstadoOperacion != null)
                txtEstadoOperacion.text = $"ESTADO: {sistemaObjetivos.Fase}";

            if (txtInstrucciones != null)
                txtInstrucciones.text = sistemaObjetivos.MensajeEstado;
        }
    }
}