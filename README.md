# Project Genesis

A small game I'm working on.

Project Genesis is a Unity third-person action RPG prototype built around a persistent, living world. The project is currently focused on establishing its SDK and gameplay foundations through an existing playable Sandbox.

## Current stage

The project is at the **foundation and Sandbox prototype** stage. Player checkpoint save/load is implemented for the Sandbox. This is not yet a production vertical slice or a complete world persistence system.

## Open the project

1. Install Unity **6000.0.35f1** (the version used for the latest recorded validation).
2. Clone this repository and open its root folder in Unity Hub.
3. Open `Assets/Scenes/Sandbox/Sandbox.unity` to work with the existing playable Sandbox.
4. Let Unity resolve packages from `Packages/manifest.json` and import the project assets.

Generated Unity and IDE files such as `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `.csproj`, and `.sln` files are excluded from Git. Unity will recreate them locally.

## Implemented foundations

- Third-person player movement, crouching, camera, and animation foundations.
- Actor-owned inventory, equipment, modular appearance, and Sandbox inventory UI.
- Data-driven character-creation workflow and a predefined Sandbox starting profile.
- Partial skill progression, NPC perception, event, god-resonance, and authority systems.
- Single-slot player checkpoint in the Sandbox. **F5** saves and **F9** explicitly loads; **I** opens/closes inventory.
- Save data uses schema version 1, stable authored IDs, validation, a corruption checksum, atomic replacement, and a backup. Loading does not replay gameplay reward events.

The checkpoint only covers the player and related Sandbox progress. It does not persist NPCs, world simulation, other scenes, camera orbit, or settings. There is no autosave, cloud save, save selection UI, or schema migration yet.

## Validation

`Assets/Docs/Genesis-Save-Load-Report.md` documents the implementation, limitations, manual Editor checks, and validation evidence. It records **42/42 Play Mode tests passed** and real Sandbox checks in Unity 6000.0.35f1. Those are the report's results; running tests in Unity is still required to verify a different checkout or later code changes.

## Next work

1. Define durable actor and spawn identity, assigned before actors initialize.
2. Define a versioned world-state snapshot contract and the actors/simulation state it covers.
3. Extend persistence beyond the Sandbox player and add migrations when the format evolves.
4. Continue toward the design-bible vertical slice. Combat, full-world simulation/persistence, complete character-creation UI, and broad race visuals are not complete.

## Developer handoff

- Start with `CLAUDE.md` for the current stage, task order, and handoff notes.
- Read `Agent.md` and the relevant documents under `Assets/Docs/` before changing systems. The Design Bible is the design authority.
- Preserve Unity `.meta` files and GUID references; follow existing systems instead of creating parallel replacements.

## Project layout

- `Assets/Genesis/` — Genesis runtime modules and gameplay code.
- `Assets/Content/` — authored definitions and configuration.
- `Assets/Scenes/Sandbox/` — existing playable Sandbox scene.
- `Assets/Tests/` — Unity Play Mode tests.
- `Assets/Docs/` — design documents and implementation reports.
- `Validation/` — recorded test evidence and historical before-state snapshots.

