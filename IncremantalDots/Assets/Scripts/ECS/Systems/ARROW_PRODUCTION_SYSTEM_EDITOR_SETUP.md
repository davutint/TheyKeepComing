# ArrowProductionSystem — Editor Setup Rehberi (M1.6)

## Gereksinimler
- Unity 6 LTS + Entities package
- `ArrowProductionSystem.cs` → `Assets/Scripts/ECS/Systems/`
- `BuildingComponents.cs` → ArrowProducer component tanimli
- `ResourceComponents.cs` → ArrowSupply component tanimli
- `BuildingConfigSO.cs` → Fletcher SO destegi

## ScriptableObject Olusturma

### BuildingConfig_Fletcher SO Asset
1. Project panelinde `Assets/ScriptableObject/` klasorune sag tikla
2. **Create → DeadWalls → Building Config**
3. Dosya adi: `BuildingConfig_Fletcher`

#### Inspector Degerleri

| Alan | Deger |
|------|-------|
| Type | Fletcher |
| Display Name | Ok Atolyesi |
| Grid Width | 3 |
| Grid Height | 3 |
| Wood Cost | 40 |
| Stone Cost | 20 |
| Iron Cost | 10 |
| Food Cost | 0 |
| Produced Resource | Wood |
| Rate Per Worker Per Min | 0 |
| Max Workers | 3 |
| Population Capacity | 0 |
| Food Cost Per Min | 0 |
| Arrows Per Worker Per Min | 10 |
| Wood Cost Per Batch Per Min | 2 |

> `RatePerWorkerPerMin = 0` — Fletcher kaynak uretmiyor, ok uretiyor. `ResourceProducer` component'i sadece `AssignedWorkers` / `MaxWorkers` icin kullanilir.

### BuildingConfig_Blacksmith SO Asset
1. Project panelinde `Assets/ScriptableObject/` klasorune sag tikla
2. **Create → DeadWalls → Building Config**
3. Dosya adi: `BuildingConfig_Blacksmith`

#### Inspector Degerleri

| Alan | Deger |
|------|-------|
| Type | Blacksmith |
| Display Name | Demirci |
| Grid Width | 3 |
| Grid Height | 3 |
| Wood Cost | 50 |
| Stone Cost | 30 |
| Iron Cost | 20 |
| Food Cost | 0 |
| Max Workers | 0 |
| Population Capacity | 0 |
| Food Cost Per Min | 0 |

> Blacksmith sadece `BuildingData` component'i tasir — baska component eklenmez. Upgrade mekanigi M2+ milestone'inda eklenecek.

### SO'lari BuildingGridManager'a Atama
1. Sahnedeki `BuildingGridManager` GameObject'ini sec
2. **Building Configs** array'ine `BuildingConfig_Fletcher` ve `BuildingConfig_Blacksmith` SO'larini surukle

## HUDController Ayarlari

### ArrowText TMP_Text Atamasi
1. HUD Canvas'ta yeni bir `TMP_Text` olustur (veya mevcut ok gostergesi)
2. Sahnedeki `HUDController` GameObject'ini sec
3. **Arrow Text** alanina olusturulan TMP_Text'i surukle
4. HUDController her frame `ArrowSupply.Current` degerini bu text'e yazar

## Manuel System Kaydi
- Manuel kayit gerekmez — `[UpdateInGroup]`, `[UpdateAfter]`, `[UpdateBefore]` attribute'lari ile otomatik siralanir

## Entity Debugger'da Dogrulama
1. Play mode'a gir
2. Fletcher yerlestir + isci ata
3. **Window → Entities → Entity Debugger** ac
4. Fletcher entity'sinde `BuildingData` + `ResourceProducer` + `ArrowProducer` component'larini dogrula
5. GameState entity'sinde `ArrowSupply.Current` artisini gozlemle
6. `ResourceConsumptionRate.WoodPerMin` degerinde Fletcher tuketiminin eklenmis oldugunu dogrula
7. Blacksmith entity'sinde SADECE `BuildingData` oldugunu dogrula

## Test Kontrol Listesi
- [ ] BuildingConfig_Fletcher SO olusturuldu mu?
- [ ] SO degerleri dogru mu? (ArrowsPerWorkerPerMin=10, WoodCostPerBatchPerMin=2, MaxWorkers=3)
- [ ] BuildingConfig_Blacksmith SO olusturuldu mu? (sadece BuildingData)
- [ ] HUDController'da ArrowText atandi mi?
- [ ] Fletcher'a isci atayinca ok uretimi basliyor mu?
- [ ] ArrowSupply.Current HUD'da dogru gorunuyor mu?
- [ ] WoodPerMin Fletcher tuketimini iceriyor mu?
- [ ] Isci sayisi artinca ok uretim hizi da artiyor mu?
- [ ] Blacksmith yerlestirince sadece BuildingData ekleniyor mu?
