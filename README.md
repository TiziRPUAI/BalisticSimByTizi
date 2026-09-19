# Simulador Balístico 3D

Simulador hecho en Unity donde se dispara un proyectil con un cañón para derribar una torre de bloques unidos con joints. Al final de cada tiro se muestra un reporte con los datos del impacto y un puntaje.

**Versión de Unity:** 6000.4.1f1

**Video:** (link de YouTube)

## Cómo jugar

1. Abrir el proyecto con Unity 6000.4.1f1 y cargar la escena `SampleScene`.
2. Dar Play y esperar a que diga "Listo para disparar".
3. Elegir ángulo, fuerza y masa con los sliders.
4. Apretar **DISPARAR**.
5. Cuando todo se asienta aparece el reporte de tiro.
6. Apretar **REINTENTAR** para volver a probar.

## Controles

- **Ángulo:** 5° a 85°
- **Fuerza (impulso):** 20 a 400 N·s
- **Masa:** 1 a 20 kg
- **DISPARAR / REINTENTAR:** botones en pantalla

La velocidad inicial es fuerza / masa y se muestra en pantalla.

## Reporte y puntaje

El reporte muestra: tiempo de vuelo, punto de impacto, velocidad relativa, impulso de colisión y piezas derribadas.

El puntaje es `100 puntos por pieza derribada × multiplicador de eficiencia`. El multiplicador es mayor cuanto menos energía se usa en el disparo (entre x0,5 y x2).

## Scripts

- `CannonController`: sliders, cálculo de la dirección y disparo.
- `ProjectileTelemetry`: registra los datos del impacto.
- `StructureTarget`: piezas de la torre y detección de derribo.
- `SimulationManager`: estados, puntaje y reporte.

## Criterios de evaluación

- Controles de ángulo, fuerza y masa.
- Proyectil con Rigidbody y Collider.
- Estructura con joints, estable al iniciar.
- Telemetría del impacto.
- Conteo de piezas derribadas.
- Reporte final con puntaje.
