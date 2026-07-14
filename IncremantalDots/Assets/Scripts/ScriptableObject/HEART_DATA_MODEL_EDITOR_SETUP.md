# Castle Heart Data Model - Editor Kurulumu

## Bu pakette gereken scene ayari

Ek GameObject veya component eklenmez. `GameStateAuthoring` Baker'i `GraveEssence`
singleton'ini mevcut GameState entity'sine ekler.

- `InitialGraveEssence`: V1 normal run icin `0` kalir.
- Unity refresh sonrasinda SubScene yeniden bake edilmelidir.
- Runtime'da entity uzerinde `GraveEssence.Current = 0` gorulmelidir.

## Heart node asset'i

Menu yolu:

`Create > DeadWalls > Castle Heart > Heart Node Definition`

E1 yalniz contract'i kurar. Production node asset'lerini toplu olusturma veya legacy
asset'leri otomatik donusturme bu paketin parcasi degildir. Grave Essence maliyetleri,
node siniflari ve Keystone esleri migration review'u olmadan tahmin edilmemelidir.

Elle deneme asset'i olusturulursa:

1. Benzersiz `Id`, yon, node tipi ve depth araligi girilir.
2. `BaseGraveEssenceCost > 0`, `CostGrowthPerLevel >= 0` tutulur.
3. Yalniz Keystone icin tam bir karsi Keystone Id'si yazilir.
4. Asset'te runtime level/reveal/lock verisi aranmaz; bunlar generated run state'tir.

Catalog asset'inde `CatalogVersion >= 1` olmalidir. Node/effect/cost/conflict veya
player-facing content degisikliginde version artirilir; eski run graph'i yeni catalog'a
sessizce map edilmez. Ayrintili kural `HEART_GRAPH_PERSISTENCE_EDITOR_SETUP.md` dosyasindadir.

## Dogrulama

- EditMode: `HeartDataModelTests` ve `RunPersistenceTests`.
- PlayMode: `ExactRunContinuePlayModeTests.GraveEssence_UsesHeartTransactionPersistsOnContinueAndResetsWithRun`.
- Unity Console'da compile error olmamalidir.

Graph layout, guarantee node'lari, reveal ve fallback testi E2/E3 kapsamindadir.
Heart panel pause, +1/+10/Buy Max ve effect uygulama E4/E5 kapsamindadir.
