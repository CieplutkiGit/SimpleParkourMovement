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
