#if UNITY_EDITOR
using System.Collections;
using Genesis.Character;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Genesis.Tests
{
    public sealed class PlayerAnimationTests
    {
        private GameObject _player;
        private Animator _animator;
        private GameObject _runtime;

        [SetUp]
        public void SetUp()
        {
            if (Genesis.Core.GenesisRuntime.Instance == null)
            {
                _runtime = new GameObject("Animation test runtime");
                _runtime.AddComponent<Genesis.Core.GenesisRuntime>();
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Genesis/Character/Prefabs/GenesisPlayer.prefab");
            Assert.That(prefab, Is.Not.Null, "Run the explicit migration before these tests");
            _player = Object.Instantiate(prefab);
            _player.GetComponent<PlayerController>().enabled = false;
            _player.GetComponent<PlayerAnimation>().enabled = false;
            _animator = _player.GetComponentInChildren<Animator>();
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _animator.Rebind();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_player);
            if (_runtime != null) Object.DestroyImmediate(_runtime);
        }

        private void Evaluate(float speed, bool grounded, float vertical, int frames = 20)
        {
            _animator.SetFloat("Speed", speed);
            _animator.SetBool("IsGrounded", grounded);
            _animator.SetFloat("VerticalVelocity", vertical);
            for (int i = 0; i < frames; i++) _animator.Update(1f / 60f);
        }

        [UnityTest]
        public IEnumerator PrefabRetargetsKevinClipsWithoutMovingActorRoot()
        {
            Assert.That(_animator.avatar.isValid && _animator.avatar.isHuman, Is.True);
            Assert.That(_animator.applyRootMotion, Is.False);
            Assert.That(_player.GetComponentsInChildren<PlayerController>().Length, Is.EqualTo(1));
            Assert.That(_player.GetComponentsInChildren<MeshCollider>().Length, Is.Zero);
            Vector3 position = _player.transform.position;
            string[] names = { "HumanM@Idle01", "HumanM@Walk01_Forward", "HumanM@Run01_Forward", "HumanM@Sprint01_Forward" };
            float[] speeds = { 0f, 1.6f, 4f, 6f };
            for (int i = 0; i < speeds.Length; i++)
            {
                Evaluate(speeds[i], true, -5, 120);
                Assert.That(_animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), Is.True);
                var clips = _animator.GetCurrentAnimatorClipInfo(0);
                Assert.That(System.Array.Exists(clips, c => c.clip.name == names[i] && c.weight > 0.95f), Is.True);
                Assert.That(_player.transform.position, Is.EqualTo(position));
                Assert.That(_animator.GetBoneTransform(HumanBodyBones.LeftFoot), Is.Not.Null);
                Assert.That(_animator.GetBoneTransform(HumanBodyBones.RightFoot), Is.Not.Null);
            }
            Evaluate(0, true, -5);
            Assert.That(_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name, Is.EqualTo(names[0]));
            Evaluate(1.6f, true, -5, 7);
            Vector3 foot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
            float time = _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            Evaluate(1.6f, true, -5, 7);
            Assert.That(_animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.GreaterThan(time));
            Assert.That(Vector3.Distance(foot, _animator.GetBoneTransform(HumanBodyBones.LeftFoot).position),
                Is.GreaterThan(0.001f), "The retargeted rig must animate, not just change state names");
            yield return null;
        }

        [UnityTest]
        public IEnumerator MotorMovementDrivesAnimatorThroughPresentation()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                floor.transform.position = new Vector3(0, -0.5f, 0);
                floor.transform.localScale = new Vector3(100, 1, 100);
                _player.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var motor = _player.GetComponent<PlayerController>();
                motor.CameraTransform = null;
                var presentation = _player.GetComponent<PlayerAnimation>();
                Physics.SyncTransforms();
                var inputs = new[] {
                    new PlayerMovementInput.Frame(Vector2.zero),
                    new PlayerMovementInput.Frame(Vector2.up),
                    new PlayerMovementInput.Frame(Vector2.up, run: true),
                    new PlayerMovementInput.Frame(Vector2.zero),
                    new PlayerMovementInput.Frame(Vector2.up, sprint: true)
                };
                float[] speeds = { 0, 1.6f, 4, 0, 6 };
                for (int mode = 0; mode < inputs.Length; mode++)
                {
                    for (int frame = 0; frame < 90; frame++)
                    {
                        motor.Simulate(inputs[mode], 1f / 60f);
                        presentation.SendMessage("Update");
                        _animator.Update(1f / 60f);
                    }
                    Assert.That(_animator.GetFloat("Speed"), Is.EqualTo(motor.PlanarSpeed).Within(0.0001f),
                        "Presentation must report achieved displacement, including capsule contact resolution");
                    Assert.That(motor.PlanarSpeed, Is.EqualTo(speeds[mode]).Within(speeds[mode] * 0.1f + 0.01f));
                    Assert.That(_animator.GetBool("IsGrounded"), Is.True);
                    Assert.That(_animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), Is.True);
                }
                motor.Simulate(new PlayerMovementInput.Frame(Vector2.zero, jump: true), 1f / 60f);
                presentation.SendMessage("Update");
                Assert.That(_animator.GetBool("IsGrounded"), Is.False);
                Assert.That(_animator.GetFloat("VerticalVelocity"), Is.GreaterThan(0));
                Assert.That(_animator.GetFloat("VerticalVelocity"), Is.EqualTo(motor.VerticalVelocity));
                LogAssert.NoUnexpectedReceived();
            }
            finally { Object.DestroyImmediate(floor); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator JumpFallLandAndLedgeTransitionsRecoverWithoutLoops()
        {
            Evaluate(0, true, -5);
            Evaluate(0, false, 8);
            Assert.That(_animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"), Is.True);
            Evaluate(0, false, -2);
            Assert.That(_animator.GetCurrentAnimatorStateInfo(0).IsName("Fall"), Is.True);
            Evaluate(0, true, -5, 10);
            Assert.That(_animator.GetCurrentAnimatorStateInfo(0).IsName("Land"), Is.True);
            Evaluate(0, true, -5, 120);
            Assert.That(_animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), Is.True);
            Evaluate(4, false, -3);
            Assert.That(_animator.GetCurrentAnimatorStateInfo(0).IsName("Fall"), Is.True);
            Evaluate(4, true, -5, 25);
            Assert.That(_animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), Is.True);
            Evaluate(0, false, 8, 2);
            Evaluate(0, true, -5, 90);
            Assert.That(_animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), Is.True,
                "Short jumps / ceiling contacts must not freeze a transition");
            yield return null;
        }
    }
}
#endif
