# Genesis systems Sandbox

Completed in the real Unity project through Unity MCP on 2026-09-26. The previous crouch task was completed before this systems extension. See `Genesis-Crouch-Sandbox-Report.md` for its complete character, Avatar, clip, controller, camera, collision, NPC, and scene report.

The existing `Assets/Scenes/Sandbox/Sandbox.unity` remains the development scene. The prior hero, Kevin Iglesias Humanoid locomotion/crouch Animator, CharacterController, camera, crouch course, and NPCs are retained. No new gameplay scene or controller framework is introduced.

## Controls and scenario

- WASD: move; Shift: run; Left Alt: sprint; Space: jump; C: toggle crouch.
- E: interact with the nearest visible station in range.
- Left mouse: practice at the dummy while standing, grounded, and holding Iron Sword.
- F1: expand the small test readout (owned items, equipment, mastery, Observer relationship, and event count).

Collect Iron Sword, Blackwood Shield, Healing Potion, Soul Coins, and Ash-Kin's Ember. Equip owned sword/shield at the equipment stand. Practice from different sides of the dummy for meaningful Swordsmanship/Warfare use. Observe three distinct targets for Perception, Knowledge/Curiosity progress, and Observer resonance/favor. Accept earned Insight at the shrine; visiting alone grants nothing. The Authority gate reports missing Pride/Humility mastery and the Trial of Understanding condition.

## Ownership and architecture

Definitions are ScriptableObjects; actor components own all mutable session state. Item definitions describe identity, stack limits, slot, and presentation. InventoryState stores bounded stacks, EquipmentState checks ownership, and EquipmentPresentation attaches imported visuals to Humanoid hand bones. Actors share definitions but never inventory, skill, domain, favor, or eligibility state.

SkillSystem remains the existing per-actor MonoBehaviour. Skill events now carry an optional Source reference, preserving old constructors while allowing domain/God listeners to filter by actor. SkillDomainSystem consumes authored weighted mappings. GodResonanceSystem computes capped resonance/favor, exposes an earned blessing opportunity, and requires explicit acceptance. AuthoritySystem evaluates authored skill mastery and conditions, emits eligibility once, and does not activate reality-altering powers.

Mastery stages and authored skill tiers are distinct. Mastery gain does not automatically evolve Basic into Extra/Unique. Meaningful-use novelty is enforced by the Sandbox interaction layer: a given observation counts once per actor session; dummy practice counts once per approach quadrant, with a cooldown. Range, view direction, line of sight, equipment, grounding, and crouch checks gate actions. Repeated interactions cannot farm these test contexts indefinitely.

