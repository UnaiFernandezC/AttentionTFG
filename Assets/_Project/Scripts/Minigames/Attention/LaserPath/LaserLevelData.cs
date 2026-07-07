// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
/// <summary>
/// Datos de nivel para el minijuego Camino Laser.
/// Codificacion de caracteres por fila:
///   '>' '<' '^' 'v'  = emisor con direccion derecha/izquierda/arriba/abajo
///   'T'              = objetivo (META)
///   's'              = espejo ROTABLE que empieza en /  (necesita girar a \)
///   'b'              = espejo ROTABLE que empieza en \  (necesita girar a /)
///   'X'              = pared opaca (bloquea el laser)
///   '.'              = celda vacia
///
/// REFLEXION de espejos:
///   / (Slash)    : derecha->arriba, izquierda->abajo, arriba->derecha, abajo->izquierda
///   \ (Backslash): derecha->abajo,  izquierda->arriba, arriba->izquierda, abajo->derecha
///
/// REGLA DE DISENO: todos los espejos empiezan en posicion INCORRECTA.
/// El jugador tiene que girarlos TODOS para que el laser llegue a META.
/// </summary>

public enum LaserCellType   { Empty, Mirror, Wall, Emitter, Target }
public enum LaserMirrorKind { Slash, Backslash }   // / vs \
public enum LaserDirection  { Right, Left, Up, Down }

public class LaserCell
{
    public LaserCellType   type;
    public LaserMirrorKind mirrorKind;
    public bool            isFixed;
    public LaserDirection  emitterDir;

    public LaserCell Clone()
    {
        return new LaserCell
        {
            type       = type,
            mirrorKind = mirrorKind,
            isFixed    = isFixed,
            emitterDir = emitterDir
        };
    }
}

public class LaserLevelData
{
    public int          rows, cols;
    public LaserCell[,] grid;
    public float        timeLimit;
    public string       hint;

    /// <summary>Crea un nivel a partir de strings por fila.</summary>
    public static LaserLevelData Build(float time, string hint, params string[] rowStrings)
    {
        int r = rowStrings.Length;
        int c = rowStrings[0].Length;
        var ld = new LaserLevelData
        {
            rows      = r,
            cols      = c,
            timeLimit = time,
            hint      = hint
        };
        ld.grid = new LaserCell[r, c];

        for (int i = 0; i < r; i++)
        {
            for (int j = 0; j < c; j++)
            {
                char ch = j < rowStrings[i].Length ? rowStrings[i][j] : '.';
                var cell = new LaserCell();
                switch (ch)
                {
                    case '>': cell.type = LaserCellType.Emitter; cell.emitterDir = LaserDirection.Right; break;
                    case '<': cell.type = LaserCellType.Emitter; cell.emitterDir = LaserDirection.Left;  break;
                    case '^': cell.type = LaserCellType.Emitter; cell.emitterDir = LaserDirection.Up;    break;
                    case 'v': cell.type = LaserCellType.Emitter; cell.emitterDir = LaserDirection.Down;  break;
                    case 'T': cell.type = LaserCellType.Target;  break;
                    case 'X': cell.type = LaserCellType.Wall;    cell.isFixed = true; break;
                    // Espejo ROTABLE: empieza en / (incorrecto), hay que girarlo a \
                    case 's': cell.type = LaserCellType.Mirror; cell.mirrorKind = LaserMirrorKind.Slash;     break;
                    // Espejo ROTABLE: empieza en \ (incorrecto), hay que girarlo a /
                    case 'b': cell.type = LaserCellType.Mirror; cell.mirrorKind = LaserMirrorKind.Backslash; break;
                    default:  cell.type = LaserCellType.Empty;   break;
                }
                ld.grid[i, j] = cell;
            }
        }
        return ld;
    }

    /// <summary>Devuelve una copia profunda de la cuadricula.</summary>
    public LaserCell[,] CloneGrid()
    {
        var copy = new LaserCell[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                copy[r, c] = grid[r, c].Clone();
        return copy;
    }
}

/// <summary>
/// Coleccion de niveles predefinidos.
/// TODOS los espejos son rotatables (ningun espejo fijo).
/// TODOS los espejos empiezan en posicion incorrecta: el jugador los gira todos.
/// </summary>
public static class LaserLevels
{
    // =========================================================================
    // FACIL: cuadricula 5x5, 45 s, 2-3 espejos rotatables
    // =========================================================================
    // 's' comienza como / (hay que girarlo a \)
    // 'b' comienza como \ (hay que girarlo a /)
    // -------------------------------------------------------------------------
    public static readonly LaserLevelData[] Easy =
    {
        // E1: 3 espejos
        // Camino solucion: >(0,0) -> derecha -> s(0,2)=\ -> abajo
        //                  -> b(2,2)=/ -> derecha -> s(2,4)=\ -> abajo -> T(4,4)
        // Inicio (incorrecto):
        //   laser >(0,0) -> (0,1) -> (0,2)=/ -> sube -> sale arriba  [no llega]
        LaserLevelData.Build(45f,
            "Gira los 3 espejos naranjas para guiar el laser",
            ">.s..",
            ".....",
            "..s.s",
            ".....",
            "....T"),

        // E2: 2 espejos
        // Camino solucion: ^(4,0) -> arriba -> b(2,0)=/ -> derecha
        //                  -> s(2,3)=\ -> abajo -> T(4,3)
        // Inicio (incorrecto):
        //   laser ^(4,0) -> (3,0) -> (2,0)=\ -> izquierda -> sale  [no llega]
        LaserLevelData.Build(45f,
            "El laser sube y necesita doblar dos veces",
            ".....",
            ".....",
            "b..s.",
            ".....",
            "^..T."),

        // E3: 3 espejos
        // Emitter en (2,4) yendo izquierda.
        // Camino solucion: <(2,4) -> izq -> s(2,2)=\ -> arriba
        //                  -> b(0,2)=/ -> derecha -> s(0,4)=\ -> abajo
        //                  -> (1,4)(2,4 emitter pasa)(3,4)(4,4)=T
        // Inicio (incorrecto):
        //   laser <(2,4) -> (2,3) -> (2,2)=/ -> baja -> (3,2)(4,2) sale  [no]
        LaserLevelData.Build(45f,
            "El laser viene desde la derecha, guialo hasta META",
            "..b.s",
            ".....",
            "..s.<",
            ".....",
            "....T"),
    };

