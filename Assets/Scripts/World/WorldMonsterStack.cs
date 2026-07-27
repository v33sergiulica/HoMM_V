using System.Collections.Generic;
using UnityEngine;
using HommClone.Creatures;

namespace HommClone.World
{
    /// <summary>
    /// Represents a stack of wild/neutral monsters standing on a World Map tile.
    /// Triggers a battle encounter when the WorldHero approaches or collides with it.
    /// </summary>
    public class WorldMonsterStack : MonoBehaviour
    {
        [Header("Monster Stack Data")]
        [SerializeField] private CreatureData creatureData;
        [SerializeField] private int count = 12;
        [SerializeField] private Vector2Int gridPosition = new Vector2Int(10, 10);

        [Header("Visuals")]
        [SerializeField] private float heightOffset = 0.4f;

        private WorldGridManager _gridManager;

        public CreatureData CreatureData => creatureData;
        public int Count => count;
        public Vector2Int GridPosition => gridPosition;

        private void Start()
        {
            _gridManager = FindFirstObjectByType<WorldGridManager>();
            SnapToGridPosition(gridPosition);
            CreateMonsterVisuals();
        }

        public void Initialize(CreatureData data, int count, Vector2Int pos)
        {
            this.creatureData = data;
            this.count = count;
            this.gridPosition = pos;

            SnapToGridPosition(pos);
            CreateMonsterVisuals();
        }

        public void SnapToGridPosition(Vector2Int pos)
        {
            gridPosition = pos;
            if (_gridManager == null) _gridManager = FindFirstObjectByType<WorldGridManager>();
            if (_gridManager != null)
            {
                Vector3 wPos = _gridManager.GridToWorldPosition(pos);
                wPos.y += heightOffset;
                transform.position = wPos;
            }
        }

        [ContextMenu("Generate Monster Stack Visuals")]
        public void CreateMonsterVisuals()
        {
            // Ensure BoxCollider exists for interaction
            if (GetComponent<Collider>() == null && GetComponentInChildren<Collider>() == null)
            {
                BoxCollider col = gameObject.AddComponent<BoxCollider>();
                col.size = new Vector3(1f, 1.5f, 1f);
                col.center = new Vector3(0f, 0.75f, 0f);
            }

            if (transform.childCount == 0)
            {
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "MonsterVisual";
                body.transform.SetParent(transform, false);
                body.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                body.transform.localPosition = new Vector3(0f, 0.5f, 0f);

                Renderer r = body.GetComponent<Renderer>();
                if (r != null)
                {
                    Color monsterColor = new Color(0.9f, 0.15f, 0.15f, 1f); // Crimson enemy red
                    MaterialUtils.SetRendererColor(r, monsterColor);
                }
            }
        }
    }
}
