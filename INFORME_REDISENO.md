# INFORME DE REDISEÑO — AttentiON como producto

Revisión completa de la experiencia de juego: 26 minijuegos analizados uno a uno, sistemas
compartidos de game feel, flujo de navegación simplificado y progresión de dificultad
inteligente en todas las categorías. Este documento explica qué se cambió, por qué, y qué
problemas se encontraron.

---

## 1 · Problemas encontrados en la auditoría inicial

La auditoría (5 análisis por categoría + 1 de navegación) reveló:

- **Dos juegos de Atención no existían**: `AlgoNoCuadra` era una escena legacy con scripts
  perdidos (guids sin fichero) y `Attention_OptimalPath` un cascarón vacío que además
  duplicaba el OptimalPath de Planificación.
- **Bugs de ciclo de vida**: Simon Says llamaba a `CompleteMinigame()` incluso al perder
  (el informe del tutor contaba derrotas como victorias); WordMemory seguía ejecutando tras
  fallar; PathMemory y Aventura Emocional nunca notificaban su resultado; Aventura Emocional
  ni siquiera heredaba de `MinigameBase` (rompía telemetría e informes).
- **Dificultad falsa**: EmotionalBalance era idéntico en Fácil/Medio/Difícil; OrdenCorrecto y
  ResourceManagement dependían de valores de escena sin variación real; InverseResponse
  medía el tiempo de reacción al doble de velocidad (bug de doble tick).
- **Redundancias**: DontPressYet ≈ StopAndGo (dos veces "espera y pulsa al verde");
  Simon ≈ PatternRecall ≈ PositionMemory (tres veces "memoriza la secuencia");
  AttractionControl ≈ EmotionalBalance (dos veces "mantente en la zona").
- **Desalineación pedagógica**: SilentCountdown no entrenaba inhibición; DontFollowMajority
  era discriminación perceptual, no control de impulsos; ObjectTracking era pasivo y
  aburrido.
- **Flujo roto**: se elegía dificultad al crear el perfil y LUEGO el selector de dificultad
  volvía a preguntarla.
- **Sin identidad común**: cada juego tenía su propia pantalla final, sin estrellas, sin
  celebración, con botones distintos.

## 2 · Sistemas compartidos nuevos (afectan a los 26 juegos a la vez)

| Sistema | Fichero | Qué aporta |
|---|---|---|
| GameFeel | `Systems\UI\GameFeel.cs` | Confeti, textos flotantes (+10), sacudidas, flashes, contadores animados y **sonidos procedurales** (acorde de éxito, buzz de error, pop, ding de estrella) sin necesidad de assets |
| ResultsPanel | `Systems\UI\ResultsPanel.cs` | Pantalla de resultados única: **estrellas 1-3** animadas en secuencia, **robot NEO/AXEL/TITAN** que celebra (o anima si pierdes), contador de puntos, confeti, botones grandes "Jugar otra vez"/"Elegir juego". Mensajes de mentalidad de crecimiento ("¡Casi lo tienes!") |
| ButtonJuice | `Systems\UI\ButtonJuice.cs` | Squash & stretch en botones (hover crece, pulsar aplasta); integrado automáticamente en todos los botones de KidUI |
| MinigameBase.ShowResults | `Minigames\MinigameBase.cs` | Cada juego muestra resultados con una línea; estrellas con `GameFeel.StarsFromRatio` |

Consistencia: los 26 juegos comparten ahora la misma pantalla final, el mismo lenguaje de
feedback (verde/pop = bien, sacudida/flash rojo = error) y la misma progresión por estrellas.

## 3 · Flujo simplificado (dificultad UNA sola vez)

Antes: Inicio → perfil (elige edad→dificultad) → **otra vez selector de dificultad** → menú.
Ahora:

```
Inicio → ¿Quién juega hoy? (perfil) → DIRECTO a las categorías de SU dificultad
```

- La dificultad queda **guardada en el perfil** (`ProfileData.dificultad`); la recomendada
  por edad es el valor inicial.
- El selector de dificultad sigue existiendo solo como **cambio manual** (menú ESC →
  "Elegir dificultad") y cualquier cambio se **persiste en el perfil** automáticamente
  (hook en `GameManager.SetDifficulty` → `ProfileManager.PersistDifficulty`).
- ENTER en la pantalla inicial: con perfil → categorías; sin perfil (invitado) → selector
  clásico. La pantalla de perfiles bloquea el ENTER mientras está abierta.

## 4 · Cambios por categoría

### ATENCIÓN (2 juegos nuevos + 1 rediseño + 3 mejorados)
- **Algo No Cuadra — CONSTRUIDO DESDE CERO** (antes: escena rota sin código). Odd-one-out:
  busca la ficha diferente. Fácil: color evidente 2x3 · Medio: letras parecidas (E/F, O/Q)
  3x4 con tiempo · Difícil: **letras espejo (b/d, p/q)** 4x4 — directamente relevante para
  atención y confusión lectora.
