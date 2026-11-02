# Prototype polish, inventory and creation foundation

Validated in the real project `C:\Users\lisow\Genesis`, Unity 6000.0.35f1, through Unity MCP on 2026-09-27/28. The existing `Assets/Scenes/Sandbox/Sandbox.unity` remains the gameplay scene. No new gameplay or character-creation scene was created.

## Scope and completion

Stages A and B were implemented and validated before applying Stage C. Stage C follows the attached **IMPORTANT IMPLEMENTATION BOUNDARY**: build the underlying creation architecture and use a predefined Sandbox profile. This is not a claim that the future cinematic, full creation UI, living-world simulation, or all future acceptance criteria are implemented.

The Design Bible, World Lore, Art Production document and both attached prompts guided this work. Authored definitions remain ScriptableObjects; actor inventory, equipment, skill progress and creation sessions remain runtime state. No permanent class, forced Guild membership, new input framework or movement controller was introduced.

## Stage A: existing Sandbox

- Dressed the existing courtyard with discovered Synty cliffs, mountains, trees, bushes, paths, workbench, crates, barrels and lanterns. Kept the existing surrounding scene.
- Organized the equipment and supplies stations; retained the existing training, observation, Observer shrine and authority stations. Added five armor pickups in Stage B.
- Hid unnecessary debug labels and raw movement-course renderers while preserving their collision geometry. Decorative imported meshes do not replace the tested collision volumes.
- Adjusted daylight, ambient light, fog and the existing camera's serialized framing settings.
- Preserved the low ceiling, narrow passage, stairs and open movement area. Standing capsule height is 1.9 m; crouching is 1.25 m; test ceiling clearance is 1.42 m. Crouched movement is 0.8 m/s.
- Preserved all four existing NPCs: NPC_TestGuard, NPC_Soldier, NPC_Blacksmith and NPC_Merchant.

This remains a small gameplay prototype. It is not a production-quality recreation of the reference artwork. Background dressing has deliberately limited collision coverage.

## Stage B: inventory and modular player

`I` opens/closes the actual Canvas in Sandbox; Escape and the Close button also close it. The panel captures and restores cursor, camera, movement-input and interaction state. Gravity continues through the existing motor. Inventory and equipment state remain owned by ActorInventory, never by the UI.

Single-click selects an item. The selected-item panel shows name, category, quantity, slot, status and description. A large Equip/Unequip button requests the existing equipment operation. All eight slot labels show equipped or empty state; occupied slots select their item. Actions show brief feedback. Soul Coins have a persistent total using the project's existing inventory-backed currency representation. There was no separate currency service to duplicate. Healing Potion is displayed with a disabled Use action because consumption is not connected.

Optional filtering, double-click equip, hover tooltips and a separate character-preview camera were deferred. Placeholder initials are used where items have no authored icon.

The canonical player is `Assets/Genesis/Character/Prefabs/GenesisPlayer.prefab`. Its visual child is the new `Assets/Genesis/Character/Prefabs/GenesisModularHuman.prefab`, derived from the inspected `Assets/Synty/PolygonFantasyHeroCharacters/Prefabs/ModularCharacters.prefab`. The imported source assets were not altered. The original player visual remains inactive for reference.

The Avatar is the valid Humanoid Avatar imported from `Assets/Synty/PolygonFantasyHeroCharacters/Models/ModularCharacters.fbx`. The generated derivative excludes the source demo's CharacterRandomizer. Body identity is independent of equipped layers. Removing gear restores the selected head, hair, facial hair and base clothing. Helmet uses `Chr_HeadCoverings_No_Hair_04`; cape uses `Chr_BackAttachment_03`. Weapon attachment compensates for the source skeleton's 0.01 bone scale, preventing tiny weapons.

The existing `Assets/Genesis/Character/Animatinos/PlayerAnimator.controller` was retained, including its GUID and parameter names: Speed, IsGrounded, VerticalVelocity and IsCrouching. No Animator Controller was created or modified during these A/B/C stages. Root motion is disabled. PlayerMovementInput owns input; PlayerController owns CharacterController movement and rotation; PlayerAnimation writes Animator parameters. Neither PlayerController nor ThirdPersonCamera was rewritten.

Actual retained Kevin clip names are HumanM@Idle01, HumanM@Walk01_Forward, HumanM@Run01_Forward, HumanM@Sprint01_Forward, HumanM@Jump01 - Begin, HumanM@Fall01, HumanM@Jump01 - Land, HumanM@Crouch01_Idle and HumanM@Crouch01_Walk_Forward. Exact FBX paths and import findings are in `Genesis-Crouch-Sandbox-Report.md`.

## Stage C: creation foundation

The session enforces:

`New Game → Lore Intro → Race → Appearance → Personality Questions → Starting Skills → Unique Awakening → Character Summary → Enter Aethel`

The lore introduction is authored text for a future presentation layer. Five Bible races are authored: Human, Beastfolk, Ash-Kin, Root-Gnawers and Weavers. Beastfolk includes Canine, Feline, Ursine, Avian and Reptilian. Only Human currently has a compatible visual prefab. The other four races remain selectable in the data workflow, but applying them to an actor fails explicitly until appropriate visuals exist.

