# Castle Heart Production Catalog Editor Setup

Use **Window > DeadWalls > Rebuild Castle Heart Production Catalog** while
`Assets/Scenes/NewGameScene.unity` is active.

The command is idempotent. It creates or updates the canonical node assets, validates catalog and
graph generation, binds the catalog to the scene `GameManager`, applies the approved Heart layout
tuning to the generated HUD prefab and active scene, saves assets, and saves the scene.

Use **Window > DeadWalls > Apply Castle Heart Pixel Icon Map** when only the reviewed RPG Pixel
icon references must be repaired. This command updates the existing 37 node assets without
rebuilding data, touching the scene, or polishing the legacy prefab.

After running it:

1. Wait for Unity compilation/import to finish.
2. Confirm the Console has no errors.
3. Enter Play Mode and open **Castle Heart**.
4. Grant test Grave Essence through an editor-only test route, then verify `RESEARCH/UPGRADE`,
   direct-child reveal, dotted connector animation, PC right inspector, mobile bottom inspector,
   and exact Continue.

Do not hand-edit the generated root or add Basic/Moat nodes to the production catalog.
