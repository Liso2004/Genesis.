using UnityEngine;

namespace Genesis.Sandbox
{
    [RequireComponent(typeof(SandboxActor))]
    public sealed class SandboxInteractor : MonoBehaviour
    {
        [SerializeField] private SandboxStation[] stations = System.Array.Empty<SandboxStation>();
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private string practiceButton = "Fire1";
        private SandboxActor _actor;
        private Camera _camera;
        private float _nextPoll;
        public SandboxStation Selected { get; private set; }
        private void Awake() { _actor = GetComponent<SandboxActor>(); _camera = Camera.main; }
        private void Update()
        {
            if (_actor.Scenario == null) return;
            if (Time.time >= _nextPoll) { _nextPoll = Time.time + 0.1f; Selected = FindStation(false); }
            if (Input.GetKeyDown(interactKey))
            {
                var station = FindStation(false); // Revalidate on the input frame.
                if (station != null) station.Interact(_actor);
                else _actor.Report("Move closer and face a test station.");
            }
            if (Input.GetButtonDown(practiceButton))
            {
                var target = FindStation(true);
                if (target != null) _actor.Practice(target, Time.time);
                else _actor.Report("Face the training dummy within practice range.");
            }
        }
        private SandboxStation FindStation(bool trainingOnly)
        {
            SandboxStation best = null; float nearest = float.PositiveInfinity;
            foreach (var target in stations)
            {
                if (target == null || !target.Available || (trainingOnly && target.Kind != SandboxStationKind.Training)) continue;
                if (!CanReach(target, trainingOnly)) continue;
                float distance = (target.FocusPoint - transform.position).sqrMagnitude;
                if (distance < nearest) { best = target; nearest = distance; }
            }
            return best;
        }
        public bool CanReach(SandboxStation target, bool training = false)
        {
            if (target == null || !target.Available || _actor.Scenario == null) return false;
            var config = _actor.Scenario;
            float range = training ? config.practiceDistance : target.Kind == SandboxStationKind.Observation
                ? config.observationDistance * _actor.Gods.ObservationRangeMultiplier : config.interactionDistance;
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            Vector3 delta = target.FocusPoint - origin;
            if (delta.sqrMagnitude > range * range) return false;
            Vector3 aim = _camera != null ? _camera.transform.forward : transform.forward;
            if (Vector3.Angle(aim, delta) > config.observationFieldOfView * 0.5f) return false;
            if (Physics.Raycast(origin, delta.normalized, out var hit, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                return hit.transform == target.transform || hit.transform.IsChildOf(target.transform);
            return true;
        }
    }
}
