# Segunda mitad: células adaptativas

Implementación integrada con la primera mitad del repositorio. No requiere librerías de ML adicionales. El código completo, separado por archivo en bloques C#, está en `CodigoCompleto.md`.

## Cómo aprende

Se implementa un algoritmo evolutivo simple, una forma de optimización basada en experiencias, sin redes neuronales ni entrenamiento por gradiente. Cada individuo tiene cuatro genes: rojo, verde, azul y escala. Aprende la población entre rondas; una célula no cambia de apariencia durante su ronda.

1. La primera ronda usa genomas aleatorios dentro de los límites.
2. Una eliminación registra fitness `-1 + 0.5 * Clamp01(tiempoVivo / duraciónRonda)`; una supervivencia registra `+1`. Incluso morir al último instante sigue siendo negativo.
3. La memoria guarda regiones de genomas exitosos. Si una experiencia está suficientemente cerca de una región guardada, actualiza su fitness medio, incluidos los resultados negativos. Un fracaso sin región cercana no crea una nueva entrada.
4. Al comenzar la siguiente ronda, el 70 % (redondeado al entero más cercano) procede de regiones con al menos el 80 % del mejor fitness positivo. Se seleccionan con probabilidad proporcional a su fitness y se mutan los cuatro genes. El 30 % restante es completamente aleatorio. Sin memoria positiva se explora el 100 %.
5. Los colores se limitan a RGB [0,1], alfa siempre 1, y la escala al intervalo configurado. La mutación del tamaño es una fracción del intervalo. La memoria agrupa genomas cercanos y tiene capacidad fija; si se llena reemplaza una región de menor fitness.

La exploración mantiene variabilidad y permite recuperar otras estrategias, aunque no garantiza una separación mínima entre todos los individuos. El mejor color depende del fondo y de qué células seleccione el jugador. No se garantiza mejora monotónica en cinco rondas: la selección es estocástica. La memoria dura toda la partida, no se guarda en disco. El promedio histórico puede adaptarse lentamente si cambia el comportamiento del jugador. Es una aproximación educativa: agrupa experiencias de individuos parecidos y no controla el efecto de la posición u oclusión.

## Integración

- Se conservan `ICellAI`, los eventos de `RoundManager`, las referencias serializadas y la firma original de `CellSpawner.SpawnCellsForRound(int)`.
- `CellSpawner` crea automáticamente un `EvolutionManager` si no hay uno asignado; este usa configuración predeterminada si falta el asset. La escena existente funciona con sus referencias actuales.
- Para ajustar parámetros, crea **Assets > Create > Células > Configuración adaptativa**, añade `EvolutionManager` al objeto del spawner y asigna su `config`. También puedes asignar ese componente al campo `evolutionManager` del spawner.
- `CellController.AssignAI` consulta la IA mediante `Initialize`, que aplica su genoma con `ApplyGenome`. Los clics llaman al spawner, que registra la muerte antes de desactivar el objeto.
- `RoundManager.OnRoundEnded` ya existe. `GameManager` llama ahora a `FinishRound`: registra las supervivientes, imprime estadísticas y libera el pool. La siguiente ronda empieza en `LateUpdate`, después de todos los oyentes de fin de ronda.
- `ClearAllCells` cancela y limpia sin premiar supervivencia. No lo uses como sustituto de `FinishRound`.
- No añadas un segundo oyente que vuelva a crear la población al terminar la ronda. Los genes se mutan al construir la generación; `ICellAI.Mutate` no modifica células durante una ronda activa.
- El pool y la memoria se dimensionan una vez. Cambia capacidades, configuración y límites antes de pulsar Play; para redimensionar, reinicia Play.
- El tamaño es escala local: mantén el padre del pool en escala (1,1,1) para que coincida con el tamaño esperado en el mundo. El collider del prefab debe ajustarse al sprite.

## Parámetros

| Parámetro | Predeterminado | Efecto |
|---|---:|---|
| minSize / maxSize | 0.5 / 2 | Límites de escala local |
| exploitation | 0.7 | Fracción de descendientes; el resto explora |
| colorMutation | 0.12 | Cambio uniforme máximo por canal RGB |
| sizeMutation | 0.15 | Cambio máximo como fracción del intervalo de escala |
| diversityDistance | 0.08 | Distancia RMS normalizada para agrupar genomas cercanos |
| memoryCapacity | 64 | Máximo de regiones guardadas |
| maxCells | 100 | Tope de configuración; además limita maxCellsPerRound |
| logStatistics | true | Resumen por ronda en consola |
| maxCellsPerRound | 20 | Capacidad del pool solicitada en el spawner |
| useFullPopulation | false | Población normal: min(5 + 2*ronda, capacidad); true usa todo el pool |

## Rendimiento

