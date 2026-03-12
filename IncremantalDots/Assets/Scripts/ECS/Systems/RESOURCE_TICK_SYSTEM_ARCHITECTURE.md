# ResourceTickSystem - Mimari (M1.1)

## Genel Bakis
Kaynak uretim/tuketim tick sistemi. Her frame net hizi hesaplar, accumulator'a ekler,
±1.0 gecince ResourceData int'e transfer eder.

## Dosya: ResourceTickSystem.cs

### Ozellikler
- `[BurstCompile]` — struct + OnUpdate
- `[UpdateInGroup(typeof(SimulationSystemGroup))]`
- `[UpdateBefore(typeof(WaveSpawnSystem))]` — frame basinda calisir (sistem sirasi 0)
- GameOver'da calismaz (`IsGameOver` kontrolu)
- Performans: <0.01ms (sadece singleton okuma/yazma)

### Algoritma
```
1. Net hiz = ProductionRate - ConsumptionRate (per-minute)
2. Accumulator += netHiz * dt / 60f
3. Accumulator >= +1.0 → int'e pozitif transfer
4. Accumulator <= -1.0 → int'ten negatif transfer (0 siniri)
5. Kaynak yetersizse → resource = 0, accumulator = 0
```

### TransferAccumulator Mantigi
- Pozitif birikim: `(int)accumulator` kadar ekle, accumulator'dan cikart
- Negatif birikim: `ceil(abs(accumulator))` kadar cikart
  - Kaynak yeterliyse: normal transfer
  - Kaynak yetersizse: resource=0, accumulator=0 (borcsuz)

## Sistem Sirasi
```
SimulationSystemGroup:
 0. ResourceTickSystem ← BURASI
 1. WaveSpawnSystem
 2. ArcherShootSystem
 ...
```

## Singleton Erisim
- `SystemAPI.GetSingleton<ResourceProductionRate>()` (read-only)
- `SystemAPI.GetSingleton<ResourceConsumptionRate>()` (read-only)
- `SystemAPI.GetSingletonRW<ResourceData>()` (int transfer)
- `SystemAPI.GetSingletonRW<ResourceAccumulator>()` (float birikim)

## GameManager Entegrasyonu
- `GameManager.Resources` — ResourceData okunur (her frame)
- `GameManager.ResourceProduction` — ResourceProductionRate okunur
- `GameManager.ResourceConsumption` — ResourceConsumptionRate okunur
- `GameManager.RestartGame()` — 4 component reset edilir

## HUD Entegrasyonu
- `HUDController` — 4 TMP_Text (WoodText, StoneText, IronText, FoodText)
- Format: `"Ahsap: 150 (+5.0/dk)"`, negatif: `"Yemek: 42 (-2.0/dk)"`, sifir: `"Demir: 34"`
- String alloc caching: sadece deger degisince guncellenir
