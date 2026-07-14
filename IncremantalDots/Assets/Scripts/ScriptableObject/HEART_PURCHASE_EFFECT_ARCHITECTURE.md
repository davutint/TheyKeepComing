# Castle Heart Purchase ve Effect Pipeline - Mimari

## Kapsam

`DW-E-PURCHASE`, `GeneratedRunGraph` uzerindeki node satin alimlarinin tek transaction
contract'ini kurar. Blueprint'teki su kurallari uygular:

- Heart yalniz Grave Essence harcar.
- Unlock, Evolution ve Keystone tek sefer satin alinir.
- Repeatable node `+1`, `+10` ve `Buy Max` destekler.
- Ilk satin alim, `0 -> 10` gibi bulk gecis dahil, outgoing komsulari reveal eder.
- Keystone yalniz exact es Keystone'u kilitler.
- Numeric effect'in player-facing degeri ile runtime'a uygulanacak deger ayni hesap owner'indan gelir.

Bu paket production node katalogu, Grave Essence drop orani veya denge sayisi uretmez.
`MobileCastleHudRoot.prefab` ve canli runtime adapter cutover'i E5'te; exact graph
save/restore ve deterministic effect replay E6'da tamamlanmistir.

## Transaction owner

`HeartPurchaseService` quote ve commit'in tek owner'idir. Sirasi:

1. Graph, catalog, visibility, lock, node tipi ve quantity preflight'i.
2. Exact Grave Essence maliyet quote'u.
3. Effect pipeline'in fail edebilen butun baseline/policy preflight'i.
4. `IHeartGraveEssenceWallet.TrySpendGraveEssenceAtHeart` ile tek currency harcamasi.
5. Graph level, exact Keystone partner lock, prepared effect commit ve first-purchase reveal.

Harcama oncesindeki herhangi bir hata graph level'ini, visibility'yi, lock state'ini veya
effect runtime'ini degistirmez. Harcamadan sonraki adimlar preflight edilmis ve fail etmeyen
commit adimlaridir.

`GameManager`, mevcut `GraveEssenceAmount` ve `TrySpendGraveEssenceAtHeart` metotlariyla
`IHeartGraveEssenceWallet` uygular. Heart pipeline ana `ResourceCost`/Wood/Stone/Iron/Food
harcama yolunu kabul etmez.

## Quantity semantigi

- `One`: tum node tiplerinde bir level/tek satin alim.
- `Ten`: yalniz Repeatable node'da exact 10 level. Kismi `+10` yoktur.
- `BuyMax`: yalniz Repeatable node'da mevcut Essence ile alinabilen en yuksek exact level sayisi.
- Tek seferlik node'da `+10` veya `BuyMax` fail eder; UI bu kontrolleri gostermemelidir.
- `int.MaxValue` yalniz teknik serialization limitidir; gameplay max level'i degildir.

## Maliyet formulu

Authored data `BaseGraveEssenceCost` (`long`) ve `CostGrowthPerLevel` (`double`) tasir.
Linear ve bulk-safe evaluator:

```text
growthStep = ceil(baseCost * costGrowthPerLevel)
unitCost(currentLevel) = baseCost + currentLevel * growthStep
```

Bulk toplam, arithmetic-series ile exact hesaplanir. Boylece `+10` ve `Buy Max`, ayni
level'lari arka arkaya `+1` almayla ayni toplam maliyeti verir. `Buy Max` per-level loop
yerine binary search kullanir. Maliyet `long` sinirini asarsa saturate edip sessizce
harcamak yerine `CostOverflow` ile fail eder.

Formul production balance sayisi secmez. Base cost ve growth owner review'lu catalog
icerigidir.

## Node semantigi

| Node tipi | Level kurali | Effect/reveal |
|---|---|---|
| Unlock | `0 -> 1` | Authored sistemi acar, outgoing komsular reveal olur |
| Repeatable | `0 -> N`, sonra `N -> N+M` | Her level stat raw investment'ini buyutur; yalniz ilk transaction reveal eder |
| Evolution | `0 -> 1` | Split Shot, Burning Ground veya Second Blast gibi authored behavior flag'i acar |
| Keystone | `0 -> 1` | Yalniz simetrik catalog partner'ini `KeystoneConflict` ile kilitler |

Partner disindaki normal node veya baska Keystone mutate edilmez. Hidden, kilitli veya
tek seferlik sahipli node satin alinamaz.

## Effect pipeline

`HeartEffectPipeline` iki contract'i ayni hesap state'iyle uygular:

- `IHeartEffectTransactionPlanner`: Satin alimdan once effect batch'i hazirlar; commit
  aninda raw investment ve behavior flag'lerini yazar.
