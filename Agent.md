# AGENTS.md

## Project Genesis

Project Genesis is a Unity/C# third-person action RPG combined with a persistent living-world simulation.

Before modifying code, assets, prefabs, scenes, Animator Controllers, ScriptableObjects, or project settings, you MUST read the project documentation located in:

`Docs/`

The documentation defines the intended game systems, architecture, lore, world simulation, gameplay philosophy, vertical-slice scope, and art direction.

Do not treat the repository code as the only source of truth.

---

# 1. REQUIRED DOCUMENT READING

At the beginning of a new task, inspect the `Docs/` directory.

Read the relevant documents before making changes.

If present, prioritize documents in approximately this order:

1. `Docs/Genesis-Design-Bible.*`
2. `Docs/Genesis-World-Lore.*`
3. `Docs/Genesis-Art-Production.*`
4. Any architecture documentation
5. Any system-specific documentation
6. Any implementation notes
7. Any TODO or roadmap files relevant to the task

File extensions may differ.

Documents may be `.md`, `.txt`, `.pdf`, or another readable format.

Do not assume exact filenames if they differ from the examples above.

Inspect the actual contents of `Docs/`.

---

# 2. SOURCE OF TRUTH

The Project Genesis Design Bible is the primary design authority.

Use the following priority when information conflicts:

1. Explicit instructions from the current user/task
2. Current Project Genesis Design Bible
3. System-specific documentation under `Docs/`
4. World Lore documentation
5. Existing architecture documentation
6. Existing implementation
7. Reasonable engineering assumptions

If code conflicts with the Design Bible, do not automatically change the code.

First identify the conflict.

Explain:

- what the documentation requires;
- what the current implementation does;
- why they differ;
- what should be changed.

Do not silently reinterpret the Design Bible.

---

# 3. DO NOT INVENT PROJECT REQUIREMENTS

Do not invent:

- lore;
- factions;
- races;
- abilities;
- skills;
- gods;
- quests;
- locations;
- gameplay mechanics;
- architectural requirements;
- asset names;
- animation names.

If information is missing, state that it is missing.

Use the documentation and repository as evidence.

Do not replace Project Genesis concepts with generic RPG systems unless explicitly requested.

---

# 4. CORE GENESIS DESIGN PRINCIPLES

Preserve the following principles unless explicitly instructed otherwise.

## Living World

The world exists independently of the player.

Civilizations, economies, politics, ecology, NPCs, factions, and history should be capable of changing without direct player involvement.

The player participates in the world.

The player is not the center of all world simulation.

## Persistent Consequences

Meaningful actions or failures should be capable of changing:

- NPCs;
- relationships;
- settlements;
- factions;
- economies;
- world state;
- history;
- quests;
- future opportunities.

Do not automatically reset meaningful consequences simply to preserve a predefined story.

## Player Agency

Do not silently alter outcomes in order to force a preferred narrative.

The Director may curate opportunities.

It must not fabricate impossible events or invalidate legitimate player decisions.

## Dungeon Philosophy

The Dungeon is not only an enemy-filled level system.

It acts as a learning environment.

Dungeon floors should introduce, test, reinforce, or combine gameplay principles.

## Skill Philosophy

Skills improve through meaningful use, experimentation, challenge, and mastery.

Avoid simple repetition-only progression where possible.

---

# 5. ENGINEERING RULES

Prefer data-driven systems.

Use ScriptableObjects for authored definitions where appropriate.

Examples may include:

- skills;
- items;
- abilities;
- NPC archetypes;
- factions;
- Trials;
- quests;
- event definitions;
- enemies;
- status effects.

Do NOT store mutable runtime state directly inside shared ScriptableObject definition assets.

Separate:

`Definition / authored data`

from:

`Runtime state / instance data`

Keep simulation logic separated from presentation wherever practical.

Examples:

Simulation:

- NPC goals
- faction state
- economy
- world events
- history
- relationships
- skill progression

Presentation:

- Animator
- UI
- particle effects
- sounds
- visual feedback
- scene presentation

Do not introduce Unity ECS/Entities solely because the project contains many NPCs.

Use ECS only if profiling demonstrates that it is justified.

Avoid premature optimization.

Profile first.

---

# 6. EXISTING CODE FIRST

Before creating a new system:

1. search the repository;
2. find existing implementations;
3. inspect related scenes/prefabs;
4. inspect ScriptableObjects;
5. inspect references;
6. inspect existing documentation.

Do not create:

`PlayerController2`

`NewPlayerController`

`BetterMovement`

`EventSystemNew`

or similar replacement systems simply because the existing implementation is difficult to understand.

Prefer repairing or extending existing architecture when appropriate.

Create a replacement only when there is a clear architectural reason.

Explain that reason.

---

# 7. UNITY PROJECT SAFETY

Do not casually modify or delete Unity `.meta` files.

Do not rename/move Unity assets without considering GUID references.

Before modifying a prefab, identify whether it is the canonical prefab.

Before modifying scene objects, determine whether they are prefab instances.

Do not make a scene instance the only working version of a system if the intended source of truth is a prefab.

Preserve references unless they intentionally need to change.

Check dependencies before deleting assets.