- **Camino Numérico — CONSTRUIDO DESDE CERO** (reutiliza las escenas del OptimalPath
  duplicado). Trail-Making Test infantil: toca 1→8 en orden con línea dibujada; Medio añade
  distractores de letras; Difícil alterna número-letra (1-A-2-B…, TMT-B real con cambio de
  set atencional). *Pendiente: añadir su botón en el selector de Atención (ver §6).*
- **ObjectTracking — REDISEÑO TOTAL**: de "sigue un punto con el ratón" (pasivo) a
  **seguimiento de objetos múltiples (MOT)**: memoriza qué bolas son "amigas", se mezclan
  rebotando, y tócalas al pararse. 4/1 → 5/2 → 7/3 bolas/objetivos.
- **QuickReaction**: pulsar antes del verde ahora es fallo explícito ("¡Espera al verde!") y
  Difícil añade rondas trampa amarillas (aguantar sin pulsar) — suma inhibición a la velocidad.
- **RuleSwitch** (el mejor juego del proyecto): Difícil añade 4º color y **reglas inversas**
  ("pulsa todos MENOS el rojo"); textos de feedback clarificados.
- **LaserPath**: Difícil pasaba de frustrante (22s) a justo (35s); juice al girar espejos.

### MEMORIA (2 rediseños para eliminar redundancia + 2 bugs críticos)
- **Simon Says**: BUG corregido (perder contaba como victoria). Progresión 4 botones→6
  botones en Difícil con audio pentatónico.
- **WordMemory**: BUG corregido (seguía tras fallar). Difícil con distractores semánticos
  (GATO/GATA).
- **PatternRecall — REDISEÑADO**: ahora es patrón **simultáneo** (todas las celdas a la vez,
  reproducir en cualquier orden = memoria espacial pura); Difícil añade celda señuelo. Ya no
  es un clon de Simon.
- **PositionMemory — REDISEÑADO**: "¿Dónde estaba?" — binding objeto-lugar (ve los objetos,
  se ocultan, "¿dónde estaba la estrella?"). Nota: no tiene escena propia asignada aún.
- **FindChange / ColorMatch** (los mejores de la categoría): dificultad por código
  (6/8/12 objetos y parejas), vista previa solo en Fácil, límite de tiempo solo en Difícil.

### CONTROL DE IMPULSOS (3 rediseños completos — la categoría más transformada)
- **StopAndGo — REDISEÑO TOTAL → Go/No-Go clásico** (el paradigma nuclear de evaluación de
  inhibición en TDAH): ráfaga de estímulos verde=toca/rojo=frénate; mide **comisiones,
  omisiones y RT**. Difícil sube la proporción GO al 80% (más tentación) y añade el naranja
  sorpresa. Eliminada la redundancia con DontPressYet.
- **SilentCountdown — REDISEÑO TOTAL → "El semáforo escondido"**: cuenta atrás que se
  oculta; pulsa exactamente en el 0. Pulsar antes de que se oculte = impulsivo. Ventanas de
  precisión ±0.8/±0.5/±0.35s.
- **DontFollowMajority — REDISEÑO TOTAL → Flanker infantil**: responde la dirección del pez
  CENTRAL ignorando al banco (40%→60% de incongruencia). Ahora el nombre es literal y el
  paradigma es el correcto (efecto flanker).
- **InverseResponse**: BUG de doble tick corregido (los RT clínicos salían inflados ×2);
  ahora un juego perfecto puede lograr 3 estrellas.
- **DontPressYet**: Difícil añade falsas alarmas (verde parpadeante ≠ verde fijo).

### GESTIÓN EMOCIONAL (1 rescate estructural + reencuadres pedagógicos)
- **Aventura Emocional — RESCATE ESTRUCTURAL**: no heredaba de MinigameBase (invisible para
  telemetría e informes). Ahora integrada, con **15 preguntas de reconocimiento emocional y
  empatía** definidas en código, dificultad real (6/3 → 8/2 → 10/1 preguntas/errores) y
  salto 3D conservado de forma segura.
- **EmotionalBalance**: dificultad real por fin (8s/11s/14s + deriva creciente) y "ráfagas de
  viento emocional" **anunciadas** en Difícil (entrena anticipación, no castiga por sorpresa);
  carita central que refleja el estado.
- **AttractionControl**: reencuadre "tu burbuja de calma" vs "distracciones"; en Difícil, un
  estímulo dorado premia **ignorar lo llamativo**. Corregido: perder ahora es FailMinigame.
