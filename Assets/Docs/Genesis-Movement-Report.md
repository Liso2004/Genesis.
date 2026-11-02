# Project Genesis movement and animation report

## 1. Repository findings

The Design Bible remains the primary authority. The [repository audit](Genesis-Repository-Audit.md) records the reviewed architecture, system status, dependencies, performance/persistence risks and initial implementation plan. Genesis currently has small Character, Core, Skills and NPC modules: a CharacterController motor, mouse orbit camera, actor identity, synchronous typed events, per-actor skill progression and basic NPC perception/chase. Most larger gameplay and living-world systems are planned folders or documentation, not implementations. There is no implemented combat, health/damage, inventory, interaction, abilities or save/load system to regression-test. Skill definitions correctly use shared ScriptableObjects with separate runtime state; persistence and actor-qualified events are still missing.

Neither gameplay scene previously instantiated a canonical gameplay player prefab. Both embedded equivalent player roots around the vendor hero visual. SampleScene alone is enabled in build settings; the intended Bootstrap/Sandbox loading flow is not implemented. Existing vendor convex-authoring assets contain unresolved script references; this work does not remove their data. See the audit for exact evidence and scope limits.

## 2. Movement issue

The old motor forced the player's spawn height to zero, overwrote capsule settings, used an overlong ground ray without slope/trigger filtering, delayed jumps in a coroutine and could cancel ascent through grounding. It discarded analog input magnitude and lacked explicit ceiling and terminal-velocity handling. Animation followed input rather than actual displacement. Redundant player mesh colliders competed with the capsule. Camera obstruction checks could include the player itself and smoothing could cross a wall.

The existing component and script GUID are retained. It now owns one CharacterController.Move per step, immediate jumps, bounded gravity, slope-aware nonallocating ground probes, acceleration/deceleration, camera-relative analog movement and smooth facing. Spawn placement and authored capsule dimensions are preserved at runtime. The existing camera caches its target and ignores the player's hierarchy during obstruction checks.

## 3. Animation issue

The previous controller used separate idle/walk/sprint/jump states plus an empty layer. Shift could leave the stationary player in sprint; falling and landing were not represented. No combat states, animation-event combat dependencies or intentionally root-driven exploration system were present. The controller asset identity is retained while its locomotion graph is rebuilt.

## 4. Kevin Iglesias assets selected

All paths below are relative to `Assets/Kevin Iglesias/Human Animations/Animations/Male/`:

| Asset | Purpose |
|---|---|
| `Idles/HumanM@Idle01.fbx` | Looping neutral idle |
| `Movement/Walk/HumanM@Walk01_Forward.fbx` | In-place walk; playback 0.8 for 1.6 m/s |
| `Movement/Run/HumanM@Run01_Forward.fbx` | In-place run at 4 m/s |
| `Movement/Sprint/HumanM@Sprint01_Forward.fbx` | In-place sprint at 6 m/s |
| `Movement/Jump/HumanM@Jump01 - Begin.fbx` | Nonlooping jump takeoff |
| `Movement/Jump/HumanM@Fall01.fbx` | Looping airborne fall |
| `Movement/Jump/HumanM@Jump01 - Land.fbx` | Nonlooping stationary landing |

These are actual imported Humanoid clips, referenced directly without duplication or vendor importer changes. The matching root-motion FBXs indicate nominal travel speeds of 2/4/6 m/s; walk cadence is scaled to the existing 1.6 m/s walk. Retargeted leg proportions still require visual foot-placement review. Forward locomotion matches the existing exploration policy of turning toward travel, including backward/sideways input. Directional combat strafing is not added without an existing combat mode.

The [complete FBX inventory](Genesis-Kevin-Animation-Inventory.csv) records all 409 FBXs, clip names, Humanoid settings, looping and root baking. The pack includes male/female locomotion, jumps, rolls, slides, knockdowns, stuns and social motions. No weapon attack set was found, so none is invented or connected to nonexistent combat.

## 5. Files changed

| File | Change |
|---|---|
| `Assets/Genesis/Character/PlayerController.cs` | Repair the existing authoritative capsule motor; expose measured movement state and a deterministic stepping entry point. |
| `Assets/Genesis/Character/PlayerMovementInput.cs` | Sample existing legacy axes/buttons once per frame; configurable run/sprint keys. |
| `Assets/Genesis/Character/PlayerAnimation.cs` | Read motor state after movement and update Animator parameters; enforce code-driven translation. |
| `Assets/Genesis/Character/ThirdPersonCamera.cs` | Preserve initial orientation, bind an unassigned target at spawn, filter player collisions and constrain smoothed camera placement. |
| `Assets/Genesis/Character/Animatinos/PlayerAnimator.controller` | Retain GUID/path; replace the audited locomotion-only graph with the Kevin blend tree and airborne states. |
| `Assets/Genesis/Character/Prefabs/GenesisPlayer.prefab` | Establish canonical gameplay prefab around the existing hero and Avatar; configure capsule, input, motor and animation adapter. No fixed prefab actor identity or scene camera reference. |
| `Assets/Scenes/Sandbox/Sandbox.unity` | Connect existing player to canonical prefab, retaining scene identity, placement, camera and NPC references. |
| `Assets/PolygonFantasyKingdom/Scenes/Demo_ExteriorOnly_Optimized.unity` | Connect its existing player to the same prefab and retain scene references/overrides. |
| `Assets/Genesis/Character/Editor/Genesis.Character.Editor.asmdef` | Isolate migration tooling from runtime builds. |
| `Assets/Genesis/Character/Editor/PlayerLocomotionMigration.cs` | Explicit, guarded Unity migration and scene/prefab/Avatar/reference validation. Does not run automatically. |
| `Assets/Tests/Genesis.Character.Tests.asmdef` | Add Unity Test Framework PlayMode test assembly. |
| `Assets/Tests/PlayerMovementTests.cs` | Nine native Unity physics/movement/camera regression cases. |
| `Assets/Tests/PlayerAnimationTests.cs` | Exercise actual prefab retargeting, blend selection, bone animation and airborne transition recovery. |
| `Assets/Docs/Genesis-Repository-Audit.md` | Audit findings and the implementation plan recorded before implementation. |
| `Assets/Docs/Genesis-Kevin-Animation-Inventory.csv` | Complete Kevin FBX metadata inventory. |
| `Assets/Docs/Genesis-Player-Movement.md` | Ownership, controls, setup and manual verification instructions. |
| `Assets/Docs/Genesis-Movement-Report.md` | This report. |
| `Validation/GenesisMovement/Motor-PlayMode.xml` | Recorded nine-test Unity PlayMode result. |

