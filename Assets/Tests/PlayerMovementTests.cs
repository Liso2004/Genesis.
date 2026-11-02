using System.Collections;
using System.Collections.Generic;
using Genesis.Character;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Genesis.Tests
{
    public sealed class PlayerMovementTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private const float Dt = 1f / 60f;

        private GameObject Box(Vector3 position, Vector3 scale, bool trigger = false)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _objects.Add(obj);
            obj.transform.position = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Collider>().isTrigger = trigger;
            return obj;
        }

        private PlayerController Player(Vector3 position)
        {
            var obj = new GameObject("Test player");
            _objects.Add(obj);
            obj.transform.position = position;
            var capsule = obj.AddComponent<CharacterController>();
            capsule.height = 1.9f;
            capsule.center = Vector3.up * 0.95f;
            capsule.radius = 0.2f;
            capsule.skinWidth = 0.03f;
            capsule.minMoveDistance = 0f;
            var motor = obj.AddComponent<PlayerController>();
            motor.enabled = false;
            return motor;
        }

        private static void Step(PlayerController motor, PlayerMovementInput.Frame input, int frames = 1)
        {
            Physics.SyncTransforms();
            for (int i = 0; i < frames; i++) motor.Simulate(input, Dt);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _objects) Object.DestroyImmediate(obj);
            _objects.Clear();
        }

        [UnityTest]
        public IEnumerator SpawnPreservesHeightAndCapsuleSettings()
        {
            var motor = Player(new Vector3(3, 12, 7));
            Assert.That(motor.transform.position, Is.EqualTo(new Vector3(3, 12, 7)));
            Assert.That(motor.GetComponent<CharacterController>().center.y, Is.EqualTo(0.95f));
            yield return null;
        }

        [Test]
        public void CameraRelativeInputPreservesAnalogAndNormalizesDiagonals()
        {
            var camera = new GameObject("Basis");
            _objects.Add(camera);
            camera.transform.rotation = Quaternion.Euler(70, 90, 0);
            var direction = PlayerController.CameraRelativeDirection(new Vector2(0, 0.25f), camera.transform);
            Assert.That(direction.x, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(direction.y, Is.EqualTo(0f));
            Assert.That(PlayerController.CameraRelativeDirection(Vector2.one, camera.transform).magnitude,
                Is.EqualTo(1f).Within(0.001f));
            camera.transform.rotation = Quaternion.Euler(90, 0, 0);
            Assert.That(PlayerController.CameraRelativeDirection(Vector2.up, camera.transform).magnitude,
                Is.EqualTo(1f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator GroundedMovementSupportsDirectionsSpeedsStoppingAndWallContact()
        {
            Box(new Vector3(0, -0.5f, 0), new Vector3(100, 1, 100));
            var motor = Player(Vector3.zero);
            Step(motor, new PlayerMovementInput.Frame(Vector2.zero), 10);
            float groundY = motor.transform.position.y;
            Step(motor, new PlayerMovementInput.Frame(Vector2.zero), 300);
            Assert.That(motor.IsGrounded, Is.True);
            Assert.That(motor.transform.position.y, Is.EqualTo(groundY).Within(0.005f));
            foreach (var direction in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right, Vector2.one })
            {
                Step(motor, new PlayerMovementInput.Frame(direction), 30);
                Assert.That(motor.PlanarSpeed, Is.EqualTo(1.6f).Within(0.08f));
                Assert.That(Vector3.Dot(motor.transform.forward,
                    new Vector3(direction.normalized.x, 0, direction.normalized.y)), Is.GreaterThan(0.98f));
            }
            Step(motor, new PlayerMovementInput.Frame(Vector2.up, run: true), 30);
            Assert.That(motor.PlanarSpeed, Is.EqualTo(4f).Within(0.1f));
            Step(motor, new PlayerMovementInput.Frame(Vector2.up, sprint: true), 30);
            Assert.That(motor.PlanarSpeed, Is.EqualTo(6f).Within(0.1f));
            Step(motor, new PlayerMovementInput.Frame(Vector2.zero, run: true), 20);
            Assert.That(motor.PlanarSpeed, Is.LessThan(0.01f));
            Box(motor.transform.position + new Vector3(0, 2, 1), new Vector3(10, 4, 0.5f));
            Step(motor, new PlayerMovementInput.Frame(Vector2.up), 120);
            Assert.That(motor.PlanarSpeed, Is.LessThan(0.02f), "Animation must stop against a wall");
            yield return null;
        }

        [UnityTest]
        public IEnumerator JumpAppliesImmediatelyCannotRepeatInAirAndLands()
        {
            Box(new Vector3(0, -0.5f, 0), new Vector3(100, 1, 100));
            var motor = Player(Vector3.zero);
            Step(motor, new PlayerMovementInput.Frame(Vector2.zero), 10);
            float initialY = motor.transform.position.y;
            Step(motor, new PlayerMovementInput.Frame(Vector2.zero, jump: true));
            Assert.That(motor.IsGrounded, Is.False);
            Assert.That(motor.transform.position.y, Is.GreaterThan(initialY));
            float firstVelocity = motor.VerticalVelocity;
            Step(motor, new PlayerMovementInput.Frame(Vector2.zero, jump: true));
            Assert.That(motor.VerticalVelocity, Is.LessThan(firstVelocity));
            float peak = motor.transform.position.y;
            for (int i = 0; i < 180; i++)
            {
                Step(motor, new PlayerMovementInput.Frame(Vector2.zero));
                peak = Mathf.Max(peak, motor.transform.position.y);
            }
            Assert.That(peak - initialY, Is.InRange(1.6f, 1.9f));
            Assert.That(motor.IsGrounded, Is.True);
            Assert.That(motor.transform.position.y, Is.EqualTo(initialY).Within(0.05f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CeilingStopsAscentAndTriggersDoNotGroundPlayer()
        {
            Box(new Vector3(0, -0.5f, 0), new Vector3(10, 1, 10));
            Box(new Vector3(0, 2.6f, 0), new Vector3(10, 0.2f, 10));
            var motor = Player(Vector3.zero);
            Step(motor, new PlayerMovementInput.Frame(Vector2.zero), 10);
            Step(motor, new PlayerMovementInput.Frame(Vector2.zero, jump: true));
            Step(motor, new PlayerMovementInput.Frame(Vector2.zero), 10);
            Assert.That(motor.VerticalVelocity, Is.LessThanOrEqualTo(0f));
            var airborne = Player(new Vector3(20, 5, 0));
            Box(new Vector3(20, 4.5f, 0), new Vector3(5, 1, 5), true);
            Step(airborne, new PlayerMovementInput.Frame(Vector2.zero), 20);
            Assert.That(airborne.IsGrounded, Is.False);
            Assert.That(airborne.transform.position.y, Is.LessThan(4.5f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator LedgeTransitionsToFallAndTerminalVelocityIsBounded()
        {
            Box(new Vector3(0, -0.5f, 0), new Vector3(2, 1, 2));
            var motor = Player(Vector3.zero);
            Step(motor, new PlayerMovementInput.Frame(Vector2.up), 90);
            Assert.That(motor.IsGrounded, Is.False);
            Assert.That(motor.transform.position.y, Is.LessThan(-0.3f));
            Step(motor, new PlayerMovementInput.Frame(Vector2.zero), 600);
            Assert.That(motor.VerticalVelocity, Is.EqualTo(-50f).Within(0.01f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator WalkableSlopeRemainsGroundedAndSteepSlopeDoesNotPermitJump()
        {
            var ramp = Box(new Vector3(0, -0.5f, 0), new Vector3(10, 1, 20));
            ramp.transform.rotation = Quaternion.Euler(-20, 0, 0);
            var motor = Player(new Vector3(0, 0.3f, 0));
            Step(motor, new PlayerMovementInput.Frame(Vector2.zero), 60);
            Assert.That(motor.IsGrounded, Is.True);
            float start = motor.transform.position.y;
            Step(motor, new PlayerMovementInput.Frame(Vector2.up), 90);
            Assert.That(motor.IsGrounded, Is.True);
            Assert.That(motor.transform.position.y, Is.GreaterThan(start + 0.3f));
            ramp.transform.position = new Vector3(30, -0.5f, 0);
            ramp.transform.rotation = Quaternion.Euler(-65, 0, 0);
            var steep = Player(new Vector3(30, 0.8f, 0));
            Step(steep, new PlayerMovementInput.Frame(Vector2.zero), 30);
            Step(steep, new PlayerMovementInput.Frame(Vector2.zero, jump: true));
            Assert.That(steep.IsGrounded, Is.False);
            Assert.That(steep.VerticalVelocity, Is.LessThanOrEqualTo(0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator WalksUpStairEdgesWithoutLosingNativeStepContact()
        {
            Box(new Vector3(0, -0.5f, 0), new Vector3(20, 1, 20));
            for (int i = 0; i < 6; i++)
                Box(new Vector3(0, (i + 1) * 0.09f, i * 0.45f),
                    new Vector3(2, (i + 1) * 0.18f, 0.45f));
            Box(new Vector3(0, 0.54f, 3.25f), new Vector3(2, 1.08f, 1.5f));
            var motor = Player(new Vector3(0, 0.03f, -1.1f));
            Step(motor, new PlayerMovementInput.Frame(Vector2.up), 175);
            Assert.That(motor.transform.position.z, Is.GreaterThan(3f));
            Assert.That(motor.transform.position.y, Is.InRange(1.05f, 1.15f));
            Assert.That(motor.IsGrounded, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TravelDistanceIsConsistentAtThirtyAndOneHundredTwentyFps()
        {
            Box(new Vector3(0, -0.5f, 0), new Vector3(100, 1, 100));
            var slow = Player(Vector3.zero);
            var fast = Player(Vector3.right * 10);
            Physics.SyncTransforms();
            var input = new PlayerMovementInput.Frame(Vector2.up, run: true);
            for (int i = 0; i < 60; i++) slow.Simulate(input, 1f / 30f);
            for (int i = 0; i < 240; i++) fast.Simulate(input, 1f / 120f);
            Assert.That(slow.transform.position.z, Is.EqualTo(fast.transform.position.z).Within(0.08f));
            Assert.That(slow.IsGrounded && fast.IsGrounded, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CameraProbeIgnoresPlayerAndClampsToWall()
        {
            var motor = Player(Vector3.zero);
            var obj = new GameObject("Test camera");
            _objects.Add(obj);
            var camera = obj.AddComponent<ThirdPersonCamera>();
            camera.BindIfUnassigned(motor.transform);
            var probe = typeof(ThirdPersonCamera).GetMethod("GetSafeDistance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Physics.SyncTransforms();
            float clear = (float)probe.Invoke(camera, new object[] { new Vector3(0, 1, 1), Vector3.back, 4f });
            Assert.That(clear, Is.EqualTo(4f).Within(0.01f));
            Box(new Vector3(0, 1, -1), new Vector3(10, 3, 0.2f));
            Physics.SyncTransforms();
            float blocked = (float)probe.Invoke(camera, new object[] { new Vector3(0, 1, 1), Vector3.back, 4f });
            Assert.That(blocked, Is.InRange(1.3f, 1.7f));
            yield return null;
        }
    }
}
