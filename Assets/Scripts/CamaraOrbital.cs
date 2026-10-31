using UnityEngine;
using UnityEngine.EventSystems;

public class CamaraOrbital : MonoBehaviour
{
    [Header("Objetivo de Enfoque")]
    [SerializeField] private Transform puntoEnfoque;
    [SerializeField] private Vector3 offsetEnfoque = new Vector3(0f, 4f, 0f);

    [Header("Parámetros Orbitales")]
    [SerializeField] private float distancia = 22f;
    [SerializeField] private float distanciaMin = 8f;
    [SerializeField] private float distanciaMax = 40f;
    [SerializeField] private float velocidadOrbita = 120f;
    [SerializeField] private float velocidadZoom = 15f;
    [SerializeField] private float limiteInclinacionMin = 5f;
    [SerializeField] private float limiteInclinacionMax = 80f;

    private float rotacionX = 20f;
    private float rotacionY = 180f;

    private void Start()
    {
        Vector3 angulos = transform.eulerAngles;
        rotacionX = angulos.x;
        rotacionY = angulos.y;
    }

    private void LateUpdate()
    {
        Vector3 centro = (puntoEnfoque != null) ? puntoEnfoque.position + offsetEnfoque : offsetEnfoque;

        bool cursorSobreUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (!cursorSobreUI)
        {
            if (Input.GetMouseButton(1) || Input.GetMouseButton(0))
            {
                rotacionY += Input.GetAxis("Mouse X") * velocidadOrbita * Time.deltaTime;
                rotacionX -= Input.GetAxis("Mouse Y") * velocidadOrbita * Time.deltaTime;
                rotacionX = Mathf.Clamp(rotacionX, limiteInclinacionMin, limiteInclinacionMax);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                distancia = Mathf.Clamp(distancia - scroll * velocidadZoom, distanciaMin, distanciaMax);
            }
        }

        Quaternion rotacion = Quaternion.Euler(rotacionX, rotacionY, 0f);
        Vector3 posicion = centro - (rotacion * Vector3.forward * distancia);

        transform.rotation = rotacion;
        transform.position = posicion;
    }
}