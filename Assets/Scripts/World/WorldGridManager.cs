using System.Collections.Generic;
using UnityEngine;

namespace HommClone.World
{
    /// <summary>
    /// Manages grid generation, spatial tile lookup, and A* pathfinding
    /// for World Map exploration and Movement Point navigation.
    /// </summary>
    public class WorldGridManager : MonoBehaviour
    {
        [Header("World Grid Configuration")]
        [SerializeField] private int width = 25;
        [SerializeField] private int height = 25;
        [SerializeField] private float tileSize = 1.2f;

        [Header("Tile Prefab / Base Mesh")]
        [SerializeField] private GameObject tilePrefab;

        private Dictionary<Vector2Int, WorldTile> _grid = new Dictionary<Vector2Int, WorldTile>();

        public int Width => width;
        public int Height => height;
        public float TileSize => tileSize;

        private void Awake()
        {
            InitializeGrid();
        }

        public void InitializeGrid()
        {
            _grid.Clear();
            WorldTile[] childTiles = GetComponentsInChildren<WorldTile>();

            if (childTiles.Length > 0)
            {
                foreach (WorldTile tile in childTiles)
                {
                    Vector2Int pos = tile.GridPosition;
                    _grid[pos] = tile;
                    tile.Initialize(pos, tile.TileType);
                }
                Debug.Log($"[WorldGridManager] Cached {childTiles.Length} existing World Tiles from scene.");
            }
            else
            {
                GenerateWorldGrid();
            }
        }

        [ContextMenu("Generate World Map Grid")]
        public void GenerateWorldGrid()
        {
            ClearWorldGrid();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    Vector3 worldPos = GridToWorldPosition(pos);

                    GameObject tileObj;
                    if (tilePrefab != null)
                    {
                        tileObj = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                    }
                    else
                    {
                        tileObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        tileObj.transform.position = worldPos;
                        tileObj.transform.localScale = new Vector3(tileSize * 0.95f, 0.2f, tileSize * 0.95f);
                        tileObj.transform.SetParent(transform);

                        // Use URP or Standard shader fallback to prevent magenta error material
                        Renderer r = tileObj.GetComponent<Renderer>();
                        if (r != null)
                        {
                            Shader defaultShader = Shader.Find("Universal Render Pipeline/Lit");
                            if (defaultShader == null) defaultShader = Shader.Find("Universal Render Pipeline/Simple Lit");
                            if (defaultShader == null) defaultShader = Shader.Find("Standard");
                            if (defaultShader == null) defaultShader = Shader.Find("Unlit/Color");
                            if (defaultShader == null) defaultShader = Shader.Find("Sprites/Default");

                            if (defaultShader != null)
                            {
                                r.material = new Material(defaultShader);
                            }
                        }
                    }

                    tileObj.name = $"WorldTile_{x}_{y}";
                    WorldTile wTile = tileObj.GetComponent<WorldTile>();
                    if (wTile == null) wTile = tileObj.AddComponent<WorldTile>();

                    WorldTileType terrainType = DetermineTerrainType(x, y);
                    wTile.Initialize(pos, terrainType);

                    _grid[pos] = wTile;
                }
            }

