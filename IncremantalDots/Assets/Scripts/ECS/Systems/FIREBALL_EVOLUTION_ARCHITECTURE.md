# Fireball Evolution Systems — Mimari

## Amaç ve sahiplik

`FireballStrikeSystem` primary impact'i tek authoritative giriş olarak işler. Cast anındaki
`FireballEvolutionFlags` üzerinden yalnız primary strike şu state'leri kurabilir:

- `FireballDelayedBlast` -> `FireballSecondBlastSystem`
- `FireballBurningGround` -> `FireballBurningGroundSystem`

Secondary ve pulse strike'lar evolution flag taşımadığı için kendilerini tekrar üretemez.

## Sistem sırası

Her iki evolution sistemi `SimulationSystemGroup` içinde `FireballProjectileSystem` sonrasında ve
`FireballStrikeSystem` öncesinde çalışır. Üretilen strike'lar EndSimulation ECB ile yaratılır ve bir
sonraki simulation update'inde ortak damage job'una girer. Bu bir-frame command-buffer sınırı exact
delay/tick state'inin sahibini değiştirmez.

## Scorched Earth

State kalan duration, sonraki tick süresi, exact kalan tick sayısı, merkez, radius ve
damage-per-tick değerlerini taşır. Sistem büyük delta-time ve float sınırında bile launch
sözleşmesindeki beş pulse'u exact catch-up eder; bütün tick'ler işlendiğinde alan entity'sini
siler. Pulse başına tek aggregate strike vardır.

## Echoing Detonation

Timer yalnız kalan delay, merkez, radius ve damage taşır. Süre sıfıra indiğinde tek
`SecondBlast` strike üretir ve timer entity'sini siler.

## Save ve sunum sınırı

`GameManager` projectile, pending strike, delayed blast ve burning ground state'ini `RunSaveState
v16` içine alır. `SpellCastUI` state'i yalnız okur; gameplay timer veya damage üretmez. Burning
ground pulse'ları per-enemy VFX/SFX event üretmez.

## Doğrulama

- `SpellFeedbackHierarchyTests.FireballEvolutions_LockExactCombatAndFixedRendererVfxDirection`
- `SpellFeedbackHierarchyPlayModeTests.FireballEvolutions_ApplyExactAggregateDamageAndFixedGroundPresentation`
- `SpellFeedbackHierarchyPlayModeTests.FireballEvolutionRuntimeState_SaveAndContinueRestoresExactTimersAndTicks`
- `RunPersistenceTests.JsonRoundTrip_PreservesExactCycleCombatCouncilAndAbilityState`
- `RunPersistenceTests.TryLoad_Version15Snapshot_MigratesToEmptyFireballEvolutionRuntimeState`
