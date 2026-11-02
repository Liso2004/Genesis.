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
        [Header("Crouch")]
        [SerializeField, Min(0.01f)] private float crouchSpeed = 0.8f;
        [SerializeField, Min(0.1f)] private float crouchedHeight = 1.25f;
        [SerializeField] private LayerMask standingObstructions = ~0;
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
        private float _standingHeight;
        private Vector3 _standingCenter;
        private readonly Collider[] _standingHits = new Collider[32];

        public bool IsCrouching { get; private set; }

        public bool IsGrounded { get; private set; }
        public Vector3 Velocity { get; private set; }
        public float VerticalVelocity => _verticalVelocity;
        public float PlanarSpeed => new Vector2(Velocity.x, Velocity.z).magnitude;
        public Transform CameraTransform { get => cameraTransform; set => cameraTransform = value; }
        /// <summary>Explicit checkpoint placement; leaves no velocity from the previous position.</summary>
        public void RestorePose(Vector3 position, Quaternion rotation, bool crouching)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            IsCrouching = crouching;
            _controller.height = crouching ? Mathf.Clamp(crouchedHeight,_controller.radius*2f,_standingHeight) : _standingHeight;
            _controller.center = _standingCenter - Vector3.up * ((_standingHeight-_controller.height)*.5f);
            _controller.stepOffset = Mathf.Min(_stepOffset,_controller.height-2f*_controller.radius);
            _planarVelocity = Vector3.zero; Velocity = Vector3.zero; _verticalVelocity = 0;
            _controller.enabled = true;
            Physics.SyncTransforms();
            IsGrounded = ProbeGround(out _groundNormal);
            if (IsGrounded) _verticalVelocity = -groundStickSpeed;
        }
        public bool CanRestorePose(Vector3 position, Quaternion rotation, bool crouching)
        {
            float height = crouching ? Mathf.Clamp(crouchedHeight,_controller.radius*2f,_standingHeight) : _standingHeight;
            Vector3 scale = transform.lossyScale;
            Vector3 localCenter = _standingCenter-Vector3.up*((_standingHeight-height)*.5f);
            Vector3 center = position+rotation*Vector3.Scale(localCenter,scale);
            float radius = _controller.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));
            float skin = _controller.skinWidth*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
            Vector3 offset = Vector3.up*Mathf.Max(0,height*Mathf.Abs(scale.y)*.5f-radius);
            int count = Physics.OverlapCapsuleNonAlloc(center-offset,center+offset,Mathf.Max(.01f,radius-skin),_standingHits,standingObstructions,QueryTriggerInteraction.Ignore);
            if (count == _standingHits.Length) return false;
            for (int i=0;i<count;i++)
            {
                var hit = _standingHits[i];
                if (hit == _controller || hit.transform.IsChildOf(transform)) continue;
                if (!Physics.GetIgnoreLayerCollision(gameObject.layer,hit.gameObject.layer) && !Physics.GetIgnoreCollision(_controller,hit)) return false;
            }
            return true;
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<PlayerMovementInput>();
            _stepOffset = _controller.stepOffset;
            _standingHeight = _controller.height;
            _standingCenter = _controller.center;
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
            if (input.ToggleCrouch) TryToggleCrouch();

            Vector3 direction = CameraRelativeDirection(input.Move, cameraTransform);
            float speed = IsCrouching ? crouchSpeed : input.Sprint ? sprintSpeed : input.Run ? runSpeed : walkSpeed;
            Vector3 desiredVelocity = direction * speed;
            float rate = desiredVelocity.sqrMagnitude > _planarVelocity.sqrMagnitude ? acceleration : deceleration;
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, desiredVelocity, rate * deltaTime);
            if (_planarVelocity.sqrMagnitude > 0.0001f)
            {
                Quaternion rotation = Quaternion.LookRotation(_planarVelocity, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation,
                    1f - Mathf.Exp(-rotationSpeed * deltaTime));
            }

            if (input.Jump && !IsCrouching && IsGrounded && jumpHeight > 0f)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                IsGrounded = false;
            }

            Vector3 movement = _planarVelocity;
            if (IsGrounded)
                movement = Vector3.ProjectOnPlane(movement, _groundNormal).normalized * movement.magnitude;
            _verticalVelocity = Mathf.Max(_verticalVelocity + gravity * deltaTime, -terminalFallSpeed);
            movement.y += _verticalVelocity;
            // The capsule can still touch a stair edge outside the narrower ground probe.
            // Preserve native stepping at that contact, but never while rising or in free fall.
            bool canStep = IsGrounded || (_verticalVelocity <= 0f && _controller.isGrounded);
            _controller.stepOffset = canStep ? Mathf.Min(_stepOffset, _controller.height - 2f * _controller.radius) : 0f;
            Vector3 before = transform.position;
            CollisionFlags collisions = _controller.Move(movement * deltaTime);
            Velocity = (transform.position - before) / deltaTime;
            if ((collisions & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
                _verticalVelocity = 0f;
            IsGrounded = _verticalVelocity <= 0f && ProbeGround(out _groundNormal);
            if (IsGrounded) _verticalVelocity = -groundStickSpeed;
        }

        /// <summary>A blocked stand consumes this press and remains crouched until another press.</summary>
        public bool TryToggleCrouch()
        {
            if (IsCrouching && !CanStand()) return false;
            IsCrouching = !IsCrouching;
            float height = IsCrouching
                ? Mathf.Clamp(crouchedHeight, _controller.radius * 2f, _standingHeight)
                : _standingHeight;
            // Preserve the feet and the authored horizontal centre; never scale the actor.
            _controller.height = height;
            _controller.center = _standingCenter - Vector3.up * ((_standingHeight - height) * 0.5f);
            _controller.stepOffset = Mathf.Min(_stepOffset, height - 2f * _controller.radius);
            return true;
        }

        public bool CanStand()
        {
            Vector3 scale = transform.lossyScale;
            float radialScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float radius = _controller.radius * radialScale;
            float halfHeight = Mathf.Max(_standingHeight * Mathf.Abs(scale.y) * 0.5f, radius);
            Vector3 center = transform.TransformPoint(_standingCenter);
            Vector3 offset = Vector3.up * (halfHeight - radius);
            // Respect capsule skin tolerance so the supporting floor does not obstruct standing.
            float skin = _controller.skinWidth * Mathf.Max(radialScale, Mathf.Abs(scale.y));
            float queryRadius = Mathf.Max(0.01f, radius - Mathf.Max(skin, 0.005f));
            int count = Physics.OverlapCapsuleNonAlloc(center - offset, center + offset,
                queryRadius, _standingHits, standingObstructions, QueryTriggerInteraction.Ignore);
            // A full buffer may have omitted a blocking collider: fail closed.
            if (count == _standingHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var obstacle = _standingHits[i];
                if (obstacle == _controller || obstacle.transform.IsChildOf(transform)) continue;
                if (!Physics.GetIgnoreLayerCollision(gameObject.layer, obstacle.gameObject.layer) &&
                    !Physics.GetIgnoreCollision(_controller, obstacle)) return false;
            }
            return true;
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
            crouchSpeed = Mathf.Clamp(crouchSpeed, 0.01f, walkSpeed);
            runSpeed = Mathf.Max(walkSpeed, runSpeed);
            sprintSpeed = Mathf.Max(runSpeed, sprintSpeed);
        }
    }
}
