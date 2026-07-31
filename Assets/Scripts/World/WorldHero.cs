using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HommClone.World
{
    /// <summary>
    /// Represents the 3D Hero avatar moving across the World Map grid.
    /// Deducts Movement Points (MP) per tile step and manages world position.
    /// </summary>
    public class WorldHero : MonoBehaviour
    {
        [Header("Hero Attributes")]
        [SerializeField] private string heroName = "Knight";
        [SerializeField] private Sprite heroPortrait;
        [SerializeField] private int playerIndex = 1;
        [SerializeField] private Vector2Int gridPosition = new Vector2Int(2, 2);
        [SerializeField] private float heightOffset = 0.5f;

        private WorldGridManager _gridManager;
        private bool _isMoving = false;

        public string HeroName => heroName;
        public Sprite HeroPortrait => heroPortrait;
        public int PlayerIndex => playerIndex;
        public Vector2Int GridPosition => gridPosition;
        public bool IsMoving => _isMoving;

        public HeroData Data
        {
            get
            {
                var manager = GameDataManager.GetOrCreateInstance();
                if (manager != null)
                {
                    var data = playerIndex == 1 ? manager.player1Hero : manager.player2Hero;
                    if (data != null && heroPortrait != null) data.heroPortrait = heroPortrait;
                    return data;
                }
                return null;
            }
        }

        private void Start()
        {
            _gridManager = FindFirstObjectByType<WorldGridManager>();
            if (Data != null)
            {
                gridPosition = Data.worldPosition;
                if (heroPortrait != null) Data.heroPortrait = heroPortrait;
            }

            SnapToGridPosition(gridPosition);
            CreateHeroVisuals();
        }

        public void SetPlayerIndexAndPosition(int pIndex, Vector2Int pos)
        {
            playerIndex = pIndex;
            gridPosition = pos;
            if (Data != null)
            {
                Data.worldPosition = pos;
            }
            SnapToGridPosition(pos);
            CreateHeroVisuals();
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

        private void CreateHeroVisuals()
        {
            // Clear old visual children if any
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

            // 1. If HeroData has a custom 3D Prefab, instantiate it!
            if (Data != null && Data.heroPrefab != null)
            {
                GameObject prefabInstance = Instantiate(Data.heroPrefab, transform);
                prefabInstance.name = "HeroPrefabModel";
                prefabInstance.transform.localPosition = Vector3.zero;
                prefabInstance.transform.localRotation = Quaternion.identity;
                return;
            }

            // 2. Fallback: Low-Poly Hero Avatar model
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "HeroDynamicModel";
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            body.transform.localPosition = new Vector3(0f, 0.3f, 0f);

            var primCollider = body.GetComponent<Collider>();
            if (primCollider != null)
            {
                if (Application.isPlaying) Destroy(primCollider);
                else DestroyImmediate(primCollider);
            }

            Renderer r = body.GetComponent<Renderer>();
            if (r != null)
            {
                Color heroColor = playerIndex == 1 ? new Color(0.2f, 0.5f, 0.95f) : new Color(0.95f, 0.2f, 0.2f);
                MaterialUtils.SetRendererColor(r, heroColor);
            }
        }

        public IEnumerator MoveAlongPathCoroutine(List<Vector2Int> path, System.Action onComplete)
        {
            if (_gridManager == null) _gridManager = FindFirstObjectByType<WorldGridManager>();

            if (_isMoving || path == null || path.Count <= 1 || _gridManager == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            _isMoving = true;
            HeroData data = Data;

            if (HommClone.Audio.AudioManager.Instance != null)
            {
                HommClone.Audio.AudioManager.Instance.PlayMoveSound();
            }

            int startIndex = (path[0] == gridPosition) ? 1 : 0;

            for (int i = startIndex; i < path.Count; i++)
            {
                Vector2Int node = path[i];
                WorldTile tile = _gridManager.GetTileAt(node);
                if (tile == null || !tile.IsPassable) break;

                float currentMP = data != null ? data.currentMovementPoints : 0f;
                float mpCost = tile.MovementCost;

                if (currentMP < mpCost)
                {
                    Debug.Log($"[WorldHero] Stopped movement at tile {node}! Needed MP: {mpCost:F1}, Remaining MP: {currentMP:F1}");
                    break; // Strictly stop when Movement Points run out!
                }

                // Deduct Movement Points
                if (data != null)
                {
                    data.currentMovementPoints = Mathf.Max(0f, data.currentMovementPoints - mpCost);
                }

                Vector3 targetPos = _gridManager.GridToWorldPosition(node);
                targetPos.y += heightOffset;
                Vector3 startPos = transform.position;

                float elapsed = 0f;
                float duration = 0.25f; // Step travel speed

                Vector3 dir = (targetPos - startPos).normalized;
                dir.y = 0;
                if (dir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(dir);
                }

                while (elapsed < duration)
                {
                    transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                transform.position = targetPos;
                gridPosition = node;
                if (data != null) data.worldPosition = node;

                var mapManager = FindFirstObjectByType<WorldMapManager>();
                if (mapManager != null && mapManager.CheckTileEncounter(node))
                {
                    Debug.Log($"[WorldHero] Battle encounter triggered at step {node}! Stopping hero movement immediately.");
                    if (HommClone.Audio.AudioManager.Instance != null)
                    {
                        HommClone.Audio.AudioManager.Instance.StopMoveSound();
                    }
                    _isMoving = false;
                    yield break;
                }
            }

            if (HommClone.Audio.AudioManager.Instance != null)
            {
                HommClone.Audio.AudioManager.Instance.StopMoveSound();
            }

            _isMoving = false;
            onComplete?.Invoke();
        }
    }
}
