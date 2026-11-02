# Player movement and animation

## Ownership

`PlayerMovementInput.Read â†’ PlayerController.Simulate â†’ CharacterController.Move â†’ PlayerAnimation.Update â†’ Animator`.

PlayerMovementInput samples the existing legacy axes and keys once. PlayerController owns all translation, grounded/jump state and root facing; it exposes achieved velocity to presentation. PlayerAnimation runs after the motor and before Animator evaluation, writes four hashed parameters and disables root motion. ThirdPersonCamera remains the mouse-orbit presentation component in LateUpdate.

The new Input System asset is intentionally not enabled by another movement reader. Both input backends remain configured as before. WASD/arrows walk, either Shift runs, Space jumps; Left Alt adds sprint. C toggles crouch; a blocked stand requires another press after reaching clearance. Keys/axis names are serialized on PlayerMovementInput. The existing Fire2 binding also mentions Alt, but no Genesis combat/Fire2 consumer exists; resolve this binding when combat input is authored. Gamepad movement follows the pre-existing legacy axes; full gamepad look/buttons are not implemented by this task.

## Authoritative prefab

`Assets/Genesis/Character/Prefabs/GenesisPlayer.prefab` is generated from the existing Sandbox player by the explicit migration. Both existing player scenes reference it. It retains the Polygon Dungeon Realms male hero and the Humanoid Avatar in `Models/Characters/Characters.fbx`; animation retargeting uses that character's own Avatar. The Kevin demo characters and demo scripts are not used as gameplay controllers.

The prefab contains CharacterController, GenesisActor, PlayerController, PlayerMovementInput, PlayerAnimation, per-actor SkillSystem, existing NPC_Setup for Perception registration, and the existing nested visual prefab. Redundant player mesh colliders/orphan renderer are removed; the CharacterController is its only movement collider. Capsule geometry is authored in the prefab, not rewritten in Awake. The blank prefab actor ID generates an instance ID; existing scene ID overrides are retained. This is not a persistence solution.

Prefab camera references remain empty because scene objects cannot be embedded in assets. Existing scenes retain their references. A runtime-spawned player caches Camera.main once, and binds ThirdPersonCamera only when its target is unassigned. Set `CameraTransform` explicitly for custom camera/spawn arrangements. No per-frame scene search is performed.

## Motor behavior

Defaults: walk 1.6 m/s, run 4 m/s, sprint 6 m/s; acceleration 30 and deceleration 40 m/sÂ², facing response 18/s, jump height 1.8 m, gravity -20 m/sÂ², terminal descent 50 m/s. These are serialized tuning values, not lore requirements.

Input magnitude is clamped without discarding analog values. The camera basis is flattened and orthonormal; a vertical-camera fallback avoids zero direction. Velocity accelerates toward the request, with exponential facing interpolation. One CharacterController.Move call resolves each step. Animation observes actual displacement, so a blocked player stops animating locomotion.

A reusable nonalloc spherecast buffer probes the capsule feet, excludes the actor's hierarchy and triggers, and accepts only surfaces within the capsule slope limit. Grounding is suppressed while rising. Walkable-slope motion is projected onto its normal. Ground stick and gravity keep contact; jump velocity is applied immediately, ceiling contact clears ascent, and stepping is disabled in the air. No spawn teleport, jump coroutine or Animator motion is involved. Moving platforms and arbitrary gravity are outside the existing controller's scope.

Camera obstruction probes exclude the player, ignore triggers and also constrain the smoothed result. Camera yaw/pitch initialize from the authored camera rotation. Dense scenes exceeding the reusable cast buffers, corner behavior and camera starts inside geometry still need interactive assessment.

## Animator contract

The existing `Animatinos/PlayerAnimator.controller` path and GUID are retained. Base Layer contains Locomotion, Jump, Fall, Land and Crouch. The unused layer and locomotion-only old graph are replaced. No existing combat states or clip events were present.

| Parameter | Meaning |
|---|---|
| Speed | Achieved horizontal metres per second; no input-only or extra delayed smoothing |
| IsGrounded | Motor grounding result after physical movement |
| VerticalVelocity | Authoritative jump/gravity velocity |
| IsCrouching | Authoritative posture, including rejected attempts to stand |

Simple1D `Locomotion Speed` Blend Tree: idle at 0, walk at 1.6, run at 4, sprint at 6. The free-exploration character faces movement, so no strafe/lock-on state is fabricated. Directional clips are available for future combat movement.

The matching RM FBX root curves travel 1.6 m in 0.8 s (walk), 2.4 m in 0.6 s (run), and 3.2 m in 0.5333 s (sprint). The walk child's playback rate is 0.8 to match 1.6 m/s; run/sprint remain 1.0. These RM assets are measurement references only. Retargeted leg-length differences, starts/stops and slopes still need visual foot-contact tuning.

All clip paths below are relative to `Assets/Kevin Iglesias/Human Animations/Animations/Male/`:

