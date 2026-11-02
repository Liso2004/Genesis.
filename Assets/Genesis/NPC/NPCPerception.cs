using UnityEngine;

namespace Genesis.NPC
{
    [RequireComponent(typeof(Genesis.Character.GenesisActor))]
    public sealed class NPCPerception : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField]
        private float detectionRange = 10f;

        [SerializeField]
        [Range(1f, 360f)]
        private float fieldOfView = 120f;

        [Header("Target")]
        [SerializeField]
        private Transform target;

        public bool HasTarget { get; private set; }

        public Transform Target => target;

        public float DetectionRange => detectionRange;

        public float FieldOfView => fieldOfView;

        private void Update()
        {
            EvaluateTarget();
        }

       private void EvaluateTarget()
{
    if (target == null)
    {
        HasTarget = false;
        return;
    }

    Vector3 direction =
        target.position - transform.position;

    float distance = direction.magnitude;

    if (distance > detectionRange)
    {
        HasTarget = false;
        return;
    }

    direction.Normalize();

    float angle =
        Vector3.Angle(
            transform.forward,
            direction
        );

    if (angle > fieldOfView * 0.5f)
    {
        HasTarget = false;
        return;
    }

    Vector3 origin =
        transform.position + Vector3.up * 0.5f;

    Vector3 targetPoint =
        target.position + Vector3.up * 0.5f;

    Vector3 rayDirection =
        targetPoint - origin;

    float rayDistance =
        rayDirection.magnitude;

    rayDirection.Normalize();

    if (Physics.Raycast(
        origin,
        rayDirection,
        out RaycastHit hit,
        rayDistance,
        ~0,
        QueryTriggerInteraction.Ignore))
    {
        HasTarget =
            hit.transform == target ||
            hit.transform.IsChildOf(target);
    }
    else
    {
        HasTarget = false;
    }
}
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(
                transform.position,
                detectionRange
            );

            Vector3 leftBoundary =
                Quaternion.Euler(
                    0f,
                    -fieldOfView * 0.5f,
                    0f
                ) * transform.forward;

            Vector3 rightBoundary =
                Quaternion.Euler(
                    0f,
                    fieldOfView * 0.5f,
                    0f
                ) * transform.forward;

            Gizmos.DrawRay(
                transform.position,
                leftBoundary * detectionRange
            );

            Gizmos.DrawRay(
                transform.position,
                rightBoundary * detectionRange
            );
        }
    }
}