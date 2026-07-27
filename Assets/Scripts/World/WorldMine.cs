using UnityEngine;
using HommClone.Grid;

namespace HommClone.World
{
    public enum ResourceType
    {
        Gold,
        Wood,
        Ore,
        Gems
    }

    /// <summary>
    /// Represents a conquerable resource mine (factory) on the World Map.
    /// Generates resources every Day Skip for its owner player.
    /// </summary>
    public class WorldMine : MonoBehaviour
    {
        [Header("Mine Configuration")]
        [SerializeField] private ResourceType mineType = ResourceType.Gold;
        [SerializeField] private int dailyIncome = 1000;
        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private int ownerPlayerIndex = 0; // 0 = Unclaimed Neutral, 1 = Player 1, 2 = Player 2

        [Header("Visuals")]
        [SerializeField] private Renderer flagRenderer;

        public ResourceType MineType => mineType;
        public int DailyIncome => dailyIncome;
        public Vector2Int GridPosition => gridPosition;
        public int OwnerPlayerIndex => ownerPlayerIndex;

        private void Start()
        {
            var grid = FindFirstObjectByType<WorldGridManager>();
            if (grid != null)
            {
                transform.position = grid.GetTileWorldPosition(gridPosition);
            }
            SetupMineVisuals();
        }

        public void Initialize(ResourceType type, int income, Vector2Int pos, int owner = 0)
        {
            mineType = type;
            dailyIncome = income;
            gridPosition = pos;
            ownerPlayerIndex = owner;

            // Align to 3D world grid position
            var grid = FindFirstObjectByType<WorldGridManager>();
            if (grid != null)
            {
                transform.position = grid.GetTileWorldPosition(gridPosition);
            }

            SetupMineVisuals();
        }

        public void ClaimMine(int playerIndex)
        {
            ownerPlayerIndex = playerIndex;
            UpdateFlagColor();

            Debug.Log($"[WorldMine] Mine at {gridPosition} claimed by Player {playerIndex}! Daily Income: +{dailyIncome} {mineType}");

            var ui = FindFirstObjectByType<UI.ResourceBarUI>();
            if (ui != null) ui.UpdateUI();
        }

        public void UpdateFlagColor()
        {
            if (flagRenderer == null)
            {
                var ren = GetComponentInChildren<Renderer>();
                if (ren != null) flagRenderer = ren;
            }

            if (flagRenderer != null)
            {
                Color c = Color.gray; // Unclaimed neutral
                if (ownerPlayerIndex == 1) c = new Color(0.1f, 0.5f, 0.95f); // Player 1 Blue
                else if (ownerPlayerIndex == 2) c = new Color(0.95f, 0.2f, 0.2f); // Player 2 Red

                MaterialUtils.SetRendererColor(flagRenderer, c);
            }
        }

        private void SetupMineVisuals()
        {
            var existingRenderers = GetComponentsInChildren<Renderer>();
            if (existingRenderers == null || existingRenderers.Length == 0)
            {
                CreateFallbackMineModel();
            }
            else
            {
                UpdateFlagColor();
            }
        }

        private void CreateFallbackMineModel()
        {
            // Base Building Box
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = "MineBuilding";
            building.transform.SetParent(transform, false);
            building.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            building.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            Color bColor = Color.gray;
            switch (mineType)
            {
                case ResourceType.Gold: bColor = new Color(0.9f, 0.75f, 0.2f); break; // Gold Yellow
                case ResourceType.Wood: bColor = new Color(0.55f, 0.35f, 0.15f); break; // Brown Wood
                case ResourceType.Ore: bColor = new Color(0.45f, 0.45f, 0.5f); break; // Steel Grey
                case ResourceType.Gems: bColor = new Color(0.2f, 0.8f, 0.75f); break; // Cyan Crystal
            }

            var bRen = building.GetComponent<Renderer>();
            if (bRen != null) MaterialUtils.SetRendererColor(bRen, bColor);

            // Flag Pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "FlagPole";
            pole.transform.SetParent(transform, false);
            pole.transform.localPosition = new Vector3(0.35f, 0.9f, 0.35f);
            pole.transform.localScale = new Vector3(0.08f, 0.6f, 0.08f);

            // Flag Top
            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "FlagTop";
            flag.transform.SetParent(pole.transform, false);
            flag.transform.localPosition = new Vector3(0f, 0.6f, 0.3f);
            flag.transform.localScale = new Vector3(0.1f, 0.4f, 0.6f);

            flagRenderer = flag.GetComponent<Renderer>();
            UpdateFlagColor();
        }
    }
}
