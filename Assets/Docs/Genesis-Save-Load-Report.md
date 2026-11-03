# Sandbox player checkpoints

## Camera and sword-combat follow-up (2026-11-03)

The player camera now starts in close third person, with a wider third-person view and first-person view available by cycling with **V**. First-person hides the player's renderers and restores their prior visibility when leaving that view. The camera keeps collision checks and adjusts framing while crouching. The Sandbox and optimized exterior demo scene camera settings were updated. `ThirdPersonCamera.FollowTarget` had a duplicate local `rotation` declaration removed after a compiler error.

Sword-combat support has been added as code and imported animation assets. `PlayerSwordCombat` maps left click to a three-step light combo, **F** to a heavy attack, and held right mouse to block while a weapon is equipped. **Genesis > Character > Setup Sword Combat Animations** has now run successfully in Unity. It built the masked Animator layer and its armed idle, three light attacks, heavy stab, and block states; the canonical prefab references the combat component and controller, with its Humanoid Avatar validated and root motion disabled. The setup script's entry-transition cleanup was corrected to use Unity's `AnimatorTransition` API after a compiler error. The Editor log records successful compilation and the setup completion message. Play Mode input and visual animation playback still need a live check; Unity MCP's console/state requests currently time out while pinging the Editor.

Current stage: camera POV implementation and sword Animator/prefab wiring are in the project. Next, verify the POV cycle and equipped/unequipped light combo, heavy attack, and block actions in Play Mode. These follow-up changes have not been covered by the earlier checkpoint test results below.

Implemented and validated through Unity MCP in the real Genesis workspace on 2026-09-28, Unity 6000.0.35f1. The existing `Assets/Scenes/Sandbox/Sandbox.unity` is configured and saved. No new gameplay scene, player controller, inventory authority or skill system was created.

## Use

- Focus the Game View and press **F5** to save the current checkpoint.
- Press **F9** to explicitly restore it. Loading replaces current progress with the selected checkpoint.
- Save while grounded; crouching is supported. Saving during a jump/fall is rejected.
- The HUD displays controls and success/failure messages. Inventory continues to use **I**.
- There is no automatic save/load, scene switch or silent new-game fallback.

Current slot: `C:/Users/lisow/AppData/LocalLow/DefaultCompany/Genesis/Genesis/sandbox-player-v1.json`. Runtime code derives this from `Application.persistentDataPath`; it does not hard-code the Windows user path. The keyboard playtest created a checkpoint containing the collected test gear and earned Observer blessing. A fresh Play session still begins unequipped until F9 is pressed.

Each successful replacement keeps the previous file as `.bak`. A damaged primary file is reported; the backup is not loaded silently. To recover manually, stop Play Mode, preserve the damaged primary elsewhere, and copy the verified `.bak` over the primary. The backup uses the same validated format.

## Saved state

The schema stores stable authored IDs and primitive values, not Unity object references:

- Immutable creation identity: name, race/sub-race, initial appearance, answers/personality, initial Basic Skills, awakened Unique Skill, resonance hints and generation metadata. Loading restores the recorded awakening rather than rerunning selection.
- Current modular appearance, independently from the initial profile and equipped layers.
- Inventory ownership and stack counts, including existing inventory-backed Soul Coins; equipped item IDs.
- Every learned skill's mastery stage, residual progress and meaningful-use count, including skills learned after creation.
- Domain progress, god resonance/favor, opportunity/claimed blessing state, authority conditions/opportunities.
- Sandbox meaningful-use history and all ten pickup collection flags, preventing restored rewards from becoming collectible again.
- Player position, normalized rotation and crouch posture. Old movement velocity is cleared on restore.

The ancillary progress and pickup fields preserve dependencies of the requested player state. They do not implement full world simulation or NPC persistence.

## Architecture and validation

`SaveCatalog` is authored data resolving existing item, skill, progression, race and authority definitions by stable IDs. The generated Sandbox catalog uses the existing Perception progression configuration; saving rejects a runtime skill bound to a different progression configuration. Authored IDs must remain stable. Catalog expansion is required when new content is added.

`PlayerSaveData` is schema version 1. The file envelope has a format version, JSON payload and SHA-256 corruption checksum. This checksum detects accidental corruption; it is not authentication or anti-cheat protection. Files are capped at 4 MiB. Writes flush a sibling temporary file and atomically replace the primary, retaining the previous primary as backup. If replacement is unsupported or fails, an error is reported rather than falling back to destructive overwrite.

