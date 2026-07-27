using UnityEngine;

namespace HommClone.World
{
    public enum WorldTileType
    {
        Grass,
        Dirt,
        Road,
        Water,
        Mountain
    }

    public enum WorldObjectType
    {
        None,
        Hero,
        Castle,
        ResourcePile,
        Mine,
        EnemyStack
    }

    /// <summary>
    /// Represents a single tile node on the World Map grid.
    /// Manages terrain type, movement cost, path highlights, and world objects.
    /// </summary>
    public class WorldTile : MonoBehaviour
    {
        [Header("Tile State")]
        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private WorldTileType tileType = WorldTileType.Grass;
        [SerializeField] private WorldObjectType objectType = WorldObjectType.None;

        private Renderer _tileRenderer;
        private MaterialPropertyBlock _propBlock;
        private Color _baseColor;

        public Vector2Int GridPosition => gridPosition;
        public WorldTileType TileType => tileType;
        public WorldObjectType ObjectType { get => objectType; set => objectType = value; }

        public bool IsPassable => tileType != WorldTileType.Water && tileType != WorldTileType.Mountain;

        /// <summary>
        /// Movement Point cost per tile traversal.
        /// </summary>
        public float MovementCost
        {
            get
            {
                switch (tileType)
                {
                    case WorldTileType.Road: return 0.75f;
                    case WorldTileType.Grass: return 1.0f;
                    case WorldTileType.Dirt: return 1.25f;
                    case WorldTileType.Water: return 999f;
                    case WorldTileType.Mountain: return 999f;
                    default: return 1.0f;
                }
            }
        }

        public void Initialize(Vector2Int pos, WorldTileType type)
        {
            gridPosition = pos;
            tileType = type;

            EnsureComponents();
            ApplyTerrainStyling();
        }

        private void EnsureComponents()
        {
            if (_tileRenderer == null) _tileRenderer = GetComponent<Renderer>();
            if (_tileRenderer == null) _tileRenderer = GetComponentInChildren<Renderer>();

            // Ensure collider exists for physics raycasting
            if (GetComponent<Collider>() == null && GetComponentInChildren<Collider>() == null)
            {
                gameObject.AddComponent<BoxCollider>();
            }
        }

        public void ApplyTerrainStyling()
        {
            EnsureComponents();

            switch (tileType)
            {
                case WorldTileType.Grass:
                    _baseColor = new Color(0.28f, 0.58f, 0.22f, 1f);
                    break;
                case WorldTileType.Dirt:
                    _baseColor = new Color(0.55f, 0.4f, 0.25f, 1f);
                    break;
                case WorldTileType.Road:
                    _baseColor = new Color(0.7f, 0.65f, 0.48f, 1f);
                    break;
                case WorldTileType.Water:
                    _baseColor = new Color(0.15f, 0.4f, 0.75f, 1f);
                    break;
                case WorldTileType.Mountain:
                    _baseColor = new Color(0.45f, 0.45f, 0.48f, 1f);
                    break;
            }

            SetTileColor(_baseColor);
        }

        public void HighlightAsPath(bool inRange)
        {
            if (!IsPassable) return;
            Color pathColor = inRange ? new Color(0.2f, 0.9f, 0.2f, 1f) : new Color(0.9f, 0.2f, 0.2f, 1f);
            SetTileColor(pathColor);
        }

        public void HighlightHover(bool isValid)
        {
            if (!IsPassable) return;
            Color hoverColor = isValid ? new Color(1f, 0.9f, 0.2f, 1f) : new Color(0.9f, 0.2f, 0.2f, 1f);
            SetTileColor(hoverColor);
        }

        public void ResetHighlight()
        {
            SetTileColor(_baseColor);
        }

        private void SetTileColor(Color color)
        {
            EnsureComponents();
            if (_tileRenderer != null)
            {
                Material mat = Application.isPlaying ? _tileRenderer.material : _tileRenderer.sharedMaterial;
                if (mat != null)
                {
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", color);
                    }
                    else if (mat.HasProperty("_Color"))
                    {
                        mat.SetColor("_Color", color);
                    }
                    else
                    {
                        mat.color = color;
                    }
                }
            }
        }
    }
}
