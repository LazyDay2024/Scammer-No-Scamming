# Canyon mounted ambush

The integration coordinator adds `RiderAmbush.Build(level);` inside `FrontierLevelBuilder.Build()` after the terrain and level root exist, then rebuilds the Frontier scene. This change intentionally leaves `FrontierLevelBuilder.cs` untouched.

`RiderAmbush` creates three inactive rider templates behind canyon boulders near Road[3], separate rider/horse Health and HitZones, and private waypoint routes continuing through Road[6]. The riders begin 18/23/28 m behind Road[3], 9 m to its flank, keeping their initial separation about 20–30 m. Routes exit 3 m forward and 3 m inward around cover before following the road flank; even the nearest first egress remains over 15 m from a player stopped at Road[3]. Missing rider/horse prefabs or Animators produce an explicit build error. HorseModel and ScammerModel retain their authored scale and controllers; horse motion uses the controller's Speed parameter and accelerated Walk for galloping.

`RiderAmbushTrigger` is an Ambush Trap with zero direct damage. Its oriented box polls the player position, including when mounting disables the player collider. CaseFlowDemo's existing trap activation/reset hooks apply. Every activation clones an inactive immutable template; reset disables and removes the live pack before restoring trap state. Templates remain outside the trigger hierarchy so the general Trap reset never edits them.

Live checks in Unity 6000.3.6f1:

- Rebuild the scene and inspect boulder concealment, saddle alignment, rider/horse hit zones, first egress segments, and canyon ground clearance.
- Cross Road[3] both on foot and mounted: exactly three riders emerge once; no ambush before the action phase.
- Ride toward the hideout: riders follow the road without NavMesh, hold 15–30 m distance with a road-safe lateral flank, and shoot while the horse walks/gallops.
- Stop, reverse direction, and take canyon curves: confirm rider movement preserves clearance. Riders stop when the route or terrain cannot provide a safe step; check that they resume sensibly when clearance is available.
- Shoot horse versus rider HitZones: horse death dismounts a surviving rider, rider death ends shooting, and fallen horses remain independent of foot movement.
- Die/retry after damaging/killing/dismounting riders; verify three fresh mounted enemies at original hidden positions, no old horse/corpse/waypoint clones, and another single trigger activation.
- Run EditMode tests, including MountedScammerTests and RiderAmbushTriggerTests, then inspect the Console for errors.

Validation (24 October 2026, Unity 6000.3.6f1, existing Editor via EditorBridge): EditMode 117 passed, PlayMode 86 passed, zero failures or skipped tests. This includes native reset/death tests, eight Tick movement regressions, station-to-2D-gene flow and six actual-prefab revolver placement cases. Independent source review passed. The rebuilt Frontier_Case01 is first in Build Settings. Live station solve opened the existing gene screen and confirmation dispatched; mount reported true; case 01 was updated to include Ambush and its road trigger spawned exactly three mounted riders with full horse health. Screenshots are retained outside source under D:/AI/runtime/editor-bridge/shots. Sustained chase feel, long-route curves and a complete death/retry combat playthrough remain unverified. Earlier standalone validation also caught an inverted dismount condition by mutation.
