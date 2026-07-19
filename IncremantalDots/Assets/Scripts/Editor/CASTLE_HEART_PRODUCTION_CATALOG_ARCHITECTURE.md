# Castle Heart Production Catalog Architecture

`CastleHeartProductionCatalogBuilder` is the canonical editor owner for the V1 production
`HeartNodeCatalogSO`. It performs a clean migration from useful legacy tech concepts without
mutating or deleting the legacy `TechTreeCatalogSO` assets.

## Ownership

- Production catalog: `Assets/ScriptableObject/MobileCastle/CastleHeart/HeartNodeCatalog.asset`
- Authored nodes: `Assets/ScriptableObject/MobileCastle/CastleHeart/Nodes`
- Launch content/effect authority: `Assets/Docs/DEAD_WALLS_V1_CASTLE_HEART_LAUNCH_CATALOG.md`
- Runtime graph owner: `HeartGraphGenerator` + `HeartGraphValidator`
- Purchase/effect owner: `HeartPurchaseService` + `HeartEffectPipeline`
- Scene binding: serialized `GameManager.heartCatalog`
- Player-facing presentation: `GameplayHUDToolkitUI.CastleHeart`

The generated root remains system-owned as `castle_heart`; no authored asset may reuse that Id.
Basic Archer and dormant Moat routes are deliberately excluded. All purchase costs are Grave
Essence, and the builder validates several deterministic seeds before accepting the catalog.

Every migrated node records its dormant `TechTreeCatalogSO` source Id in
`LegacySourceNodeIds`. This field is provenance only: it never supplies runtime state, old
resource costs, prerequisites, reveal edges or purchased levels. Eighteen useful legacy concepts
map once into the launch catalog; `castle_heart`, `basic_archer`, `moat_flame` and `moat_dig`
remain deliberately excluded.

Catalog version changes are compatibility boundaries. Increment `CatalogVersion` only when an
intentional launch-catalog change should reject an older exact graph instead of silently mapping it.
Presentation copy and provenance-only edits do not require a version bump when node Id, effect,
cost, conflict and generator eligibility remain identical.

## Pixel icon ownership

`GetIconPath(nodeId)` is the persistent icon source of truth for all 37 production nodes. Paths
are selected from `Assets/RPG Icons Pixel Art` by inspected visual family and actual image content,
not by filename guesswork. Both full catalog rebuild and the narrow
**Apply Castle Heart Pixel Icon Map** command use the same mapping, so a future rebuild cannot
silently restore the retired generated HUD icons.