New Unity assets and folders also receive their corresponding `.meta` files. Existing script, Animator, scene and vendor asset GUIDs are preserved. Previous animation clips and vendor assets are retained; no old asset is deleted. Temporary copies and logs under ignored `Temp/` are validation work products, not shipped project content.

## 6. Animator changes

Controller: `Assets/Genesis/Character/Animatinos/PlayerAnimator.controller` (existing spelling and GUID retained). One Base Layer, with `Locomotion Speed` 1D tree at 0/1.6/4/6 m/s and Jump/Fall/Land states. Parameters: `Speed` (float, measured planar metres/second), `IsGrounded` (bool), `VerticalVelocity` (float). No sprint bool or redundant directional parameters.

Locomotion/Land enter Jump while rising and Fall while descending off ground. Jump enters Fall at its apex. Jump/Fall enter Land on ground contact. Moving landings recover immediately; stationary landings recover at 80% of the landing clip. Short interruptible 0.08-second blends avoid a landing input lock. Idle, walk, run, sprint and fall loop; takeoff/landing do not.

The existing Polygon Dungeon Realms `Characters.fbx` Humanoid Avatar is retained on `Chr_Hero_Male_01`. Kevin clips use their supplied HumanM source Avatar and retarget onto that hero. Selected clips bake root rotation and position; `Animator.applyRootMotion` stays false. Feet IK is enabled on states; there is no procedural terrain foot-placement solver.

## 7. Movement architecture

| Responsibility | Owner |
|---|---|
| Movement input | `PlayerMovementInput`, legacy Horizontal/Vertical/Jump and configured keys |
| Physical movement, jump, gravity, grounding | Existing `PlayerController`, using one CharacterController |
| Character rotation | `PlayerController`, facing travel direction |
| Animation parameters | `PlayerAnimation`, after the motor in Update |
| Animation blending/state transitions | Existing `PlayerAnimator.controller` |
| Mouse orbit and camera collision | Existing `ThirdPersonCamera`, LateUpdate |

Controls remain WASD/arrows/legacy joystick axes, Shift to run and Space to jump; Left Alt enables the available faster sprint. Existing input assets and bindings are unchanged. Alt is also a legacy Fire2 binding, so future combat integration should explicitly settle that currently unused overlap.

## 8. Testing

Nine movement tests passed in Unity 6000.0.35f1 PlayMode using native physics in an isolated validation project. [Recorded results](../../Validation/GenesisMovement/Motor-PlayMode.xml): spawn/capsule preservation; analog/diagonal camera-relative direction; cardinal movement, walk/run/sprint, stopping and walls; immediate jump/no air rejump/landing; ledges and terminal speed; ceiling/trigger handling; walkable versus steep slopes; 30/120 FPS travel consistency; and camera wall/player filtering.

Recovery completed on 2026-09-20 directly in the real workspace: both gameplay scenes passed Unity prefab/Avatar/clip/camera/NPC reference checks, and all three PlayMode animation tests passed. See the [recovery report](../../Validation/GenesisMovement/Recovery-Report.md) and [test results](../../Validation/GenesisMovement/Workspace-Animation.xml). The original interruption had left the old Animator assigned and the canonical prefab absent; these assets are now applied. Destination-only transition interruption fixes the takeoff restart found by the first real-workspace test run. Automated tests cannot establish subjective control feel or guarantee absence of visible foot sliding on every retargeted pose and terrain surface.

## 9. Remaining issues

Use the [manual Editor checklist](Genesis-Player-Movement.md) to review actual feel, fast facing changes, all animation blends, retargeted foot placement, slopes/steps and camera behaviour in both gameplay scenes. The successful real-workspace batch validation has no rendered visual review. No new combat or dodge behaviour is introduced: those systems are not implemented. Build/scene flow, persistent identity and serialization remain pre-existing foundation gaps. The audit also records existing vendor missing-script data and design-document contradictions.

## 10. Recommended next task

Follow the Design Bible's SDK and vertical-slice priority with a small, testable combat sandbox: authored action definitions, runtime health/stamina/damage and actor-qualified action/damage events integrated with this controller. Establish stable actor identity and versioned persistence before connecting durable world consequences. Do not begin the large world simulation before these foundations work.