Se instancian células y cerebros una sola vez al crear el pool. En rondas posteriores se reutilizan objetos, arrays y estructuras. No hay LINQ, búsquedas globales ni Update por célula. El temporizador utiliza `TMP.SetText` para evitar interpolar una cadena por frame. Los mensajes de depuración sí crean cadenas una vez por ronda; desactiva `logStatistics` para medir sin ese coste. La memoria consume O(capacidad); su búsqueda es lineal por resultado. Estos scripts no garantizan por sí solos un frame sin GC en toda la escena: comprueba también UI, plugins y otros componentes con el Profiler.

## Pruebas reproducibles

### Aprendizaje, fitness y límites

Menú **Células > Verificar aprendizaje y límites**. `AdaptiveCellChecks.Run` también se puede ejecutar con `-batchmode -nographics -executeMethod AdaptiveCellChecks.Run -quit`.

La prueba fija la semilla 12345, simula cinco rondas de 100 células con un jugador que elimina células grandes o con mucho rojo, valida cambios de genes y cuotas 70/30, verifica conteos y capacidad de memoria, comprueba que una eliminación reduce el fitness almacenado y que una cancelación no premia supervivencia. Después valida 10 000 mutaciones. Restaura el estado aleatorio al terminar. El jugador simulado es una condición de prueba, no una regla del juego.

Para verificar la interacción: en Play, elimina una célula, espera el fin de ronda y comprueba que eliminadas + supervivientes coincide con la población. Confirma que hacer doble clic no suma dos muertes, que no quedan supervivientes antiguas activas y que el número de objetos hijos del spawner permanece constante durante cinco rondas.

### 100 células y 30 FPS

1. Antes de Play, fija `maxCellsPerRound=100`, `config.maxCells=100` y `useFullPopulation=true`.
2. Añade `CellPerformanceProbe` y asigna el spawner. Mantén las rondas en 10 segundos; calentamiento 5 segundos y muestra 50 segundos.
3. Para comprobar las 100 células simultáneas, no hagas clic durante esta medición. Ejecuta preferentemente un build en el equipo objetivo, con renderizado y sin Deep Profile. Registra resolución, GPU y configuración gráfica.
4. Tras 55 segundos imprime PASS solo si todos los frames medidos tenían 100 células y ninguno superó 33.33 ms. Incluye FPS medio, FPS mínimo y número de frames lentos. Se incluyen cambios de ronda después del calentamiento.
5. Repite jugando y utiliza el Profiler para localizar asignaciones y picos; esta segunda prueba tiene una población decreciente y no sustituye la prueba fija de 100.

Un resultado headless o únicamente el FPS promedio no demuestra que el juego nunca baje de 30 FPS. Consulta `Resultados.md` para distinguir comprobaciones ejecutadas de las pendientes.

## Guion de vídeo (60–90 segundos)

- 0–10 s: mostrar el Inspector y explicar: «Cada célula tiene color RGB y tamaño; la primera generación es aleatoria».
- 10–25 s: jugar una ronda de 10 s eliminando preferentemente las células grandes y rojizas. Mostrar supervivientes y fitness en consola.
- 25–45 s: mostrar rondas 2 y 3 con el mismo criterio: «Las supervivientes aportan genes; mutamos el 70 % y exploramos con el 30 %».
- 45–65 s: mostrar rondas 4 y 5 y comparar las estadísticas de tamaños y colores. Explicar que existe azar y que cinco rondas no garantizan mejora constante.
- 65–80 s: mostrar límites en el asset y resultado de las pruebas. Añadir una toma de la medición con 100 células cuando se haya ejecutado.
- 80–90 s: cerrar: «La población conserva experiencias durante la partida y se adapta al comportamiento del jugador».

### Ejecución automática de integración y benchmark

`CellBenchmarkBuild.Run` se ejecuta en un Editor separado con `-batchmode -executeMethod CellBenchmarkBuild.Run -quit`. Carga MainScene, crea una copia temporal con 100 células y el medidor, construye `Builds/CellBenchmark/CellBenchmark.exe` y elimina la copia temporal. No guarda cambios en MainScene. Requiere soporte de compilación Windows instalado.

El ejecutable valida primero cinco rondas de integración: puntuación, notificaciones duplicadas, devolución al pool, identidad de los 100 objetos y una ronda donde todas mueren. Después calienta y mide 50 segundos con 100 células. `runIntegrationChecks` y `quitWhenComplete` son opciones exclusivas de prueba; permanecen desactivadas por defecto. El código de salida del ejecutable es 0 si pasa rendimiento, 1 si falla rendimiento y 2 si falla integración.

La medición automática final usa modo gráfico (sin `-batchmode` ni `-nographics` en el ejecutable), solicita un máximo de 120 FPS y permite ejecución en segundo plano. El medidor cuenta los frames que llegan a `RenderPipelineManager.endCameraRendering` de URP; también falla si no se renderiza la muestra completa, salvo el último frame que todavía no se ha dibujado al registrar el resultado en LateUpdate. Estos ajustes solo se aplican cuando `quitWhenComplete` está activado. Una prueba del bucle en modo batch no sustituye la medición gráfica.
