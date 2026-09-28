# Simulador Balístico 3D

Simulador hecho en Unity donde se dispara un proyectil con un cañón para derribar distintas estructuras (un muro, una torre y una plataforma con un bloque dorado) unidas con joints. Cada disparo se guarda en la nube y al final se muestra un reporte con los datos del impacto y un puntaje.

**Versión de Unity:** 6000.4.1f1

**Video:** (https://youtu.be/pIDh3XBIyOI)

## Cómo jugar

1. Abrir el proyecto con Unity 6000.4.1f1 y cargar la escena `SampleScene`.
2. Dar Play y esperar a que diga "Listo para disparar".
3. Elegir ángulo, azimut, fuerza y masa con los sliders. La línea amarilla muestra la trayectoria antes de disparar.
4. Apretar **DISPARAR**. La cámara sigue al proyectil en vuelo y vuelve a la vista general al terminar.
5. Cuando todo se asienta aparece el reporte de tiro.
6. Apretar **REINTENTAR** para volver a probar.
7. Apretar **VER HISTORIAL** para ver todos los disparos guardados hasta ahora, incluso de sesiones anteriores.

## Controles

- **Ángulo:** 5° a 85°
- **Azimut:** -60° a 60° (gira el cañón a los lados)
- **Fuerza (impulso):** 20 a 400 N·s
- **Masa:** 1 a 20 kg
- **DISPARAR / REINTENTAR / VER HISTORIAL:** botones en pantalla

La velocidad inicial es fuerza / masa y se muestra en pantalla.

## Objetivos

- **Muro:** cerca y a la izquierda, pide un tiro más raso.
- **Torre:** al frente, el objetivo clásico.
- **Plataforma:** lejos y a la derecha, con un bloque dorado arriba que vale más puntos.

## Reporte y puntaje

El reporte muestra: ángulo, azimut, tiempo de vuelo, punto de impacto, velocidad relativa, impulso de colisión y piezas derribadas (de todas las estructuras juntas).

El puntaje es `puntos de la pieza (100, o 300 si es la dorada) por cada pieza derribada × multiplicador de eficiencia`. El multiplicador es mayor cuanto menos energía se usa en el disparo (entre x0,5 y x2).

## Historial en la nube

Cada disparo se guarda en Unity Cloud Save con autenticación anónima, en su propia clave (`Shot_0001`, `Shot_0002`, ...), sin borrar los anteriores. El botón **VER HISTORIAL** los recupera y los muestra: fecha, ángulo, azimut, fuerza, masa, resultado, distancia y piezas derribadas.

Para que funcione hace falta conexión a internet y que el proyecto esté vinculado a Unity Cloud (Project Settings > Services) con Cloud Save activado.

## Scripts

- `CannonController`: sliders, cálculo de la dirección y disparo.
- `TrajectoryPreview`: dibuja la línea de trayectoria antes de disparar.
- `CameraDirector`: cambia entre la vista general y la cámara que persigue al proyectil.
- `ProjectileTelemetry`: registra los datos del impacto.
- `StructureTarget`: piezas de las estructuras y detección de derribo.
- `SimulationManager`: estados, puntaje y reporte.
- `SimulationRecord`: datos de un disparo guardado.
- `UGSServiceManager`: autenticación anónima y guardado/lectura en Cloud Save.
- `CloudSaveBridge`: guarda cada disparo cuando termina.
- `HistoryUIController` y `SimulationRecordItemUI`: pantalla del historial.

## Criterios de evaluación

- Controles de ángulo, azimut, fuerza y masa.
- Proyectil con Rigidbody y Collider.
- Estructuras con joints, estables al iniciar.
- Telemetría del impacto.
- Conteo de piezas derribadas.
- Reporte final con puntaje.
- Guardado y recuperación de resultados desde Cloud Save (video).