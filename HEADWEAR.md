# Headwear feature branch: game-ready foundations, NOT a finished AAA outfit

Branch: `feature/aaa-headwear-prototype`
Scene: `Assets/Scenes/Main.unity`

## What runs today

- `HeadwearController` resolves the Y Bot Humanoid head socket and owns the
  cap/hood/scarf/bare cosmetic states. Styles are switched via the H key (for
  development) or `SetStyle` from a cosmetics menu.
- The default **Technical Cap** is a procedural test mesh: a multi-panel crown,
  rolled lower band, curved reinforced brim, panel seams, top button,
  embroidery-like detail, and rear attachment. This is *not* finished art.
- `HeadwearStrapDynamics` simulates the small cap adjustment tail, not the
  rigid crown. It uses a constrained segmented ribbon, bounded substeps,
  per-segment lengths, damping, gravity, global WindSO, head-sphere collision,
  rest-pose pull and reset on large head transforms. The mesh follows the
  simulation. WindWake is ordered before the strap update.
- The **Raised Hood** default is a procedural lined shell with open face.
  It is a fit/proportions placeholder, not a rigged full hoodie.
- `HeadwearSkinnedGarmentBinding` supports replacing the placeholder with
  a *properly authored* skinned hood/garment exported against the same exact
  Y Bot rest pose. It remaps mesh bone names to the animated skeleton, checks
  missing bones up front and rejects incomplete remappings.
- The original scarf geometry and spring code remain. Re-enabling the scarf
  resets the spring simulation to avoid stale-velocity bursts.

## Play and validate

1. Open the **feature branch** in Unity **6 / URP 17**.
2. Open `Assets/Scenes/Main.unity` and press Play. The player should spawn
   wearing the charcoal prototype cap with a small reacting rear strap.
3. Cycle **H**: Technical Cap -> Raised Hood -> Bare -> Original Scarf -> Cap.
4. Menu: **Tools > Parkour > Validate Headwear Geometry**. This editor tool
   builds the cap, hood and dynamic strap in a disposable hierarchy and checks
   vertex data, triangles, bounds, degeneracy, and the head bone in Main.
5. Inspect the character in Play Mode while sprinting, jumping, vaulting,
   wall-running, landing, grabbing, shimmying and climbing. Check for clipping,
   material issues, head offset/rotation, strap popping, and no motion changes.
6. Switch back to the scarf after several seconds of motion. It should re-seed
   rather than lurch from stale spring positions.

## Editable source assets and baked render assets

These are **real committed source models**, not only runtime C# procedural
meshes:

- `Assets/Art/Headwear/Source/TechnicalCap.obj` — 3,443 vertices, 6,028
  triangles, crown, dimensional curved bill, band, seams, ventilation eyelets,
  brim stitching and adjustment details
- `Assets/Art/Headwear/Source/RaisedHood.obj` — 1,464 vertices, 2,450
  triangles, outer shell, lining, rolled face rim, structural seams
- `Assets/Art/Headwear/Source/HeadwearMaterials.mtl` — simple placeholder
  OBJ preview colors (not final PBR authoring)
- `Assets/Art/Headwear/Source/Textures/Fabric_Weave_Albedo.png` — committed
  256x256 repeatable fabric albedo map
- `Assets/Art/Headwear/Source/Textures/Fabric_Weave_Normal.png` — committed
  256x256 woven surface normals, imported as a normal map
- `Assets/Art/Headwear/Source/Textures/Fabric_Weave_Occlusion.png` — committed
  256x256 small-scale crevice shading map

Source meshes were checked for invalid face indices and degenerate triangles.
They can be imported into Blender for artist retopology/UV work; their current
shape is algorithmically authored and not yet a professionally sculpted hero
asset. The hood OBJ is **not** weight painted.

**One-click Unity bake/assignment:** Open `Main.unity` in Unity 6, then use
`Tools > Parkour > Build and Assign Headwear to Open Scene`. This tool:

1. Imports the OBJ models as editable source and creates persistent native
   mesh assets in `Assets/Art/Headwear/Baked`.
2. Uses the committed repeatable weave albedo, normal and occlusion maps
   (with deterministic generated textures as a fallback).
3. Creates three URP Lit materials (fabric, rubberized trim, accent) and
   assigns them by OBJ material and mesh-group names.
4. Creates cap/hood prefabs with full-detail LOD0 and a simplified LOD1
   that excludes most seams, eyelets and small details.
5. Assigns the prefabs and materials to the selected scene's Player
   `HeadwearController`. **Save the scene** afterward to persist references.

The bake can be repeated without intentionally changing existing mesh/material
asset GUIDs. When OBJ geometry is imported as a single combined mesh, a fallback
LOD preserves visibility; actual triangle reduction requires splitting the
source into distinct parts. The source PNGs and OBJ meshes are committed. Native Unity mesh assets,
materials and prefabs are generated into the baked folder and should be
committed once visually reviewed in the Editor.

For non-Unity inspection, you can run:
`python3 tools/validate_headwear_assets.py`
The GitHub Actions `headwear-integrity.yml` workflow also runs these
source/scene checks. Passing this static check does **not** establish that
Unity compiles or that the model looks correct on the character.

**Fit:** Cap and rigid hood offsets/eulers are interpreted in Animator
model-root space on spawn and then parented to the live head bone. This avoids
assuming Mixamo's head bone local axes match the character's forward axis.
If the Y Bot's imported forward axis differs, tune Cap/Hood Euler in Inspector.

## Unity 6 compile, import and rig verification

