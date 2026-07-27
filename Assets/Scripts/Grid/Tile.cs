using UnityEngine;
using TMPro;

namespace HommClone.Grid
{
    /// <summary>
    /// Represents an individual square tile on the battlefield.
    /// Manages grid coordinates and visual styling (coloring, debug labels).
    /// </summary>
    public class Tile : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private Vector2Int gridPosition;
        
        /// <summary>
        /// The 2D grid coordinate where X maps to world X and Y maps to world Z.
        /// </summary>
        public Vector2Int GridPosition => gridPosition;

        [Header("Tile State")]
        [SerializeField] private bool isObstacle = false;
        public bool IsObstacle => isObstacle;

        [Header("Visual Components")]
        [SerializeField] private TextMeshPro coordinateText;
        [SerializeField] private Renderer tileRenderer;
        [SerializeField] private Color coordinateColor = new Color(0.7f, 0.7f, 0.7f, 0.5f); // Semi-transparent faded gray by default

        private Color _baseColor = Color.white;

        private void OnValidate()
        {
            UpdateCoordinateText();
        }

        /// <summary>
        /// Initializer for the tile's grid coordinate.
        /// </summary>
        public void Initialize(Vector2Int position)
        {
            gridPosition = position;
            UpdateCoordinateText();
            
            // Cache base color from the renderer if available (using sharedMaterial to avoid memory leaks in Edit Mode)
            if (tileRenderer != null)
            {
                if (tileRenderer.sharedMaterial != null)
                {
                    _baseColor = tileRenderer.sharedMaterial.color;
                }
            }
            else
            {
                var r = GetComponentInChildren<Renderer>();
                if (r != null)
                {
                    tileRenderer = r;
                    if (r.sharedMaterial != null)
                    {
                        _baseColor = r.sharedMaterial.color;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the TextMeshPro debugging text to show (X, Y) coordinates.
        /// </summary>
        public void UpdateCoordinateText()
        {
            if (coordinateText != null)
            {
                coordinateText.text = $"({gridPosition.x}, {gridPosition.y})";
                coordinateText.color = coordinateColor;
            }
        }

        /// <summary>
        /// Sets the color of the tile's renderer.
        /// </summary>
        public void SetColor(Color color)
        {
            if (tileRenderer != null)
            {
                tileRenderer.material.color = color;
            }
        }

        /// <summary>
        /// Resets the tile's color back to its original base color.
        /// </summary>
        public void ResetColor()
        {
            if (tileRenderer != null)
            {
                tileRenderer.material.color = _baseColor;
            }
        }
    }
}
