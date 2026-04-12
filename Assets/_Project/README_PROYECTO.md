# AttentionTFG — Estructura del Proyecto
> Última reorganización: Abril 2026

---

## Estructura de carpetas

```
Assets/
├── _Project/                    ← TODO el código y assets propios
│   ├── Scripts/
│   │   ├── Core/                ← Sistemas globales (GameManager, SceneLoader, DifficultyManager)
│   │   ├── Systems/
│   │   │   ├── UI/              ← UIManager, ButtonHoverScale, StartScreenFadeText
│   │   │   ├── Score/           ← CoinManager, CoinManager2
│   │   │   └── Navigation/      ← LevelButton
│   │   ├── Minigames/
│   │   │   ├── MinigameBase.cs  ← Clase base abstracta para TODOS los minijuegos
│   │   │   ├── Memory/          ← SimonGame, MemoryGameManager, CameraController, MonedaManager
│   │   │   ├── EmotionalManagement/ ← CharacterJumper, MemorySelector, CoinPickUp
│   │   │   ├── Planning/        ← Spawner, DetectorColision, MoverPlataforma
│   │   │   ├── Attention/       ← [vacío, listo para nuevos minijuegos]
│   │   │   └── ImpulseControl/  ← [vacío, listo para nuevos minijuegos]
│   │   └── Decorative/          ← CloudMover, Float, RotatingCoin
│   ├── Scenes/
│   │   ├── Main/                ← PrimeraPantalla.unity, DifficultySelector.unity, MainMenu.unity
│   │   ├── GameSelectors/       ← GameSelector.unity (Easy), GameSelector2.unity (Medium), GameSelector3.unity (Hard)
│   │   └── Minigames/
│   │       ├── Memory/          ← SimonSays.unity, ¡Algo no cuadra!.unity
│   │       ├── EmotionalManagement/ ← Aventura emocional.unity
│   │       ├── Attention/       ← Attention.unity
│   │       ├── Planning/        ← [vacío, pendiente de crear FabricaLoca.unity]
│   │       └── ImpulseControl/  ← [vacío]
│   ├── Prefabs/
│   │   ├── UI/
│   │   ├── Common/
│   │   └── Minigames/
│   │       ├── Memory/
│   │       └── EmotionalManagement/ ← AudioCoin.prefab
│   ├── Materials/               ← CoinColoro.mat, Coin.mat
│   ├── Audio/
│   │   ├── SFX/                 ← Sonidos de colores (SimonSays), monedas, etc.
│   │   └── Music/
│   └── Data/                    ← ScriptableObjects, configuraciones JSON
│
├── ExternalAssets/              ← Assets descargados de Asset Store (NO TOCAR)
│   ├── DuNguyn_CloudsPack/
│   ├── RPGTinyHeroDuo/
│   ├── Supercyan_FreeForestSample/
│   ├── LightningPoly_FootballEssentials/
│   ├── TierrasDeRol_Basketball/
│   ├── LowPolyObjectsPack/
│   └── MaximeBrunoni_CountryHouse/
│
└── TextMesh Pro/                ← Paquete de Unity (NO TOCAR)
```

---

## Convención de nombres de escena

Los nombres de escena se mantienen para no romper referencias. En el futuro, al añadir nuevos minijuegos, usa este formato:

```
[Categoria]_[NombreJuego]_[Dificultad]
Ejemplos:
  Memory_ColorMatch_Easy
  Attention_FindTheObject_Medium
  ImpulseControl_StopTheBar_Hard
```

---

## Mapa de escenas actuales

