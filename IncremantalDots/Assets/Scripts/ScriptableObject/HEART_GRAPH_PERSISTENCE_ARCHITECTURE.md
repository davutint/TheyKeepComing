# Castle Heart Exact Graph Persistence - Mimari

## Kapsam

`DW-E-SAVE`, generated Castle Heart graph'ini `RunSaveState v10` icinde exact run state
olarak saklar. Continue yeni graph uretmez; saved graph'i clone eder, structural/runtime
state validation'dan gecirir ve satin alinmis node effect'lerini level state'inden replay eder.

Production node, maliyet, Keystone veya Evolution icerigi bu paket tarafindan uretilmez.

## Save contract

`RunSaveState.HeartGraph` su primitive state'i birlikte tasir:

- `GraphVersion`, `CatalogVersion`, run seed ve root Id;
- sirali node Id, branch ve depth;
- exact edge listesi;
- hidden/revealed state;
- level;
- Keystone lock state ve `LockedByNodeId`.

Unity `JsonUtility` null nested class'i default object olarak yazabildigi icin
`RunSaveState.HasHeartGraph` discriminator'i payload varliginin tek otoritesidir. False
iken serialize edilmis default `HeartGraph` payload'i ignore edilir.

`HeartGraphPersistenceUtility.CloneExact`, runtime listeleriyle save DTO arasinda referans
paylasmaz. Graph save'e girdikten sonra source veya clone mutation'i digerini degistiremez.

## Catalog version kapisi

`HeartNodeCatalogSO.CatalogVersion` sifirdan buyuk olmak zorundadir. Launch catalog'unda
node, effect, cost, tag, branch, conflict veya player-facing icerik degistiginde version
artirilir. Saved `CatalogVersion` aktif catalog ile ayni degilse Continue graph'i yeni
catalog'a map etmez ve yeni graph zar atmaz; acik hata ile reddeder.

## Capture ve Continue sirasi

Capture:

1. Production catalog yoksa `HasHeartGraph = false` olarak acik content gate durumunda kalir.
2. Catalog varsa `GameManager` runtime graph'i hazirlar.
3. Runtime reveal/level/lock state'i restore validator'undan gecirilir.
4. Exact deep clone `RunSaveState.HeartGraph` alanina yazilir.

Continue:

1. Saved graph, `RestartGame` state mutation'indan once preflight edilir.
2. Graph/catalog/version/edge/node/guarantee/Keystone state'i uyumsuzsa Continue reddedilir.
3. Base run state ve Arrow paid seviyeleri restore edilir.
4. Satin alinmis node'lar depth + Id sirasinda `HeartEffectPipeline` icine replay edilir.
5. Deferred sink butun replay basarili olmadan runtime owner'lara effect uygulamaz.
6. Numeric/behavior effect'ler aktive edilir; Arrow current son effective capacity ile bir
   kez clamp edilir.
7. Wall current HP, worker allocation ve kalan exact state Heart aggregate'i kurulduktan
   sonra restore edilir.

Bu sira Arrow current'in parcali capacity replay sirasinda erken kirpilmasini ve Wall
current HP'nin Heart'siz max HP'ye clamp edilmesini engeller.

## Runtime validation

Restore gate su durumlari reddeder:

- graph veya catalog version uyusmazligi;
- duplicate/unknown node ve invalid/disconnected edge;
- eksik Rapid/Frost/Fireball/Wall guarantee veya branch sink;
- purchased hidden node, negatif level ve non-repeatable level `> 1`;
- normal node lock'i;
- exact ve simetrik olmayan Keystone lock/partner state'i;
- catalog'da bulunmayan saved node/effect replay'i.

Validation saved graph'i mutate etmez. Structural kontrol icin exact clone initial state'e
normalize edilir; gercek reveal/level/lock degerleri ayri runtime-state kontrolunde kalir.

## Migration

Schema `v9 -> v10` migration'i Grave Essence'i korur fakat v9'da bulunmayan graph'i
uydurmaz. `HasHeartGraph = false` acikca korunur ve Continue edilen bu run'da Heart yeni
catalog'dan uretilmez. Boylece eski save sessizce farkli roguelike sonucuna donusmez.

## Performans

Graph clone/validation/replay maliyeti `O(N + E)` ve yalniz save/Continue/Heart capture
sinirinda calisir. Frame loop'a yeni is eklenmez. Combat snapshot boyutuna gore Heart graph
kucuk ve bounded run state'tir.
