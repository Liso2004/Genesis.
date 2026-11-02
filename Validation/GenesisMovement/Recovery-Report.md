# Locomotion migration recovery ? 2026-09-20

## Confirmed interruption

At recovery start, `Assets/Genesis/Character/Prefabs/GenesisPlayer.prefab` did not exist. Both gameplay scenes referenced the old `PlayerAnimator.controller` GUID `dcfef7f32d316644cb7ad65165b56454`. Its parameters were Speed, IsGrounded, IsRunning and Jump, with idle/walking/Sprint/Jump states and an empty layer. The new PlayerAnimation instead writes Speed, IsGrounded and VerticalVelocity. The Animator/prefab/scene portion of the previous migration had therefore not been applied to the workspace.

The existing migration utility was used directly against `C:/Users/lisow/Genesis`, preserving the previously selected Kevin clips, hero model and Avatar. PlayerController is not rewritten. Its starting SHA256 is `805D491AA949D91C228F872FF2FAFC768104BDEC19B6DD59C31590D8E2723403`.

## Applied asset changes

- Modify `Assets/Genesis/Character/Animatinos/PlayerAnimator.controller` in place, preserving its GUID: one locomotion tree (idle/walk/run/sprint), Jump/Fall/Land, Speed/IsGrounded/VerticalVelocity.
- Create `Assets/Genesis/Character/Prefabs/GenesisPlayer.prefab` and metadata, retaining the existing Polygon hero Humanoid Avatar and disabling root motion.
- Modify `Assets/Scenes/Sandbox/Sandbox.unity` to instantiate this prefab with its scene camera and actor identity overrides.
- Modify `Assets/PolygonFantasyKingdom/Scenes/Demo_ExteriorOnly_Optimized.unity` to use the same prefab while preserving camera/NPC references.
- Extend `Assets/Tests/PlayerAnimationTests.cs` with a motor-to-presentation integration check; existing tests exercise clip selection, animated foot bones and jump/fall/land recovery.

Original Animator and scene snapshots are retained in `BeforeMigration/`. No old animation assets are removed.

## Execution

`Workspace-Migration.log` records the real project path, all seven selected Humanoid clips and `[Genesis migration] Prefab, Avatar, clips, camera and NPC references PASS`; Unity exited with code 0. Both saved gameplay scenes were reopened by Unity during validation. Their diffs only replace player hierarchy/component blocks and update scene-root bookkeeping; other scene object blocks remain unchanged. The final PlayerController hash matches the starting hash exactly.

Prefab GUID: `2b9ca8d5e7f67db4eaf67980f131e1c4`. Existing Animator GUID remains `dcfef7f32d316644cb7ad65165b56454`. The retained hero Avatar is from `Assets/PolygonDungeonRealms/Models/Characters/Characters.fbx`, GUID `9ad76de1617627d45ba4fa8ddf255af4`, fileID 9000000. The controller uses the previously selected Kevin Idle01, Walk01_Forward, Run01_Forward, Sprint01_Forward, Jump01 - Begin, Fall01 and Jump01 - Land clips directly. All reported average root speeds are zero; root motion is disabled. Walk playback is 0.8 at 1.6 m/s, run 4 m/s and sprint 6 m/s.

## Runtime verification in the real workspace

All **3/3 PlayMode tests passed**, with zero failures/skips, in `Workspace-Animation.xml` (2026-09-20 18:37:27 UTC). `Workspace-Animation.log` records `-projectPath C:/Users/lisow/Genesis`.

- `PrefabRetargetsKevinClipsWithoutMovingActorRoot`: valid Humanoid Avatar, root motion off, one motor/no redundant MeshColliders, idle/walk/run/sprint clip weights, return to idle, advancing state time and actual foot-bone displacement while the actor root stays fixed.
- `JumpFallLandAndLedgeTransitionsRecoverWithoutLoops`: jump, fall, stationary landing recovery, ledge fall, moving landing recovery and short/ceiling-contact transition recovery.
- `MotorMovementDrivesAnimatorThroughPresentation`: actual capsule displacement drives Speed through PlayerAnimation across idle/walk/run/stop/sprint; grounded and positive jump velocity are forwarded correctly. No unexpected test logs or Animator parameter errors.

The first run passed rig retargeting but exposed a transition restart: SourceThenDestination interruption with unordered source re-evaluation could restart the still-true takeoff transition repeatedly. The controller and migration generator now use Destination interruption. The speed integration assertion was corrected to compare animation with measured displacement, allowing normal capsule contact resolution to differ from requested speed. The initial failure evidence is retained in `Workspace-Animation-Initial.xml` and `.log`.

Both gameplay scenes passed Unity reopen, canonical prefab, camera and NPC target checks in the migration run. The later change only adjusts the controller's transition interruption policy, not scene or prefab references. PlayerController, PlayerAnimation and PlayerMovementInput runtime implementations were not changed during recovery. The earlier nine physics tests remain historical evidence; they were not unnecessarily rerun because the motor is unchanged.

These are headless runtime checks, not a rendered manual playthrough of both scenes. Visual feel, foot contact on terrain and camera appearance still need Editor playtesting. Unity startup reported licensing/cloud/headless graphics warnings; these did not prevent successful migration or tests. No old animation asset was removed.

## Additional changed files and artifacts

- `Assets/Genesis/Character/Editor/PlayerLocomotionMigration.cs`: fixes transition interruption in future migration output.
- `Assets/Tests/PlayerAnimationTests.cs`: adds measured motor-to-Animator integration coverage.
- `Assets/Docs/Genesis-Movement-Report.md` and `Assets/Docs/Genesis-Player-Movement.md`: record recovery completion and corrected transition behaviour.
- `Assets/Genesis/Character/Prefabs.meta` and `Assets/Genesis/Character/Prefabs/GenesisPlayer.prefab.meta`: Unity-created folder/prefab identities.
- This report, migration/test logs, initial/final test XML, scene/Animator backups and Sandbox diff in `Validation/GenesisMovement/` provide review and recovery evidence.