Human appearance currently supports two inspected male heads, two hair options, optional eyebrows and one optional facial-hair piece, plus the established body. Equipment pieces are rejected as identity options. Female bodies, material customization and broader race visuals are not implemented by this foundation.

Six authored situational questions contribute to multiple personality traits. Let Fate Decide can skip the questions; Surprise Me uses random starting Basic selection. Guided, Choice and Random are supported. Choice offers five candidates and selects three. Eight Basic candidates reuse Perception and Swordsmanship and add authored Dodge, Appraise, Track, Forage, Light and Sneak definitions.

Unique awakening is a single weighted random draw after Basic selection, using personality, race and optional resonance hints. It cannot be directly selected or rerolled within the session. The prototype pool contains Great Sage, Predator, Defender and Cartographer. Weights are tunable prototype balancing, not new lore or permanent race restrictions. Similar profiles can receive different awakenings with different seeds; this does not provide global multiplayer uniqueness.

PlayerCreationProfile is an immutable initial snapshot referencing authored definitions. Existing SkillSystem owns subsequent progress. Bootstrap applies once to a new empty actor and rejects existing belongings or learned skills rather than clearing them. The player's previous automatic Perception/scenario registration is disabled only for the creation-configured player; NPC initialization remains enabled.

The actual Sandbox preset is Human / Sandbox Wanderer, seed 2, Perception + Swordsmanship + Dodge, and Great Sage. It starts with zero owned items, zero equipment, Allegiance None and Reputation Unknown. Its deterministic test seed keeps the existing Perception and sword-practice stations usable; normal creation does not search for a preferred Unique outcome.

New skill definitions and racial traits do not implement their full gameplay abilities or racial modifiers. No faction/reputation simulation, save/load adapter, full new-game UI, cinematic or settlement system was added. Initial resonance hints influence selection without granting god favor.

## Validation and evidence

- Unity compilation completed successfully. Final Console: zero errors, zero warnings; zero missing scripts. Sandbox was saved and left in Edit Mode. See `Validation/GenesisPolish/Final-status.json`.
- 35/35 Play Mode tests passed, zero failed/skipped, MCP job `a1a9ee5a28f84c90b07e4db3bdf1419b`. This includes movement/grounding/crouch, existing systems, equipment ownership and restoration, and eight creation tests.
- Creation tests cover step ordering, no direct Unique selection, seeded reproducibility and variation, Fate/Random, offered Basic choices, all five races/sub-races, immutable answers, equipment exclusion from identity, resonance weighting and one-time actor application.
- Real Sandbox fresh-start inspection confirmed exactly four registered skills, zero inventory and zero equipment: `StageC-fresh-start.json`.
- Actual scene pickups and the UI button callback passed equip/unequip/ownership/visible armor/restoration checks for sword, shield, helmet, chest, hands, legs and cape: `StageC-equipment-regression.json`. Transient action feedback and restored input were also checked after the final UI change.
- Previous live I-key open/close test passed using keyboard input; cursor, camera and input restored: `StageB-keyboard.json`. Full outfit and coin/potion checks: `StageB-equipment.json` and `StageB-validation.txt`.
- Real Sandbox motor/Animator sampling measured walk 1.6, run 4 and sprint 6 m/s, matching Speed. Bone rotations changed; crouch parameter and actual crouch idle clip matched the shorter capsule: `StageC-animation.json`.
- Actual Jump → Fall → Land → grounded locomotion clips were observed: `StageC-airborne.json`. Root motion remains off.
- The live crouch course passed blocked standing, blocked crouch jumping, movement under the ceiling, standing after exit, normal jumping and landing: `StageA-runtime.json`.
- Four NPCs retained Perception; scene missing-script count was zero. `StageC-runtime.json` is an inspection taken after runtime gear collection, not the fresh-start result.
- Visual evidence inspected: `StageA-overview.png`, `Inventory-visible.png`, `Full-Outfit-Verified.png`, and `Creation-Base-Appearance.png`. The older inventory screenshot predates the final description/brief-feedback text cleanup. Earlier exploratory screenshots in the same folder are not final acceptance evidence.

These are automated and MCP-driven runtime checks, not a claim that every possible mouse interaction, camera angle, terrain edge or human feel judgment has been exhaustively playtested. A user feel/readability pass at the intended Game View resolution remains useful. No standalone player build was produced.

## Changed files

Scene/prefabs:

- `Assets/Scenes/Sandbox/Sandbox.unity`: dressing, station layout, lighting/camera settings, modular player bindings, inventory Canvas/EventSystem, armor pickups, creation registration settings.
- `Assets/Genesis/Character/Prefabs/GenesisPlayer.prefab`: modular visual/Animator bindings, equipment appearance reference, creation preset/bootstrap, player-only legacy skill-registration opt-out.
- `Assets/Genesis/Character/Prefabs/GenesisModularHuman.prefab` (new): pruned compatible Synty visual, existing Animator, modular identity/equipment parts and appearance options.

Runtime code:

