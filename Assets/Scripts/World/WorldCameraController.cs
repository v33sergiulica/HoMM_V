using UnityEngine;
using UnityEngine.InputSystem;

namespace HommClone.World
{
    /// <summary>
    /// Smooth World Map camera controller with WASD panning, mouse edge-pan, scroll zoom, and Hero follow.
    /// Uses Unity's New Input System package.
    /// </summary>
    public class WorldCameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private float panSpeed = 15f;
        [SerializeField] private float zoomSpeed = 8f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 25f;
        [SerializeField] private Vector3 defaultOffset = new Vector3(0f, 14f, -10f);

        [Header("Target Follow")]
        [SerializeField] private Transform targetHero;
        [SerializeField] private bool followHero = true;

        private Camera _cam;

        private void Start()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;

            if (targetHero == null)
            {
                var hero = FindFirstObjectByType<WorldHero>();
                if (hero != null) targetHero = hero.transform;
            }

            if (targetHero != null)
            {
                transform.position = targetHero.position + defaultOffset;
                transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            }
        }

        public void SetTargetHero(Transform heroTransform)
        {
            targetHero = heroTransform;
            followHero = true;
            if (targetHero != null)
            {
                transform.position = targetHero.position + defaultOffset;
            }
        }

        private void LateUpdate()
        {
            HandleInputs();

            if (followHero && targetHero != null)
            {
                Vector3 targetPos = targetHero.position + defaultOffset;
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
            }
        }

        private void HandleInputs()
        {
            float moveX = 0f;
            float moveZ = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveZ += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveZ -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX += 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX -= 1f;

                if (Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    followHero = true;
                }
            }

            if (moveX != 0 || moveZ != 0)
            {
                followHero = false; // Disable auto follow when player manual pans
                Vector3 moveDir = new Vector3(moveX, 0f, moveZ).normalized;
                transform.position += moveDir * panSpeed * Time.deltaTime;
            }

            // Scroll Zoom
            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    Vector3 pos = transform.position;
                    pos.y -= Mathf.Sign(scroll) * zoomSpeed * 0.8f;
                    pos.y = Mathf.Clamp(pos.y, minZoom, maxZoom);
                    transform.position = pos;
                }
            }
        }
    }
}
