# PROMPT DE CONTINUACIÓN — Rediseño de minijuegos AttentiON

Copia todo lo que hay debajo de la línea en una conversación nueva (Claude Code o Cowork con esta carpeta conectada).

---

Trabajas en el proyecto Unity 2022.3 "AttentiON" (carpeta raíz del repo) — juego educativo para niños de 3-10 años con TDAH, 25 minijuegos en 5 categorías. Toda la UI nueva se construye 100% POR CÓDIGO (prohibido editar escenas .unity o .meta).

## Contexto obligatorio (léelo antes de tocar nada)
- `Assets/_Project/Scripts/Minigames/MinigameBase.cs` — ciclo de vida: en `Start()` se asigna `minigameName` y `category` y se llama `base.Start()`; overrides `GetIntroDescription()`, `OnMinigameStart()`, `OnMinigameComplete()`, `OnMinigameFailed()`; métodos `CompleteMinigame(score)`, `FailMinigame()`, `ReportEvent(acierto, rtMs)`, `ShowResults(success, stars, score, string[] stats, title, subtitle)`, `RestartMinigame()`, `ReturnToGameSelector()`.
- `Assets/_Project/Scripts/Systems/UI/KidUI.cs` — helpers de UI por código: `MakeCanvas`, `BuildSpaceBackground`, `Img`, `RoundImg`, `CircleAt`, `Txt`, `Btn`, `Sprite`; colores `ACCENT/GOOD/WARN/BAD/DIM/PANEL/BTNC`; componentes `StarTwinkle`, `FloatBob`.
- `Assets/_Project/Scripts/Systems/UI/GameFeel.cs` — `PlaySuccess/PlayError/PlayPop/PlayStar`, `Confetti`, `FloatingText`, `Shake`, `ScreenFlash`, `CountUp`, `Success`, `Error`, `StarsFromRatio`.
- `Assets/_Project/Scripts/Systems/UI/UITween.cs` — `PopIn`, `FadeIn`, `FadeOut`, `PulseOnce`; y `ButtonJuice.Attach(go)`.
- Ejemplo de minijuego bien hecho: `Assets/_Project/Scripts/Minigames/Memory/SimonGameManager.cs`.
- Helper YA CREADO para caras de robot con emociones: `Assets/_Project/Scripts/Minigames/EmotionalManagement/EmotionFaceArt.cs` (úsalo en los juegos A y B).

## Reglas de oro
1. NO editar escenas ni .meta. CONSERVA los nombres de clase y TODOS los campos `public`/`[SerializeField]` existentes (aunque queden sin uso) para no romper referencias serializadas.
2. Cabecera en cada archivo tocado: `// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com`
3. Comentarios y textos de UI en español, tono amable para niños.
4. Dificultad vía `GameManager.Instance.CurrentDifficulty` (Easy/Medium/Hard) con parámetros claramente distintos.
5. UI nueva en `OnMinigameStart()` sobre `KidUI.MakeCanvas("...", 50, transform)` + `KidUI.BuildSpaceBackground(root)` (fondo OPACO que tapa y bloquea la UI vieja de la escena).
6. Si la escena tiene sub-controladores legacy que estorben (Update/corrutinas/spawners), VACÍA su comportamiento (deja la clase y sus campos). Escenas en `Assets/_Project/Scenes/Minigames/EmotionalManagement/...`.
7. Sesiones de 1-3 minutos, feedback inmediato y cálido, sin castigos duros (en emocional, especialmente amable).
8. Color de Gestión emocional: verde (0.18, 0.80, 0.58). Impulsos: naranja (0.95, 0.55, 0.12). Planificación: azul (0.28, 0.60, 1.00).
9. Verificación estática final: relee tus archivos y comprueba cada llamada contra las firmas reales.

## YA HECHO (no repetir; revisar solo si la verificación detecta fallos)
- Planificación: `ActionSequenceController.cs` reescrito como **"El tren de Attentia"** y `ResourceGameController.cs` como **"Torres de energía"**.
- Rediseños visuales hechos: Camino Láser (`Attention/LaserPath/`), Reacción Rápida (`Attention/QuickReaction*`), Repite el Dibujo (`Memory/PatternGameController.cs`).
- `Systems/World/GameCatalog.cs` ya tiene los nombres nuevos: "El tren de Attentia", "Torres de energía", "Detective de emociones", "Ordena la emoción", "Vuelve a la calma" (los `minigameName` de los juegos nuevos deben coincidir EXACTAMENTE con estos).
- `EmotionFaceArt.cs` creado.
- **"Vuelve a la calma"** (`RegulationGameManager.cs`) reescrito ✓.
- **"Detective de emociones"** (`MemorySelector.cs`) reescrito ✓ — pero fue lo último antes de un corte: LÉELO ENTERO y verifica que está completo y compila-encaja (llaves balanceadas, llamadas correctas a EmotionFaceArt/KidUI/MinigameBase); repáralo si quedó a medias.

## PENDIENTE — 3 encargos

(Nota: "Ordena la emoción" fue DESCARTADO; en su lugar ya está escrito **"Rescate emocional"** en `ConsequencesGameManager.cs` — elegir la estrategia que calma al robot. GameCatalog ya actualizado. No tocar.)

### D) MEJORA VISUAL (mecánica y nombre INTACTOS): "Atraccion Emocional"
Archivo: `Assets/_Project/Scripts/Minigames/EmotionalManagement/AttractionGameManager.cs`. Solo presentación: fondo espacial, orbes con glow y estelas, paleta verde, HUD redondeado, feedback juicy, PopIn de entrada. NO cambiar reglas/telemetría/minigameName.

### E) MEJORA VISUAL (mecánica y nombre INTACTOS): "No pulses todavia"
Archivo: `Assets/_Project/Scripts/Minigames/ImpulseControl/DontPressGameManager.cs`. Botonazo central enorme con aro de estado (rojo=espera, verde=¡ya!), señales grandes e inequívocas, tensión visual sutil, HUD limpio, paleta naranja.

### F) MEJORA VISUAL (mecánica y nombre INTACTOS): "Ruta optima"
Archivo: `Assets/_Project/Scripts/Minigames/Planning/OptimalPathController.cs`. Nodos redondeados con glow azul, trazo del camino animado, resaltado del recorrido, confeti al ganar. NO tocar `ActionSequenceController.cs` ni `ResourceGameController.cs` (reescritos hoy).

## Verificación final (obligatoria)
1. Grep de `minigameName =` en los 3 juegos nuevos → deben coincidir EXACTAMENTE con GameCatalog: "Detective de emociones", "Ordena la emoción", "Vuelve a la calma".
2. Ninguna clase eliminada ni campo serializado quitado.
3. Cada llamada a KidUI/GameFeel/UITween cotejada con su firma real.
4. Los juegos reescritos de hoy (tren/torres) compilan-encajan con MinigameBase (léelos por encima y corrige errores obvios si los hubiera).
