using UnityEngine;
using HommClone.World;

namespace HommClone.World
{
    public class WorldTreasureChest : MonoBehaviour
    {
        [Header("Treasure Chest Position & Rewards")]
        [SerializeField] private Vector2Int gridPosition = new Vector2Int(0, 0);
        [SerializeField] private int goldAmount = 2000;
        [SerializeField] private int xpAmount = 1000;

        public Vector2Int GridPosition
        {
            get => gridPosition;
            set => gridPosition = value;
        }

        public int GoldAmount => goldAmount;
        public int XpAmount => xpAmount;

        private void Awake()
        {
            EnsureCollider();
        }

        private void EnsureCollider()
        {
            if (GetComponent<Collider>() == null)
            {
                var col = gameObject.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.5f, 0f);
                col.size = new Vector3(1f, 1f, 1f);
            }
        }

        private void Start()
        {
            EnsureCollider();
            // Auto-align 3D world position to grid coordinate if grid manager exists
            var grid = FindFirstObjectByType<WorldGridManager>();
            if (grid != null)
            {
                transform.position = grid.GridToWorldPosition(gridPosition);
            }
        }

        /// <summary>
        /// Triggered when hero steps onto the treasure chest tile.
        /// Opens the Treasure Chest Choice UI window.
        /// </summary>
        public void Interact(HeroData hero)
        {
            var chestUI = UI.TreasureChestUI.Instance;
            if (chestUI == null)
            {
                GameObject obj = new GameObject("TreasureChestUI");
                chestUI = obj.AddComponent<UI.TreasureChestUI>();
            }

            chestUI.ShowChestChoice(hero, goldAmount, xpAmount, onChoiceMade: () =>
            {
                Debug.Log($"[WorldTreasureChest] Chest at {gridPosition} collected and opened!");
                Destroy(gameObject);
            });
        }
    }
}
