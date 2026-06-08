using System.Collections.Generic;
using UnityEngine;

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
        new LevelConfig { gridSize = 5, displaySeconds = 5f },
        new LevelConfig { gridSize = 5, displaySeconds = 4f },
        new LevelConfig { gridSize = 5, displaySeconds = 3f },
    };

    static readonly List<Vector2Int>[] _routes = new List<Vector2Int>[]
    {

        new List<Vector2Int>
        {
            new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(3,1),
            new Vector2Int(3,2), new Vector2Int(3,3)
        },

        new List<Vector2Int>
        {
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
            new Vector2Int(3,0), new Vector2Int(3,1), new Vector2Int(3,2),
            new Vector2Int(2,2), new Vector2Int(1,2)
        },

        new List<Vector2Int>
        {
            new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2),
            new Vector2Int(1,2), new Vector2Int(2,2), new Vector2Int(2,1),
            new Vector2Int(2,0), new Vector2Int(3,0), new Vector2Int(4,0),
            new Vector2Int(4,1), new Vector2Int(4,2), new Vector2Int(4,3),
            new Vector2Int(4,4)
        },
    };

    public List<Vector2Int> GetRoute(int level)
    {
        int l = Mathf.Clamp(level, 0, _routes.Length - 1);
        return new List<Vector2Int>(_routes[l]);
    }

    public LevelConfig GetConfig(int level)
    {
        int l = Mathf.Clamp(level, 0, _configs.Length - 1);
        return _configs[l];
    }
}
