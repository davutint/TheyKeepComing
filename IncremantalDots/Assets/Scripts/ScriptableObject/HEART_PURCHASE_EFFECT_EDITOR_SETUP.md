# Castle Heart Purchase ve Effect Pipeline - Editor Setup

## Mevcut durum

E4 transaction/effect cekirdegi hazirdir ancak production Heart catalog ve prefab cutover
henuz yapilmamistir. Bu belge future binding sirasini tanimlar; mevcut legacy
`TechTreeCatalogSO` asset'lerini otomatik migrate etmez.

## Production node authoring gate

Owner review olmadan su alanlari doldurmayin:

- `BaseGraveEssenceCost` ve `CostGrowthPerLevel`.
- Unlock/Repeatable/Evolution/Keystone sinifi.
- Branch, depth, rarity ve generator tag'leri.
- Keystone partnerleri.
- Effect `Value` ve `SoftCap` sayilari.
- Split Shot, Burning Ground veya Second Blast launch pool'u.

Legacy Wood/Stone/Iron/Food fiyatini otomatik Grave Essence'a cevirmeyin.

## Effect authoring kurallari

1. Numeric effect'te `Value > 0` ve finite olmali.
2. Fire rate, archer range, Frost slow, spell radius ve spell cooldown effect'lerinde
   `SoftCap` zorunludur.
3. Cooldown reduction ve Frost minimum multiplier `SoftCap` degeri `0 < cap < 1` olmali.
4. Frost slow effect'i `ArcherType.Frost` hedeflemeli.
5. Ayni effect target'ina yazan farkli node'lar ayni `SoftCap` policy'sini kullanmali.
6. Evolution yalniz onayli behavior effect enum'unu kullanmali.
7. `None` effect production asset'te birakilmamali.

`ArcherType` damage/fire-rate/range target'ini ayirir. Her archer turu icin ayri actual
baseline gerekiyorsa definition'da ayri effect satiri yazin. `EconomyFocusType.Balanced`
ve tekil resource seciminin actual baseline anlami runtime adapter tarafinda acik olmalidir.

## Runtime adapter gereksinimi

E5 binding'den once `GameManager` veya ayri run owner'i su iki adapter'i saglamali:

- `IHeartEffectBaselineProvider`: Heart bonusu eklenmemis actual baseline degerleri.
- `IHeartRuntimeEffectSink`: Hazirlanmis numeric actual deger ve behavior enable uygulamasi.

Baseline yakalama, mevcut legacy `_tech...` multiplier'larini Heart sonucu diye tekrar
okumamali. Aksi halde her satin alimda compound drift olusur.

Sink en az su owner'lara route edilmelidir:

- Basic/Rapid/Frost archer stats ve unlock state.
- Tek Wall Max HP/repair multiplier.
- Worker config capacity/production/population growth aggregate'i.
- `ArrowSupply` capacity/efficiency state'i.
- Fireball unlock/damage/radius/cooldown ve onayli evolution flag'leri.

Sink metotlari commit aninda fail etmemelidir. Eksik entity/config/pool kontrolu
`TryPrepare` oncesi baseline/preflight katmaninda tamamlanmalidir.

## E5 UI binding

`Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab` tek UI truth source'udur.
Eski export/import pipeline'ini kullanmayin.

Heart node UI:

- Unlock/Evolution/Keystone: tek buy kontrolu.
- Repeatable: `+1`, `+10`, `Buy Max`.
- Quote: exact total Grave Essence maliyeti ve kalan bakiye.
- Numeric row: `IHeartEffectValueResolver` current/after/delta.
- `EffectInformationComplete == false`: buy control kapali, authored raw degeri actual diye yazma.
- Keystone: `HeartGraphPresentation` partner basligi/safe slot marker'i.
- Failure reason: hidden/locked/need Essence/max/invalid content ayrimini koru.

## E6 restore binding

Continue sirasinda:

1. Exact graph DTO'yu restore et.
2. Effect pipeline'i sifir state ile olustur.
3. Satin alinmis node'lari graph level sirasindan deterministic replay et.
4. Baseline/sink apply bittikten sonra UI/resolver'i ac.
5. Graph level ile pipeline tracked level uyusmazsa sessizce devam etme.

Source catalog'tan yeni fiyat veya yeni graph zar atarak eski save'i degistirme.

## Dogrulama

- Hedefli EditMode: `DeadWalls.Tests.HeartPurchasePipelineTests`.
- Full EditMode regression.
- E5 sonrasinda gerçek prefab uzerinde +1/+10/Buy Max ve Keystone UI QA.
- E6 sonrasinda Continue replay ve exact balance/level/lock PlayMode testi.
