using UnityEngine;

namespace Genesis.Character
{
    /// <summary>Owns player translation and facing. Animation never moves the actor.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(GenesisActor), typeof(PlayerMovementInput))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement (metres per second)")]
        [SerializeField, Min(0.01f)] private float walkSpeed = 1.6f;
        [SerializeField, Min(0.01f)] private float runSpeed = 4f;
        [SerializeField, Min(0.01f)] private float sprintSpeed = 6f;
        [SerializeField, Min(0.01f)] private float acceleration = 30f;
        [SerializeField, Min(0.01f)] private float deceleration = 40f;
        [SerializeField, Min(0.01f)] private float rotationSpeed = 18f;
        [Header("Camera")]
        [SerializeField] private Transform cameraTransform;
        [Header("Jump and gravity")]
        [SerializeField, Min(0f)] private float jumpHeight = 1.8f;
        [SerializeField] private float gravity = -20f;
        [SerializeField, Min(0.1f)] private float terminalFallSpeed = 50f;
        [SerializeField, Min(0.1f)] private float groundStickSpeed = 5f;
        [Header("Ground check")]
        [SerializeField, Range(0.01f, 0.15f)] private float groundCheckDistance = 0.08f;
        [SerializeField] private LayerMask groundLayerMask = ~0;

        private CharacterController _controller;
        private PlayerMovementInput _input;
        private readonly RaycastHit[] _groundHits = new RaycastHit[16];
        private Vector3 _planarVelocity;
        private Vector3 _groundNormal = Vector3.up;
        private float _verticalVelocity;
        private float _stepOffset;

        public bool IsGrounded { get; private set; }
        public Vector3 Velocity { get; private set; }
        public float VerticalVelocity => _verticalVelocity;
        public float PlanarSpeed => new Vector2(Velocity.x, Velocity.z).magnitude;
        public Transform CameraTransform { get => cameraTransform; set => cameraTransform = value; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<PlayerMovementInput>();
            _stepOffset = _controller.stepOffset;
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            if (cameraTransform != null && cameraTransform.TryGetComponent(out ThirdPersonCamera camera))
                camera.BindIfUnassigned(transform);
        }

        private void Update()
        {
            if (_input != null && _input.isActiveAndEnabled)
                Simulate(_input.Read(), Time.deltaTime);
        }

        /// <summary>One motor step. Tests may call this with automatic Update disabled.</summary>
        public void Simulate(PlayerMovementInput.Frame input, float deltaTime)
        {
            if (deltaTime <= 0f || !_controller.enabled) return;
            IsGrounded = _verticalVelocity <= 0f && ProbeGround(out _groundNormal);
            if (IsGrounded) _verticalVelocity = -groundStickSpeed;

            Vector3 direction = CameraRelativeDirection(input.Move, cameraTransform);
            float speed = input.Sprint ? sprintSpeed : input.Run ? runSpeed : walkSpeed;
            Vector3 desiredVelocity = direction * speed;
            float rate = desiredVelocity.sqrMagnitude > _planarVelocity.sqrMagnitude ? acceleration : deceleration;
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, desiredVelocity, rate * deltaTime);
            if (_planarVelocity.sqrMagnitude > 0.0001f)
            {
                Quaternion rotation = Quaternion.LookRotation(_planarVelocity, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation,
                    1f - Mathf.Exp(-rotationSpeed * deltaTime));
            }

            if (input.Jump && IsGrounded && jumpHeight > 0f)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                IsGrounded = false;
            }

            Vector3 movement = _planarVelocity;
            if (IsGrounded)
                movement = Vector3.ProjectOnPlane(movement, _groundNormal).normalized * movement.magnitude;
            _verticalVelocity = Mathf.Max(_verticalVelocity + gravity * deltaTime, -terminalFallSpeed);
            movement.y += _verticalVelocity;
            _controller.stepOffset = IsGrounded ? _stepOffset : 0f;
            Vector3 before = transform.position;
            CollisionFlags collisions = _controller.Move(movement * deltaTime);
            Velocity = (transform.position - before) / deltaTime;
            if ((collisions & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
                _verticalVelocity = 0f;
            IsGrounded = _verticalVelocity <= 0f && ProbeGround(out _groundNormal);
            if (IsGrounded) _verticalVelocity = -groundStickSpeed;
        }

        public static Vector3 CameraRelativeDirection(Vector2 input, Transform camera)
        {
            input = Vector2.ClampMagnitude(input, 1f);
            Vector3 forward = camera != null ? Vector3.ProjectOnPlane(camera.forward, Vector3.up) : Vector3.forward;
            if (forward.sqrMagnitude < 0.0001f && camera != null)
                forward = Vector3.ProjectOnPlane(camera.up, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();
            return forward * input.y + Vector3.Cross(Vector3.up, forward) * input.x;
        }

        private bool ProbeGround(out Vector3 normal)
        {
            normal = Vector3.up;
            Vector3 scale = transform.lossyScale;
            float radius = _controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float halfHeight = Mathf.Max(_controller.height * Mathf.Abs(scale.y) * 0.5f, radius);
            float skin = _controller.skinWidth * Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            float probeRadius = Mathf.Max(0.01f, radius - skin);
            Vector3 bottomSphere = transform.TransformPoint(_controller.center) - Vector3.up * (halfHeight - radius);
            float lift = skin + 0.02f;
            int count = Physics.SphereCastNonAlloc(bottomSphere + Vector3.up * lift, probeRadius,
                Vector3.down, _groundHits, lift + (radius - probeRadius) + groundCheckDistance,
                groundLayerMask, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            float minimumUp = Mathf.Cos(_controller.slopeLimit * Mathf.Deg2Rad);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _groundHits[i];
                if (hit.collider == _controller || hit.transform.IsChildOf(transform)) continue;
                if (hit.normal.y < minimumUp || hit.distance >= nearest) continue;
                nearest = hit.distance;
                normal = hit.normal;
            }
            return nearest < float.PositiveInfinity;
        }

        private void OnDisable()
        {
            _planarVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            Velocity = Vector3.zero;
            IsGrounded = false;
            if (_controller != null) _controller.stepOffset = _stepOffset;
        }

        private void OnValidate()
        {
            gravity = Mathf.Min(-0.01f, gravity);
            runSpeed = Mathf.Max(walkSpeed, runSpeed);
            sprintSpeed = Mathf.Max(runSpeed, sprintSpeed);
        }
    }
}
