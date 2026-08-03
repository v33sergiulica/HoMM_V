using UnityEngine;

namespace HommClone.World
{
    /// <summary>
    /// Represents a one-time pickable resource pile on the World Map.
    /// Collected when the hero steps on or interacts with the tile.
    /// </summary>
    public class WorldResourcePickable : MonoBehaviour
    {
        [Header("Resource Configuration")]
        [SerializeField] private ResourceType resourceType = ResourceType.Gold;
        [SerializeField] private int amount = 500;
        [SerializeField] private Vector2Int gridPosition;

        public ResourceType ResourceType => resourceType;
        public int Amount => amount;
        public Vector2Int GridPosition => gridPosition;

        private void Start()
        {
            var grid = FindFirstObjectByType<WorldGridManager>();
            if (grid != null)
            {
                transform.position = grid.GetTileWorldPosition(gridPosition);
            }
            SetupResourceVisuals();
        }

        public void Initialize(ResourceType type, int qty, Vector2Int pos)
        {
            resourceType = type;
            amount = qty;
            gridPosition = pos;

            var grid = FindFirstObjectByType<WorldGridManager>();
            if (grid != null)
            {
                transform.position = grid.GetTileWorldPosition(gridPosition);
            }

            SetupResourceVisuals();
        }

        public void Collect(int playerIndex)
        {
            var gameData = GameDataManager.GetOrCreateInstance();
            if (gameData != null)
            {
                var targetRes = (playerIndex == 1) ? gameData.player1Resources : gameData.player2Resources;
                if (targetRes != null)
                {
                    switch (resourceType)
                    {
                        case ResourceType.Gold: targetRes.gold += amount; break;
                        case ResourceType.Wood: targetRes.wood += amount; break;
                        case ResourceType.Ore: targetRes.ore += amount; break;
                        case ResourceType.Gems: targetRes.gems += amount; break;
                    }
                }

                Debug.Log($"[WorldResource] Player {playerIndex} collected +{amount} {resourceType}!");

                Color resColor = (resourceType == ResourceType.Gold) ? new Color(1f, 0.85f, 0.3f) :
                                 (resourceType == ResourceType.Wood) ? new Color(0.85f, 0.65f, 0.4f) :
                                 (resourceType == ResourceType.Ore) ? new Color(0.7f, 0.75f, 0.85f) : new Color(0.4f, 0.9f, 0.95f);

                UI.WorldNotificationUI.ShowNotification(
                    "RESOURCE COLLECTED",
                    $"Player {playerIndex} collected <b>+{amount:N0} {resourceType}</b>!",
                    accentColor: resColor
                );
            }

            var ui = FindFirstObjectByType<UI.ResourceBarUI>();
            if (ui != null) ui.UpdateUI();

            // Destroy pickup object upon collection
            Destroy(gameObject);
        }

        private void SetupResourceVisuals()
        {
            var existingRenderers = GetComponentsInChildren<Renderer>();
            if (existingRenderers == null || existingRenderers.Length == 0)
            {
                CreateFallbackVisual();
            }
        }

        private void CreateFallbackVisual()
        {
            GameObject pile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pile.name = "ResourcePileVisual";
            pile.transform.SetParent(transform, false);
            pile.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            pile.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            Color rColor = Color.yellow;
            switch (resourceType)
            {
                case ResourceType.Gold: rColor = new Color(1f, 0.85f, 0.1f); break;
                case ResourceType.Wood: rColor = new Color(0.6f, 0.35f, 0.1f); break;
                case ResourceType.Ore: rColor = new Color(0.5f, 0.5f, 0.55f); break;
                case ResourceType.Gems: rColor = new Color(0.2f, 0.9f, 0.9f); break;
            }

            var ren = pile.GetComponent<Renderer>();
            if (ren != null) MaterialUtils.SetRendererColor(ren, rColor);
        }
    }
}
