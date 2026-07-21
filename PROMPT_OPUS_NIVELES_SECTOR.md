# PROMPT PARA CLAUDE OPUS — Niveles de sector por dificultad creciente (AttentiON)

Copia todo lo que hay debajo de la línea en una conversación nueva de Claude (Opus) con acceso a la carpeta del proyecto.

---

Trabajas en el proyecto Unity 2022.3 "AttentiON" (raíz del repo) — juego educativo para niños de 3-10 años con TDAH: 25 minijuegos en 5 categorías (Memoria, Control de impulsos, Gestión emocional, Atención, Planificación), con perfiles, telemetría local y un mundo narrativo (planeta Attentia dividido en 5 distritos; cada distrito tiene 5 sectores = 5 minijuegos que "reviven" al completarse). Toda la UI nueva se construye 100% POR CÓDIGO. Tu misión: implementar el sistema de **NIVELES DE SECTOR POR RETO CRECIENTE** descrito abajo, de principio a fin.

## 1. Contexto obligatorio (léelo ANTES de escribir nada)

- `Assets/_Project/Scripts/Minigames/MinigameBase.cs` — clase base de los 25 minijuegos: campos `minigameName`, `category`; ciclo `Start()` → intro → `OnMinigameStart()`; métodos `CompleteMinigame(score)`, `FailMinigame()`, `ReportEvent(acierto, rtMs)`, `ShowResults(...)`, `ReturnToGameSelector()`.
- `Assets/_Project/Scripts/Systems/World/GameCatalog.cs` — catálogo de los 25 juegos: nombre visible, nombre de telemetría, `sceneBase` (p. ej. "Memory_SimonSays"; la escena real añade `_Easy/_Medium/_Hard`) y ruta del logo. ES LA FUENTE DE VERDAD para localizar cada juego.
- `Assets/_Project/Scripts/Systems/World/DistrictScreen.cs` — pantalla de distrito con las 5 tarjetas-sector (estado roto/revivido, logo al revivir, robot guía). Aquí vive también `DistrictArt` y el router `WorldNavRouter`.
- `Assets/_Project/Scripts/Systems/World/DistrictPickScreen.cs` — selector de distritos.
- `Assets/_Project/Scripts/Systems/UI/ProgressMapScreen.cs` — hub del planeta con los 5 medallones (aro de progreso radial por categoría) y la misión diaria.
- `Assets/_Project/Scripts/Systems/UI/KidUI.cs` — helpers de UI por código (`MakeCanvas`, `BuildSpaceBackground`, `Img`, `RoundImg`, `CircleAt`, `Txt`, `Btn`, `Sprite`, colores `ACCENT/GOOD/WARN/BAD/DIM/PANEL/BTNC`, componentes `StarTwinkle`, `FloatBob`). En `ProgressMapScreen` hay además un sprite de aro (`RingThin/RingThick`) y un componente `SlowSpin` reutilizables como referencia.
- `Assets/_Project/Scripts/Systems/UI/GameFeel.cs` — `PlaySuccess/PlayError/PlayPop/PlayStar`, `Confetti`, `FloatingText`, `Shake`, `ScreenFlash`, `StarsFromRatio`.
- `Assets/_Project/Scripts/Systems/UI/UITween.cs` — `PopIn`, `FadeIn`, `FadeOut`, `PulseOnce`; y `ButtonJuice.Attach(go)`.
- `Assets/_Project/Scripts/Systems/Data/ProfileManager.cs` — perfil activo (`Instance.ActiveProfile.id`), `Store` (IDataStore con `GetResults(profileId)`).
- Un minijuego de referencia bien hecho: `Assets/_Project/Scripts/Minigames/Memory/SimonGameManager.cs` (mira su `ApplyDifficulty()`).

## 2. Reglas de oro (innegociables)

1. NO editar escenas `.unity` ni archivos `.meta`. CONSERVA los nombres de clase y TODOS los campos `public`/`[SerializeField]` existentes de los scripts que toques (las escenas los referencian).
2. Cabecera en cada archivo creado o modificado: `// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com`
3. Comentarios y textos de UI en español, tono cálido para niños.
4. NO tocar: informes (`ReportGenerator`), telemetría (`TelemetryManager`, `MinigameResultData`), consentimiento, perfiles, PIN, modo profesional, `IntroVideoScreen`. Nada de eso cambia.
5. NO hacer los encargos de `PROMPT_CONTINUACION.md` (rediseños visuales pendientes de otros juegos): es trabajo separado.
6. Verificación estática final obligatoria: relee cada archivo tocado y coteja cada llamada con las firmas reales de KidUI/GameFeel/UITween/MinigameBase.

## 3. EL DISEÑO — Rangos por reto creciente

