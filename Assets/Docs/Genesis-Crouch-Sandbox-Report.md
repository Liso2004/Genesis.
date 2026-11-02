# Genesis crouch and Sandbox implementation

2026-09-25. Implemented and inspected in the real `C:/Users/lisow/Genesis` Unity 6000.0.35f1 workspace through Unity MCP. Existing scene edits were saved before changes; backups and evidence are under `Validation/GenesisCrouch`.

## 1. Selected player character

Retained the existing Polygon Dungeon Realms male hero. This already integrates with the canonical player and has a lean, valid Humanoid rig. Imported Modular Fantasy Hero presets were also inspected: the sampled animated presets contain 719 optional skinned renderers. Switching to their modular hierarchy offered no advantage for this crouch task.

## 2. Exact asset paths

- Visual: `Assets/PolygonDungeonRealms/Prefabs/Characters/Chr_Hero_Male_01.prefab`.
- Canonical gameplay prefab: `Assets/Genesis/Character/Prefabs/GenesisPlayer.prefab`.
- Humanoid model/Avatar source: `Assets/PolygonDungeonRealms/Models/Characters/Characters.fbx`.
- Hip-mounted presentation sword: `Assets/Synty/PolygonFantasyHeroCharacters/Prefabs/Weapons/SM_Wep_Sword_01.prefab` under the removable `PresentationWeapon` mount. No inventory or attack behavior is attached.

## 3. Rig type

Unity Humanoid. Live inspection confirmed the selected Animator's Avatar is valid and human. Existing skeleton, bone mapping and model are preserved.

## 4. Avatar configuration

The hero uses its own existing Avatar, retargeting compatible Kevin Iglesias Humanoid clips. Root motion is disabled; the CharacterController remains the only player movement authority. Imported clips and Avatar assets were referenced directly, with no duplicated clips or guessed GUIDs. Live screenshots show different standing/crouch bone poses without model scaling.

## 5. Animator Controller

Modified existing `Assets/Genesis/Character/Animatinos/PlayerAnimator.controller`, preserving its GUID and assigned prefab reference. Existing Locomotion, Jump, Fall and Land remain. Added Crouch with a `Crouch Speed` 1D Blend Tree. Parameters: Speed (float), IsGrounded (bool), VerticalVelocity (float), IsCrouching (bool). Crouch thresholds: 0 and 0.8 m/s. Grounded posture transitions take 0.1 seconds and use destination interruption, preserving the previous fix for endlessly restarting source transitions. Land chooses crouch or standing locomotion from the authoritative posture. Crouching off a ledge enters Fall.

The earlier interrupted locomotion migration had already been applied before this crouch work. Current inspection found the intended controller, hero Avatar, canonical prefab and standing locomotion clips present. This task extends those assets rather than rerunning or replacing that migration.

## 6. Actual animation clips

All paths below are relative to `Assets/Kevin Iglesias/Human Animations/Animations/Male/`.

| Purpose | Actual FBX |
|---|---|
| Idle | `Idles/HumanM@Idle01.fbx` |
| Walk | `Movement/Walk/HumanM@Walk01_Forward.fbx` |
| Run | `Movement/Run/HumanM@Run01_Forward.fbx` |
| Sprint | `Movement/Sprint/HumanM@Sprint01_Forward.fbx` |
| Jump | `Movement/Jump/HumanM@Jump01 - Begin.fbx` |
| Fall | `Movement/Jump/HumanM@Fall01.fbx` |
| Land | `Movement/Jump/HumanM@Jump01 - Land.fbx` |
| Crouch idle | `Movement/Crouch/HumanM@Crouch01_Idle.fbx` |
| Crouch walk | `Movement/Crouch/CrouchWalk/HumanM@Crouch01_Walk_Forward.fbx` |

The imported Kevin basic-motion demo and Synty character demo/controller assets were inspected through AssetDatabase without replacing the gameplay scene with a demo.

## 7. Crouch selection

