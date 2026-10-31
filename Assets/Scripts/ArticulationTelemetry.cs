using UnityEngine;

[System.Serializable]
public struct JointTelemetryData
{
    public string nombre;
    public float target;
    public float valorReal;
    public string unidad;
}

public class ArticulationTelemetry : MonoBehaviour
{
    [SerializeField] private BrazoMecanicoController controller;

    private JointTelemetryData telemetriaBase;
    private JointTelemetryData telemetriaHombro;
    private JointTelemetryData telemetriaCodo;
    private JointTelemetryData telemetriaPinza;
    private JointTelemetryData telemetriaMuneca;

    public JointTelemetryData TelemetriaBase => telemetriaBase;
    public JointTelemetryData TelemetriaHombro => telemetriaHombro;
    public JointTelemetryData TelemetriaCodo => telemetriaCodo;
    public JointTelemetryData TelemetriaPinza => telemetriaPinza;
    public JointTelemetryData TelemetriaMuneca => telemetriaMuneca;

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<BrazoMecanicoController>();
        }
    }

    private void Update()
    {
        ActualizarLecturas();
    }

    private void ActualizarLecturas()
    {
        if (controller == null) return;

        float realBaseDeg = 0f;
        if (controller.TorretaBase != null && controller.TorretaBase.jointPosition.dofCount > 0)
        {
            realBaseDeg = controller.TorretaBase.jointPosition[0] * Mathf.Rad2Deg;
        }
        telemetriaBase = new JointTelemetryData
        {
            nombre = "Base (Torreta)",
            target = controller.TargetBase,
            valorReal = realBaseDeg,
            unidad = "°"
        };

        float realHombroDeg = 0f;
        if (controller.BrazoPrincipal != null && controller.BrazoPrincipal.jointPosition.dofCount > 0)
        {
            realHombroDeg = controller.BrazoPrincipal.jointPosition[0] * Mathf.Rad2Deg;
        }
        telemetriaHombro = new JointTelemetryData
        {
            nombre = "Brazo (Hombro)",
            target = controller.TargetBrazo,
            valorReal = realHombroDeg,
            unidad = "°"
        };

        float realCodoDeg = 0f;
        if (controller.Antebrazo != null && controller.Antebrazo.jointPosition.dofCount > 0)
        {
            realCodoDeg = controller.Antebrazo.jointPosition[0] * Mathf.Rad2Deg;
        }
        telemetriaCodo = new JointTelemetryData
        {
            nombre = "Antebrazo (Codo)",
            target = controller.TargetAntebrazo,
            valorReal = realCodoDeg,
            unidad = "°"
        };

        float targetPinzaM = 0f;
        float realPinzaM = 0f;
        if (controller.Pinza != null)
        {
            targetPinzaM = controller.Pinza.AperturaTarget;
            realPinzaM = controller.Pinza.AperturaReal;
        }
        telemetriaPinza = new JointTelemetryData
        {
            nombre = "Pinza (Efector)",
            target = targetPinzaM * 100f,
            valorReal = realPinzaM * 100f,
            unidad = "cm"
        };

        float swingAngleDeg = 0f;
        if (controller.MunecaPasiva != null && controller.MunecaPasiva.jointPosition.dofCount >= 2)
        {
            float ax = controller.MunecaPasiva.jointPosition[0] * Mathf.Rad2Deg;
            float ay = controller.MunecaPasiva.jointPosition[1] * Mathf.Rad2Deg;
            swingAngleDeg = Mathf.Sqrt(ax * ax + ay * ay);
        }
        telemetriaMuneca = new JointTelemetryData
        {
            nombre = "Muñeca Pasiva",
            target = 0f,
            valorReal = swingAngleDeg,
            unidad = "°"
        };
    }
}