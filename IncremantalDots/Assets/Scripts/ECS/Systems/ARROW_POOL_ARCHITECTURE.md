# Arrow Pool + Burst-Safe Lifetime - Mimari

## Amaç

Okçu atışları artık her projectile için `Instantiate` ve isabet/invalid target sırasında
`DestroyEntity` üretmez. Tek `Arrow.prefab` entity havuzu prewarm edilir, atışta rent
edilir ve isabet, timeout veya geçersiz hedefte aynı return yoluyla tekrar kullanılır.

Ammo ekonomisinin ayrı sahibi `ARROW_AMMO_ARCHITECTURE.md` sözleşmesidir. Pool rent'i
başarılı olmadan Arrow veya fire timer harcanmaz; başarılı rent tam `1 Arrow` tüketir.

## Veri Sözleşmesi

- `ArrowTag`: enableable aktif-projectile işaretidir. Inactive rezervde disabled kalır.
- `ArrowProjectile.RemainingLifetime`: aktif okun saniye cinsinden kalan ömrüdür.
- `ArrowPoolRuntimeData`: prewarm/expand ayarlarını, aktif/boş sayıları ve
  rent/return telemetry sayaçlarını taşır.
- `ArrowPoolAvailable`: inactive entity referanslarının LIFO buffer'ıdır.
- `ArrowPoolMember`: entity'nin havuz tarafından sahiplenildiğini ve rent generation'ını
  işaretler.

Görsel salvo seçimi ayrı component/archetype üretmez. Aktif projectile'ın
`LocalTransform.Scale` değeri temsilci için `1`, yalnız sunumdan saklanan fakat gameplay
olarak aktif ok için `0` olur.

`WaveConfigAuthoring`, aynı singleton entity üzerinde `ArrowPrefabData`,
`ArrowPoolRuntimeData` ve `ArrowPoolAvailable` buffer'ını bake eder.

## Yaşam Döngüsü

1. `ArrowPoolMaintenanceSystem`, Initialization grubunda varsayılan `1024` oku
   prewarm eder. Inactive okta `ArrowTag` disabled ve `LocalTransform.Scale = 0` olur.
2. `ArcherShootSystem`, hedef ve ammo guard'larından sonra buffer'ın sonundan bir ok
   rent eder. Rent başarısızsa fire timer, ammo ve reservation değiştirilmez.
3. Rent edilen entity'nin transform, type tint, target generation, damage, speed ve
   lifetime verileri EndSimulation ECB üzerinden yazılır; `ArrowTag` enable edilir.
   Pool `TotalRentCount` sırası ve canlı okçu sayısı yalnız transform scale'ını bounded
   temsilci sözleşmesine göre seçer.
4. `ArrowMoveSystem`, Burst-parallel job içinde lifetime'ı azaltır ve yalnız geçerli
   hedefe doğru hareket uygular.
5. `ArrowHitSystem`, isabet, lifetime timeout, disabled hedef veya generation mismatch
   durumunda tek return fonksiyonunu kullanır. Entity resetlenir, scale `0` olur,
   `ArrowTag` kapanır ve referansı buffer'a append edilir.
6. `ArrowPoolMaintenanceSystem` sonraki Initialization turunda deferred return
   sayaçlarını uzlaştırır. Önceki frame'de rezerv tükendiyse `256` entity'lik batch
   genişleme yapar.

Normal rent/return archetype değiştirmez. Yapısal instantiate yalnız ilk prewarm ve
gerçek kapasite genişlemesinde vardır. Pool üyesi olmayan legacy oklar için
`ArrowHitSystem` destroy fallback'ini korur.

## Hedef Güvenliği

Ok, atıldığı anda `EnemyPoolMember.Generation` değerini
`ArrowProjectile.TargetPoolGeneration` alanına kopyalar. Hedef pool'a dönmüşse veya
aynı entity yeni generation ile yeniden rent edilmişse ok retarget olmaz; havuzuna
döner. Böylece yeni zombiye eski okun hasarı taşınmaz.

## Save, Continue ve Restart

- Combat snapshot yalnız enabled `ArrowTag` entity'lerini kaydeder; inactive rezerv
  save'e girmez.
- `ArrowRunSaveState`, pozisyon/target/effect verisinin yanında kalan lifetime'ı tutar.
- Continue öncesinde mevcut aktif oklar rezerve döner; kaydedilen oklar havuzdan rent
  edilip exact state ile yeniden kurulur.
- Restore edilen yoğun aktif ok listesi, saved count + ordinal ile tekrar bounded görsel
  temsilci dağılımına alınır; gameplay projectile sayısı azaltılmaz.
- Eski save'de lifetime alanı `0` ise restore güvenli `5s` default'u kullanır.
- Restart aktif pool oklarını destroy etmez; `ReturnAllActive` ile rezerve döndürür.
- Pool kapasitesi ve telemetry run save'e yazılmaz; authoring ayarından yeniden kurulur.

## Ölçek Kanıtı

2026-07-16 gerçek `NewGameScene` 1.000 okçu × 10.000 düşman Editor koşusu:

- frame average: `9,77 ms`
- frame P95: `12,74 ms`
- main-thread average: `9,66 ms`
- sample sonu aktif pooled ok: `745`
- ilk salvo: `1.000` gameplay projectile / `48` görünür temsilci / stride `21`
- arrow pool: `1280` total, `3000` rent, `2255` return, `1` expansion
- draw-call average: `544`

Bu Editor regresyon kanıtıdır. Player/hardware frame-pacing kabulü Release Definition
of Done içinde açık kalır.

## Testler

- `ArrowPoolRuntimeUtilityTests`: prewarm, batch expand, rent, return, reset ve aynı
  entity reuse.
- `ArrowPoolPlayModeTests`: lifetime'ı dolan okun entity olarak var kalıp inactive
  rezerve dönmesi ve aynı entity'nin yeniden rent edilmesi.
- `ArcherTargetingPlayModeTests`: Basic/Rapid/Frost pooled projectile üretimi.
- `ExactRunContinuePlayModeTests`: stale enemy generation okun retarget olmadan pool'a
  dönmesi.
- `HordeScalePlayModeTests`: 1K × 10K birleşik telemetry ve aktif arrow pool sayaçları.
- `ArcherSalvoPresentationUtilityTests`: küçük birlik full visibility, 1K bounded
  temsilci sayısı ve ardışık salvo lane rotasyonu.