This is separate from the green Python/GitHub static checks. Use the
**Tools > Parkour > Run Unity Headwear Smoke Test** menu command, or run
Unity 6 Editor with these command-line arguments:

```text
-batchmode -nographics -projectPath /path/to/SimpleParkourMovement -executeMethod HeadwearBatchSmokeTest.RunBatch -quit -logFile headwear-unity.log
```

The batch test returns **exit code 1** on failure and logs the reason.
It builds the OBJ-based prefabs, validates their persistent Mesh assets,
URP/Lit albedo/normal maps and distance LODs, opens Main.unity, verifies
HeadwearController and the Mixamo head bone, and checks the attachment
orientation and finite renderer bounds. The test may generate assets
under `Assets/Art/Headwear/Baked`; review and commit them after Unity
visual validation.

This is not an actual Play Mode stress test. An engine session with
animation, collisions and frame profiling remains mandatory.

## AAA acceptance criteria (must be checked in a real engine session)

- [ ] Unity 6 import succeeds with no C# compile errors and no broken references
- [ ] Baked cap/hood have no pink materials, inverted faces or lighting artifacts
- [ ] Cap pivots face forward and track every head motion
- [ ] No visible clipping in sprint, wall-run, vault, ledge hang and climb-up
- [ ] Cap crown/brim are stable; the rear strap moves cleanly, resets on teleports
- [ ] Fabric grain, seams and roughness remain believable at close camera ranges
- [ ] Stable framerate with appropriate headwear LODs in the target scene
- [ ] Model, UVs, textures and corrective rigging reviewed by an artist

Until those checks pass, describe this as an advanced headwear implementation
and art-pipeline base, **not a shipping AAA garment**.

## How to replace prototype art with an actual high-end cap

A *cap* should not behave as loose cloth. Rig the rigid cap (crown + reinforced
brim) to the head bone with **no general-purpose cloth simulation**. Give it a
proper silhouette, panel structure, high-poly sculpt / normals, roughness,
weave texture, stitch maps and fitted UVs.

- Make one cap prefab with `MeshRenderer`/authored meshes.
- Use Y Bot's head transform as the coordinate/fit reference. The cap's origin
  is attached to the head; set the exact fit with the `HeadwearController`
  `Cap Offset` and `Cap Euler` fields in the Inspector.
- Assign the prefab to `Cap Prefab`. A custom static prefab replaces
  *all* procedural cap geometry and its procedural adjustment tail. Add your
  own `HeadwearStrapDynamics` on a tail transform in the prefab if you want
  physical secondary motion. Configure it from your prefab logic or adapt
  its public `Configure` call.
- For texture detail, use baked normal/height maps, trim detail, subtle albedo
  variation and calibrated fabric roughness. Use URP/Lit or custom cloth BRDF.
- Build LODs and a simplified version for distance. Avoid shadows/collision
  on tiny strips when they occupy only a few pixels.

## How to replace the hood with a REAL skinned garment

A hood attached to the head as a *rigid* object will clip during extreme poses.
For a proper hood/hoodie:

1. Export the project's **original Y Bot reference rig in rest pose** to
   Blender/Maya/Marvelous Designer. Model torso/sleeves/hood on top.
2. Skin shoulder, chest, neck and hood portions to the appropriate Y Bot
   transforms. A hood that should follow the head must blend the head,
   neck and upper-spine weights. Do not rely on rigid head mounting.
3. Export a prefab with an `SkinnedMeshRenderer` bound to the identical
   rest skeleton. Keep scale/pivot consistent with the original model rig.
4. Assign this prefab to `Hood Prefab`. The controller recognizes a
   `SkinnedMeshRenderer`, places the prefab under the Animator model root,
   and calls `HeadwearSkinnedGarmentBinding` to map the bones. If remapping
   fails it logs errors and falls back to procedural geometry.
5. Test bindposes before animation! Matching names are *not enough* if the
   prefab was exported from a different body/proportions/rest pose.
6. Add a small amount of separate simulation only to loose hems/drawstrings.
   Author corrective shapes for major shoulder and neck motion.
7. Check all existing parkour clips, especially overhead reaches, ledge
   hang, sharp wall-run transitions, and vaults; fix skin weights/shape keys.

This bone-rebinding integration is an art pipeline feature, **not** a system
that magically fits any hoodie mesh onto any body.

## Technical files

| File | Role |
| --- | --- |
| `HeadwearController.cs` | Cosmetic slots, state switches and prefab selection |
| `ProceduralHeadwearMeshes.cs` | Prototype generated mesh and accents |
| `HeadwearStrapDynamics.cs` | PBD-style strap motion, collision and mesh deformation |
| `HeadwearSkinnedGarmentBinding.cs` | Production mesh skeleton mapping |
| `HeadwearMeshValidation.cs` | Editor geometry + bone smoke checker |
| `SpringChain.cs` | Original scarf solver, with reset hook |
| `WindWake.cs` | Ordered wind sampling for motion simulation |
| `Main.unity` | Player headwear component, existing WindSO assigned |

## Remaining work and non-claims

**Not shipped:** An artist-authored textured AAA cap, fully skinned hoodie
asset, fabric normal/roughness maps, LOD chain, full-body garment collision,
animated wrinkle correctives, professional skinning, or tested Play Mode
recordings/builds. These cannot be replaced by more C# scripts.

This remote implementation was reviewed at the source/scene level, but
**no Unity Editor, compiler, or graphics/runtime test is available in this
environment**. The project must compile and be visually checked in Unity
before it is considered complete.
