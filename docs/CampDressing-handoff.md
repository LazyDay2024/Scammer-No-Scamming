# Case 01 camp dressing handoff

Entry point: `CampDressing.Build(level, hideout)`, already called immediately after
`BuildCamp`. Run **Tools > Scammer No Scamming > Build Frontier Level (Case 01)**
in the owner's Unity 6000.3.6f1 Editor. This change contains builder/runtime source;
the scene, materials and navigation assets have not been regenerated in this sandbox.

## Authored plan

Coordinates below use the hideout frame (yaw 140). Hall footprint: x +/-9.5,
z -1.5..15.5. All new ground placements sample terrain instead of adding a world Y.

- Office at (-16, 4), door facing the west passage alongside the hall: open doorway,
  window, desk/safe, one ForcedWorker Bystander. Worker uses the existing free action
  and escapes to the front road at (10, -22).
- Generator/radio hut at (16, 10): open door, generator, exhaust, roof firing deck,
  antenna. One exposed rooftop sentry; one sentry on the existing lookout deck.
- Canvas tent at (-15, -12), built from two double-sided textured planes.
- Crate islands, rounded canvas sandbags, wood/iron barrels and scanned Poly Haven
  boulders leave the main doorway axis and both side passages open.
- Perimeter has a road opening on z=-19, x=3..16, and a mine/flank opening on
  x=22, z=3..15. FlankPath enters that side gap and continues behind the hall.
- Four existing barricades move out of the road approach; three warm work lamps
  and a particle campfire mark entrances and the west rest corner.

Four yard patrols, both sentries and the captive are under `RaidEncounter.enemyRoot`.
Patrol waypoints and the worker exit clone with the raid. Existing character health,
weapon damage and AI code are unchanged. Sentries have disabled NavMeshAgents and
95m sight range, so they stay on their decks and can watch the approach/overlook.

`GroundExistingCamp` fixes an inherited placement issue locally: BuildCamp passed
world Y into helpers that add terrain height. The tower, wagon and tanks are grounded
before guards/explosives are attached; imported FBX rotations are preserved.

All three original tanks move into the raid template. `ExplosiveProp` uses Health
and existing hitscan collider lookup, with a single blast per death, linear 7m falloff,
one Health hit per victim, solid-cover occlusion, chain reactions and damage attribution
to the triggering faction. New prop peak damage is 6; NPC/player health and damage
are unchanged. Flash, soft fire/dust particles and a pitched procedural shot supply
the explosion feedback; this audio choice still needs listening in-game.

## Navigation

The existing hall surface is rebaked with `CollectObjects.Children`, keeping its
hierarchy and floor separate from the yard. A sibling Not Walkable modifier volume
excludes that hall footprint from the new yard bake. A bidirectional 2.8m doorway
NavMeshLink joins them. Yard uses terrain/physics colliders and 0.15m voxels, saved
to `Assets/Navigation/east_shophouse_yard.asset`; hall keeps its original path.
Rebakes preserve existing asset GUIDs in the dressing helper.

API references checked against the installed navigation source and official docs:
[NavMeshSurface](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshSurface.html),
[NavMeshLink](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html).

## Evidence and remaining checks

- Roslyn compiled all current runtime sources, all editor sources, and all EditMode
  tests against installed Unity 6000.3.6f1 and project package DLLs: no errors.
  Existing warnings: Npc.animation hides Component.animation; builder NavigationStatic
  is obsolete. No new warnings were reported.
- All 13 new falloff NUnit cases passed in a managed .NET harness, without starting
  Unity. An isolated compiled mutant removing falloff failed the expected distance
  assertion. Source was never mutated. This is not a Unity Test Runner result.
- `git diff --check` passed. Independent review identified the inherited height bug;
  the grounding fix was reviewed again with no additional code finding.
- No Unity execution, visual inspection, NavMesh bake, playtest or audio audition
  happened here. In Editor, inspect grounding and material scale, both approaches,
  all four patrol loops, yard-to-hall doorway traversal, captive rescue/escape,
  rooftop sightlines/visibility from the overlook, tank cover occlusion/chain blasts,
  and BeginRaid -> ResetRaid -> BeginRaid (fresh NPCs, waypoints and tanks).
- Outdoor guards count toward raid clearance (10 total guards including the 4 original
  hall guards). Confirm pacing and performance of point lights/scanned rock colliders
  in the owner's live playthrough before shipping.
- Changes remain uncommitted: `git add` could not create the worktree's `index.lock`
  because its Git metadata is outside the sandbox write root. Commit from the
  coordinator after reviewing these source files and their two new Unity `.meta` files.
