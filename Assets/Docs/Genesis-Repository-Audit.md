# Genesis repository audit — 2026-09-12

## Scope and authority

Read `Agent.md`, the complete Design Bible, World Lore and Art Production documents in this directory, all 24 C# sources, module definitions, project/input/build settings, player scene hierarchies and their asset references. Inventoried 21,107 asset files including metadata, 11 scenes, 3,301 prefabs, 3 controllers and 9 standalone clips. Imported meshes/textures are inventoried and reference-checked; this is not a visual review of every environment model. Read the Kevin Iglesias 2.5 PDF animation catalogue and inspected FBX importer settings. The archive `Assets/Genesis.zip` contains identical copies of the current C# sources and empty planned module folders; it is not another compiled implementation.

The Design Bible is authoritative. Its Part 15 prioritizes SDK foundations and defines the slice as Millford and Floors 1–10. Goliath is also listed on Floor 17 elsewhere in the Bible and Lore; the Art Production counts also conflict (210 versus 162). These require a later design clarification, not changes during locomotion work.

## Architecture and system status

| Area | Evidence and status |
|---|---|
| Engine/modules | Unity 6000.0.35f1, URP 17.0.3. Four assemblies: Core; Character; Skill → Core; NPC → Character/Skill. GameObject architecture; no ECS. Many future module folders are empty. |
| Runtime/events | `GenesisRuntime` initializes a persistent singleton and typed, synchronous `GenesisEventBus`. Subscribe/unsubscribe, duplicate suppression and snapshot dispatch are implemented; exception isolation and automated tests are absent. Each publish allocates a delegate array. Profile before changing it. |
| Actor state | `GenesisActor` stores type and ID; blank IDs become random GUIDs in Awake. Scene player ID includes trailing whitespace. Authored IDs can duplicate when cloned; random IDs do not survive reload without persistence. |
| Player/input | One custom `PlayerController`; legacy Horizontal/Vertical axes, Shift and Space. Both input backends enabled. The newer InputSystem_Actions asset has Player/UI maps but is not consumed by Genesis player/camera code. Keep existing bindings while repairing movement. |
| Movement | CharacterController, camera-relative direction, Slerp facing, raycast grounding and delayed jump coroutine. No Rigidbody motor, slope projection, acceleration, terminal fall speed, dodge or combat locomotion. Structurally repairable. |
| Camera | `ThirdPersonCamera` handles mouse orbit and spherecast collision in LateUpdate. Explicit scene target. Smoothing after obstruction calculation can put the camera inside obstacles; the collision mask includes the player. No runtime prefab target binding. |
| Animation | PlayerAnimator in `Character/Animatinos` uses idle/walking/Sprint/Jump plus an empty second layer. Speed is input magnitude rather than achieved movement; IsRunning selects Sprint. No Blend Tree, Fall or Land. Root motion is disabled on the player model scene override. |
| Combat/damage/health | No implementation in C# or player components. Combat folder is empty. No attack animation events to preserve in the old player clips. This cannot be reported as a passing combat regression. |
| Skills/abilities | Partial per-actor SkillSystem, two authored ScriptableObject definitions, six mastery stages, typed events and in-memory runtime state. Only Perception content. No active ability execution, meaningful-context validation, Trials, evolution or Authorities. Arbitrary repeated progress values can reach Innovation, short of the Bible's mastery requirements. |
| NPCs | Partial range/FOV/occlusion perception against one assigned target, direct chase/rotation and gravity. NPC_Setup registers Perception. No navigation planning, utility goals, memory, relationships, relevance tiers or independent simulation. NPCPerceptionCapability has no code or serialized callers; do not delete on this evidence alone. |
| Simulation/world/Director/quests | Empty module/content folders, not functional systems. No clock, persistent world model, economy, event chains, quest seeds or Director. |
| Save/load | Absent. Skill dictionaries are not serialized; SkillRuntimeState's private fields have no SerializeField despite Serializable. Events omit actor identity, making persistence and multi-actor history ambiguous. No schema version or migrations. |
| Shared assets | SkillDefinition and SkillProgressionConfig contain authored data only; mutable progression is correctly separate. Vendor demo intentionally edits shared materials at runtime and resets them; do not reuse that presentation script as game logic. |
| Tests/docs | Test Framework 1.4.5 installed but Assets/Tests is empty. Core/skill smoke components log results instead of assertions. DebugSkillBoost is actually attached in Sandbox and grants 100 × 50 progress at Start. Missing architecture, persistence contracts, movement and automated test documentation. |
| Scene flow | SampleScene alone is enabled in build settings; Bootstrap and Sandbox are not. No scene-loading code. Bootstrap contains runtime and smoke tests; Sandbox has runtime and NPC. Do not silently change the build entry scene in a movement task. |

No major gameplay system is established as complete or production-ready. The event-bus primitives, authored skill definitions and mastery threshold processing are implemented pieces of a partial foundation.

