# Simulador de Brazo Mecánico con ArticulationBody
**Trabajo Práctico Universitario - Cátedra Simuladores (2.º Año)**  
*Licenciatura en Desarrollo de Videojuegos y Simulaciones*

---

## 🚀 Inicio Rápido en Unity

1. Abrí el proyecto en **Unity 6.3 LTS** (`6000.3.x`).
2. Abrí la escena: `Assets/Scenes/SampleScene.unity`.
3. Podés utilizar dos métodos para inicializar el simulador:
   - **Opción A (Recomendada):** En la barra superior de Unity, hacé clic en el menú **`Simulador > Reconstruir y Configurar Escena Completa`**. Esto genera el árbol completo de 8 cuerpos `ArticulationBody`, la pinza, mesas de trabajo, carga física, Canvas UI y cámara orbital con todas sus referencias guardadas.
   - **Opción B (Automática en Play Mode):** Simplemente presiona **Play** en Unity. El componente `SimuladorRuntimeSetup` autoconfigura y vincula todo en el primer frame.

---

## 🎮 Controles de la Simulación

### Panel Izquierdo: Controles
- **Rotación Base (Torreta):** Gira la base horizontalmente `[-180°, +180°]` (Drive 1 - Revolute).
- **Brazo Principal (Hombro):** Eleva/desciende el eslabón principal `[-40°, +75°]` (Drive 2 - Revolute).
- **Antebrazo (Codo):** Flexiona el antebrazo `[-90°, +90°]` (Drive 3 - Revolute).
- **Apertura Pinza:** Desplaza simétricamente los dos dedos prismáticos `[0%, 100%]` (Drive 4 - Prismatic).
- **Reiniciar Mecanismo:** Retorna el brazo a la pose de reposo (`0°`).
- **Reiniciar Simulación:** Devuelve el brazo, las cargas físicas y la progresión de la misión al estado inicial.

### Cámara Orbital
- **Clic Derecho o Izquierdo (fuera de la UI) + Arrastrar:** Rotación orbital 3D alrededor del mecanismo.
- **Rueda del Mouse (Scroll):** Zoom in / Zoom out.

---

## 📊 Telemetría y Objetivos
- **Panel Derecho:** Muestra en tiempo real el valor **Target** configurado vs. el valor **Real** medido directamente de `jointPosition` para cada articulación.
- **Muñeca Pasiva:** Muestra la inclinación libre de la junta esférica, la cual reacciona a gravedad e inercia sin motor.
- **Panel Superior:** Orienta al operador paso a paso a través de las 5 fases de la misión de traslado y entrega.

Para más detalles teóricos, diagrama cinemático y fórmulas de los drives, consultá el archivo [`Assets/DOCUMENTACION_TECNICA.md`](file:///d:/Unity/Garra/Assets/DOCUMENTACION_TECNICA.md).