The last two clips in the table are real imported looping Humanoid animations, with zero average root speed. Sampled crouch lowers the hero's head from about 1.57 m to 1.02 m above the feet. The matching `Movement/Crouch/CrouchWalk/RootMotion/HumanM@Crouch01_Walk_Forward [RM].fbx` was measured at 1.897688 m/s solely to calibrate the in-place gait: playback rate 0.8 / 1.897688 = 0.421566. It is not assigned as movement/root motion.

## 8. C input

Serialized `KeyCode.C` on existing PlayerMovementInput uses `Input.GetKeyDown`. Its Frame carries one ToggleCrouch request. The project already uses this legacy reader; no second input framework or action reader was added. Releasing C leaves posture unchanged. A rejected stand attempt consumes that press; another press is required after leaving the obstruction.

## 9. PlayerController changes

Added authoritative IsCrouching, configurable crouchSpeed (0.8 m/s) and crouchedHeight (1.25 m), TryToggleCrouch and CanStand. Crouch overrides run/sprint speed and prevents jump. Existing acceleration, camera basis, rotation, gravity and CharacterController.Move path remain. Live stair testing exposed loss of stepping at capsule edge contact; native stepping now also recognizes descending CharacterController ground contact without granting jump eligibility on steep slopes. Added a stair regression test.

## 10. CharacterController changes

Caches original height 1.9 m and centre (0, 0.95, 0). Crouch height 1.25 m and centre (0, 0.625, 0) preserve the capsule base. Radius 0.2 m, skin 0.03 m and model scale remain. Step offset is capped to fit the shorter capsule. Dimensions were chosen after inspecting the actual hero and sampling the actual crouch animation.

## 11. Ceiling detection

Physics.OverlapCapsuleNonAlloc queries the proposed standing capsule using configured collision layers. It ignores own colliders, triggers, ignored layers and ignored collision pairs, accounts for world scale, and uses the existing skin tolerance to avoid treating the supporting floor as a ceiling. A saturated 32-entry buffer refuses standing. The scene's 1.42 m ceiling blocks a 1.9 m stand while allowing the 1.25 m crouch.

## 12. Camera

Existing ThirdPersonCamera retained. A cached motor reference controls configurable crouchHeightReduction (0.65 m), smoothly applied to framing. Existing orbit, follow and obstruction logic remain.

## 13. Sandbox scene

Extended the existing gameplay scene `Assets/Scenes/Sandbox/Sandbox.unity`; no new gameplay scene was created. The older exterior scene also received the existing GenesisRuntime component to satisfy the shared player skill dependency. The addition is `Genesis Crouch Courtyard`, at world (45, -2.16, 10). Existing world/player/camera/runtime/test guard roots remain. Select the courtyard root and press F to find it.

Includes open area, imported castle gate/walls, enterable house with roof, castle room, two market stalls, imported stairs, simple dungeon entrance/corridor, trees, rock and paths. Dedicated course: A standing approach; B 1.42 m low ceiling; C 1 m narrow passage; D six 0.18 m steps; E open area. Primitive test collision geometry complements the imported art.

Imported environment paths are recorded literally in `CrouchSandboxSetup.cs`, including Kingdom Castle/House/Props/Environments prefabs and Dungeon Realms Dwarf doorway/floor/rock prefabs. The massive vendor demo scenes were not instantiated.

## 14. NPCs

Added NPC_Merchant, NPC_Soldier and NPC_Blacksmith beneath the courtyard. Their imported prefabs are:

- `Assets/PolygonFantasyKingdom/Prefabs/Characters/SM_Chr_Merchant_01.prefab`
- `Assets/PolygonFantasyKingdom/Prefabs/Characters/SM_Chr_Soldier_Male_01.prefab`
- `Assets/PolygonFantasyKingdom/Prefabs/Characters/SM_Chr_Blacksmith_Male_01.prefab`