`CheckpointValidation` resolves and checks the complete snapshot before mutation: versions, actor/scene, IDs, duplicates, stack limits, ownership/slot conflicts, appearance categories, Basic/Unique tiers, question/personality values, finite numeric state, skill thresholds, domain/god state, station IDs and meaningful-use contexts. Missing/renamed content or unsupported versions fail explicitly. No version migration exists yet.

`SandboxSaveController` is a scene-level coordinator on the existing player. It caches components once, reads F5/F9 through the established legacy input API and calls each existing runtime owner to restore its own state. It is intentionally configured in Sandbox only: scene station references do not belong in the reusable player prefab. `SandboxSaveSetup` is the explicit editor authoring command, not an automatic startup migration.

The scene's existing actor ID (`player_001 `, including its existing trailing space) is preserved exactly. Saves reject a different actor ID; they do not rewrite it underneath systems that have cached it. The shared prefab's blank ID still produces independent actor IDs when instantiated. A future spawning/save-slot layer must assign durable IDs before those actors initialize.

Loading does not emit new item-added, skill-use, mastery, domain, favor or blessing reward events. After the complete restore it publishes one actor-scoped `ActorStateRestoredEvent`; equipment presentation rebuilds visual layers and attachments, and the inventory UI clears stale selection and refreshes from restored state. Current inventory focus/cursor ownership is preserved.

PlayerController received only checkpoint-placement helpers: validate the proposed capsule against current obstructions, restore posture/capsule dimensions, clear old velocity and probe grounding. Existing input, movement, rotation, jump and crouch algorithms remain intact. A newly obstructed destination fails before restoring state. Camera orbit is not serialized; the existing camera follows the restored player normally.

## Verification

**42/42 Play Mode tests passed**, zero failed/skipped. MCP test job: `38c97aabd93140a2a0b7b30b4feacd38`. The existing 35 movement/crouch, equipment, creation and systems tests remain green. Seven new tests cover:

1. Inventory/equipment/skill/appearance round trip and no reward-event replay.
2. Unknown schema, actor, scene and asset rejection without state mutation.
3. Invalid ownership, stack count, skill progress/duplicates and equipment-as-identity rejection.
4. Missing/corrupt files leave the actor unchanged.
5. Atomic replacement retains a readable previous checkpoint; checksum detects changed payload.
6. Obstructed placement and airborne saves are rejected.
7. Crouch restores the 1.25 m capsule and clears old motion.

Final real-Editor inspection: compilation complete, **0 Console errors, 0 warnings, 0 missing scripts**. The existing Sandbox is saved and left in Edit Mode, with the catalog assigned and all 17 station references present.

Real Sandbox validation, across two separate Play sessions:

- First session collected all ten actual pickups, equipped all seven supported gear types, observed all three real targets and claimed the earned Observer blessing.
- A fresh second session began empty; loading restored a byte-equivalent payload after excluding the capture timestamp. It restored ten inventory entries, all ten collection flags, three meaningful-use records, seven equipped items and the blessing.
- Zero gameplay reward events replayed during load.
- All ten pickups refused recollection. An already observed target was reachable, but refused a second progression award; meaningful-use count remained unchanged.
- Actual keyboard **F5** created the standard slot. After changing equipment and progress, actual **F9** restored an exact checkpoint match.
- Inspected the restored inventory UI and equipped humanoid visually. The Avatar remained valid; root motion remained off. After loading, measured walking speed and Animator Speed matched at approximately 1.6 m/s, and leg bones animated. Input, camera and inventory close behavior remained enabled.

Unity stalled during an assembly reload before the initial test attempt. The saved Editor was restarted, MCP reconnected, and the successful tests/runtime checks above were performed afterward. This was not counted as a passing test attempt. No claim is made that the underlying Editor/package reload stall itself was fixed.

Evidence is under `Validation/GenesisSave`: `Cross-session-result.json`, `Keyboard-result.json`, `No-duplicate-rewards.json`, `Post-load-movement.json`, `Before-restart.json`, `cross-session.json`, `Inventory-after-load.png`, `Character-after-load.png`, `Tests.json` and `Final-status.json`. `Before/Sandbox.unity` preserves the pre-save-feature scene. No standalone player build was produced.

