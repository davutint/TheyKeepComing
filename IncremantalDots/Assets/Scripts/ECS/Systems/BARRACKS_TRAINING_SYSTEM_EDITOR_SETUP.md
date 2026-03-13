# BarracksTrainingSystem — Editor Setup Rehberi (M1.6)

## Gereksinimler
- Unity 6 LTS + Entities package
- `BarracksTrainingSystem.cs` → `Assets/Scripts/ECS/Systems/`
- `BuildingComponents.cs` → ArcherTrainer component tanimli
- `BuildingConfigSO.cs` → Kisla SO destegi

## ScriptableObject Olusturma

### Adim 1: BuildingConfig_Barracks SO Asset
1. Project panelinde `Assets/ScriptableObject/` klasorune sag tikla
2. **Create → DeadWalls → Building Config**
3. Dosya adi: `BuildingConfig_Barracks`

### Adim 2: Inspector Degerleri

| Alan | Deger |
|------|-------|
| Type | Barracks |
| Display Name | Kisla |
| Grid Width | 4 |
| Grid Height | 4 |
| Wood Cost | 60 |
| Stone Cost | 40 |
| Iron Cost | 0 |
| Food Cost | 0 |
| Max Workers | 0 |
| Population Capacity | 0 |
| Food Cost Per Min | 0 |
| Training Duration | 30 |
| Food Cost Per Archer | 20 |
| Wood Cost Per Archer | 10 |

### Adim 3: BuildingGridManager'a Ata
1. Sahnedeki `BuildingGridManager` GameObject'ini sec
2. **Building Configs** array'ine `BuildingConfig_Barracks` SO'yu surukle

## GameStateAuthoring Ayarlari

### InitialArrows Field
- Sahnedeki `GameStateAuthoring` GameObject'ini sec
- **Initial Arrows** alanini ayarla (varsayilan: 50)
- Bu deger oyun basinda ArrowSupply singleton'una yazilir

### ArcherPrefabData
- `GameStateAuthoring` uzerinde **Archer Prefab** alani olmali
- Okcu prefab'ini surukle (SubScene icinde bake edilir)

## Manuel System Kaydi
- Manuel kayit gerekmez — `[UpdateInGroup]`, `[UpdateAfter]`, `[UpdateBefore]` attribute'lari ile otomatik siralanir

## Entity Debugger'da Dogrulama
1. Play mode'a gir
2. Kisla yerlestir
3. **Window → Entities → Entity Debugger** ac
4. Kisla entity'sinde `BuildingData` + `ArcherTrainer` component'larini dogrula
5. Egitim baslatildiginda `ArcherTrainer.IsTraining = true` ve `TrainingTimer` artisini gozlemle
6. Timer bitince yeni Archer entity'sinin olusmasi + `PopulationState.Archers` artisini dogrula

## Test Kontrol Listesi
- [ ] BuildingConfig_Barracks SO olusturuldu mu?
- [ ] SO degerleri dogru mu? (TrainingDuration=30, FoodCost=20, WoodCost=10)
- [ ] GameStateAuthoring'de InitialArrows ayarlandi mi?
- [ ] GameStateAuthoring'de ArcherPrefab atandi mi?
- [ ] Kisla yerlestirince ArcherTrainer component'i eklenyor mu?
- [ ] Yeterli kaynak + idle nufus varken egitim basliyor mu?
- [ ] Yetersiz kaynak/nufus varken egitim baslamiyor mu?
- [ ] Timer bitince okcu entity spawn oluyor mu?
- [ ] PopulationState.Archers dogru artiyor mu?