Each uses existing GenesisActor, SkillSystem, NPC_Setup, NPCPerception and NPCController. New NPCAnimation presents achieved CharacterController velocity through the shared locomotion controller with root motion disabled. Existing perception/line-of-sight and movement code were not replaced. NPC_Setup is also reused on the player to register its own Perception skill; its legacy name does not imply a new NPC controller on the player. Skill runtime state remains per actor.

## 15. Assets intentionally not used

No old animation assets were deleted. Modular hero presets remain available; the existing hero already retargets cleanly. RM clips are not movement drivers. Demo character controllers, static character variants, massive demo environments, vehicles, siege systems and unrelated assets were not added. No combat, inventory, networking, advanced AI or new simulation system was introduced.

## 16. Files modified/created

Paths below are relative to the project root. Existing user changes outside this list were not reset.

| File | Change |
|---|---|
| `Assets/Genesis/Character/PlayerMovementInput.cs` | C toggle request in existing input frame |
| `Assets/Genesis/Character/PlayerController.cs` | Crouch posture/speed/capsule/clearance/jump gate; stair contact fix |
| `Assets/Genesis/Character/PlayerAnimation.cs` | Hashed IsCrouching synchronization |
| `Assets/Genesis/Character/ThirdPersonCamera.cs` | Cached posture and smooth crouch framing |
| `Assets/Genesis/NPC/NPCAnimation.cs` (new) | Animation presentation for existing NPC motor |
| `Assets/Genesis/Character/Editor/CrouchSandboxSetup.cs` (new) | Explicit guarded authoring of existing controller/prefab/Sandbox |
| `Assets/Genesis/Character/Animatinos/PlayerAnimator.controller` | Added crouch blend/state/transitions and playback calibration |
| `Assets/Genesis/Character/Prefabs/GenesisPlayer.prefab` | Crouch settings, own SkillSystem/Perception setup, modular sword |
| `Assets/Scenes/Sandbox/Sandbox.unity` | Preserved original roots; added courtyard/course/three NPCs |
| `Assets/PolygonFantasyKingdom/Scenes/Demo_ExteriorOnly_Optimized.unity` | Added existing GenesisRuntime for shared player skills; serialized camera crouch framing default |
| `Assets/Genesis/Character/Materials/Courtyard Stone.mat` (new) | Test environment material |
| `Assets/Genesis/Character/Materials/Crouch Test Marker.mat` (new) | Test area marker material |
| `Assets/Genesis/Character/Editor/Genesis.Character.Editor.asmdef` | Editor authoring references NPC and Skill |
| `Assets/Tests/Genesis.Character.Tests.asmdef` | Integration test references Core, Skill and NPC |
| `Assets/Tests/PlayerAnimationTests.cs` | Runtime fixture for prefab's per-actor skills |
| `Assets/Tests/PlayerCrouchTests.cs` (new) | Four crouch/input-contract/animation/skill tests |
| `Assets/Tests/PlayerMovementTests.cs` | Stair-edge regression test |
| `Assets/Docs/Genesis-Player-Movement.md` | Updated controls, responsibilities, graph and Sandbox notes |
| `Assets/Docs/Genesis-Crouch-Sandbox-Report.md` (new) | This report |

Unity generates corresponding `.meta` files for new scripts, tests, report, material folder and materials. Validation captures/backups are outside Assets under `Validation/GenesisCrouch`; local MCP helper/cache files are under Temp/GenesisMCP. Unity-generated solution/project/cache files are tooling outputs, not authored gameplay changes. The older exterior gameplay scene inherits the shared player prefab/controller changes and now contains an enabled GenesisRuntime root. Saving that scene also serialized the camera crouchHeightReduction default (0.65).

## 17. Assemblies

Only the two editor/test assembly definitions listed above changed. Genesis.Core, Genesis.Character, Genesis.Skill and Genesis.NPC runtime assembly definitions were preserved. No GUIDs were fabricated or replaced.

## 18. Console and compilation

