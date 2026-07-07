# LEEME — Sistema de perfiles, telemetría e informes (AttentiON)

Fecha: julio 2026. Añadido de forma modular, sin tocar el flujo de escenas ni los 25 minijuegos.

## Paquetes instalados

**Ninguno.** Todo funciona con lo que ya incluye Unity 2022.3:

- Persistencia: JSON con `JsonUtility` (detrás de la interfaz `IDataStore`, migrable a SQLite sin tocar el resto).
- Excel: generador `.xlsx` propio con `System.IO.Compression` (formato Office Open XML).
- Informe visual: HTML autónomo con gráficas SVG/CSS (imprimible a PDF desde el navegador).
- PIN: hash SHA-256 (`System.Security.Cryptography`).

## Dónde está cada sistema

| Sistema | Ruta |
|---|---|
| Modelos de datos (`Profile`, `Session`, `MinigameResult`) | `Assets\_Project\Scripts\Systems\Data\DataModels.cs` |
| Interfaz de persistencia | `Assets\_Project\Scripts\Systems\Data\IDataStore.cs` |
| Implementación JSON | `Assets\_Project\Scripts\Systems\Data\JsonDataStore.cs` |
| Gestor de perfiles (singleton) | `Assets\_Project\Scripts\Systems\Data\ProfileManager.cs` |
| Telemetría (singleton) | `Assets\_Project\Scripts\Systems\Data\TelemetryManager.cs` |
| Generador de informes | `Assets\_Project\Scripts\Systems\Data\ReportGenerator.cs` |
| Escritor XLSX sin dependencias | `Assets\_Project\Scripts\Systems\Data\XlsxWriter.cs` |
| Pantalla de perfiles ("¿Quién juega hoy?") | `Assets\_Project\Scripts\Systems\UI\ProfileScreenController.cs` |
| Teclado de PIN | `Assets\_Project\Scripts\Systems\UI\PinPrompt.cs` |
| Área del tutor | `Assets\_Project\Scripts\Systems\UI\TutorPanel.cs` |
| Helpers de UI por código | `Assets\_Project\Scripts\Systems\UI\KidUI.cs` |
| Avatares (copias de sprites existentes) | `Assets\_Project\Resources\Avatars\` |

`ProfileManager` y `TelemetryManager` se auto-crean con `[RuntimeInitializeOnLoadMethod]`
(mismo patrón que `SceneTransition`): **no hay que colocarlos en ninguna escena**.

### Ficheros existentes modificados

- `MinigameBase.cs`: hooks de telemetría en `LaunchGame`/`CompleteMinigame`/`FailMinigame` y
  nueva API opcional `ReportEvent(bool acierto, float tiempoReaccionMs = -1f)` para que cada
  minijuego pueda reportar rondas (aciertos/errores/tiempo de reacción). Los minijuegos que
  no la usan siguen funcionando igual.
- `PauseMenuController.cs`: dos botones nuevos en el menú ESC: **"Descargar informe"**
  (protegido por PIN) y **"Cambiar jugador"**. Panel ampliado de 420 a 500 px.

## Flujo

1. Al arrancar (escena `PrimeraPantalla`), aparece el selector de perfiles sobre la pantalla:
   tarjetas grandes con avatar, "Nuevo jugador", "Jugar sin guardar" (invitado, no persiste)
   y botón discreto "ADULTO".
2. Registro apto para no lectores: nombre (lo puede escribir un adulto), avatar por iconos y
   edad con los tres robots (3-5 → NEO/Fácil, 5-7 → AXEL/Medio, 7-10 → TITAN/Difícil, fija la
   dificultad recomendada automáticamente).
3. Al elegir perfil se abre una `Session`; cada minijuego terminado guarda un `MinigameResult`.
   La sesión actualiza su hora de fin tras cada partida (robusto ante cierres bruscos) y se
   cierra al salir o cambiar de jugador.
4. "ADULTO" (o "Descargar informe" en ESC) pide un **PIN de 4 dígitos** (se crea la primera vez,
   con confirmación). El área del tutor muestra resumen por niño, genera informes, permite
   **borrar todos los datos del menor** y cambiar el PIN.

## Datos e informes: dónde se guardan

- **Datos**: `%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\AttentiONData\`
  (`Application.persistentDataPath`): un `profile_<id>.json` por niño + `tutor_settings.json`.
  Todo local, sin nube.
- **Informes**: `Documentos\AttentiON\Informes\Informe_<nombre>_<fecha>.{xlsx,csv,html}`.
  El HTML se abre automáticamente al generarse (los tres quedan en la carpeta).
  - **XLSX**: hojas *Resumen*, *Por categoría*, *Por minijuego*, *Sesiones*, *Detalle partidas*.
  - **CSV**: detalle de partidas (separador `;`, UTF-8 con BOM, apto para Excel en español).
  - **HTML**: gráfica de barras por categoría, línea de evolución temporal, tablas e
    interpretación automática prudente (sin diagnóstico). Imprimir → Guardar como PDF.

## Cómo probar

1. Abrir el proyecto en Unity 2022.3.51f1 y esperar la recompilación (no hay que añadir nada a
   ninguna escena).
2. Play en `PrimeraPantalla` → debe aparecer "¿QUIÉN JUEGA HOY?". Crear un perfil (nombre,
   avatar, edad) y comprobar en consola `[ProfileManager] Perfil activo...` y
   `[Telemetry] Sesión iniciada...`.
3. Jugar 2-3 minijuegos hasta completarlos/fallarlos → consola `[Telemetry] Resultado guardado...`.
4. Pulsar ESC → "Descargar informe" → crear el PIN (2 veces) → se abre el informe HTML en el
   navegador y quedan XLSX + CSV en `Documentos\AttentiON\Informes`.
5. ESC → "Cambiar jugador" → vuelve al selector. "Jugar sin guardar" = modo invitado (nada se
   persiste).
6. Botón "ADULTO" del selector → PIN → área del tutor: resumen, informe, borrar datos, cambiar PIN.

## Notas de diseño / decisiones

- **PIN global de tutor** (en `tutor_settings.json`) en lugar de un PIN por perfil: el adulto es
  el mismo para todos los niños del dispositivo y simplifica el uso. El campo queda hasheado.
- **Telemetría por ronda ya instrumentada:** los 27 minijuegos llaman a
  `ReportEvent(acierto, tiempoReaccionMs)` en cada acierto/fallo. Por tanto los informes
  incluyen **% de acierto y tiempo de reacción medio** además de partidas, puntuaciones,
  duraciones y monedas. (Los minijuegos que no midan tiempo de reacción pasan `-1f`.)
- Máximo 5 perfiles (6 tarjetas grandes en 2 filas, sin scroll). Ampliable en
  `ProfileScreenController.MAX_PROFILES` (requeriría añadir scroll o reducir tarjetas).
- Para migrar a SQLite: implementar `IDataStore` y cambiar la línea `new JsonDataStore()` en
  `ProfileManager.Awake()`.
