# HORSE: City map generator

One click builds the whole city into **Scenes > City map**.

## Install

1. Copy the `HorseCity` folder into your project's `Assets/` folder.
2. Wait for Unity to compile.
3. Menu **HORSE > Build City Map**. Takes about a minute the first time.
4. Press Play. If the scene had no camera, a fly camera is added at the police stable:
   hold right mouse to look, WASD to move, Q/E down/up, Shift to go fast.

Running the menu again rebuilds everything under `CityMap (generated)` with the same seed.
Anything else you put in the scene is left alone. The only change outside that object is that
other directional lights are switched off (not deleted) so the dusk sun is the only sun.

Works with URP (adds bloom, ACES tonemapping and color grading) and with the built-in pipeline.
Compile-checked outside Unity; first run inside the editor is still to be confirmed.

## Layout (north = +Z)

Follows the ring sketch:

| Ring | What is there |
| --- | --- |
| Center plaza | Central Tower (the biggest building, ~345 m with spire), equestrian statue, fountain |
| Inner ring | Vet clinic (west), hidden weapon shop (north-east, in a back court reached by a narrow alley), glass towers all around the south |
| Middle ring | Houses + shophouses (NW), horse auction house with holding pens (NE), police station + police stable with paddock (SW), apartments and offices (SE) |
| Outer ring | Random mix of apartments, shophouses, warehouses, offices |
| City edge | Warehouses, overgrown trees, dirt tracks leading out of town |

Four avenues cross the city along the axes. Ring roads separate the rings. Side streets cut the
blocks. Power poles and sagging wires line the middle and outer streets (Thai / Japanese street
look).

## Interiors

Only these five have interiors. Everything else is exterior only.

- **Vet clinic**: reception and waiting room, treatment hall with examination stocks, overhead hoist,
  medicine cabinets and x-ray light box, three recovery stalls, turnout paddock behind.
- **Horse auction house**: lobby with registration counter and office, sale ring with sand floor,
  tiered bleachers, auctioneer booth, entry and exit lanes, holding pens outside.
- **Police station**: lobby with front desk and wanted board, bullpen with six desks, three holding
  cells, briefing room, armory, locker room, chief's office. Upper floor is exterior only.
- **Police stable**: 15 stalls along a central aisle, tack room with saddle racks, feed room,
  training paddock behind.

- **Hidden weapon shop**: a long table out front under the lean-to with guns laid out, and inside a
  glass display counter plus three tall glass cabinets with rifles standing up, ammo shelves and crates.
  The guns come from the **Low Poly Weapons VOL.1** pack: import it anywhere in the project before
  building. Without it the shop is built empty and the Console says so.

## Quest points

`CityMap (generated)/QuestPoints` holds a `QuestPoint` marker for each location
(PlayerSpawn, PoliceStation, PoliceStable, Vet, HorseSeller, WeaponShop, CentralTower,
SuspectHideout). Find one from code with `QuestPoint.Find(QuestPointKind.Vet)`.

## Performance (low to mid end PCs)

- All geometry is simple, flat-shaded and generated, with small 256-512 px textures.
- Buildings, roads and props are marked static, so Unity batches them.
- Small props (poles, wires, lamps, trees, road paint) are merged into a few meshes per map chunk.
- Small buildings and interiors use LOD culling, so they disappear when they are tiny on screen.
- Street lamps all glow through emissive bulbs, but `CityLightCuller` only turns on real lights
  for the 12 nearest lamps (tune `maxActiveLights` on `StreetLights`).
- `InteriorZone` keeps each interior's lights off until the camera is near that building.
- No light baking needed. Fog hides the far distance, and a cheap skyline fills the horizon.
- Optional: **HORSE > Bake Occlusion Culling** so hidden buildings behind towers are not drawn.

Tuning tips: lower the sun's shadow distance in your URP asset (80-120 m is plenty), lower
`maxActiveLights`, or raise the fog density in Lighting settings to cut draw distance.

## Files

- `Editor/` the generator (only runs in the editor, not included in builds)
- `Runtime/` the small components the scene uses (light culler, interior zones, quest points, fly camera)
- `Generated/` textures, materials and meshes written by the generator. Commit it so teammates
  don't have to rebuild.
