# ResourceTickSystem - Mimari (M1.1)

## Genel Bakis
Kaynak uretim/tuketim tick sistemi. Her frame net hizi hesaplar, accumulator'a ekler,
1.0 esigi gecince `ResourceData` int degerlerine transfer eder.

## Dosya: ResourceTickSystem.cs

### Ozellikler
- `[BurstCompile]` - struct + OnUpdate
- `[UpdateInGroup(typeof(SimulationSystemGroup))]`
- `[UpdateBefore(typeof(WaveSpawnSystem))]` - frame basinda calisir
- GameOver'da calismaz (`IsGameOver` kontrolu)
- Mobile castle mode'da economy focus uygulanmis effective production kullanir
- Performans: singleton okuma/yazma disinda ek allocation yapmaz

### Algoritma
```
1. Mobile castle mode varsa EconomyFocusState ile effective ProductionRate hesapla
2. Net hiz = effective ProductionRate - ConsumptionRate (per-minute)
3. Accumulator += netHiz * dt / 60f
4. Accumulator >= +1.0 ise int'e pozitif transfer
5. Accumulator <= -1.0 ise int'ten negatif transfer (0 siniri)
6. Kaynak yetersizse resource = 0, accumulator = 0
```

### TransferAccumulator Mantigi
- Pozitif birikim: `(int)accumulator` kadar ekle, accumulator'dan cikart.
- Negatif birikim: `ceil(abs(accumulator))` kadar cikart.
- Kaynak yeterliyse normal transfer yapar.
- Kaynak yetersizse resource `0`, accumulator `0` olur.

## Singleton Erisim
- `SystemAPI.GetSingleton<ResourceProductionRate>()` (read-only)
- `SystemAPI.GetSingleton<ResourceConsumptionRate>()` (read-only)
- `SystemAPI.GetSingleton<MobileCastleCombatConfig>()` + `EconomyFocusState` (opsiyonel mobile focus)
- `SystemAPI.GetSingletonRW<ResourceData>()` (int transfer)
- `SystemAPI.GetSingletonRW<ResourceAccumulator>()` (float birikim)

## GameManager Entegrasyonu
- `GameManager.Resources` - ResourceData okunur.
- `GameManager.ResourceProduction` - base ResourceProductionRate okunur.
- `GameManager.ResourceConsumption` - ResourceConsumptionRate okunur.
- `GameManager.GetEffectiveResourceProduction()` - mobile HUD icin focus uygulanmis hizlari dondurur.
- `GameManager.RestartGame()` - resource component'lerini ve mobile focus'u resetler.

## HUD Entegrasyonu
- `HUDController` - `WoodText`, `StoneText`, `IronText`, `FoodText`.
- Mobile HUD rate gosterimi effective production kullanir.
- String alloc caching: sadece deger degisince guncellenir.
