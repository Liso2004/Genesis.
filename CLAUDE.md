# Claude Code handoff — Project Genesis

## Current stage

Project Genesis is at the SDK/foundation and playable Sandbox prototype stage. The existing Sandbox now has movement and crouch, modular equipment and inventory, a data-driven character-creation foundation, skills and other partial runtime systems, and a tested single-slot player checkpoint. It is not yet a production vertical slice or a persistent living-world simulation.

The active work just completed is the Sandbox player save/load checkpoint. Read `Assets/Docs/Genesis-Save-Load-Report.md` for its design, implementation, test evidence, exact changed files, limits, and next work.

## Completed foundation

- Existing Sandbox movement, crouch, animation and camera foundations, with regression coverage.
- Inventory ownership, equipment, modular player appearance, and Sandbox inventory UI.
- Data-driven character-creation workflow and predefined Sandbox starting profile.
- Partial skill, NPC perception, event, god-resonance and authority foundations.
- Explicit F5 save / F9 load for one Sandbox player checkpoint. Schema v1, content/actor validation, checksum, atomic replacement and backup; no automatic load or reward replay.
- Save/load Play Mode validation: 42/42 tests passed in Unity 6000.0.35f1. The report records real Editor checks and evidence under `Validation/GenesisSave/`.

These are prototype foundations. Do not describe combat, full creation UI, broad race visuals, complete world simulation, or full-world persistence as done.

## Next stage

1. Define durable spawn/actor identity and assign IDs before actors initialize.
2. Define a versioned world-state snapshot contract, including which actors and simulation state are in scope.
3. Extend persistence beyond the Sandbox player only after those identity and ownership rules are settled; add schema migration when a later schema requires it.
4. Continue toward the design-bible vertical slice in small, testable steps. The current player checkpoint is the tested baseline, not the finished world-save system.

## Instructions for continuing

- Read `Agent.md` first. It records project constraints and says to read design/system documentation before code changes.
- Project documentation is under `Assets/Docs/` (the instruction file's older `Docs/` path does not match this checkout). Read the relevant design bible and system reports before implementation; treat the Design Bible as the design authority.
- Preserve Unity `.meta` files and GUID references. Keep authored definitions separate from mutable runtime state. Extend existing systems instead of creating parallel replacements.
- The save report claims 42/42 Play Mode tests passed in the real Unity Editor. Do not claim new tests or Editor checks unless you run them.
- Use Unity 6000.0.35f1 as the recorded Editor version. Open `Assets/Scenes/Sandbox/Sandbox.unity` for the existing playable Sandbox.

## Repository notes

- Unity project source is in `Assets/`, `Packages/`, and `ProjectSettings/`; generated `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `.csproj` and `.sln` files are excluded by `.gitignore`.
- `Validation/` contains prior-state backups and evidence. Treat `Before/` snapshots as historical evidence, not current source assets.
- No Git remote is configured yet. Add the intended hosted repository as `origin` before pushing.