## Files changed

New:

- `Assets/Genesis/Save/Genesis.Save.asmdef`: save module references existing runtime modules and the Sandbox adapter.
- `Assets/Genesis/Save/SaveCatalog.cs`: authored ID/definition resolution.
- `Assets/Genesis/Save/PlayerSaveData.cs`: versioned data-only snapshot structures.
- `Assets/Genesis/Save/SaveFileStore.cs`: checksummed, bounded, atomic files and backup.
- `Assets/Genesis/Save/CheckpointValidation.cs`: complete preflight and resolved definitions.
- `Assets/Genesis/Save/SandboxSaveController.cs`: capture, explicit save/load, validated commit and input.
- `Assets/Genesis/Core/Runtime/Events/ActorStateRestoredEvent.cs`: actor-scoped completed-restore notification.
- `Assets/Genesis/Skills/Runtime/State/SkillCheckpoint.cs`: validated skill state data.
- `Assets/Genesis/Gameplay/Sandbox/Editor/SandboxSaveSetup.cs`: guarded setup of the existing Sandbox.
- `Assets/Content/Creation/SandboxSaveCatalog.asset`: actual discovered content references.
- `Assets/Tests/PlayerSaveTests.cs`: seven checkpoint tests.
- This report and supporting validation artifacts; Unity generated `.meta` files for new assets.

Modified:

- `Assets/Scenes/Sandbox/Sandbox.unity`: save coordinator, catalog and all existing station references. No prefab or Animator Controller was modified for this task.
- `Assets/Genesis/Character/PlayerController.cs`: checkpoint pose validation/restoration only.
- `Assets/Genesis/Character/ModularAppearance.cs`: copy current identity for capture.
- `Assets/Genesis/Creation/PlayerCreationProfile.cs`: constructor accessible to the validated checkpoint resolver; still copies collections and holds no future progress.
- `Assets/Genesis/Creation/PlayerCreationBootstrap.cs`: restore already-resolved identity without re-awakening/re-registering skills.
- `Assets/Genesis/Items/InventoryState.cs`: detached validated inventory/equipment reconstruction without acquisition events.
- `Assets/Genesis/Items/ActorInventory.cs`: atomically install both reconstructed states.
- `Assets/Genesis/Items/EquipmentPresentation.cs`: refresh equipment on completed restore.
- `Assets/Genesis/Skills/Runtime/State/SkillRuntimeState.cs`: capture and assembly-owned reconstruction.
- `Assets/Genesis/Skills/Runtime/SkillSystem.cs`: validate/restore learned states and resolve progression configuration.
- `Assets/Genesis/Skills/Runtime/SkillDomainSystem.cs`: capture/restore domain progress without replay.
- `Assets/Genesis/Gods/Runtime/GodResonanceSystem.cs`: validated relationship reconstruction and restore.
- `Assets/Genesis/Authorities/Runtime/AuthoritySystem.cs`: capture/restore conditions and opportunities.
- `Assets/Genesis/Gameplay/Sandbox/MeaningfulUseTracker.cs`: copy/restore novelty history.
- `Assets/Genesis/Gameplay/Sandbox/SandboxActor.cs`: expose actor-owned history restoration and reset practice cooldown.
- `Assets/Genesis/Gameplay/Sandbox/SandboxStation.cs`: capture/restore pickup collection status and visibility.
- `Assets/Genesis/Gameplay/Sandbox/InventoryPanel.cs`: refresh restored state without stale selected items.
- `Assets/Genesis/Gameplay/Sandbox/SandboxFeedback.cs`: F5/F9 instructions and sufficient HUD height.
- `Assets/Genesis/Gameplay/Sandbox/Editor/Genesis.Sandbox.Editor.asmdef`: Genesis.Save authoring dependency.
- `Assets/Tests/Genesis.Character.Tests.asmdef`: Genesis.Save test dependency.

## Limits and next work

This is a single-slot Sandbox checkpoint, not a complete world save system. NPC memories/positions, simulation clocks, combat transients, other scenes, camera orbit and user settings are not captured. Unsupported scene/content changes are rejected rather than silently losing state. No cloud saves, autosave, save selection UI or historical schema migration was added.

Next, define stable spawn identity and a versioned world-state snapshot contract before calling the entire living world persistent. The existing player checkpoint provides the tested baseline for that work or for the future New Game/Continue interface.
