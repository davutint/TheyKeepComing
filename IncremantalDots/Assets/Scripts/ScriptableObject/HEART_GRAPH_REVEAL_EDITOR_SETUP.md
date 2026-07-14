# Castle Heart Reveal ve Player Information - Editor Setup

## Bu pakette scene kurulumu yok

`DW-E-REVEAL` pure run-state ve presentation contract'idir. Production Heart catalog
asset'i owner onayi bekledigi ve aktif prefab halen legacy `TechTreeUI` kullandigi icin
bu pakette scene/prefab binding yapma.

## E4 purchase entegrasyonu

Yeni Heart purchase owner'i eklendiginde transaction sirasi su olmali:

1. Node'un gorunur, unlocked ve alinabilir oldugunu dogrula.
2. Grave Essence'i tek transaction kapisindan harca.
3. Node level'ini arttir ve effect'i uygula.
4. Transaction oncesi level'i `previousLevel` olarak sakla ve basarili alimdan sonra
   `HeartGraphRevealService.RevealAfterFirstPurchase(graph, nodeId, previousLevel)` cagir.
5. Reveal sonucunda yalniz `NewlyRevealedNodeIds` icin UI notification uret.

Repeatable `+10` veya Buy Max transaction'i `0 -> N` gecisi yapiyorsa servis bunu ilk
alim kabul eder. Sonraki transaction'larda `previousLevel > 0` oldugu icin reveal no-op'tur.

E4 effect aggregate owner'i `IHeartEffectValueResolver` uygulamali. Resolver authored
`Value` alanini dogrudan UI metnine cevirmez; soft-cap/diminishing return sonrasi gercek
current, after-purchase ve delta degerlerini dondurur.

## E5 prefab/UI entegrasyonu

Heart UI yalniz `HeartGraphPresentationBuilder` ciktisini tuketmelidir:

- Hidden node icin `ExactNodeId`, title, description, icon ve effects okumaya calisma.
- Hidden slotu `SlotId + Branch + Depth` ile yerlestir.
- Edge'leri `FromSlotId -> ToSlotId` ile ciz ve branch rengine boya.
- `IsExactContentVisible == false` iken tooltip veya buy button acma.
- `EffectInformationComplete == false` iken node satin alimini UI'da acma; acik hata ver.
- `KeystoneConflict` varsa karsi basligi node kartinda goster ve
  `ConflictingChoiceSlotId` hedefini net conflict marker ile isaretle.
- `WillLockOnPurchase`, `IsAlreadyLockedByThisChoice` ve
  `SourceIsLockedByConflictingChoice` durumlarini ayni warning stiliyle karistirma;
  secim oncesi, secilmis ve kilitlenmis state'ler ayri okunmalidir.
- Hidden Keystone partnerinin internal Id'sini veya diger authored bilgisini UI cache'ine
  kopyalama.

Aktif UI source-of-truth:

`Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab`

Eski UI export/import pipeline'ini yeniden kullanma. Prefab cutover E5'te prefab stage
ve setup binding tool'u birlikte guncellenerek yapilmalidir.

## E6 save/load entegrasyonu

Exact snapshot `GeneratedRunGraph` icindeki node Id, edge, visibility, level, lock ve
locked-by state'ini oldugu gibi kaydetmelidir. Continue:

- Generator'i tekrar cagirmamali.
- `InitializeRunVisibility` ile eski reveal state'ini yeniden genisletmemeli.
- Saved graph version/catalog uyumsuzlugunda sessiz reroll yapmamali.
- Presentation'i restore edilen exact graph'tan yeniden kurmalidir.

## Hedefli test

EditMode suite:

`DeadWalls.Tests.HeartGraphRevealTests`
