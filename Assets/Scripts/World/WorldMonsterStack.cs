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

            // Clear old visual children
            List<GameObject> childrenToDestroy = new List<GameObject>();
            foreach (Transform child in transform)
            {
                childrenToDestroy.Add(child.gameObject);
            }
            foreach (var child in childrenToDestroy)
            {
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            // 1. If custom 3D Prefab exists on creatureData, instantiate it!
            if (creatureData != null && creatureData.Prefab != null)
            {
                GameObject prefabInstance = Instantiate(creatureData.Prefab, transform);
                prefabInstance.name = "MonsterPrefabModel";
                prefabInstance.transform.localPosition = Vector3.zero;
                prefabInstance.transform.localRotation = Quaternion.identity;
                return;
            }

            // 2. Fallback: Dynamic 3D model shaped by Tier & Faction Color
            PrimitiveType primitive = PrimitiveType.Capsule;
            Vector3 modelScale = new Vector3(0.7f, 0.7f, 0.7f);
            Color factionColor = new Color(0.9f, 0.15f, 0.15f, 1f); // Default Neutral Crimson

            if (creatureData != null)
            {
                switch (creatureData.Tier)
                {
                    case 1: primitive = PrimitiveType.Cube; modelScale = new Vector3(0.6f, 0.6f, 0.6f); break;
                    case 2: primitive = PrimitiveType.Cylinder; modelScale = new Vector3(0.6f, 0.5f, 0.6f); break;
                    case 3: primitive = PrimitiveType.Capsule; modelScale = new Vector3(0.6f, 0.6f, 0.6f); break;
                    case 4: primitive = PrimitiveType.Sphere; modelScale = new Vector3(0.7f, 0.7f, 0.7f); break;
                    case 5: primitive = PrimitiveType.Cube; modelScale = new Vector3(0.8f, 0.8f, 0.8f); break;
                    case 6: primitive = PrimitiveType.Cylinder; modelScale = new Vector3(0.8f, 0.7f, 0.8f); break;
                    case 7:
                    default: primitive = PrimitiveType.Capsule; modelScale = new Vector3(1.0f, 0.9f, 1.0f); break;
                }

                switch (creatureData.FactionType)
                {
                    case Faction.Haven: factionColor = new Color(0.95f, 0.8f, 0.2f); break;
                    case Faction.Inferno: factionColor = new Color(0.95f, 0.2f, 0.1f); break;
                    case Faction.Necropolis: factionColor = new Color(0.5f, 0.15f, 0.7f); break;
                    case Faction.Academy: factionColor = new Color(0.2f, 0.7f, 0.95f); break;
                    case Faction.Dungeon: factionColor = new Color(0.2f, 0.2f, 0.25f); break;
                    case Faction.Sylvan: factionColor = new Color(0.2f, 0.85f, 0.3f); break;
                    case Faction.Fortress: factionColor = new Color(0.85f, 0.45f, 0.15f); break;
                }
            }

            GameObject body = GameObject.CreatePrimitive(primitive);
            body.name = "MonsterDynamicVisual";
            body.transform.SetParent(transform, false);
            body.transform.localScale = modelScale;
            body.transform.localPosition = new Vector3(0f, 0.4f, 0f);

            var primCollider = body.GetComponent<Collider>();
            if (primCollider != null)
            {
                if (Application.isPlaying) Destroy(primCollider);
                else DestroyImmediate(primCollider);
            }

            Renderer r = body.GetComponent<Renderer>();
            if (r != null)
            {
                MaterialUtils.SetRendererColor(r, factionColor);
            }
        }
    }
}
