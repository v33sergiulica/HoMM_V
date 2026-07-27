using UnityEngine;
using System.Collections.Generic;
using HommClone.Creatures;

namespace HommClone.Grid
{
    /// <summary>
    /// Generates and manages the 10x12 square tile grid battlefield.
    /// Exposes search, boundary, distance, and adjacency query APIs.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Size")]
        [SerializeField] private int width = 10;
        [SerializeField] private int height = 12;

        [Header("Prefabs")]
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private float spacing = 1.0f;

        private Tile[,] _grid;

        public int Width => width;
        public int Height => height;

        private void Awake()
        {
            InitializeGrid();
        }

        /// <summary>
        /// Initializes the grid array by loading existing children or generating a new grid if empty.
        /// </summary>
        private void InitializeGrid()
        {
            _grid = new Tile[width, height];
            Tile[] childTiles = GetComponentsInChildren<Tile>();

            if (childTiles.Length > 0)
            {
                int count = 0;
                foreach (Tile tile in childTiles)
                {
                    Vector2Int pos = tile.GridPosition;
                    if (IsPositionInBounds(pos))
                    {
                        _grid[pos.x, pos.y] = tile;
                        tile.Initialize(pos); // Cache visual styling/base color at runtime
                        count++;
                    }
                }
                Debug.Log($"[GridManager] Loaded {count} pre-existing tiles from scene children.");
            }
            else
            {
                GenerateGrid();
            }
        }