### 3.1 Concepto
Cada sector (minijuego) tiene un RANGO 0-4: **0 = en ruinas** (nunca completado), **1 = BRONCE** (revivido), **2 = PLATA**, **3 = ORO**, **4 = DIAMANTE**. El rango NO sube por acumular partidas: sube **una vez por victoria**, y cada rango endurece la SIGUIENTE partida de ese juego. Es decir: completas el juego en su nivel actual → el sector sube de rango → la próxima vez ese juego es más difícil. Perder nunca baja el rango; se reintenta el mismo nivel. Diamante es el tope (las victorias posteriores no suben más, pero el juego se queda en su endurecimiento máximo).

### 3.2 La pendiente depende de la dificultad base del perfil
El multiplicador de reto de una partida es: `factor = 1 + rangoActual * paso`, con `rangoActual` 0-3 (a partir de diamante se usa 3) y `paso` según `GameManager.Instance.CurrentDifficulty`:
- **Easy (NEO): paso = 0.12** → diamante ≈ +36% sobre el fácil base (suave, se nota sin frustrar).
- **Medium (AXEL): paso = 0.22** → diamante ≈ +66%.
- **Hard (TITAN): paso = 0.38** → diamante ≈ +114% (muy exigente, para quien domina).

### 3.3 Persistencia
El rango se guarda en PlayerPrefs por perfil y juego: clave `"reto_<profileId>_<sceneBase>"` (usa el `sceneBase` de GameCatalog, que es estable; para invitado usa `"guest"`). También guarda `"reto_visto_<profileId>_<sceneBase>"` (último rango celebrado) para las celebraciones de subida.

### 3.4 Archivo nuevo: `Assets/_Project/Scripts/Systems/World/ChallengeSystem.cs`
Clase estática con al menos:
- `int Rank(string profileId, string sceneBase)` — rango 0-4 guardado. IMPORTANTE (migración): si el rango guardado es 0 pero el juego YA figura completado en la telemetría (mismo criterio que usa `GameCatalog.IsCompleted`), devuelve 1 y persiste 1 (los niños que ya revivieron sectores no pierden nada).
- `int RankEscenaActual()` — resuelve el juego actual a partir de `SceneManager.GetActiveScene().name` quitando el sufijo `_Easy/_Medium/_Hard` y buscando el `sceneBase` en GameCatalog; usa el perfil activo. Devuelve 0 si la escena no es un minijuego del catálogo.
- `float Factor()` — el multiplicador de la partida actual según 3.2 (usa `RankEscenaActual()`); devuelve 1f fuera de un minijuego.
- Helpers de escalado para usar en los `ApplyDifficulty()` de los juegos:
  - `int Mas(int v)` → `Mathf.RoundToInt(v * Factor())` (más rondas/objetivos/distractores…)
  - `float Menos(float t, float minimo)` → `Mathf.Max(minimo, t / Factor())` (menos tiempo/ventana, con suelo de seguridad)
  - `float MasF(float v)` → `v * Factor()` (velocidades)
- `void RegistrarVictoria()` — sube 1 el rango del juego actual (tope 4). La llama MinigameBase (ver 3.5). Debe ser idempotente dentro de una misma partida (guarda un flag de sesión o compara contra un timestamp) por si `CompleteMinigame` se invocara dos veces.
- `int SumaDistrito(string profileId, int categoria)` — suma de rangos de los 5 juegos del distrito (0-20).
- Utilidades de presentación: `string NombreRango(int r)` ("EN RUINAS", "BRONCE", "PLATA", "ORO", "DIAMANTE") y `Color ColorRango(int r)`: bronce (0.80, 0.50, 0.25), plata (0.75, 0.80, 0.88), oro (1.00, 0.82, 0.12), diamante (0.45, 0.90, 1.00); rango 0 → gris `KidUI.DIM`.

### 3.5 Enganche central en MinigameBase
En `MinigameBase.CompleteMinigame(...)`, añade UNA llamada: `ChallengeSystem.RegistrarVictoria();` (respetando cualquier guard interno que evite completar dos veces — léelo antes). NO toques nada más de MinigameBase salvo, opcionalmente, el chip de reto de 3.8.

### 3.6 Integración en DistrictScreen (tarjetas de sector)
- Marco de la tarjeta con el color del rango (el borde "Edge" que hoy solo sale al revivir pasa a usar `ColorRango`).
- Chip de rango junto a la etiqueta del sector ("PLANTA 2 · ORO"), y bajo el nombre del juego el CTA según estado: rango 0 → "¡REVIVIR!" (como hoy); rangos 1-3 → "¡Supéralo para PLATA/ORO/DIAMANTE!"; rango 4 → "SECTOR DE DIAMANTE" + gema (círculo cian con `StarTwinkle`) y, si quieres lucirte, un aro `RingThick` girando lento alrededor del logo (copia el patrón `SlowSpin` de ProgressMapScreen o crea uno local — SIN duplicar el nombre de clase `SlowSpin`, que ya existe: reutilízala, es pública).
- La cabecera "FUENTE: n/5" pasa a **"FUENTE: X/20"** con `SumaDistrito`; con 20/20 → "FUENTE LEGENDARIA" en dorado.
- CELEBRACIÓN de subida: al construirse la pantalla, si `Rank > reto_visto`, banner centrado ("¡LA PLANTA 2 YA ES DE ORO!", color del rango) + `GameFeel.Confetti` + `PlayStar`, y persiste `reto_visto`. Patrón de referencia: las celebraciones de `ProgressMapScreen.CelebrateNewBadges`.
- El logo del minijuego sigue siendo el protagonista del sector revivido: el rango lo ENMARCA, no lo sustituye.

