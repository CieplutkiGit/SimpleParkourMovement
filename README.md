# Parkour Controller (Unity 6, URP)

Third-person parkour controller, project built for interview purposes only, not a game-demo.

Ongoing - project not finished but enough code for review.

## Features

**Movement**
- Walk, sprint, momentum-aware air control
- Jump with coyote time and jump buffering
- Vault (root motion, MatchTarget-corrected)

**Wall traversal**
- Run-along on side walls
- Run-up on front walls
- Wall-to-wall chaining on alternating-facing walls

**Ledges**
- Grab and hang
- Corner shimmy (inner and outer)
- Climb-up (root motion, MatchTarget-corrected)

**Presentation**
- Procedural cape (hand-rolled spring-bone solver with body collision)
- Cinemachine director driven by the same state event the animator uses
- Global wind ScriptableObject sampled by cape, audio and shader uniform
- Velocity-gated impact FX (land dust, wall scuff, vault burst)
- Speed-reactive wind audio (no audio yet)


## Controls

| Action | Key |
| --- | --- |
| Move | WASD |
| Look | Mouse |
| Sprint | Left Shift |
| Jump / vault / wall-run | Space |
| Grab ledge | Hold Space at ledge |
| Shimmy | A / D while hanging |
| Climb up | Space while hanging |

## Build / run

- Unity 6.0+ (URP 17)
- Open the project, then `Assets/Scenes/<main scene>` and press Play
- No external runtime dependencies beyond stock packages (Cinemachine, Input System, URP)

## Architecture (short version)

`PlayerController` is the composition root. It owns the system components, the detectors, and the StateMachine. States call into systems; systems do not know about states. Detectors run their physics queries and expose results as properties, nothing else in the codebase re-issues the same query.

Anything reacting to state transitions subscribes to StateMachine.OnStateChanged (animator, camera director, impact FX). Anything reacting to wind samples WindSO.Current. No singletons, no static state.



## Known limitations

- **Brief side-camera flash on wall-to-fall exit.** Sub-second cosmetic. Root-caused (Brain samples a player-parented look-root before CameraController re-imposes look rotation that frame). Fix would restructure a rig documented as final, so scoped out.
- **Vertical walls only.** Sloped walls out of scope.
- **Climb-up has visible foot sliding.** Underlying pipeline is correct, polish budget went elsewhere, also used free mixamo animations, did what i could to make it look good, was kinda nice and something went wrong on merging branches, wasnt strong enough mentaly to re-do it again.
- **Run-along vs run-up decided by which ray hit.** No player-intent disambiguation.


There are also few bugs and I turned cape into scarf, looks better.
## Credits

- Character model and animations: Mixamo (Y Bot rig)
- Engine and packages: Unity 6, URP 17, Cinemachine 2.x, Input System