Eight God definitions use the Bible's pantheon and domains. Only the Observer has an interactive test mapping and blessing here. Pride (Sovereign's Will) and Humility (Servant's Heart) are requirement definitions, not automatically learned player skills. The positive Authority branch is exercised by tests using explicit fulfilled conditions; the ordinary Sandbox does not manufacture a completed Trial.

## Imported presentation proxies

The actual imported paths, Unity GUIDs, renderer counts, collider counts, and dimensions are recorded in `Validation/GenesisSystems/Imported-Assets.json`. Genesis identities come from ItemDefinition, not the proxy mesh name.

- Iron Sword: `Assets/Synty/PolygonFantasyHeroCharacters/Prefabs/Weapons/SM_Wep_Sword_01.prefab`.
- Blackwood Shield: the same Weapons directory, `SM_Wep_Shield_01.prefab`.
- Healing Potion: `Assets/PolygonFantasyKingdom/Prefabs/Items/SM_Item_Bottle_01.prefab` (temporary bottle proxy).
- Soul Coins: `Assets/PolygonDungeonRealms/Prefabs/Props/SM_Prop_Gold_Coins_01.prefab`.
- Ash-Kin's Ember: `Assets/PolygonFantasyKingdom/Prefabs/Items/SM_Item_Crystal_01.prefab` (temporary crystal proxy, not a finished canonical necklace).
- Dummy and rack: Kingdom `Props/BattleGround/SM_Prop_Dummy_Armour_01.prefab` and `SM_Prop_Weapon_Rack_01.prefab`.
- Shrine: Kingdom Angel statue and plinth as architectural proxies; no claim that the statue is a finished Observer character model.
- Authority gate: Dungeon Dwarf single doorframe, with a prototype light cue.

## Scope and limitations

This is a compact foundation and test scenario inspired by the supplied references, not a reproduction of their entire landscapes. No full inventory UI, world map, regional cities, multiplayer, deity character models, complete Trial system, all Sins/Holies, or reality-warping effects are included. Potion consumption, currency spending, dropping items, save/load, and full combat remain future work. Session state currently resets on leaving Play mode; assets remain unchanged by play.

No compatible imported sword attack clip was found in the searched animation assets. Dummy practice therefore validates a prototype action and pulses the target; it does not claim a complete animated melee strike. Existing locomotion and crouch clips are preserved.

## Validation

Final real-project PlayMode run: **24/24 passed**, zero failures/skips. Evidence: `Validation/GenesisSystems/PlayMode-24-Passed.xml`. All 17 prior movement, animation, crouch, camera, grounding, and stair regression tests still pass, plus seven systems tests.

- `Scene-Preflight.json`: saved existing Sandbox, 12 stations, four existing NPC controllers (three courtyard fantasy NPCs and the original guard), valid Humanoid Avatar, assigned canonical Animator, root motion disabled, no missing scripts, and no duplicate player components.
- `Live-All-Pickups.json`: all five actual scene pickups reachable and collected; inventory contains one sword, shield, potion, Ember, and 25 Soul Coins.
- `Live-Scene-Flow.json`: real scene station interactions and physics visibility checks, equipped sword/shield, varied practice accepted/repeated context rejected, all three observations accepted/repeats rejected, 30 Observer favor, blessing denied before earning and accepted afterward, 1.15 observation range multiplier, and missing Authority prerequisites correctly reported. This check uses actual interaction methods while positioning the player at each station through MCP; it does not claim continuous manual traversal.
- `Live-Keyboard-E-Mouse-C.json`: real Windows keyboard/mouse input, guarded by Unity foreground PID. E collects sword/shield and equips them; left click earns one practice use; C crouches with the Animator bool true and C again stands. An optional extended keyboard-only run aborted before sending input when Unity lost foreground focus. The complete logical scene flow and all pickups were verified separately through MCP, as above.
- Both negative and positive Authority outcomes, single event emission, separate actor state, bounded inventory/equipment ownership, non-finite progress rejection, and observation range/FOV/LOS/novelty are covered by tests. The positive Authority test seeds an explicit test-only completed condition; ordinary shrine interaction does not grant it.
- Visual screenshots: `Sandbox-overview.png`, `Player-equipped.png`, `Equipment-inspection.png`, and `Equipment-aligned.png`. Inspection caught incorrect imported hand orientation; both item definitions now author local Z rotation -90 degrees. The final aligned preview was inspected before saving those values.

The initial 23/24 run caught a missing domain reference created during asynchronous asset imports. The authoring command now caches created assets and imports existing files before resolving them. The incomplete scene setup was resumed, its duplicate added component overrides removed, and the corrected references retested. No original scene objects or old animation assets were deleted.

Unity is left in Edit Mode in the existing saved Sandbox. No manual asset assignment is required. Prototype visuals and animation coverage remain as described in Scope and limitations.

## Next foundation task

Persist actor-owned runtime state with stable actor/content IDs and versioned snapshots. Round-trip tests should cover inventory/equipment, skill mastery, domain progress, God relationships, accepted blessings, Authority opportunities, and meaningful-use history before expanding the world simulation.


## Files and assembly changes

`Validation/GenesisSystems/Changed-Files.txt` lists exact files, including Unity metadata. New `.meta` files are Unity-generated identities/import settings for the corresponding new scripts, folders, and authored assets; no imported asset GUID was fabricated.

- `GenesisPlayer.prefab`: adds ActorInventory, EquipmentPresentation, SkillDomainSystem, GodResonanceSystem, and AuthoritySystem. Existing actor, CharacterController, controller/input, animation, skill setup, model, Avatar, camera target, and Animator are retained. The previous decorative hip sword object is retained; equipment presentation hides it at runtime and displays owned/equipped hand visuals.
- `Sandbox.unity`: adds SandboxActor, SandboxInteractor, and SandboxFeedback to the existing player instance; adds the 12 stations and border props under the existing courtyard; assigns all scenario/interaction references; colors the courtyard ground with a separate grass material; moves the scene player start into the courtyard and offsets its existing camera accordingly. Existing course, buildings, dungeon entrance, market, stairs, paths, NPCs, and older world geometry remain.
- `SkillDefinition.cs` and new `SkillMetadata.cs` / `SkillDomainDefinition.cs`: authored tier/category/series/icon/domain metadata, separate from runtime mastery.
- `SkillSystem.cs`: exposes registered definitions, rejects non-finite progress, supplies actor Source on events. Existing mastery logic remains.
- `SkillMeaningfulUseEvent.cs` / `SkillMasteryChangedEvent.cs`: optional Source property/constructor argument for backwards-compatible actor-scoped listeners.
- `SkillDomainSystem.cs`: weighted per-actor domain progression and domain events.
- `Items/ItemDefinition.cs`: authored item identity, stacking, equipment slot, and presentation poses.
- `Items/InventoryState.cs`: bounded actor inventory, ownership-based equipment, and item events.
- `Items/ActorInventory.cs`: independent runtime inventory/equipment ownership component.
- `Items/EquipmentPresentation.cs`: cached Animator, hand attachment visuals, old presentation mount handling, and actor-scoped equip listener.
- `Gods/Runtime/GodDefinition.cs` / `BlessingDefinition.cs`: authored affinity, limits, thresholds, and blessing effect data.
- `Gods/Runtime/GodResonanceSystem.cs`: actor-specific relationships, earned opportunities, explicit blessing acceptance, and typed events.
- `Authorities/Runtime/AuthorityDefinition.cs`: authored mastery/condition/resonance requirements.
- `Authorities/Runtime/AuthoritySystem.cs`: eligibility evaluation, missing requirement reporting, actor-local conditions/opportunities, one-time event.
- `Gameplay/Sandbox/SandboxScenarioDefinition.cs`: references and configurable tuning for this specific test scenario.
- `SandboxActor.cs`: scenario action validation and feedback; existing motor remains movement authority.
- `MeaningfulUseTracker.cs`: per-actor novelty records.
- `SandboxStation.cs`: pickup/equipment/observation/practice/shrine/Authority interactions and prototype target feedback.
- `SandboxInteractor.cs`: cached station list and camera, existing legacy E/Fire1 input, distance/FOV/LOS checks; no per-frame scene searches.
- `SandboxFeedback.cs`: small F1 readout and scoped event tracing; not a production UI.
- `SandboxLabel.cs`: labels face the cached gameplay camera.
- `Editor/SandboxSystemsAssets.cs`: actual imported asset inspection, safe data authoring, and verified item poses.
- `Editor/SandboxSystemsSetup.cs`: guarded authoring of the existing Sandbox/prefab, dimension-based prop placement, and recovery of this task's incomplete initial setup.
- `Tests/GenesisSystemsTests.cs`: seven additional ownership, progression, isolation, visibility, novelty, and eligibility tests.
- `Docs/Genesis-Systems-Sandbox-Report.md`: this implementation and validation report.

New runtime assemblies: Genesis.Items (Core, Character), Genesis.Gods (Core, Skill), Genesis.Authorities (Core, Skill, Gods), Genesis.Sandbox (Core, Character, Skill, Items, Gods, Authorities). New Genesis.Sandbox.Editor isolates authoring code. Genesis.Character.Tests adds the new runtime assembly references for integration tests. Existing Core, Character, Skill, and NPC assembly definitions were not modified in this phase.

The Animator Controller, Kevin Iglesias clips, Avatar/model assets, PlayerController, PlayerMovementInput, PlayerAnimation, ThirdPersonCamera, and NPC scripts were not changed in this systems phase. Their prior crouch implementation is documented separately.

### Authored content assets

Existing `Assets/Content/Skills/Perception/SkillDefinition.asset` gains Knowledge/Curiosity contributions (0.5 each) and category metadata. All new definitions/materials below live in `Assets/Content/Sandbox/Systems/`. God affinities come from the Bible; numeric thresholds and the Insight observation-range multiplier are explicitly prototype tuning, not new lore.

- `Authority-SovereignServant.asset`: Pride/Humility Mastery plus Trial of Understanding eligibility requirements.
- `CourtyardGrass.mat`: Separate courtyard ground material; imported materials remain intact.
- `Domain-Balance.asset`: Authored domain identity and presentation.
- `Domain-Beginnings.asset`: Authored domain identity and presentation.
- `Domain-Boundaries.asset`: Authored domain identity and presentation.
- `Domain-Chaos.asset`: Authored domain identity and presentation.
- `Domain-Choices.asset`: Authored domain identity and presentation.
- `Domain-Consequence.asset`: Authored domain identity and presentation.
- `Domain-Creation.asset`: Authored domain identity and presentation.
- `Domain-Curiosity.asset`: Authored domain identity and presentation.
- `Domain-Hope.asset`: Authored domain identity and presentation.
- `Domain-Justice.asset`: Authored domain identity and presentation.
- `Domain-Knowledge.asset`: Authored domain identity and presentation.
- `Domain-Loss.asset`: Authored domain identity and presentation.
- `Domain-Luck.asset`: Authored domain identity and presentation.
- `Domain-Potential.asset`: Authored domain identity and presentation.
- `Domain-Rebirth.asset`: Authored domain identity and presentation.
- `Domain-Resistance.asset`: Authored domain identity and presentation.
- `Domain-Sacrifice.asset`: Authored domain identity and presentation.
- `Domain-TheDungeon.asset`: Authored domain identity and presentation.
- `Domain-Time.asset`: Authored domain identity and presentation.
- `Domain-Transformation.asset`: Authored domain identity and presentation.
- `Domain-Transitions.asset`: Authored domain identity and presentation.
- `Domain-UnexpectedOutcomes.asset`: Authored domain identity and presentation.
- `Domain-Warfare.asset`: Authored domain identity and presentation.
- `God-dungeons-will.asset`: Bible pantheon identity and domain affinities; Observer additionally references earned Insight.
- `God-first-flame.asset`: Bible pantheon identity and domain affinities; Observer additionally references earned Insight.
- `God-forge-mother.asset`: Bible pantheon identity and domain affinities; Observer additionally references earned Insight.
- `God-laughing-trickster.asset`: Bible pantheon identity and domain affinities; Observer additionally references earned Insight.
- `God-observer.asset`: Bible pantheon identity and domain affinities; Observer additionally references earned Insight.
- `God-silent-judge.asset`: Bible pantheon identity and domain affinities; Observer additionally references earned Insight.
- `God-watcher-of-thresholds.asset`: Bible pantheon identity and domain affinities; Observer additionally references earned Insight.
- `God-weeping-mother.asset`: Bible pantheon identity and domain affinities; Observer additionally references earned Insight.
- `Humility.asset`: Holy requirement definition: Servant's Heart.
- `Insight.asset`: Earned Observer blessing; observation range multiplier 1.15.
- `Item-ash-kin-ember.asset`: Starter item identity, category, stack limit, actual imported proxy and equipment pose where applicable.
- `Item-blackwood-shield.asset`: Starter item identity, category, stack limit, actual imported proxy and equipment pose where applicable.
- `Item-healing-potion.asset`: Starter item identity, category, stack limit, actual imported proxy and equipment pose where applicable.
- `Item-iron-sword.asset`: Starter item identity, category, stack limit, actual imported proxy and equipment pose where applicable.
- `Item-soul-coins.asset`: Starter item identity, category, stack limit, actual imported proxy and equipment pose where applicable.
- `Pride.asset`: Sin requirement definition: Sovereign's Will.
- `SandboxScenario.asset`: Scenario asset connecting definitions, interaction ranges, cooldown, and progress tuning.
- `Swordsmanship.asset`: Basic active skill with Warfare contribution.
