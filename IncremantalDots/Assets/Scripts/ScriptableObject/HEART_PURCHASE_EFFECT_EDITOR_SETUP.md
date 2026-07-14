# Castle Heart Purchase ve Effect Pipeline - Editor Setup

## Mevcut durum

E4 transaction/effect cekirdegi ile E5 runtime/prefab cutover'i hazirdir. Aktif
`NewGameScene/MobileCastleHudRoot` `HeartScreenUI` kullanir; legacy `TechTreeUI` aktif owner
degildir. Production Heart catalog owner icerik onayi bekler. Bu belge mevcut legacy
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

## Runtime adapter binding'i

`GameManager.HeartRuntime` su iki adapter'i saglar:

- `IHeartEffectBaselineProvider`: Heart bonusu eklenmemis actual baseline degerleri.
- `IHeartRuntimeEffectSink`: Hazirlanmis numeric actual deger ve behavior enable uygulamasi.

Baseline yakalama, mevcut legacy `_tech...` multiplier'larini Heart sonucu diye tekrar
okumamali. Aksi halde her satin alimda compound drift olusur.

Sink su owner'lara route edilir:

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

Aktif isim sozlesmesi ve Play Mode QA icin
`MonoBehaviour/HEART_SCREEN_EDITOR_SETUP.md` dosyasini kullan. `GameManager.heartCatalog`
null ise panel acilir/pause calisir fakat graph fail-closed hata gosterir; legacy catalog'a
fallback yapilmaz.

## E6 restore binding

Continue sirasindaki canli binding:

1. Exact graph DTO restore preflight'inden gecer.
2. Effect pipeline sifir state ve deferred sink ile olusturulur.
3. Satin alinmis node'lar depth + Id sirasinda deterministic replay edilir.
4. Butun replay basarili olunca sink aktive edilir ve UI/resolver acilir.
5. Graph/pipeline/catalog uyusmazligi Continue'i acik hata ile reddeder.

Source catalog'tan yeni fiyat veya yeni graph zar atarak eski save'i degistirme.

## Dogrulama

- Hedefli EditMode: `DeadWalls.Tests.HeartPurchasePipelineTests`.
- Full EditMode regression.
- Gercek prefab uzerinde +1/+10/Buy Max ve Keystone UI QA.
- `HeartGraphContinuePlayModeTests` exact Continue replay ve level/lock JSON testi.
