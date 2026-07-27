using UnityEngine;
using UnityEngine.InputSystem;

namespace HommClone.CameraControl
{
    /// <summary>
    /// Manages battle camera controls including panning (WASD), orbiting (Right-Click + Drag), and zooming (Scroll Wheel).
    /// </summary>
    public class BattleCameraController : MonoBehaviour
    {
        [Header("Movement Speeds")]
        [SerializeField] private float panSpeed = 10f;
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float rotateSpeed = 15f;

        [Header("Camera Constraints")]
        [SerializeField] private float minZoom = 3f;
        [SerializeField] private float maxZoom = 25f;
        [SerializeField] private float minPitch = 15f;
        [SerializeField] private float maxPitch = 80f;

        // Target camera state
        private Vector3 _positionTarget;
        private float _yawTarget = 0f;
        private float _pitchTarget = 45f;
        private float _distanceTarget = 12f;

        // Current interpolated camera state
        private Vector3 _currentPosition;
        private float _currentYaw = 0f;
        private float _currentPitch = 45f;
        private float _currentDistance = 12f;

        // Saved original camera state for restoring after cinematic
        private Vector3 _originalTargetPos;
        private float _originalYaw;
        private float _originalPitch;
        private float _originalDistance;
        private bool _isCinematicMode = false;

        private void Start()
        {
            // Set initial camera focus point near the center of the 10x12 grid
            _positionTarget = new Vector3(5f, 0f, 6f);

            // Configure camera background clear flags to solid color
            Camera cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.14f, 0.15f, 0.18f, 1f); // Sleek charcoal slate grey
            }
            
            // Read initial angles from existing transform values if camera is already set up
            Vector3 angles = transform.eulerAngles;
            _yawTarget = angles.y;
            _pitchTarget = Mathf.Clamp(angles.x, minPitch, maxPitch);
            _distanceTarget = Vector3.Distance(transform.position, _positionTarget);
            
            if (_distanceTarget < minZoom || _distanceTarget > maxZoom)
            {
                _distanceTarget = 12f;
            }

            // Sync current state to target at start
            _currentPosition = _positionTarget;
            _currentYaw = _yawTarget;
            _currentPitch = _pitchTarget;
            _currentDistance = _distanceTarget;
            
            UpdateCameraPosition();
        }

        private void LateUpdate()
        {
            if (Mouse.current == null || Keyboard.current == null) return;

            // Only allow manual player controls if NOT in cinematic mode
            if (!_isCinematicMode)
            {
                // 1. Zoom Control (Mouse Scroll Wheel)
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    _distanceTarget -= scroll * zoomSpeed * Time.deltaTime * 0.05f;
                    _distanceTarget = Mathf.Clamp(_distanceTarget, minZoom, maxZoom);
                }

                // 2. Rotate Orbit Control (Hold Right Click + Drag Mouse)
                if (Mouse.current.rightButton.isPressed)
                {
                    Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                    _yawTarget += mouseDelta.x * rotateSpeed * Time.deltaTime;
                    _pitchTarget -= mouseDelta.y * rotateSpeed * Time.deltaTime;
                    _pitchTarget = Mathf.Clamp(_pitchTarget, minPitch, maxPitch);
                }

                // 3. Pan Control (WASD / Keyboard Arrows)
                Vector3 panDirection = Vector3.zero;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                {
                    panDirection += transform.forward;
                }
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                {
                    panDirection -= transform.forward;
                }
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                {
                    panDirection -= transform.right;
                }
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                {
                    panDirection += transform.right;
                }

                // Keep the camera panning flat on the horizontal ground plane
                panDirection.y = 0f;
                if (panDirection.sqrMagnitude > 0.01f)
                {
                    _positionTarget += panDirection.normalized * panSpeed * Time.deltaTime;
                }
            }

            // Smoothly interpolate current camera state towards target values
            float lerpFactor = Time.deltaTime * 6f; // Adjust to tweak camera responsiveness
            _currentPosition = Vector3.Lerp(_currentPosition, _positionTarget, lerpFactor);
            _currentYaw = Mathf.LerpAngle(_currentYaw, _yawTarget, lerpFactor);
            _currentPitch = Mathf.Lerp(_currentPitch, _pitchTarget, lerpFactor);
            _currentDistance = Mathf.Lerp(_currentDistance, _distanceTarget, lerpFactor);

            UpdateCameraPosition();
        }

        private void UpdateCameraPosition()
        {
            Quaternion rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -_currentDistance);
            
            transform.position = _currentPosition + offset;
            transform.rotation = rotation;
        }

        /// <summary>
        /// Smoothly transitions the camera to a dramatic close-up view of a combat strike.
        /// </summary>
        public void StartCinematicStrike(Vector3 attackerPos, Vector3 defenderPos)
        {
            if (_isCinematicMode) return;

            // Save player's custom camera state to restore later
            _originalTargetPos = _positionTarget;
            _originalYaw = _yawTarget;
            _originalPitch = _pitchTarget;
            _originalDistance = _distanceTarget;

            _isCinematicMode = true;

            // Focus on the midpoint between the two units
            _positionTarget = (attackerPos + defenderPos) / 2f;

            // Bring camera close to the action
            _distanceTarget = 4.0f;

            // Set a lower, heroic dramatic pitch angle
            _pitchTarget = 24f;

            // Calculate yaw perpendicular to the strike direction for a side profile action view
            Vector3 attackDir = (defenderPos - attackerPos).normalized;
            Vector3 sideDir = Vector3.Cross(attackDir, Vector3.up).normalized;
            
            // Look at the fight from one of the sides
            _yawTarget = Mathf.Atan2(sideDir.x, sideDir.z) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Restores the camera view back to the player's previous custom orientation.
        /// </summary>
        public void StopCinematicStrike()
        {
            if (!_isCinematicMode) return;

            // Restore saved values
            _positionTarget = _originalTargetPos;
            _yawTarget = _originalYaw;
            _pitchTarget = _originalPitch;
            _distanceTarget = _originalDistance;

            _isCinematicMode = false;
        }
    }
}
