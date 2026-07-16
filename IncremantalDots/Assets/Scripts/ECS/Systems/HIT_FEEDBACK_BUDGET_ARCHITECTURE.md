# Hit Feedback Budget - Mimari

## Amaç

On binlerce düşman ve bin okçu altında her isabet için ayrı VFX/SFX entity'si ve
flipbook üretmek yerine, gameplay sonucunu eksiksiz uygulayıp yalnız presentation
yoğunluğunu sabit bir bütçe içinde tutmak.

## Akış

1. `ArrowHitSystem.ArrowHitJob` her gerçek isabette damage, Frost slow ve arrow pool
   return işlemlerini normal biçimde tamamlar.
2. Hit pozisyonu ve türü, `CombatHitFeedbackBudget.GetSpatialKey` ile `0.75`
   world-unit hücreye çevrilir.
3. Aynı hücre + aynı hit türü, sabit `512` kapasiteli
   `NativeParallelHashMap<int3, CombatHitFeedbackCandidate>` içinde tek örnek olur.
4. `EmitHitFeedbackJob`, candidate map'ini iki tür için sayar ve toplam `24` VFX
   slotunu dağıtır. İki tür birlikteyse normal dağılım `16 Arrow / 8 Frost` olur;
   düşük sayılı türün boş bıraktığı slotlar diğer türe geçer.
5. Arrow ve Frost için mevcut tür başına yalnız bir SFX event'i üretilir.
   `CombatSfxEvent.Multiplicity`, cue'nun temsil ettiği spatial candidate sayısıdır.
6. `CombatFeedbackBridge`, producer sınırını aşabilecek başka kaynaklara karşı
   hit flipbook oynatımını tekrar `24 / frame` ve `0.04s` aralıkla sınırlar.

## Değişmeyen Gameplay

- Damage her isabet için uygulanır.
- Frost slow her uygun isabette yenilenir.
- Pooled arrow return ve legacy destroy fallback aynı kalır.
- Yalnız VFX/SFX sunum sayısı örneklenir; combat sonucu örneklenmez.

## Telemetri

`CombatFeedbackBudgetTelemetryData` son frame ve run toplamı için spatial candidate,
VFX emitted, SFX emitted ve VFX dropped değerlerini taşır. `CombatFeedbackBridge`
ayrıca processed/played/dropped hit VFX ve aktif flipbook sayısını yayınlar.

## Kanıt

- EditMode: `CombatHitFeedbackBudgetTests` — spatial collapse, tür ayrımı ve slot
  dağıtımı.
- PlayMode: `DenseArrowHits_EmitSpatiallySampledVfxAndAggregatedSfx` — `1000` gerçek
  hit -> `40` candidate -> `24` VFX + `2` SFX.
- PlayMode: `HitFeedbackBridge_EnforcesPlaybackBudgetAndRateLimit` — `80` talep ->
  `24 played / 56 dropped`; aynı zaman penceresindeki ikinci burst `0 played`.
