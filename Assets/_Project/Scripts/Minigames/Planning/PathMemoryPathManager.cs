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
        var cfg = GetConfig(level);
        int size = cfg.gridSize;
        return GenerateRandomRoute(size, level + 4 + level * 2);
    }

    List<Vector2Int> GenerateRandomRoute(int gridSize, int length)
    {
        var route = new List<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        var start = new Vector2Int(0, 0);
        route.Add(start);
        visited.Add(start);
        var dirs = new Vector2Int[]{ Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
        var current = start;
        int attempts = 0;
        while (route.Count < length && attempts < 500)
        {
            attempts++;
            var shuffled = new List<Vector2Int>(dirs);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = shuffled[i]; shuffled[i] = shuffled[j]; shuffled[j] = tmp;
            }
            bool moved = false;
            foreach (var d in shuffled)
            {
                var next = current + d;
                if (next.x >= 0 && next.x < gridSize && next.y >= 0 && next.y < gridSize && !visited.Contains(next))
                {
                    route.Add(next);
                    visited.Add(next);
                    current = next;
                    moved = true;
                    break;
                }
            }
            if (!moved) break;
        }
        return route;
    }

    public LevelConfig GetConfig(int level)
    {
        int l = Mathf.Clamp(level, 0, _configs.Length - 1);
        return _configs[l];
    }
}
