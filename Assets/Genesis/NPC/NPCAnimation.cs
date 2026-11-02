using UnityEngine;

namespace Genesis.NPC
{
    /// <summary>Presentation for the existing NPC capsule motor; never moves the actor.</summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class NPCAnimation : MonoBehaviour
    {
        private CharacterController _capsule;
        private Animator _animator;
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Grounded = Animator.StringToHash("IsGrounded");
        private static readonly int Vertical = Animator.StringToHash("VerticalVelocity");
        private static readonly int Crouching = Animator.StringToHash("IsCrouching");
        private void Awake()
        {
            _capsule = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
            if (_animator != null) _animator.applyRootMotion = false;
        }
        private void Update()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null) return;
            Vector3 velocity = _capsule.velocity;
            _animator.SetFloat(Speed, new Vector2(velocity.x, velocity.z).magnitude);
            _animator.SetBool(Grounded, _capsule.isGrounded);
            _animator.SetFloat(Vertical, velocity.y);
            _animator.SetBool(Crouching, false);
        }
    }
}
