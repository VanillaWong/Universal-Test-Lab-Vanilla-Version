# Universal Test Lab v0.12.0-beta.3

This beta adds a landable, resuppliable airport on the Clean Test Range.

## Changes

- **Airport (Clean Test Range)**: addAirfield-based airport with radar marker (visibleOnHud), 1250 m resupply area, and engine auto-resupply (ammo/repairs) when landing on the runway strip.
- **Two spawn modes** (OPTIONS tab > Spawn Mode):
  - *Air spawn (with speed)*: default airborne spawn, respawn in air after death.
  - *Airport takeoff (stationary)*: start on the runway and taxi for takeoff; after death you respawn on the runway and can taxi again (spawnOnAirfield-based respawn).
- Fixed task-list hiding / FATAL ERROR crashes caused by inline multi-field BLK lines, mixed line endings, and a leftover runway object (placeOnCollision side effects).
- No longer writes a non-hot mission BLK (matches the playable vanilla beta.3 layout).

Solo sandbox mode from beta.2 is unchanged.