`SkillRuntimeState1` is an empty placeholder with no code callers or serialized references found. `NPCPerceptionCapability` similarly has no callers; its hardcoded mastery multipliers are not connected to the live perception component. Neither is deleted or labeled conclusively abandoned. The two scene-authored player configurations and the archived Genesis sources are the concrete duplication found. SkillSystem's dependency on the runtime singleton and MonoBehaviour, and the original motor's direct input/Animator ownership, are the main coupling points.

The serialized GUID scan found pre-existing imported-content problems: 949 references to missing script GUID `5b71ad40e238046238f9b0c6f33c3791` in convex-collider authoring assets, with no matching asset or installed-package metadata. Investigate the missing authoring tool separately; do not discard embedded collision data. References absent from Assets are not automatically broken: player material GUID `31321ba15b8f8eb4c954353edc038b1d` resolves to URP's package-owned Lit material. Other unresolved imported-pack references remain outside this movement migration.

## Movement execution and defects

`Update → HandleMovement → CheckGround → legacy input → camera-plane direction → RotateToward → HandleJump/coroutine → CharacterController.Move → UpdateAnimation (reads input again)`.

Awake forcibly sets world Y to zero and overwrites authored capsule settings. This breaks elevated spawns and future save restores. The downward ray extends about 0.8 m from near the origin, ignores slope normal and allows triggers. Grounding remains true during early ascent, suppressing gravity; repeated jump requests can occur near the floor. Jump is artificially delayed. Upward velocity is not cleared after a ceiling hit. Direction normalization discards analog magnitude. Speed changes are instantaneous. Input-based animation keeps walking against walls. Sprint has no direct stop transition while Shift stays held. Jump exits on stale grounded state; ledges have no falling state.

## Canonical player and dependencies

No gameplay player prefab exists before this task. The two player roots are ordinary scene objects in `Scenes/Sandbox/Sandbox.unity` and `PolygonFantasyKingdom/Scenes/Demo_ExteriorOnly_Optimized.unity`, with matching component IDs/configuration. Each contains ModelRoot and the nested vendor `PolygonDungeonRealms/Prefabs/Characters/Chr_Hero_Male_01.prefab`. That vendor prefab is the visual source, not a gameplay player. It uses the Humanoid Avatar in `PolygonDungeonRealms/Models/Characters/Characters.fbx`.

Player root has CharacterController, GenesisActor, PlayerController, an orphan MeshRenderer and an additional static MeshCollider using the character mesh; the visual has an added empty MeshCollider. Camera targets and NPC target refer to scene transforms. Preserve these local reference IDs when connecting a gameplay prefab. Do not edit the vendor visual prefab or replace the character's own Avatar with an animation-source Avatar.

## Asset availability

Kevin Iglesias Human Basic Motions 2.5 contains 404 animation FBXs, two humanoid models, three demo prop/base models, nine Avatar masks, two demo character prefabs and one demo Animator Controller. Male/female sets include idle variations; eight-way walk/run; five-way sprint; strafe walk/run; jump begin/full/land and Fall01; turns; roll and run-slide; crouch/swim; knockdown fall/ground/stand-up and stun; social/emotion and interaction poses. There are drowning/dead-in-water poses, but no general land death or weapon-attack set. Pain01 is a social emotion, not a verified combat hit reaction. Do not substitute these for missing combat design.

Selected candidate clips: HumanM@Idle01, HumanM@Walk01_Forward, HumanM@Run01_Forward, HumanM@Sprint01_Forward, HumanM@Jump01 - Begin, HumanM@Fall01, HumanM@Jump01 - Land. These are Humanoid imports, bake rotation/Y/XZ into pose, use original root orientation/position, and have no events. Idle/walk/run/sprint/fall loop; begin/land do not. Matching [RM] variants exist and are not selected. Retarget to the existing hero Avatar; actual validity, stride speed and foot contact require Unity evaluation.

## Short implementation plan

1. Repair the existing motor; preserve spawn transforms and authored capsule settings, use immediate jumps, collision-aware grounding/gravity and achieved velocity.
2. Separate input sampling and Animator presentation into small components. Keep one CharacterController.Move owner and disable Animator root motion.
3. Retain the PlayerAnimator asset GUID, replace the verified locomotion-only graph with a speed Blend Tree and jump/fall/land states. Reference original Kevin FBX clips without copying them.
4. Establish a Genesis player prefab from the matching scene setup, preserving hero appearance/Avatar and external scene targets. Remove only redundant player collision components, not old assets.
5. Compile and run movement/Animator/reference checks; document unperformed interactive checks explicitly.

After stabilization, build the Bible's combat sandbox with data-authored actions, damage/health/stamina, explicit actor-qualified events and tests. Resolve stable actor IDs/versioned persistence before claiming persistent consequences.