    // =========================================================================
    // MEDIO: cuadricula 6x6, 30 s, 3-4 espejos rotatables
    // =========================================================================
    public static readonly LaserLevelData[] Medium =
    {
        // M1: 3 espejos
        // Camino solucion: >(0,0) -> derecha -> s(0,2)=\ -> abajo
        //                  -> s(4,2)=\ -> derecha -> b(4,5)=/ -> arriba -> T(0,5)
        // Inicio (incorrecto):
        //   laser >(0,0) -> (0,1) -> (0,2)=/ -> sube -> sale  [no llega T(0,5)]
        LaserLevelData.Build(30f,
            "Tres espejos, el laser tiene que llegar arriba a la derecha",
            ">.s..T",
            "......",
            "......",
            "......",
            "..s..b",
            "......"),

        // M2: 4 espejos
        // Camino solucion: ^(5,0) -> arriba -> b(2,0)=/ -> derecha
        //                  -> s(2,4)=\ -> abajo -> b(4,4)=/ -> izquierda
        //                  -> s(4,1)=\ -> arriba -> T(0,1)
        // Inicio (incorrecto):
        //   laser ^(5,0) -> (4,0)(3,0)(2,0)=\ -> izquierda -> sale  [no]
        LaserLevelData.Build(30f,
            "Cuatro espejos, el laser hace un camino de ida y vuelta",
            ".T....",
            "......",
            "b...s.",
            "......",
            ".s..b.",
            "^....."),

        // M3: 4 espejos
        // Camino solucion: v(0,2) -> abajo -> b(2,2)=/ -> izquierda
        //                  -> s(2,0)=\ -> arriba -> b(0,0)=/ -> derecha
        //                  -> (pasa por emitter 0,2)(0,3)(0,4) -> s(0,4)=\ -> abajo -> T(4,4)
        // Inicio (incorrecto):
        //   laser v(0,2) -> (1,2) -> (2,2)=\ -> derecha -> sale  [no llega T(4,4)]
        LaserLevelData.Build(30f,
            "El laser baja y da una vuelta grande, ayudalo",
            "b.v.s.",
            "......",
            "s.b...",
            "......",
            "....T.",
            "......"),
    };

    // =========================================================================
    // DIFICIL: cuadricula 7x7, 35 s, 5-6 espejos rotatables
    // =========================================================================
    public static readonly LaserLevelData[] Hard =
    {
        // H1: 5 espejos
        // Camino solucion: v(0,3) -> abajo -> b(2,3)=/ -> izquierda
        //                  -> s(2,0)=\ -> arriba -> b(0,0)=/ -> derecha
        //                  -> (0,1)(0,2)(0,3 emitter pasa)(0,4)(0,5)(0,6) -> s(0,6)=\ -> abajo
        //                  -> (1,6)(2,6)(3,6)(4,6) -> b(4,6)=/ -> izquierda -> T(4,0)
        // Inicio (incorrecto):
        //   laser v(0,3) -> (1,3) -> (2,3)=\ -> derecha -> sale  [no llega T(4,0)]
        LaserLevelData.Build(35f,
            "Cinco espejos en zigzag, piensa el camino antes de hacer clic",
            "b..v..s",
            ".......",
            "s..b...",
            ".......",
            "T.....b",
            ".......",
            "......."),

        // H2: 5 espejos
        // Camino solucion: <(3,6) -> izquierda -> b(3,4)=/ -> abajo
        //                  -> s(6,4)=\ -> derecha -> b(6,6)=/ -> arriba
        //                  -> (5,6)(4,6)(3,6 emitter pasa)(2,6)(1,6) -> s(1,6)=\ -> izquierda
        //                  -> (1,5)(1,4)(1,3)(1,2)(1,1) -> b(1,1)=/ -> abajo -> T(6,1)
        // Inicio (incorrecto):
        //   laser <(3,6) -> (3,5) -> (3,4)=\ -> sube -> sale  [no llega T(6,1)]
        LaserLevelData.Build(35f,
            "El laser viene desde la derecha y da cinco giros",
            ".......",
            ".b....s",
            ".......",
            "....b.<",
            ".......",
            ".......",
            ".T..s.b"),

        // H3: 6 espejos
        // Camino solucion: ^(6,0) -> arriba -> b(4,0)=/ -> derecha
        //                  -> s(4,4)=\ -> abajo -> b(6,4)=/ -> izquierda
        //                  -> s(6,2)=\ -> arriba -> b(2,2)=/ -> derecha
        //                  -> (2,3)(2,4)(2,5)(2,6) -> b(2,6)=/ -> arriba -> T(0,6)
        // Inicio (incorrecto):
        //   laser ^(6,0) -> (5,0) -> (4,0)=\ -> izquierda -> sale  [no llega T(0,6)]
        LaserLevelData.Build(35f,
            "Seis espejos, el laser da muchas vueltas. Piensa bien el orden!",
            "......T",
            ".......",
            "..b...b",
            ".......",
            "b...s..",
            ".......",
            "^.s.b.."),
    };
}
