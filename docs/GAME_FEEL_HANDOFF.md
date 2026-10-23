# Game feel handoff — feature/game-feel

All source changes are in `D:/AI/snc-feel`. No gameplay damage, health, movement speed,
cooldown or AI tuning values changed. No external art/audio was added.

## Wiring

`PlayerSandbox.CreatePlayer()` now adds:

- `RevolverViewModel`: runtime camera child, mouse sway, breathing, movement bob and
  critically damped position/rotation recoil. Hidden while `GameScreen.AnyOpen`.
  The editor assigns `revolverPrefab` to `Revolver_1.fbx` (both 1 and 2 exist).
  The prefab instance is normalized to 34 cm, has colliders removed and Built-in
  materials converted to URP instance materials. Null prefab uses primitive geometry.
- `WeaponFeel`: subscribes to `Weapon.Fired`; pooled muzzle flare, short point light,
  smoke, red flesh impacts, tan world dust/sparks, layered shot and spatial impact audio.
  The editor serializes `Assets/Shaders/FeelParticle.shader` so it is included in builds.
  The shader generates a radial particle falloff in code; no texture asset is needed.
- `PlayerCameraFeel` on the actual camera (under the existing look pivot): camera
  recoil, damped fire/damage shake, directional damage kick, edge vignette, small
  ground bob, up to 3.5 degrees extra FOV and 1.25 degrees of mounted turn roll.
  `motionAmount`, `shakeAmount`, `vignetteAmount` can each be reduced to zero.
- `PlayerFeelAudio`: spatial grounded footsteps and a quiet looping wind bed.
- `KillHitStop`: 50 ms freeze only for a confirmed lethal player weapon result.
  Uses unscaled time, restores the prior scale, yields to open GameScreens, listener
  pause (including GameScreen's deferred focus handoff), or another
  time-scale writer and releases its own freeze when disabled/destroyed.

`CreateHorse()` adds `HorseFeelAudio`: grounded paired clops with cadence/volume
following absolute horse speed, alternating pitch, and occasional spatial snorts.

`Npc.Awake()` installs `NpcImpactReaction` at runtime; `ArmedEnemy.Awake()` installs
`EnemyShotAudio`. This covers existing serialized NPCs and newly spawned ones without
changing the scene builders. The reaction offsets render-only children. For primitive
NPCs sharing mesh and collider transforms it makes rendering proxies, preserving live
collider/agent positions. Death falls in the bullet travel direction; a real Defeat
clip gets a smaller directional lean, while a missing clip gets a full 78-degree fall.
`Npc.ResetForAction()` restores the visual pose for retries/pooling.

Existing public constructors, properties and fire/damage events are retained.
`DamageInfo.Direction` and `ShotResult.Hit/Normal/Killed` are additive.
`HitscanWeapon` captures the health transition for kill detection without changing damage.
The existing tracer uses the view model muzzle. `HitFeedback` suppresses its old shot
audio only while `WeaponFeel` is active. `PlayerVitals` retains its legacy full-screen
flash on old rigs and its directional indicator; new rigs use the edge vignette.

## Coordinator actions

1. Import these source files and regenerate the playable scene using its existing
   builder, so it invokes the updated `CreatePlayer/CreateHorse`. Existing saved
   player/horse objects do not acquire new components until rebuilt (or manually added).
   No hooks in Screens, Interaction, WorldSceneArt or CaseFlowSceneBuilder are required.
2. Run EditMode tests, including `GameFeelTests`, then the full PlayMode suite.
3. In the Editor verify fire against flesh/world/misses, headshot kills, walking,
   sprinting, all horse gaits, turns, damage, missing-animation NPCs, and retry/reset.
   Open a GameScreen during a kill freeze and verify it stays paused until closed.
4. Check the imported revolver's orientation/muzzle placement in the actual camera;
   `modelEuler` defaults to `(0,90,0)`, with public `restPosition` available for fitting.
   Check the null-prefab fallback too. Check shader rendering and levels with audio on.

## Verification actually performed

- Compiled every runtime C# file against installed **Unity 6000.3.6f1** engine DLLs,
  Unity Input System and Unity's .NET Standard 2.1 reference assemblies, without Unity.
- Compiled both player and `UNITY_EDITOR` runtime branches (including AssetDatabase
  assignment), all existing EditMode tests plus the new tests, and all existing
  PlayMode tests. No new compiler warnings; existing Npc.animation/JSON DTO warnings remain.
- Executed **13 pure EditMode cases** directly through NUnit on .NET 9 using the actual
  compiled project/test assemblies: spring recovery at 30/60/144 FPS, frame-step
  invariance/zero-delta hold, periodic bounded bob, all six generated PCM lengths,
  finite samples/headroom/nonzero energy/clean endpoints, and pause ownership.
- A temporary mutation removing the GameScreen ownership check was rejected by the
  pause test. Production source was not mutated.
- `git diff --check` passed.

Unity Editor, native AudioClip cache test, shader compilation, rendered output, audible
mix and live gameplay were **not run**. The native cache test is included for Unity.
Standalone compile checks are not a substitute for the actual Unity test runs.

Local verification setup notes: Roslyn `csc.dll` can reference Unity's
`Editor/Data/NetStandard/ref/2.1.0/netstandard.dll` plus the `compat/2.1.0/shims/netfx/mscorlib.dll`
facade for NUnit. For editor symbols use `UnityEditor.CoreModule.dll`; adding the
aggregate UnityEditor.dll alongside it duplicates AssetDatabase in this installation.
No dependency restore, project import or Unity process is needed for these checks.

## Limits

- Procedural audio supplies timing/weight, not the fidelity of recorded revolvers,
  horses or environment acoustics. No indoor reverb/occlusion or surface taxonomy.
- Death reactions are visual tweens, not ragdolls. Defeat clips and model roots need
  live checks for ground penetration. Skinned meshes are expected under a model child
  as in the current builder; root-bound skinning with colliders may need a visual pivot.
- The view model uses the world camera; there is no separate overlay camera, hand rig,
  reload animation or near-wall avoidance. Confirm fitting and close-wall clipping live.
- Freeze ownership explicitly supports the project's `GameScreen.AnyOpen` and listener-pause contract.
  Any future independent pause system writing the same zero scale during a freeze must
  expose its pause state too; identical zero writes cannot be detected from Time alone.

## Git handoff

No commit was possible in this sandbox: `git add` was denied when creating
`D:/AI/scammer-no-scamming/.git/worktrees/snc-feel/index.lock`. That metadata is outside
the writable root. Files remain uncommitted on `feature/game-feel`; no push or merge.
The coordinator can make these focused commits after reviewing the working diff:

1. `feat: add deterministic feedback math, audio synthesis and pause-safe kill freeze`
   — DamageInfo, ShotResult, HitscanWeapon, FeedbackSpring, ProceduralFeelAudio,
   KillHitStop, GameFeelTests and their new metas.
2. `feat: layer revolver, camera, riding and enemy feedback into sandbox rigs`
   — the remaining runtime components, shader and wiring/integration changes, plus metas.
3. `docs: record game feel integration and verification handoff`
   — this handoff.

Review before staging; stage these explicit task files rather than unrelated future edits.
