using UnityEngine;

namespace Genesis.Character
{
    /// <summary>Observes achieved motor motion; owns parameters, never actor movement.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        private PlayerController _motor;
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Grounded = Animator.StringToHash("IsGrounded");
        private static readonly int Crouching = Animator.StringToHash("IsCrouching");
        private static readonly int Vertical = Animator.StringToHash("VerticalVelocity");

        private void Awake()
        {
            _motor = GetComponent<PlayerController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;
        }

        private void Update()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            // Motor acceleration already smooths speed. Extra damping causes sliding at walls/stops.
            animator.SetFloat(Speed, _motor.PlanarSpeed);
            animator.SetBool(Grounded, _motor.IsGrounded);
            animator.SetBool(Crouching, _motor.IsCrouching);
            animator.SetFloat(Vertical, _motor.VerticalVelocity);
        }
    }
}
