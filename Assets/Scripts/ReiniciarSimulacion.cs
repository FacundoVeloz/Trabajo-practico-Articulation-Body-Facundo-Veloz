using UnityEngine;

public class ReiniciarSimulacion : MonoBehaviour
{
    [Header("Referencias del Sistema")]
    [SerializeField] private BrazoMecanicoController controller;
    [SerializeField] private SistemaObjetivos sistemaObjetivos;
    [SerializeField] private BrazoMecanicoUI ui;
    [SerializeField] private ObjetoCarga[] cargasSimuladas;

    private void Awake()
    {
        if (cargasSimuladas == null || cargasSimuladas.Length == 0)
        {
            cargasSimuladas = FindObjectsByType<ObjetoCarga>(FindObjectsSortMode.None);
        }
    }

    public void EjecutarReinicioCompleto()
    {
        if (controller != null)
        {
            controller.ResetMecanismo();
        }

        if (cargasSimuladas != null)
        {
            foreach (var carga in cargasSimuladas)
            {
                if (carga != null)
                {
                    carga.ResetearPosicion();
                }
            }
        }

        if (sistemaObjetivos != null)
        {
            sistemaObjetivos.ResetearObjetivos();
        }

        if (ui != null)
        {
            ui.SincronizarSlidersConController();
        }
    }
}