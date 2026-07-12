# ArrowProductionSystem - Mimari

## V1 sözleşmesi

Dead Walls V1 castle loop'unda Fletcher, worker queue ve pasif Wood karşılığı arrow üretimi yoktur. `MobileCastleCombatConfig` bulunduğunda `ArrowProductionSystem` hiçbir `ArrowSupply` veya `ResourceConsumptionRate` değişikliği yapmadan çıkar.

Arrow tüketiminin owner'ı `ArcherShootSystem`dır. Wood ile anlık arrow satın alma davranışı Package D - Castle Defense içinde tamamlanacaktır; bu sistem o transaction'ın owner'ı olmayacaktır.

## Legacy uyumluluk

Mobile/castle config bulunmayan eski sahnelerde sistem mevcut `ArrowProducer` component'lerini tarar:

- `ArrowsPerWorkerPerMin` ile `ArrowSupply.Accumulator` ve `Current` artar.
- `WoodCostPerBatchPerMin` ile legacy `ResourceConsumptionRate.WoodPerMin` artar.

Bu legacy içerik V1 scene'e davranış sızdıramaz; prefab ve ScriptableObject'ler ileride migration/debug amacıyla dormant kalabilir.
