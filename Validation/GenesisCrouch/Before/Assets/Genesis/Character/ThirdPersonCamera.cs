using UnityEngine;

namespace Genesis.Character
{
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField]
        private Transform target;

        [Header("Distance")]
        [SerializeField]
        private float distance = 6f;

        [SerializeField]
        private float height = 2.5f;

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

        public void BindIfUnassigned(Transform player)
        {
            if (target == null) target = player;
        }

        private void Start()
        {
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

            HandleLook();
            FollowTarget();
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

            Vector3 targetPosition =
                target.position +
                Vector3.up * height;

            Vector3 direction =
                -(rotation * Vector3.forward);

            float safeDistance =
                GetSafeDistance(
                    targetPosition,
                    direction
                );

            Vector3 desiredPosition =
                targetPosition +
                direction * safeDistance;

            Vector3 smoothedPosition =
                Vector3.SmoothDamp(
                    transform.position,
                    desiredPosition,
                    ref _velocity,
                    positionSmoothTime
                );

            // Smoothing may cross an obstruction even when the desired orbit is clear.
            Vector3 offset = smoothedPosition - targetPosition;
            float length = offset.magnitude;
            if (length > 0.001f)
            {
                float clearDistance = GetSafeDistance(targetPosition, offset / length, length);
                transform.position = targetPosition + offset / length * clearDistance;
                if (clearDistance < length) _velocity = Vector3.zero;
            }
            else transform.position = smoothedPosition;

            transform.LookAt(targetPosition);
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

        private void OnDestroy()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
