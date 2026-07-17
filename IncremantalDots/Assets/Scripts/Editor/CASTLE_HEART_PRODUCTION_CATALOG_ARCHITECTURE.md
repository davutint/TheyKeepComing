# Castle Heart Production Catalog Architecture

`CastleHeartProductionCatalogBuilder` is the canonical editor owner for the V1 production
`HeartNodeCatalogSO`. It performs a clean migration from useful legacy tech concepts without
mutating or deleting the legacy `TechTreeCatalogSO` assets.

## Ownership

- Production catalog: `Assets/ScriptableObject/MobileCastle/CastleHeart/HeartNodeCatalog.asset`
- Authored nodes: `Assets/ScriptableObject/MobileCastle/CastleHeart/Nodes`
- Runtime graph owner: `HeartGraphGenerator` + `HeartGraphValidator`
- Purchase/effect owner: `HeartPurchaseService` + `HeartEffectPipeline`
- Scene binding: serialized `GameManager.heartCatalog`
- Player-facing presentation: `HeartScreenUI`

The generated root remains system-owned as `castle_heart`; no authored asset may reuse that Id.
Basic Archer and dormant Moat routes are deliberately excluded. All purchase costs are Grave
Essence, and the builder validates several deterministic seeds before accepting the catalog.

Catalog version changes are compatibility boundaries. Increment `CatalogVersion` only when an
intentional launch-catalog change should reject an older exact graph instead of silently mapping it.