            Debug.Log($"[WorldGridManager] Generated World Map Grid ({width}x{height}) with {width * height} tiles.");
        }

        [ContextMenu("Clear World Map Grid")]
        public void ClearWorldGrid()
        {
            _grid.Clear();
            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in transform)
            {
                children.Add(child.gameObject);
            }

            foreach (GameObject child in children)
            {
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        private WorldTileType DetermineTerrainType(int x, int y)
        {
            // Map boundaries as mountains/water
            if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
            {
                return (x + y) % 2 == 0 ? WorldTileType.Mountain : WorldTileType.Water;
            }

            // Perlin noise terrain distribution
            float noise = Mathf.PerlinNoise(x * 0.15f + 1.2f, y * 0.15f + 3.4f);
            if (noise < 0.25f) return WorldTileType.Water;
            if (noise > 0.72f) return WorldTileType.Mountain;

            // Road sample strip
            if (y == 5 && x > 2 && x < width - 3) return WorldTileType.Road;

            return (noise > 0.5f) ? WorldTileType.Dirt : WorldTileType.Grass;
        }

        public Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x * tileSize, 0f, gridPos.y * tileSize);
        }

        public Vector3 GetTileWorldPosition(Vector2Int gridPos) => GridToWorldPosition(gridPos);

        public WorldTile GetTileAt(Vector2Int gridPos)
        {
            if (_grid.TryGetValue(gridPos, out WorldTile tile))
            {
                return tile;
            }
            return null;
        }

        #region A* Pathfinding for Movement Points
        public class PathNode
        {
            public Vector2Int pos;
            public float gCost;
            public float hCost;
            public PathNode parent;
            public float FCost => gCost + hCost;

            public PathNode(Vector2Int pos, float gCost, float hCost, PathNode parent)
            {
                this.pos = pos;
                this.gCost = gCost;
                this.hCost = hCost;
                this.parent = parent;
            }
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int target)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            WorldTile targetTile = GetTileAt(target);
            if (targetTile == null || !targetTile.IsPassable) return path;

            Dictionary<Vector2Int, PathNode> openSet = new Dictionary<Vector2Int, PathNode>();
            Dictionary<Vector2Int, PathNode> closedSet = new Dictionary<Vector2Int, PathNode>();

            PathNode startNode = new PathNode(start, 0f, Vector2Int.Distance(start, target), null);
            openSet[start] = startNode;

            while (openSet.Count > 0)
            {
                PathNode current = null;
                foreach (var node in openSet.Values)
                {
                    if (current == null || node.FCost < current.FCost || (node.FCost == current.FCost && node.hCost < current.hCost))
                    {
                        current = node;
                    }
                }

                if (current.pos == target)
                {
                    // Reconstruct path
                    PathNode curr = current;
                    while (curr != null)
                    {
                        path.Add(curr.pos);
                        curr = curr.parent;
                    }
                    path.Reverse();
                    return path;
                }

                openSet.Remove(current.pos);
                closedSet[current.pos] = current;

                foreach (var neighborPos in GetNeighbors(current.pos))
                {
                    WorldTile nTile = GetTileAt(neighborPos);
                    if (nTile == null || !nTile.IsPassable || closedSet.ContainsKey(neighborPos)) continue;

                    float stepCost = nTile.MovementCost;
                    float newGCost = current.gCost + stepCost;

                    if (!openSet.TryGetValue(neighborPos, out PathNode neighborNode) || newGCost < neighborNode.gCost)
                    {
                        if (neighborNode == null)
                        {
                            neighborNode = new PathNode(neighborPos, newGCost, Vector2Int.Distance(neighborPos, target), current);
                            openSet[neighborPos] = neighborNode;
                        }
                        else
                        {
                            neighborNode.gCost = newGCost;
                            neighborNode.parent = current;
                        }
                    }
                }
            }

            return path;
        }

        public List<Vector2Int> GetNeighbors(Vector2Int pos)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();
            Vector2Int[] dirs = new Vector2Int[]
            {
                new Vector2Int(0, 1), new Vector2Int(1, 0),
                new Vector2Int(0, -1), new Vector2Int(-1, 0)
            };

            foreach (var d in dirs)
            {
                Vector2Int nPos = pos + d;
                if (nPos.x >= 0 && nPos.x < width && nPos.y >= 0 && nPos.y < height)
                {
                    neighbors.Add(nPos);
                }
            }
            return neighbors;
        }

        public float CalculatePathMovementCost(List<Vector2Int> path)
        {
            if (path == null || path.Count <= 1) return 0f;
            float totalCost = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                WorldTile tile = GetTileAt(path[i]);
                if (tile != null) totalCost += tile.MovementCost;
            }
            return totalCost;
        }
        #endregion
    }
}