| State | Original FBX selected | Reason |
|---|---|---|
| Idle | Idles/HumanM@Idle01.fbx | Neutral looping male stance |
| Walk | Movement/Walk/HumanM@Walk01_Forward.fbx | In-place forward walk |
| Run | Movement/Run/HumanM@Run01_Forward.fbx | In-place run between walk and sprint |
| Sprint | Movement/Sprint/HumanM@Sprint01_Forward.fbx | In-place sprint |
| Jump | Movement/Jump/HumanM@Jump01 - Begin.fbx | Nonlooping ascent segment |
| Fall | Movement/Jump/HumanM@Fall01.fbx | Looping descent, also for ledges |
| Land | Movement/Jump/HumanM@Jump01 - Land.fbx | Nonlooping landing recovery |

These clips are referenced directly as FBX subassets. No clip copies or imported-rig edits are necessary. The inspected imports use Humanoid, the Kevin model Avatar source, baked root rotation/Y/XZ, original root orientation and position, and no animation events. Idle/locomotion/fall loop; jump begin/land do not. Root motion is disabled on the hero Animator. Feet IK is enabled on states; visual foot contact and stride tuning remain a playtest responsibility.

Locomotion/Land go to Jump when airborne and ascending, or Fall when airborne and descending. Jump enters Fall at the apex; Jump/Fall enter Land on grounding. Moving landings recover immediately to locomotion, while stationary recovery waits until 80% of Land. Transitions last 0.08 seconds and allow destination transitions to interrupt. Source transitions are not re-evaluated during a blend, preventing a still-true takeoff condition from restarting the blend every frame. They cannot self-transition. Physics never waits for animation. IsRunning and the Jump trigger were removed after verifying no other consumers.

## Migration and validation

`Genesis.Character.Editor.PlayerLocomotionMigration.RunBatch` is an explicit one-time migration, with a guard against overwriting an existing canonical prefab. It does not run on import. Use only on an unmigrated copy. `ValidateAssets` can be called afterward to check prefab/clip/Avatar/camera/NPC references. The migration uses Unity's [ConvertToPrefabInstance API](https://docs.unity.cn/6000.1/Documentation/ScriptReference/PrefabUtility.ConvertToPrefabInstance.html) to preserve matching scene objects and overrides.

Automated PlayMode tests live in Assets/Tests. The execution results and limitations are recorded in the task report; test source alone is not evidence that a check passed.

## Interactive checklist

Open Sandbox and the existing exterior demo scene separately. Test idle; all eight input directions; diagonal speed; walk/run/sprint; stopping while Shift remains held; rapid reversal; mouse orbit; collision near walls; jump from idle/movement; ceiling hits; flat ground; uphill/downhill slopes; steps; ledges; long falls and land-to-move/jump. Watch feet, body orientation, model offsets, transition continuity and the Console. Instantiate the canonical prefab at an elevated location and verify it keeps the spawn height and binds an available camera.

Confirm the NPC still perceives/chases the existing player and the Bootstrap skill/core smoke scene still runs. Combat, damage, health, interaction, inventory and ability regressions cannot be exercised because those gameplay implementations are absent. SampleScene remains the only enabled build scene; scene flow is a pre-existing SDK gap.

Do not delete old clips or vendor assets. The old clip assets and Genesis.zip remain available; removal requires a fresh reference audit after interactive verification.

Recovery status (2026-09-20): the migration has been applied directly in the real workspace. Both scenes passed Unity reference validation and all three animation PlayMode tests passed; see [recovery evidence](../../Validation/GenesisMovement/Recovery-Report.md).

## Crouch and the existing Sandbox (2026-09-25)

C uses the existing legacy input reader and a one-frame toggle request. The motor owns posture and refuses to jump while crouched. Crouch speed is 0.8 m/s, half the authored walk speed; run/sprint cannot override it. The standing capsule is 1.9 m tall with centre Y 0.95; crouch uses 1.25 m and centre Y 0.625, preserving the feet and model scale. A nonalloc standing-capsule overlap ignores self, triggers and ignored collision pairs. A full query buffer blocks standing conservatively. The existing camera eases its framing down by a configurable 0.65 m.

`Crouch Speed` blends `Movement/Crouch/HumanM@Crouch01_Idle.fbx` and `Movement/Crouch/CrouchWalk/HumanM@Crouch01_Walk_Forward.fbx`, relative to the Kevin male animation directory above. Thresholds are 0 and 0.8 m/s. Walk playback is 0.421566, measured against the corresponding 1.897688 m/s RM reference. Only in-place clips drive the Animator. Crouch enters from grounded locomotion, exits when standing succeeds, falls from ledges, and recovers from Land into crouch when requested. The original standing/airborne graph is retained.

The native CharacterController can keep stair-edge contact beyond the smaller ground probe. Native stepping remains enabled at that descending contact; rising and free-falling movement still disable it. The walkable-slope probe continues to decide jump eligibility.

The saved `Sandbox` now has `Genesis Crouch Courtyard` at world (45, -2.16, 10). Area B's low ceiling is centred at world (45, -0.54, 3), with 1.42 m clearance. Area A is west of B, C is east, D is farther west, and E is the open courtyard. Select the courtyard root and press F in Scene view to find it. Three new Kingdom NPCs use the existing actor, skill, perception and NPC movement components; NPCAnimation only presents their achieved velocity. The original world and test guard remain.

See [the crouch implementation report](Genesis-Crouch-Sandbox-Report.md) for exact files, asset paths and verification evidence. `CrouchSandboxSetup.Apply` is an explicit, already-applied authoring command with a duplicate-courtyard guard; do not rerun it on this scene.
