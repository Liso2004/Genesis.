using UnityEngine;

namespace Genesis.Character
{
    public enum PlayerCameraPov
    {
        CloseThirdPerson,
        ThirdPerson,
        FirstPerson
    }

    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField]
        private Transform target;

        [Header("Distance")]
        [SerializeField]
        private float distance = 3.5f;

        [SerializeField]
        private float height = 1.65f;

        [Header("POV")]
        [SerializeField] private PlayerCameraPov viewMode = PlayerCameraPov.CloseThirdPerson;
        [SerializeField] private KeyCode povCycleKey = KeyCode.V;
        [SerializeField, Min(0.1f)] private float wideViewExtraDistance = 1.7f;
        [SerializeField, Min(0f)] private float closeViewShoulderOffset = 0.3f;
        [SerializeField, Min(0.5f)] private float firstPersonEyeHeight = 1.62f;
        [SerializeField] private float firstPersonForwardOffset = 0.06f;

        [SerializeField, Min(0f)] private float crouchHeightReduction = 0.65f;
        private PlayerController _targetMotor;
        private float _framingHeight;
        private Transform _cachedTarget;
        private Renderer[] _targetRenderers = System.Array.Empty<Renderer>();
        private bool[] _rendererEnabledStates = System.Array.Empty<bool>();

        [Header("Look")]
        [SerializeField]
        private float mouseSensitivity = 3f;

        [SerializeField]
        private float minPitch = -30f;

        [SerializeField]
        private float maxPitch = 70f;

        [Header("Smoothing")]
        [SerializeField]
        private float positionSmoothTime = 0.08f;

        [Header("Collision")]
        [SerializeField]
        private float collisionRadius = 0.25f;

        [SerializeField]
        private float collisionBuffer = 0.15f;

        [SerializeField]
        private LayerMask collisionLayers = ~0;

        private float _yaw;
        private float _pitch = 15f;

        private Vector3 _velocity;
        private readonly RaycastHit[] _collisionHits = new RaycastHit[16];

        private void OnEnable()
        {
            if (target != null) CacheTargetRenderers();
        }

        public void BindIfUnassigned(Transform player)
        {
            if (target == null) target = player;
            if (target != null)
            {
                _targetMotor = target.GetComponent<PlayerController>();
                CacheTargetRenderers();
            }
        }

        private void Start()
        {
            _framingHeight = height;
            if (target != null)
            {
                _targetMotor = target.GetComponent<PlayerController>();
                CacheTargetRenderers();
            }
            _yaw = transform.eulerAngles.y;
            _pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, transform.eulerAngles.x), minPitch, maxPitch);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (Input.GetKeyDown(povCycleKey)) CycleViewMode();
            HandleLook();
            FollowTarget();
        }

        private void CycleViewMode()
        {
            viewMode = (PlayerCameraPov)(((int)viewMode + 1) % 3);
            ApplyFirstPersonVisibility();
        }

        private void HandleLook()
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            _yaw += mouseX * mouseSensitivity;
            _pitch -= mouseY * mouseSensitivity;

            _pitch = Mathf.Clamp(
                _pitch,
                minPitch,
                maxPitch
            );
        }

        private void FollowTarget()
        {
            Quaternion rotation =
                Quaternion.Euler(
                    _pitch,
                    _yaw,
                    0f
                );

            bool crouching = _targetMotor != null && _targetMotor.IsCrouching;
            float crouchOffset = crouching ? crouchHeightReduction : 0f;
            if (viewMode == PlayerCameraPov.FirstPerson)
            {
                float eyeHeight = firstPersonEyeHeight - crouchOffset * 0.65f;
                transform.position = target.position + Vector3.up * eyeHeight + rotation * Vector3.forward * firstPersonForwardOffset;
                transform.rotation = rotation;
                return;
            }

            float desiredHeight = height - crouchOffset;
            _framingHeight = Mathf.Lerp(_framingHeight, desiredHeight, 1f - Mathf.Exp(-12f * Time.deltaTime));
            Vector3 targetPosition = target.position + Vector3.up * _framingHeight;

            Vector3 direction = -(rotation * Vector3.forward);
            float desiredDistance = viewMode == PlayerCameraPov.ThirdPerson
                ? distance + wideViewExtraDistance
                : distance;
            Vector3 shoulderOffset = viewMode == PlayerCameraPov.CloseThirdPerson
                ? rotation * Vector3.right * closeViewShoulderOffset
                : Vector3.zero;

            float safeDistance =
                GetSafeDistance(
                    targetPosition,
                    direction,
                    desiredDistance
                );

            Vector3 desiredPosition =
                targetPosition +
                direction * safeDistance + shoulderOffset;

            Vector3 smoothedPosition =
                Vector3.SmoothDamp(
                    transform.position,
                    desiredPosition,
                    ref _velocity,
                    positionSmoothTime
                );

            // Smoothing may cross an obstruction even when the desired orbit is clear.
            Vector3 offset = smoothedPosition - targetPosition - shoulderOffset;
            float length = offset.magnitude;
            if (length > 0.001f)
            {
                float clearDistance = GetSafeDistance(targetPosition + shoulderOffset, offset / length, length);
                transform.position = targetPosition + shoulderOffset + offset / length * clearDistance;
                if (clearDistance < length) _velocity = Vector3.zero;
            }
            else transform.position = smoothedPosition;

            transform.LookAt(targetPosition + shoulderOffset);
        }

        private void CacheTargetRenderers()
        {
            if (_cachedTarget == target) return;
            RestoreTargetRenderers();
            _cachedTarget = target;
            _targetRenderers = target != null ? target.GetComponentsInChildren<Renderer>(true) : System.Array.Empty<Renderer>();
            _rendererEnabledStates = new bool[_targetRenderers.Length];
            for (int i = 0; i < _targetRenderers.Length; i++)
                _rendererEnabledStates[i] = _targetRenderers[i] != null && _targetRenderers[i].enabled;
            ApplyFirstPersonVisibility();
        }

        private void ApplyFirstPersonVisibility()
        {
            bool firstPerson = viewMode == PlayerCameraPov.FirstPerson;
            for (int i = 0; i < _targetRenderers.Length; i++)
                if (_targetRenderers[i] != null)
                    _targetRenderers[i].enabled = firstPerson ? false : _rendererEnabledStates[i];
        }

        private void RestoreTargetRenderers()
        {
            for (int i = 0; i < _targetRenderers.Length; i++)
                if (_targetRenderers[i] != null && i < _rendererEnabledStates.Length)
                    _targetRenderers[i].enabled = _rendererEnabledStates[i];
            _targetRenderers = System.Array.Empty<Renderer>();
            _rendererEnabledStates = System.Array.Empty<bool>();
            _cachedTarget = null;
        }

        private float GetSafeDistance(
            Vector3 origin,
            Vector3 direction,
            float maximumDistance = -1f)
        {
            if (maximumDistance < 0f) maximumDistance = distance;
            int count = Physics.SphereCastNonAlloc(
                origin,
                collisionRadius,
                direction,
                _collisionHits,
                maximumDistance,
                collisionLayers,
                QueryTriggerInteraction.Ignore);
            float result = maximumDistance;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _collisionHits[i];
                if (target != null && (hit.transform == target || hit.transform.IsChildOf(target))) continue;
                result = Mathf.Min(result, Mathf.Max(0f, hit.distance - collisionBuffer));
            }
            return result;
        }

        private void OnDisable()
        {
            RestoreTargetRenderers();
        }

        private void OnDestroy()
        {
            RestoreTargetRenderers();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
