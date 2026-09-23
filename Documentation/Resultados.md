# Resultados de validación

Fecha: 2026-09-23. Unity 6000.5.5f1, Windows.

## Algoritmo: ejecutado y aprobado

`AdaptiveCellChecks.Run` terminó con código 0. Semilla: 12345; 100 individuos por generación.

| Ronda | Supervivientes | Individuos con genes distintos respecto a la misma posición anterior | Tamaño medio |
|---|---:|---:|---:|
| 1 | 25 | No aplica | 1.266 |
| 2 | 70 | 100 | 1.014 |
| 3 | 70 | 100 | 1.033 |
| 4 | 76 | 100 | 0.994 |
| 5 | 67 | 100 | 1.045 |

También aprobados: signos de fitness, cuotas 70/30 después de la primera ronda, penalización de regiones fallidas, agrupación de duplicados, memoria acotada, cancelación sin recompensa y 10 000 mutaciones con límites válidos. La variación entre rondas muestra que la mejora no es monotónica.

Registro original local: `Logs/adaptive-checks.log` (excluido de Git).

## Integración y rendimiento

Compilación Windows x64: aprobada. Prueba de integración en el ejecutable: aprobada; cinco rondas con 100 objetos reutilizados, puntuación correcta, rechazo de notificaciones duplicadas, devolución al pool y una ronda con cero supervivientes.

La prueba preliminar en batch tuvo dos frames de más de 33.33 ms. Se descartó como validación visual. La segunda ejecución, en modo gráfico pero con ventana oculta, mantuvo 100 células y tuvo cero frames de más de 33.33 ms, pero detectó **cero frames renderizados**. Por ello el medidor devuelve FAIL y esos tiempos no se presentan como FPS reales del juego.

La comprobación visual de 30 FPS queda pendiente de ejecutar con la ventana visible. El build ya está preparado en `Builds/CellBenchmark/CellBenchmark.exe`: solicita 1280x720 al lanzarlo, no hagas clic y deja terminar los 5 segundos de calentamiento y 50 segundos de muestra. El programa se cierra automáticamente. La prueba exige cero frames lentos, 100 células durante toda la muestra y renderizado de cámara verificado.

Registros locales: `Logs/cell-build-rendered.log`, `Logs/cell-performance.log` y `Logs/cell-performance-rendered.log` (excluidos de Git).