Unity compiled successfully. Final live Sandbox Console query returned **0 errors**, including no compiler, Animator parameter, Avatar, missing-reference or runtime exception errors. Evidence: `Validation/GenesisCrouch/Console-Final-MCP.json`. The final successful Test Runner job is `d278306f5a67423092b548a236ee3d1a`.

Two MCP Test Runner initialization attempts timed out before starting tests; retries completed. An early test fixture rejected an expected Perception registration log; that expectation was corrected. Neither issue remains in the final 17-test pass.

## 19. Playtest status and evidence

**Automated and live MCP acceptance checks passed.** The final real-project PlayMode run passed **17/17 tests**, zero failures/skips: 10 movement/camera tests, 3 existing animation tests, and 4 crouch/integration tests. Evidence: `Validation/GenesisCrouch/Final-PlayMode-MCP.json`.

- Movement tests cover camera-relative analog/diagonal input, all directions, walk/run/sprint, stopping, wall contact, rotation, grounding, slopes, jump, ledges/fall/landing, ceiling impact, frame-rate consistency and stair traversal.
- Animation tests cover actual prefab/Avatar retargeting and moving bones, idle/walk/run/sprint parameters and clips, jump/fall/land recovery, crouch idle/walk, and landing into crouch. Root motion remains disabled.
- Crouch tests cover feet-preserving capsule changes, toggle/release behavior, blocked stand, no crouched jump, reduced speed despite run/sprint requests, crouched tunnel exit, and independent actor Perception skill state/progression.
- Live existing Sandbox checks: roof blocks standing and jumping while crouched; leaving it allows standing (`Live-Ceiling-MCP.json`). Narrow passage traverses successfully (`Live-Course-MCP.json`). The initial stair failure recorded in that historical file was fixed and the actual staircase then passed (`Live-Stairs-Final-MCP.json`).
- Live NPC check: Soldier sees the player, loses sight behind a temporary obstruction, and reacquires after removal (`Live-NPC-LOS-MCP.json`). Temporary test objects and runtime teleports were discarded on leaving Play Mode.
- **Actual keyboard C test passed**, with the Unity Game View foreground process verified before input: first C crouches (height 1.25, Animator bool true), second C stands (height 1.9), C beneath the low ceiling remains crouched, and C after leaving stands (`Live-Keyboard-C.json`). The initial sandboxed keyboard attempt could not access desktop focus; the guarded host-desktop test succeeded.
- Visual captures inspected: `Player-Standing.png`, `Player-Crouching.png`, and `Courtyard-Game.png`, all in `Validation/GenesisCrouch`. These confirm actual humanoid posture changes and the saved environment, beyond parameter-only assertions.

Unity is left in Edit Mode in the existing Sandbox. No required manual asset assignment remains. Further artistic polish, long-session camera/foot-contact feel assessment and expanded playtesting remain normal iteration work; exact foot locking across every surface is not claimed. Existing NPC AI remains basic. The original test guard's older setup was preserved. Build entry/scene flow and full save/load remain pre-existing SDK work.

The next SDK task should establish explicit bootstrap/scene entry and save/load contracts for stable actor IDs and independent skill state, following the Design Bible. This recommendation does not implement new world simulation.

## Compatibility follow-up completed

The previously queued MCP operation completed and saved the exterior scene on 2026-09-25 at 20:34 local time. The saved scene now contains exactly one enabled GenesisRuntime root using the actual Core script GUID, and retains the canonical player prefab reference. The saved-file diff contains only that root/root-list entry and the camera crouch framing default. Evidence: `Exterior-Saved-Validation.json` and `Exterior-Runtime.diff` in Validation/GenesisCrouch.

On resuming, Unity MCP confirmed Sandbox is the only loaded scene, active and saved, and the Console returned zero errors. The saved compatibility fix is complete. Further dynamic execute_code calls (including an optional exterior preview inspection) timed out; those attempts are not reported as successful tests. The exterior scene was checked by its saved Unity serialization; the 17 PlayMode tests and live keyboard/scene checks described above were performed in the real project/Sandbox. No additional exterior gameplay playtest is claimed.