| Escena (nombre exacto) | Ubicación | Descripción |
|---|---|---|
| `PrimeraPantalla` | Scenes/Main/ | Pantalla de inicio con animación |
| `DifficultySelector` | Scenes/Main/ | Selección de dificultad (Easy/Medium/Hard) |
| `MainMenu` | Scenes/Main/ | Menú principal alternativo |
| `GameSelector` | Scenes/GameSelectors/ | Selector de minijuegos — Dificultad FÁCIL |
| `GameSelector2` | Scenes/GameSelectors/ | Selector de minijuegos — Dificultad MEDIA |
| `GameSelector3` | Scenes/GameSelectors/ | Selector de minijuegos — Dificultad DIFÍCIL |
| `SimonSays` | Scenes/Minigames/Memory/ | Minijuego Memoria — Simón Dice |
| `¡Algo no cuadra!` | Scenes/Minigames/Memory/ | Minijuego Memoria — Encuentra el error |
| `Aventura emocional` | Scenes/Minigames/EmotionalManagement/ | Minijuego Gestión Emocional |
| `Attention` | Scenes/Minigames/Attention/ | Minijuego Atención |

---

## Niveles de dificultad

| Enum | Escena | Temática | Edad |
|---|---|---|---|
| `DifficultyLevel.Easy` | GameSelector | BosqueMagico | 6-8 años |
| `DifficultyLevel.Medium` | GameSelector2 | CastilloVolador | 9-11 años |
| `DifficultyLevel.Hard` | GameSelector3 | CuevaMisteriosa | 12+ años |

---

## Sistemas base creados

### GameManager.cs (`Scripts/Core/`)
Singleton persistente. Gestiona la dificultad activa y la puntuación total acumulada.
```csharp
GameManager.Instance.SetDifficulty(DifficultyLevel.Easy);
GameManager.Instance.AddScore(10);
```

### SceneLoader.cs (`Scripts/Core/`)
Clase estática con constantes de nombres de escena y métodos de navegación.
```csharp
SceneLoader.LoadScene(SceneLoader.MEMORY_SIMON_SAYS);
SceneLoader.LoadGameSelector();  // carga el selector según la dificultad activa
SceneLoader.GoToMainMenu();
```

### DifficultyManager.cs (`Scripts/Core/`)
Componente para los botones del DifficultySelector. Adjúntalo al gestor de la escena.
```csharp
// Conectar en Inspector a los botones:
difficultyManagerRef.SelectEasy();
difficultyManagerRef.SelectMedium();
difficultyManagerRef.SelectHard();
```

### MinigameBase.cs (`Scripts/Minigames/`)
Clase base abstracta para todos los minijuegos.
```csharp
public class MiJuego : MinigameBase
{
    protected override void OnMinigameStart()   { /* inicializar */ }
    protected override void OnMinigameComplete(){ /* victoria → CompleteMinigame(score) */ }
    protected override void OnMinigameFailed()  { /* derrota → FailMinigame() */ }
}
```

### UIManager.cs (`Scripts/Systems/UI/`)
Singleton para fade de pantalla y mensajes globales.
```csharp
UIManager.Instance.FadeAndLoadScene("SimonSays");
UIManager.Instance.ShowStatus("¡Correcto!");
```

---

## Cómo añadir un nuevo minijuego

1. Crea la escena en `Assets/_Project/Scenes/Minigames/[Categoria]/`
2. Añade la constante en `SceneLoader.cs`
3. Crea el script heredando de `MinigameBase`
4. Coloca el script en `Assets/_Project/Scripts/Minigames/[Categoria]/`
5. Añade la escena al Build Settings en Unity Editor

---

## Problemas del proyecto original (corregidos)

- Scripts mezclados con assets 3D y escenas dentro de la misma carpeta
- Assets externos (DuNguyn, Supercyan, RPG Hero...) mezclados con código propio
- `CoinManager.cs` duplicado con clase diferente (`CoinManager` vs `CoinManager2`)
- `Spwner.cs` con typo en el nombre (movido como `Spawner.cs`)
- Escenas de demo de assets externos (`Demo Scene`, `SampleScene`, etc.) en la ruta de juego
- Sin GameManager ni SceneLoader — cada escena navegaba de forma independiente
- Sin convención de nombres para escenas (GameSelector, GameSelector2, GameSelector3)
- Carpetas de categoría vacías sin estructura escalable