### 3.7 Integración en DistrictPickScreen y en el hub (ProgressMapScreen)
- DistrictPickScreen: los 5 puntos de progreso pasan a una lectura /20 (mini-barra o texto "13/20") + el marco de la tarjeta con el color del rango MEDIO del distrito (suma/5 redondeado).
- ProgressMapScreen (medallones del planeta): el aro radial se rellena con `suma/20f` y el contador central muestra "13/20". OJO: el resto del hub (misión diaria, racha, logros, ¡A JUGAR!) NO cambia; la constante `GAMES_PER_CATEGORY` se sigue usando en la misión diaria — no la rompas.
- `DistrictScreen`/`DistrictPickScreen`/medallones deben seguir funcionando perfectamente para INVITADOS (profileId null → todo rango 0, sin excepciones).

### 3.8 Chip de reto dentro del minijuego (opcional pero deseable)
Pequeño chip en una esquina superior durante la partida: "RETO: ORO" con su color. Impleméntalo UNA sola vez de forma central (p. ej. al final de `MinigameBase.Start()` o en `OnMinigameStart` de la base si existe punto común), en un canvas propio de sortingOrder alto (~60), con raycastTarget=false en todo. Si un juego concreto lo tapa, no pasa nada. Solo si `RankEscenaActual() >= 1`.

## 4. Aplicar el factor a los 25 minijuegos

Localiza cada manager partiendo de GameCatalog (grep del nombre de telemetría o del `sceneBase` en `Assets/_Project/Scripts/Minigames/`). En el `ApplyDifficulty()` (o equivalente) de CADA juego, escala 2-4 parámetros clave con los helpers (`Mas`, `Menos`, `MasF`). Reglas:

- Escala lo que hace el juego MÁS DESAFIANTE, no lo que lo hace injusto: menos tiempo por ronda, secuencias/patrones más largos, más distractores/señuelos, objetivos más rápidos o pequeños, más rondas para ganar, más desvíos/anillos/pasos.
- SIEMPRE con suelos/techos de seguridad (`Menos(t, minimo)`); ningún juego puede volverse físicamente imposible (ventanas de reacción nunca por debajo de ~450 ms en Easy, ~300 ms en Hard; tableros nunca mayores de lo que cabe en pantalla).
- Los juegos de GESTIÓN EMOCIONAL escalan EN SUAVE (son terapéuticos): "Vuelve a la calma" → más ciclos y ritmo más variable, jamás estresante; "Detective de emociones" → más opciones/matices y menos tiempo de pista; "Rescate emocional" → más opciones y situaciones más matizadas; "Balance perfecto" y "Mantén el control" → escalado normal pero con suelo generoso.
- Juegos nuevos ya preparados para crecer: "El tren de Attentia" (`ActionSequenceController.cs`) → más desvíos/tramos rotos; "Torres de energía" (`ResourceGameController.cs`) → más anillos y objetivo de movimientos más ajustado.
- Documenta en un comentario de una línea qué escala cada juego: `// Reto: +desvíos, -tiempo de planificación`.

Hazlo en dos tandas y compila mentalmente entre ambas: (1) los 13 de Atención + Memoria + Impulsos, (2) los 12 de Planificación + Emocional.

## 5. Verificación final (obligatoria, en este orden)

1. `ChallengeSystem.cs`: reléelo entero; comprueba la migración de rangos (telemetría → bronce) y la idempotencia de `RegistrarVictoria`.
2. Grep de `ChallengeSystem.` en los 25 managers: los 25 deben aparecer; ninguno debe escalar parámetros sin suelo de seguridad.
3. `MinigameBase`: exactamente UNA llamada nueva a `RegistrarVictoria`, dentro del camino de éxito.
4. DistrictScreen / DistrictPickScreen / ProgressMapScreen: sin referencias a la antigua lectura n/5 de la Fuente que hayan quedado a medias; invitado (profileId null) no lanza excepciones.
5. Nombres: no has creado ninguna clase que colisione con las existentes (`SlowSpin`, `DistrictArt`, `WorldNavRouter`, `GameCatalog`...).
6. Balance de llaves y firmas correctas en todos los archivos tocados.

Al terminar, responde con: lista de archivos creados/modificados, tabla juego → parámetros escalados, y el resultado de cada punto de la verificación.
