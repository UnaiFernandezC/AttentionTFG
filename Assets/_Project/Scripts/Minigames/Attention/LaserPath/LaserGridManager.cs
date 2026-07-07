// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona el estado de la cuadricula y el trazado del rayo laser.
/// No depende de Unity UI -- es logica pura de datos.
/// </summary>
public class LaserGridManager
{
    public int Rows { get; private set; }
    public int Cols { get; private set; }

    private LaserCell[,] _grid;

    // Resultado del ultimo trazado
    public bool           LaserReachedTarget { get; private set; }
    public List<Vector2Int> LaserPath         { get; private set; } = new List<Vector2Int>();

    // --- Inicializacion --------------------------------------------------------
    public void LoadLevel(LaserLevelData data)
    {
        Rows  = data.rows;
        Cols  = data.cols;
        _grid = data.CloneGrid();
        TraceLaser();
    }

    public LaserCell GetCell(int row, int col) => _grid[row, col];

    // --- Interaccion -----------------------------------------------------------
    /// <summary>Rota un espejo rotable entre / y \. Devuelve true si se roto.</summary>
    public bool ToggleMirror(int row, int col)
    {
        var cell = _grid[row, col];
        if (cell.type != LaserCellType.Mirror || cell.isFixed) return false;

        cell.mirrorKind = cell.mirrorKind == LaserMirrorKind.Slash
            ? LaserMirrorKind.Backslash
            : LaserMirrorKind.Slash;

        TraceLaser();
        return true;
    }

    // --- Trazado del laser -----------------------------------------------------
    public void TraceLaser()
    {
        LaserPath.Clear();
        LaserReachedTarget = false;

        // Encontrar el emisor
        int  er = -1, ec = -1;
        LaserDirection dir = LaserDirection.Right;

        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                if (_grid[r, c].type == LaserCellType.Emitter)
                {
                    er  = r; ec = c;
                    dir = _grid[r, c].emitterDir;
                }

        if (er < 0) return;

        // Trazar el rayo paso a paso
        int  cr = er, cc = ec;
        var  visited = new HashSet<(int, int, LaserDirection)>();
        int  maxSteps = Rows * Cols * 4 + 4;

        for (int step = 0; step < maxSteps; step++)
        {
            // Avanzar un paso en la direccion actual
            (cr, cc) = Step(cr, cc, dir);

            // Comprobar limites
            if (cr < 0 || cr >= Rows || cc < 0 || cc >= Cols) break;

            var key = (cr, cc, dir);
            if (visited.Contains(key)) break;   // bucle infinito -- parar
            visited.Add(key);

            LaserPath.Add(new Vector2Int(cr, cc));

            var cell = _grid[cr, cc];

            if (cell.type == LaserCellType.Target)
            {
                LaserReachedTarget = true;
                break;
            }

            if (cell.type == LaserCellType.Wall) break;

            if (cell.type == LaserCellType.Mirror)
                dir = Reflect(dir, cell.mirrorKind);

            // Emitter = no bloquea, el laser pasa por encima
        }
    }

    // --- Utilidades ------------------------------------------------------------
    private static (int r, int c) Step(int r, int c, LaserDirection dir)
    {
        return dir switch
        {
            LaserDirection.Right => (r,     c + 1),
            LaserDirection.Left  => (r,     c - 1),
            LaserDirection.Up    => (r - 1, c    ),
            LaserDirection.Down  => (r + 1, c    ),
            _                    => (r,     c    ),
        };
    }

    /// <summary>
    /// Reflexion optica:
    ///   / (Slash):  -> ^,  <- v,  ^ ->,  v <-
    ///   \ (Backsl): -> v,  <- ^,  ^ <-,  v ->
    /// </summary>
    private static LaserDirection Reflect(LaserDirection dir, LaserMirrorKind kind)
    {
        if (kind == LaserMirrorKind.Slash)
        {
            return dir switch
            {
                LaserDirection.Right => LaserDirection.Up,
                LaserDirection.Left  => LaserDirection.Down,
                LaserDirection.Up    => LaserDirection.Right,
                LaserDirection.Down  => LaserDirection.Left,
                _                    => dir,
            };
        }
        else // Backslash
        {
            return dir switch
            {
                LaserDirection.Right => LaserDirection.Down,
                LaserDirection.Left  => LaserDirection.Up,
                LaserDirection.Up    => LaserDirection.Left,
                LaserDirection.Down  => LaserDirection.Right,
                _                    => dir,
            };
        }
    }
}