- **ProgressiveRegulation**: recalibrado (era **matemáticamente imposible de ganar** en
  Medio/Difícil); 2 estrategias nuevas ("Contar hasta 10", "Escuchar música"); carita que
  evoluciona con el nivel.
- **Consequences**: banco ampliado de 7 a 15 situaciones; caritas de color en las opciones
  como apoyo para no lectores (widget `EmotionFaceWidget` reutilizable).

### PLANIFICACIÓN (planificar antes de actuar)
- **OptimalPath**: fase explícita de planificación ("Piensa tu ruta" 3-2-1 con el tablero
  visible); fallo reinicia solo la ronda; casilla "peaje" en Difícil; desvíos 5/4/3.
- **PathMemory**: capa de **replanificación** — tras memorizar la ruta aparecen muros
  sorpresa sobre ella y hay que desviarse lo mínimo (BFS valida el óptimo). Ciclo de vida
  arreglado (antes nunca notificaba resultado).
- **ResourceManagement**: Medio añade una **acción trampa** (cara e ineficiente) y Difícil
  una **acción arriesgada** (25-55% aleatorio) — decisión bajo incertidumbre real.
- **ActionSequence**: de memorizar una lista a **razonar el orden lógico** de rutinas; 6
  rutinas distintas elegidas al azar (rejugabilidad); Difícil con pasos casi idénticos.
- **OrdenCorrecto**: "Ordena la misión" — Fácil 1-6; Medio 1-10 con pistas pre-colocadas;
  Difícil cambia la regla (descendente o de 2 en 2).

## 5 · Decisiones de diseño transversales

1. **La dificultad vive en el código, no en las escenas**: las 3 variantes de escena de cada
   juego son ahora equivalentes; `ApplyDifficulty()` lee `GameManager.CurrentDifficulty`.
   Elimina 50+ puntos de configuración duplicada y hace las curvas afinables en un solo sitio.
2. **Progresión por capas cognitivas, no por velocidad**: cada salto de dificultad añade un
   elemento nuevo (distractores, reglas inversas, señuelos, incertidumbre, replanificación),
   siguiendo la "zona de reto óptimo".
3. **Paradigmas con evidencia**: Go/No-Go, Flanker, Trail-Making, MOT y odd-one-out son
   tareas estándar en investigación de funciones ejecutivas — los datos que generan (RT,
   comisiones/omisiones) son directamente interpretables por un psicopedagogo en el informe.
4. **Telemetría en todos los juegos**: los 26 llaman a `ReportEvent(acierto, RT)` por ronda →
   los informes Excel/HTML del área del tutor pasan a tener % de acierto y tiempos de
   reacción reales en todas las categorías.
5. **Fallar no humilla**: pantalla de derrota con robot que anima, "¡Casi lo tienes!", y
   reintento a un toque. Las estrellas premian la excelencia sin castigar el mínimo.

## 6 · Problemas conocidos / pendientes

- **Camino Numérico sin botón**: las escenas `Attention_OptimalPath_*` funcionan y están en
  Build Settings, pero el selector de Atención no tiene botón hacia ellas (habría que editar
  las 3 escenas selector en el editor: duplicar un botón y poner `sceneToLoad =
  Attention_OptimalPath_Easy/Medium/Hard`). Con Algo No Cuadra, Atención ya tiene 5 juegos.
- **PositionMemory** (¿Dónde estaba?) está implementado pero sin escena asignada — candidato
  a 6º juego de Memoria creando una escena con la plantilla mínima.
- **Huérfanos detectados (no borrados)**: `MemoryGameManager.cs`, `MonedaUIManager.cs`,
  `CameraController.cs` (Memory), `PlataformaBalanza/Spawner/CollisionDetector` (Planning,
  ninguna escena los referencia). `MainMenu.unity` no se usa en el flujo.
- **Sin compilación real**: todo se verificó estáticamente (contratos de API cruzados,
  ciclo Complete/Fail único por juego, nombres de clase = ficheros para no romper guids,
  firmas de GameFeel/KidUI/UITween). Falta abrir Unity 2022.3.51f1 y confirmar la
  compilación + una pasada de juego por categoría.

## 7 · Cómo probar

1. Abrir en Unity, esperar recompilación (sin paquetes nuevos).
2. Play en `PrimeraPantalla` → crear perfil → verás que entra DIRECTO a las categorías de la
   dificultad del perfil (sin segundo selector).
3. Jugar 1 juego por categoría: comprobar intro → juego con feedback vivo → pantalla de
   estrellas con robot → "Jugar otra vez"/"Elegir juego".
4. ESC → "Elegir dificultad" → cambiarla → verificar que al volver a entrar con ese perfil
   se recuerda.
5. Área del tutor → generar informe → ahora con % de acierto y RT en todas las categorías.