---

# 8. ASSET INSPECTION

Never assume the contents of an asset folder from its name.

Inspect the actual files.

For models/animations, inspect:

- FBX files;
- animation clips;
- Avatar configuration;
- rig type;
- Animator Controllers;
- Blend Trees;
- animation masks;
- prefabs;
- root-motion settings;
- looping settings;
- root transform settings.

Do not invent asset names.

Use only assets that actually exist.

---

# 9. PLAYER MOVEMENT AND ANIMATION

The current player movement/animation system is an important near-term task.

A newer set of character/animation assets exists under:

`Assets/Kevin Iglesias`

When working on player movement or animation:

1. inspect the existing player first;
2. identify the canonical player prefab;
3. trace player input;
4. trace movement;
5. trace rotation;
6. trace camera-relative movement;
7. trace gravity/grounding;
8. trace the Animator;
9. inspect combat dependencies;
10. inspect `Assets/Kevin Iglesias`.

Do not simply replace the old Animator without understanding the existing system.

Determine whether the Kevin Iglesias animations are:

- in-place;
- root-motion;
- Humanoid;
- Generic.

There must be one authoritative movement system.

Avoid both the Animator and movement code moving the player simultaneously unless intentionally designed that way.

Prevent:

- foot sliding;
- doubled movement;
- jitter;
- rotation conflicts;
- Animator snapping;
- broken combat transitions.

Do not break combat solely to update locomotion.

---

# 10. CODING STYLE

Follow existing namespaces and folder conventions where sensible.

Prefer clear responsibilities.

Avoid giant scripts that own:

- movement;
- input;
- combat;
- animation;
- inventory;
- skills;
- save/load;
- simulation

all at once.

At the same time, avoid unnecessary abstraction.

Do not split simple functionality into dozens of tiny classes unless there is a meaningful architectural benefit.

Prefer understandable Unity/C# code.

---

# 11. PERFORMANCE RULES

Avoid unnecessary expensive operations inside:

`Update()`

`FixedUpdate()`

or frequent simulation loops.

Avoid repeated runtime calls such as:

`GameObject.Find`

`FindObjectOfType`

`FindAnyObjectByType`

`GetComponent`

when the reference can reasonably be cached.

Avoid unnecessary per-frame allocations.

Do not optimize speculative bottlenecks before profiling.

---

# 12. TESTING

Major systems should be testable.

When modifying a system:

- check existing tests;
- update affected tests;
- add tests where practical;
- check for regressions.

For Unity gameplay changes, also identify manual Editor testing that must be performed.

Never claim something was tested if it was not actually tested.

Clearly distinguish between:

- automated tests performed;
- compile/build checks;
- static inspection;
- manual tests that the developer still needs to perform in Unity.

---

# 13. CHANGE SCOPE

Do not perform unrelated refactors while completing a focused task.

Do not clean the entire repository because you discovered technical debt.

Report unrelated problems separately.

Keep changes scoped.

A task to fix player movement should not also redesign:

- factions;
- economy;
- NPC AI;
- skill progression;
- save/load;
- world generation

unless a direct dependency makes it necessary.

---

# 14. BEFORE EDITING

For any substantial task, first inspect:

- relevant documentation;
- relevant scripts;
- related ScriptableObjects;
- related prefabs;
- related scenes;
- related Animator Controllers;
- related input configuration;
- related tests.

For complex tasks, provide a brief assessment before making large architectural changes.

---

# 15. AFTER EDITING

After completing a task, report:

## Files changed

List every relevant file changed.

## Changes made

Explain what was changed and why.

## Architecture

Explain which components now own important responsibilities.

## Documentation compliance

Mention which Genesis documentation or principles affected the implementation.

## Testing

Explain what was tested.

## Manual Unity checks

List anything that must still be verified in the Unity Editor.

## Remaining issues

Mention known limitations or technical debt.

Do not hide unresolved issues.

---

# 16. PROJECT PHASE

Project Genesis is currently focused on establishing the Genesis SDK/foundation before expanding into full production.

Prioritize stable foundations such as:

- Unity project architecture;
- player movement;
- combat foundation;
- player animation;
- core data definitions;
- world state;
- event/state framework;
- NPC simulation;
- skills;
- quest seeds;
- Director prototype;
- save/load;
- testing.

Do not attempt to build the entire world simultaneously.

Build systems that can later support expanding content.

---

# 17. VERTICAL SLICE

The vertical slice focuses on a dense playable subset of Project Genesis rather than the full world.

Before implementing vertical-slice content, read the exact vertical-slice requirements in the Design Bible and related documentation.

Do not assume every planned full-game system needs to be completed immediately.

Prioritize the systems necessary to prove the core Genesis gameplay loop.

---

# 18. DOCUMENTATION UPDATES

If an implementation meaningfully changes how a system works, update relevant technical documentation when appropriate.

Do not rewrite lore/design documents merely to make them match an implementation mistake.

If implementation and documentation disagree, flag the disagreement.

---

# 19. PRIMARY RULE

Before changing Project Genesis:

**Read the docs.
Inspect the existing implementation.
Understand the dependencies.
Then make the smallest correct change.**