- `Assets/Genesis/Character/ModularAppearance.cs` (new): identity validation and equipment visibility composition.
- `Assets/Genesis/Character/PlayerMovementInput.cs`: input suspension while UI owns focus.
- `Assets/Genesis/Items/InventoryState.cs`: idempotent unequip and actor-scoped event, preserving ownership.
- `Assets/Genesis/Items/ItemDefinition.cs`: authored visible/hidden modular part IDs.
- `Assets/Genesis/Items/EquipmentPresentation.cs`: modular appearance layers, unequip refresh, correct scaled bone attachments.
- `Assets/Genesis/Gameplay/Sandbox/InventoryPanel.cs` (new): inventory view, selection/actions, feedback and focus restoration.
- `Assets/Genesis/Gameplay/Sandbox/SandboxActor.cs`: open inventory at equipment stand and optional scenario skill initialization.
- `Assets/Genesis/Gameplay/Sandbox/SandboxStation.cs`: equipment station opens the panel.
- `Assets/Genesis/Gameplay/Sandbox/SandboxFeedback.cs`: compact instructions including inventory key.
- `Assets/Genesis/NPC/NPC_Setup.cs`: serialized Perception auto-registration switch, default enabled for NPCs.
- `Assets/Genesis/Skills/Runtime/SkillSystem.cs`: read-only learned-skill ID enumeration for safe creation validation.
- `Assets/Genesis/Creation/RaceDefinition.cs` (new): race/sub-race definitions and pending visual status.
- `Assets/Genesis/Creation/CreationDefinition.cs` (new): authored questions, candidate pools, weights and creation enums.
- `Assets/Genesis/Creation/PlayerCreationSession.cs` (new): ordered, seeded creation workflow.
- `Assets/Genesis/Creation/PlayerCreationProfile.cs` (new): immutable initial snapshot and summary.
- `Assets/Genesis/Creation/PlayerCreationPreset.cs` (new): authored Sandbox test input.
- `Assets/Genesis/Creation/PlayerCreationBootstrap.cs` (new): safe one-time application and awakening event.

Editor code:

- `Assets/Genesis/Gameplay/Sandbox/Editor/SandboxPolishSetup.cs` (new): guarded existing-scene dressing with backups.
- `Assets/Genesis/Gameplay/Sandbox/Editor/PrototypeInventorySetup.cs` (new): modular prefab, equipment assets and scene bindings.
- `Assets/Genesis/Gameplay/Sandbox/Editor/PrototypeInventoryView.cs` (new): actual Canvas hierarchy and styling.
- `Assets/Genesis/Gameplay/Sandbox/Editor/CreationFoundationSetup.cs` (new): creation definitions, options, preset and player setup.

Assemblies/tests:

- `Assets/Genesis/Creation/Genesis.Creation.asmdef` (new): Core, Character, Skill and Items dependencies.
- `Assets/Genesis/Gameplay/Sandbox/Genesis.Sandbox.asmdef`: Unity.ugui reference.
- `Assets/Genesis/Gameplay/Sandbox/Editor/Genesis.Sandbox.Editor.asmdef`: Unity.ugui and Genesis.Creation references.
- `Assets/Tests/Genesis.Character.Tests.asmdef`: Genesis.Creation reference.
- `Assets/Tests/InventoryEquipmentTests.cs` (new): unequip, swapping/isolation, full outfit restoration and weapon scale.
- `Assets/Tests/PlayerCreationTests.cs` (new): eight creation/data/runtime tests.
- `Assets/Tests/PlayerCrouchTests.cs`: remove obsolete player NPC-setup log expectation after creation takes initial skill ownership.

Authored assets:

- New `Assets/Content/Sandbox/Systems/Item-training-{head,chest,hands,legs,cape}.asset`: five actual modular armor definitions, including visibility rules and display references.
- Existing `Item-iron-sword.asset`, `Item-blackwood-shield.asset`, `Item-healing-potion.asset`, `Item-soul-coins.asset` in that directory: readable item descriptions.
- New `Assets/Content/Creation/Race-{human,beastfolk,ash-kin,root-gnawers,weavers}.asset`: Bible race definitions.
- New `Assets/Content/Creation/Skill-{dodge,appraise,track,forage,light,sneak,great-sage,predator,defender,cartographer}.asset`: Basic/Unique definitions.
- New `Assets/Content/Creation/GenesisCreation.asset` and `SandboxStartingProfile.asset`: catalog/configuration and test inputs.
- Unity generated corresponding `.meta` files and new-folder metadata. Existing imported asset GUIDs were preserved.

Supporting artifacts: this report, `Validation/genesis_mcp.py` (local MCP validation client), and the evidence/backups in `Validation/GenesisPolish`. Backups include the previous on-disk scene, the scene with pre-existing unsaved Editor changes, and the previous canonical player prefab. They are not alternative gameplay assets.

## Next work

Review movement feel and UI readability in the intended display resolution. Then add a versioned persistence adapter for initial identity plus existing actor runtime state before expanding into the final new-game UI. Keep unsupported race visuals and unimplemented skill effects explicit; do not imply that authored definitions provide abilities already.
