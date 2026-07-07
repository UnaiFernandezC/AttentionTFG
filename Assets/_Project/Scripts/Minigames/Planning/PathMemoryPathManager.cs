// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Genera rutas aleatorias, elige casillas bloqueadas sobre la ruta y calcula
/// distancias optimas (BFS) evitando los bloqueos. Sin estado entre rondas.
/// </summary>
public class PathMemoryPathManager : MonoBehaviour
{
    /// <summary>
    /// Genera una ruta aleatoria de <paramref name="cellCount"/> casillas dentro
    /// de un tablero gridSize x gridSize. Reintenta con distintos origenes hasta
    /// lograr la longitud pedida (las rutas cortas por callejon se descartan).
    /// </summary>
    public List<Vector2Int> GetRoute(int gridSize, int cellCount)
    {
        cellCount = Mathf.Clamp(cellCount, 2, gridSize * gridSize);

        List<Vector2Int> best = null;
        for (int attempt = 0; attempt < 60; attempt++)
        {
            var start = new Vector2Int(Random.Range(0, gridSize), Random.Range(0, gridSize));
            var route = GenerateRandomRoute(start, gridSize, cellCount);
            if (best == null || route.Count > best.Count) best = route;
            if (best.Count >= cellCount) break;
        }
        return best;
    }

    /// <summary>
    /// Elige hasta <paramref name="count"/> casillas INTERIORES de la ruta como
    /// bloqueadas, garantizando que siga existiendo un camino inicio→meta.
    /// </summary>
    public List<Vector2Int> PickBlockedCells(List<Vector2Int> route, int count, int gridSize)
    {
        var blocked = new List<Vector2Int>();
        if (route == null || route.Count < 3 || count <= 0) return blocked;

        var interior = new List<Vector2Int>();
        for (int i = 1; i < route.Count - 1; i++) interior.Add(route[i]);

        for (int i = interior.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = interior[i]; interior[i] = interior[j]; interior[j] = tmp;
        }

        Vector2Int start = route[0];
        Vector2Int goal  = route[route.Count - 1];

        foreach (var cand in interior)
        {
            if (blocked.Count >= count) break;
            blocked.Add(cand);
            if (ShortestPath(start, goal, blocked, gridSize) < 0)
                blocked.RemoveAt(blocked.Count - 1);
        }
        return blocked;
    }

    /// <summary>
    /// Longitud del camino mas corto (en pasos) entre dos casillas evitando las
    /// bloqueadas. Devuelve -1 si no hay camino.
    /// </summary>
    public int ShortestPath(Vector2Int from, Vector2Int to,
                            ICollection<Vector2Int> blocked, int gridSize)
    {
        if (from == to) return 0;

        var blockSet = blocked != null
            ? new HashSet<Vector2Int>(blocked)
            : new HashSet<Vector2Int>();
        if (blockSet.Contains(from) || blockSet.Contains(to)) return -1;

        var dist  = new Dictionary<Vector2Int, int> { [from] = 0 };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(from);

        var dirs = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            foreach (var d in dirs)
            {
                var next = cur + d;
                if (next.x < 0 || next.x >= gridSize || next.y < 0 || next.y >= gridSize) continue;
                if (blockSet.Contains(next) || dist.ContainsKey(next)) continue;
                dist[next] = dist[cur] + 1;
                if (next == to) return dist[next];
                queue.Enqueue(next);
            }
        }
        return -1;
    }

    List<Vector2Int> GenerateRandomRoute(Vector2Int start, int gridSize, int length)
    {
        var route   = new List<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        route.Add(start);
        visited.Add(start);
        var dirs = new Vector2Int[] { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
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
}