        /// <summary>
        /// Dynamically instantiates the tile map at startup.
        /// </summary>
        private void GenerateGrid()
        {
            _grid = new Tile[width, height];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (tilePrefab == null)
                    {
                        Debug.LogError("[GridManager] Tile prefab is not assigned in the inspector!");
                        return;
                    }

                    // World position: relative to GridManager transform, scaled by spacing
                    Vector3 worldPos = transform.position + new Vector3(x * spacing, 0f, y * spacing);
                    GameObject tileObj = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                    tileObj.name = $"Tile_{x}_{y}";

                    Tile tile = tileObj.GetComponent<Tile>();
                    if (tile != null)
                    {
                        tile.Initialize(new Vector2Int(x, y));
                        _grid[x, y] = tile;
                    }
                    else
                    {
                        Debug.LogWarning($"[GridManager] Tile component missing on prefab spawned at ({x}, {y})");
                    }
                }
            }
            Debug.Log($"[GridManager] Successfully generated a {width}x{height} battlefield grid.");
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Context menu command to generate the grid directly in the Unity Editor.
        /// Keeps prefab linkage for the spawned tiles.
        /// </summary>
        [ContextMenu("Generate Grid")]
        public void EditorGenerateGrid()
        {
            EditorClearGrid();

            if (tilePrefab == null)
            {
                Debug.LogError("[GridManager] Assign a Tile Prefab before generating!");
                return;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // World position: relative to GridManager transform, scaled by spacing
                    Vector3 worldPos = transform.position + new Vector3(x * spacing, 0f, y * spacing);
                    GameObject tileObj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(tilePrefab, transform);
                    tileObj.transform.position = worldPos;
                    tileObj.transform.rotation = Quaternion.identity;
                    tileObj.name = $"Tile_{x}_{y}";

                    Tile tile = tileObj.GetComponent<Tile>();
                    if (tile != null)
                    {
                        tile.Initialize(new Vector2Int(x, y));
                    }
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log($"[GridManager] Generated a {width}x{height} grid in Editor.");
        }

        /// <summary>
        /// Context menu command to clear generated child tiles in the Unity Editor.
        /// </summary>
        [ContextMenu("Clear Grid")]
        public void EditorClearGrid()
        {
            Tile[] childTiles = GetComponentsInChildren<Tile>();
            for (int i = childTiles.Length - 1; i >= 0; i--)
            {
                if (childTiles[i] != null)
                {
                    DestroyImmediate(childTiles[i].gameObject);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("[GridManager] Cleared grid children in Editor.");
        }
        #endif

        public Tile GetTileAt(Vector2Int position)
        {
            if (!IsPositionInBounds(position)) return null;

            if (_grid != null)
            {
                return _grid[position.x, position.y];
            }

            // Fallback for Editor Snapping (since _grid is null before Play Mode)
            Tile[] childTiles = GetComponentsInChildren<Tile>();
            foreach (Tile tile in childTiles)
            {
                if (tile != null && tile.GridPosition == position)
                {
                    return tile;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if the specified coordinate is within the boundaries of the grid.
        /// </summary>
        public bool IsPositionInBounds(Vector2Int position)
        {
            return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
        }

        /// <summary>
        /// Returns all valid adjacent tiles.
        /// </summary>
        /// <param name="position">The center coordinate.</param>
        /// <param name="allowDiagonals">Whether to include diagonal neighbours (8-way vs 4-way).</param>
        public List<Tile> GetNeighbours(Vector2Int position, bool allowDiagonals = true)
        {
            List<Tile> neighbours = new List<Tile>();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue; // Skip center

                    if (!allowDiagonals && dx != 0 && dy != 0) continue; // Skip diagonals if requested

                    Vector2Int checkPos = new Vector2Int(position.x + dx, position.y + dy);
                    if (IsPositionInBounds(checkPos))
                    {
                        Tile neighbor = GetTileAt(checkPos);
                        if (neighbor != null)
                        {
                            neighbours.Add(neighbor);
                        }
                    }
                }
            }

            return neighbours;
        }

        /// <summary>
        /// Calculates the grid distance between two coordinates.
        /// Uses Chebyshev distance for 8-way movement, and Manhattan distance for 4-way movement.
        /// </summary>
        public int GetDistance(Vector2Int start, Vector2Int end, bool allowDiagonals = true)
        {
            int dx = Mathf.Abs(start.x - end.x);
            int dy = Mathf.Abs(start.y - end.y);

            if (allowDiagonals)
            {
                return Mathf.Max(dx, dy); // Chebyshev
            }
            else
            {
                return dx + dy; // Manhattan
            }
        }

        /// <summary>
        /// Retrieves the active creature stack occupying a specific grid coordinate, if any.
        /// </summary>
        public CreatureStack GetCreatureAt(Vector2Int position)
        {
            var turnManager = FindFirstObjectByType<Turns.TurnManager>();
            if (turnManager != null)
            {
                foreach (var stack in turnManager.ActiveStacks)
                {
                    if (stack != null && !stack.IsDead && stack.OccupiesTile(position))
                    {
                        return stack;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if a tile is occupied by an obstacle or another creature stack.
        /// </summary>
        public bool IsTileOccupied(Vector2Int position)
        {
            Tile tile = GetTileAt(position);
            if (tile == null || tile.IsObstacle) return true;

            return GetCreatureAt(position) != null;
        }

        /// <summary>
        /// Checks if a 2x2 area is clear of obstacles and other creature stacks.
        /// </summary>
        public bool IsAreaClearForLarge(Vector2Int basePos, CreatureStack self = null)
        {
            for (int dx = 0; dx <= 1; dx++)
            {
                for (int dy = 0; dy <= 1; dy++)
                {
                    Vector2Int pos = new Vector2Int(basePos.x + dx, basePos.y + dy);
                    Tile tile = GetTileAt(pos);
                    if (tile == null || tile.IsObstacle) return false;

                    CreatureStack occupant = GetCreatureAt(pos);
                    if (occupant != null && occupant != self) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Computes all reachable tiles and their shortest paths for a unit's turn.
        /// Handles flying (ignores obstacles) and ground traversal (BFS flood fill).
        /// </summary>
        public Dictionary<Vector2Int, List<Vector2Int>> GetReachableTiles(Vector2Int start, int range, bool isFlying, bool isLarge = false)
        {
            Dictionary<Vector2Int, List<Vector2Int>> reachable = new Dictionary<Vector2Int, List<Vector2Int>>();
            CreatureStack self = GetCreatureAt(start);

            if (isFlying)
            {
                // Flying logic: any tile within Manhattan distance range that is not blocked at destination
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        if (pos == start) continue;

                        int dist = Mathf.Abs(pos.x - start.x) + Mathf.Abs(pos.y - start.y); // Manhattan range
                        if (dist <= range)
                        {
                            bool clear = false;
                            if (isLarge)
                            {
                                clear = IsAreaClearForLarge(pos, self);
                            }
                            else
                            {
                                Tile tile = GetTileAt(pos);
                                clear = (tile != null && !tile.IsObstacle && GetCreatureAt(pos) == null);
                            }

                            if (clear)
                            {
                                // Direct linear flight path representation
                                List<Vector2Int> path = new List<Vector2Int> { start, pos };
                                reachable[pos] = path;
                            }
                        }
                    }
                }
            }
            else
            {
                // Ground logic: Breadth-First Search (BFS) flood fill
                Queue<Vector2Int> queue = new Queue<Vector2Int>();
                Dictionary<Vector2Int, Vector2Int> parents = new Dictionary<Vector2Int, Vector2Int>();
                Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>();

                queue.Enqueue(start);
                distances[start] = 0;

                while (queue.Count > 0)
                {
                    Vector2Int current = queue.Dequeue();
                    int currentDist = distances[current];

                    if (currentDist >= range) continue;

                    // Ground units can only move in 4 cardinal directions (allowDiagonals = false)
                    List<Tile> neighbours = GetNeighbours(current, allowDiagonals: false);

                    foreach (Tile neighbour in neighbours)
                    {
                        Vector2Int nextPos = neighbour.GridPosition;

                        bool clear = false;
                        if (isLarge)
                        {
                            clear = IsAreaClearForLarge(nextPos, self);
                        }
                        else
                        {
                            clear = !neighbour.IsObstacle && GetCreatureAt(nextPos) == null;
                        }

                        if (!clear) continue;

                        if (!distances.ContainsKey(nextPos))
                        {
                            distances[nextPos] = currentDist + 1;
                            parents[nextPos] = current;
                            queue.Enqueue(nextPos);

                            // Reconstruct path
                            List<Vector2Int> path = new List<Vector2Int>();
                            Vector2Int currInPath = nextPos;
                            while (currInPath != start)
                            {
                                path.Add(currInPath);
                                currInPath = parents[currInPath];
                            }
                            path.Add(start);
                            path.Reverse();

                            reachable[nextPos] = path;
                        }
                    }
                }
            }

            return reachable;
        }
    }
}
