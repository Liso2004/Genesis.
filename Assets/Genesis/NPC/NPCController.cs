using UnityEngine;

namespace Genesis.NPC
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NPCPerception))]
    public sealed class NPCController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField]
        private float moveSpeed = 2.5f;

        [SerializeField]
        private float rotationSpeed = 6f;

        [Header("Stopping")]
        [SerializeField]
        private float stoppingDistance = 2f;

        [Header("Gravity")]
        [SerializeField]
        private float gravity = -20f;

        private CharacterController _controller;
        private NPCPerception _perception;

        private float _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _perception = GetComponent<NPCPerception>();
        }

        private void Update()
        {
            HandleMovement();
        }

        private void HandleMovement()
        {
            if (!_perception.HasTarget ||
                _perception.Target == null)
            {
                ApplyGravity();
                return;
            }

            Vector3 targetPosition =
                _perception.Target.position;

            Vector3 direction =
                targetPosition - transform.position;

            direction.y = 0f;

            float distance =
                direction.magnitude;

            if (distance > stoppingDistance)
            {
                direction.Normalize();

                RotateToward(direction);

                Vector3 movement =
                    direction * moveSpeed;

                ApplyGravityToMovement(
                    ref movement
                );

                _controller.Move(
                    movement * Time.deltaTime
                );
            }
            else
            {
                RotateToward(direction);

                ApplyGravity();
            }
        }

        private void RotateToward(
            Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction,
                    Vector3.up
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed *
                    Time.deltaTime
                );
        }

        private void ApplyGravityToMovement(
            ref Vector3 movement)
        {
            if (_controller.isGrounded)
            {
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = -2f;
                }
            }
            else
            {
                _verticalVelocity +=
                    gravity *
                    Time.deltaTime;
            }

            movement.y = _verticalVelocity;
        }

        private void ApplyGravity()
        {
            Vector3 movement = Vector3.zero;

            ApplyGravityToMovement(
                ref movement
            );

            _controller.Move(
                movement * Time.deltaTime
            );
        }
    }
}