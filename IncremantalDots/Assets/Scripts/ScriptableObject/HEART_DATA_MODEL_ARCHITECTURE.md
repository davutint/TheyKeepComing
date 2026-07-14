# Castle Heart Data Model - Mimari

## Kapsam

Bu katman Blueprint'teki procedural Castle Heart graph'inin veri sozlesmesini kurar.
Bu paket graph uretmez, Heart UI'yi degistirmez ve eski tech satin alma akisina yeni
bir owner eklemez.

## Source definition

`HeartNodeDefinitionSO` yalniz authored ve degismez iceriktir:

- Kimlik: `Id`, baslik, aciklama, ikon ve generator `Tags`.
- Sinif: `Unlock`, `Repeatable`, `Evolution`, `Keystone`.
- Yon: `Army`, `Defense`, `Production`, `HeartMagic`.
- Secim kurali: `Standard/Rare`, minimum ve maksimum depth.
- Ekonomi: `BaseGraveEssenceCost` ve `CostGrowthPerLevel` tuning girdisi.
- Etki: `HeartNodeEffect[]`.
- Conflict: yalniz Keystone icin tam bir karsi node Id'si.

`IsRepeatable`, `Type == Repeatable` sonucundan turetilir; ayni gercegi tasiyan ikinci
bir serialized bool yoktur. Cost growth'un satin alma formulu E4 effect/purchase
pipeline'inin sorumlulugudur; E1 yalniz tuning verisini tasir.

Definition asset'te level, hidden/revealed veya lock state bulunmaz. Validation;
bos Id, gecersiz depth, maliyet/growth ve Keystone conflict hatalarini raporlar.

## Generated run state

`GeneratedRunGraph` exact run state icin save-safe DTO'dur:

- `GraphVersion`, `Seed`, `RootNodeId`.
- Node Id, yon, depth, hidden/revealed, level, lock state ve lock sahibi.
- Directed `FromNodeId -> ToNodeId` edge listesi.

DTO yalniz primitive/enum/list alanlari tasir; `ScriptableObject`, `Sprite` veya baska
Unity object referansi tasimaz. Generator E2'de bu contract'i dolduracak. Graph'in
`RunSaveState` capture/restore entegrasyonu E6'ya aittir; E1'de bos/sahte graph save
edilmez.

## Grave Essence owner'i

`GraveEssence` ayri bir ECS singleton'dir ve `GameStateAuthoring` tarafindan ayni
GameState entity'sine `0` ile bake edilir. Diger dort kaynagin `ResourceData` alanina
karistirilmaz.

Runtime transaction siniri `GameManager` uzerindedir:

- `GrantGraveEssence(amount)`: gelecekteki drop owner'i icin pozitif, saturating kazanc.
- `TrySpendGraveEssenceAtHeart(cost)`: Heart satin alimlarinin tek harcama kapisi.
- `GraveEssenceAmount`: guncel bakiye.

Bu paket Essence drop kaynagi, oran veya node fiyat formulu uydurmaz. Mevcut
`TryBuyTechNode` halen legacy ResourceCost yoludur ve E4 cutover'a kadar yeni harcama
kapisina baglanmaz.

## Lifecycle ve persistence

- Exact Continue: `RunSaveState.GraveEssence` ile aynen korunur.
- Save schema: v9; v8 ve daha eski snapshot'lar acik migration ile `0` Essence alir.
- Restart/yeni run: ECS singleton `0` olur.
- Game Over: run save silinir; Essence meta save'e hic yazilmaz.
- Generated graph exact save: E6 kapsaminda, henuz bagli degil.

## Test kapsami

- Dort node tipi ve derived repeatable semantigi.
- Definition validation ve source/runtime state ayrimi.
- Generated graph JSON round-trip ve Unity asset referansi olmamasi.
- Grave Essence run/meta siniri, v8 -> v9 migration ve olumde run save silinmesi.
- PlayMode transaction, long bakiye, Continue ve Restart davranisi.

