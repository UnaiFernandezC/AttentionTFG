using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Define las 3 rutas del minijuego Memoria de Ruta.
/// Nivel 0 = muy fácil, Nivel 1 = medio, Nivel 2 = difícil (patrón diferente).
/// Formato: lista de posiciones (col, row), primera = inicio, última = meta.
/// Visual: row 0 = fila superior, col 0 = columna izquierda.
/// </summary>
public class PathMemoryPathManager : MonoBehaviour
{
    public struct LevelConfig
    {
        public int   gridSize;
        public float displaySeconds;
    }

    public static readonly int TotalLevels = 3;

    static readonly LevelConfig[] _configs = new[]
    {
        new LevelConfig { gridSize = 5, displaySeconds = 5f }, // Nivel 1 – muy fácil
        new LevelConfig { gridSize = 5, displaySeconds = 4f }, // Nivel 2 – medio
        new LevelConfig { gridSize = 5, displaySeconds = 3f }, // Nivel 3 – difícil
    };

    // Todas las posiciones son adyacentes (horizontal o vertical).
    static readonly List<Vector2Int>[] _routes = new List<Vector2Int>[]
    {
        // ── Nivel 1: L pequeña centrada — 5 casillas, 4 pasos ──────
        // Derecha × 2, luego abajo × 2
        new List<Vector2Int>
        {
            new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(3,1),
            new Vector2Int(3,2), new Vector2Int(3,3)
        },

        // ── Nivel 2: L grande + vuelta — 8 casillas, 7 pasos ───────
        // Derecha × 3, abajo × 2, izquierda × 2
        new List<Vector2Int>
        {
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
            new Vector2Int(3,0), new Vector2Int(3,1), new Vector2Int(3,2),
            new Vector2Int(2,2), new Vector2Int(1,2)
        },

        // ── Nivel 3: zigzag en Z — 13 casillas, 12 pasos ───────────
        // Baja por la izquierda, cruza al centro, sube, cruza a la derecha, baja.
        new List<Vector2Int>
        {
            new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2),
            new Vector2Int(1,2), new Vector2Int(2,2), new Vector2Int(2,1),
            new Vector2Int(2,0), new Vector2Int(3,0), new Vector2Int(4,0),
            new Vector2Int(4,1), new Vector2Int(4,2), new Vector2Int(4,3),
            new Vector2Int(4,4)
        },
    };

    // ── API pública ──────────────────────────────────────────────

    public List<Vector2Int> GetRoute(int level)
    {
        int l = Mathf.Clamp(level, 0, _routes.Length - 1);
        return new List<Vector2Int>(_routes[l]);   // copia defensiva
    }

    public LevelConfig GetConfig(int level)
    {
        int l = Mathf.Clamp(level, 0, _configs.Length - 1);
        return _configs[l];
    }
}