- `IHeartEffectValueResolver`: UI icin gercek current, bir sonraki `+1` sonrasi actual
  ve gercek delta metnini uretir.

Numeric target anahtari effect type + archer type + resource scope'tur. Bu sayede Basic,
Rapid ve Frost degerleri veya Wood/Stone/Iron/Food degerleri ayni enum olsa bile
birbirine karismaz.

`IHeartEffectBaselineProvider`, Heart katkisi eklenmemis gercek runtime baseline'ini
saglamak zorundadir. `IHeartRuntimeEffectSink`, preflight sonrasi hesaplanan actual degeri
gercek owner'a yazar. Baseline yoksa satin alim fail-closed olur; authored `Value` UI'da
actual sonuc gibi gosterilmez.

## Numeric ve behavior destek matrisi

Linear percent/absolute destek:

- Archer damage.
- Wall Max HP ve repair cost multiplier.
- Worker capacity, resource production ve population growth.
- Arrow capacity ve arrows-per-Wood efficiency.
- Fireball damage.

Soft-cap/diminishing destek:

- Archer fire rate: `SoftCap`, maksimum bonus oraninin asimptotudur.
- Archer range ve Fireball radius: `SoftCap`, maksimum additive bonus asimptotudur.
- Fireball cooldown: `SoftCap`, maksimum reduction oraninin asimptotudur.
- Frost slow: `SoftCap`, ulasilan minimum movement multiplier asimptotudur.

Soft-cap bonusu `cap * (1 - exp(-raw / cap))` ile hesaplanir. Sonraki level'in deltasi
azalir ama pozitif kalir; node gizlice etkisiz olmaz. UI, resolver'dan gelen actual kalan
kazanimi gosterir.

Behavior destek:

- Rapid/Frost archer unlock.
- Spellcasting/Fireball unlock.
- Split Shot.
- Burning Ground.
- Second Blast.

Production catalog yalniz owner tarafindan onaylanan behavior'lari kullanir.

## Buyuk deger ve format

- Essence ve maliyet `long` ile tutulur.
- Effect authored `Value`/`SoftCap` ve runtime raw/actual hesaplari `double` kullanir.
- NaN, Infinity, negatif raw ve overflow fail eder.
- Player-facing formatter actual sayiyi invariant ve binlik ayracli verir; tahmini compact
  deger effect gerceginin yerine kullanilmaz.

## Runtime cutover ve persistence siniri

E5 tamamlandiginda `GameManager.HeartRuntime` generated graph'i run id'sinin stable seed'iyle
kurar, reveal/presentation/purchase servislerini tek runtime icinde birlestirir ve bu dosyadaki
baseline/sink contract'larini canli owner'lara baglar. `HeartScreenUI` yalniz
`HeartPurchaseService` quote/failure contract'ini tuketir; prefab uzerinde `+1/+10/MAX`,
Essence ve resolved effect satirlari vardir. Aktif scene HUD instance'inda legacy
`TechTreeUI` bulunmaz ve archer upgrade/direct unlock yuzeyleri player-facing kapatilmistir.

Production `HeartNodeCatalogSO` halen owner icerik onayi bekler. Null catalog acik hata verir;
legacy `TryBuyTechNode`, `TechTreeCatalogSO` veya ana kaynak maliyetine geri dusulmez. Legacy
API kodda save/migration uyumlulugu icin kalabilir fakat aktif UI owner'i degildir.

E6 runtime:

- Graph level/reveal/lock exact save edilir.
- Restore sirasinda `HeartEffectPipeline` graph level'larindan deterministic replay edilir.
- Replay tamamlanmadan resolver veya purchase acilmaz; graph/pipeline level mismatch fail eder.
- Catalog version uyusmazligi source asset'ten reroll veya silent mapping yapmaz.

## Test kapsami

`HeartPurchasePipelineTests` su contract'lari kanitlar:

- Bulk fiyat ile sequential fiyat esitligi ve loop'suz buy-max.
- `long` maliyet ve `double` effect buyuk deger destegi.
- Grave Essence-only unlock, ilk reveal ve behavior enable.
- Repeatable `+10` ve `Buy Max`.
- Yetersiz bakiye/missing baseline/overflow durumunda sifir mutation.
- Tek seferlik node quantity ve tekrar satin alma guard'i.
- Evolution behavior.
- Exact Keystone pair exclusion.
- Soft-cap actual delta'nin pozitif fakat azalan olmasi.
- Range, Frost slow, cooldown ve Arrow numeric hedefleri.

`HeartScreenPauseTests`, E5 canli adapter sinirini Arrow paid-level ayrimi ve runtime graph
settings kopyasiyla ek olarak kilitler.
