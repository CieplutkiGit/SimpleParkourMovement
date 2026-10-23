# Headwear prototype: technical cap / raised hood

Branch: `feature/aaa-headwear-prototype`. Scene: `Assets/Scenes/Main.unity`.

This is a **playable, non-destructive prototype**, not a photoreal finished hoodie
model. The actual production solution should use high-quality authored Blender /
Marvelous Designer meshes, baked textures and rigged garments. Generated fallback
geometry allows the attachment and switching architecture to work immediately
without committing arbitrary binary FBX assets.

## Try it

1. Open `Assets/Scenes/Main.unity` in Unity 6 (URP 17), then press Play.
2. The Player's `HeadwearController` automatically resolves the Y Bot Humanoid
   `mixamorig:Head` bone (and can fall back to the transform name).
3. **Technical Cap** is the startup mode. The original procedural scarf and
   its spring simulation are hidden, not deleted.
4. Press **H** to cycle: Original Scarf -> Technical Cap -> Raised Hood -> Bare.
   (From the default cap, the first press selects Raised Hood.)
5. Test standing, sprinting, jumping, wall-running, vaulting, ledge grabs and
   climbing. The attachments follow the animated head bone, independent of the
   parkour state machine.

The procedural cap contains a six-panel curved crown, stitched panel seams, a
reinforced dimensional brim, a lower band, front chevron and rear adjustment
detail. The hood has an open face, inner lining, a rolled opening edge and
structural seams. Neither headwear mesh runs cloth simulation: structurally
rigid cap/hood regions should primarily follow skeletal animation.

## Fit and customization

The `HeadwearController` is a component on Player in Main.unity. You can
fine-tune `Cap Offset`, `Cap Euler`, `Hood Offset` and `Hood Euler` in
the Inspector if your Y Bot avatar's head-bone orientation differs.

Production replacement: create a cap and/or hood prefab using real textured
meshes. Assign them to `Cap Prefab` and `Hood Prefab`. The controller
instantiates the prefab under the head bone instead of procedural geometry.
Set the prefab's local origin to its cap/hood pivot, and tune offsets if needed.
The materials slots `Fabric`, `Trim`, `Accent` are for the procedural
fallback; prefab materials remain authored on their MeshRenderers.

A **full hoodie** requires a separate torso/sleeve skinned mesh and proper
weight painting, with animation-driven corrective shapes and carefully limited
simulation on loose pieces. Simply attaching a hood to the head cannot replace
a properly modeled and rigged jacket.

## Validation and regression checklist

Use **Tools > Parkour > Validate Headwear Geometry** to run the editor geometry
checker. It builds both variants in a temporary hierarchy and verifies vertex
attributes, bounds, indices and degenerate triangles. No scene is modified.

- [ ] Press Play with default cap: no missing avatar, pink materials or errors.
- [ ] Inspect the cap from front, side, rear: crown, curved brim, closure visible.
- [ ] Press H repeatedly: exactly one style active; scarf restored when selected.
- [ ] After moving for several seconds in cap/hood, switch to scarf; ensure no
      physics snap from stale simulated positions.
- [ ] Sprint, jump, vault, run along/up walls, grab/shimmy/climb ledges in both
      new headwear modes; character movement must remain unchanged.
- [ ] Check face and shoulder intersections at extreme head/torso rotations;
      adjust offsets or replace prototype hood with a rigged garment.
- [ ] Test with custom URP materials and with prefab replacements.

## Known limitations

- This prototype deliberately has no torso/sleeve clothing or cloth-self
  collision, LODs, texture maps, or baked fabric normal details.
- The raised hood follows the head rigidly and may intersect shoulders during
  extreme turns. AAA-quality hood/neck deformations require torso/head
  weight blending, corrective shapes and authored rigging.
- No Unity Editor or player build is available to the remote code author.
  Repository-level static checks do **not** substitute for the Play Mode
  and validation steps above.

Existing `SpringChain`/scarf is preserved; a public `ResetSimulation` function
is called when returning to it after a period of inactivity to prevent
outdated spring positions from jerking on re-enable.
