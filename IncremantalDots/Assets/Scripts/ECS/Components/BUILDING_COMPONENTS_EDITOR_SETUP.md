# Building Components — Editor Setup Rehberi

## Gereksinimler
- Unity 6 LTS + Entities package
- `BuildingComponents.cs` → `Assets/Scripts/ECS/Components/`
- `BuildingConfigSO.cs` → `Assets/Scripts/ScriptableObject/`

## ScriptableObject Olusturma

### Adim 1: SO Asset Olustur
1. Project panelinde `Assets/ScriptableObject/` klasorune sag tikla
2. **Create → DeadWalls → Building Config**
3. Dosyayi bina tipine gore adlandir: `Farm_Config`, `House_Config` vs.

### Adim 2: SO Degerlerini Ayarla (Inspector)

**Ornek: Farm (Ciftlik)**
| Alan | Deger |
|------|-------|
| Type | Farm |
| Display Name | Ciftlik |
| Grid Width | 3 |
| Grid Height | 3 |
| Wood Cost | 30 |
| Stone Cost | 0 |
| Iron Cost | 0 |
| Food Cost | 0 |
| Produced Resource | Food |
| Rate Per Worker Per Min | 2.0 |
| Max Workers | 5 |
| Population Capacity | 0 |
| Food Cost Per Min | 0 |

**Ornek: House (Ev)**
| Alan | Deger |
|------|-------|
| Type | House |
| Display Name | Ev |
| Grid Width | 3 |
| Grid Height | 3 |
| Wood Cost | 40 |
| Stone Cost | 20 |
| Iron Cost | 0 |
| Food Cost | 0 |
| Max Workers | 0 |
| Population Capacity | 5 |
| Food Cost Per Min | 1.0 |

### Adim 3: BuildingGridManager'a Ata
1. Sahnedeki `BuildingGridManager` GameObject'ini sec
2. **Building Configs** array'ine SO asset'lerini surukleDaha fazla SO M1.4+ milestone'larinda olusturulacak.

## Entity Debugger'da Dogrulama
1. Play mode'a gir
2. Bir bina yerlestir
3. **Window → Entities → Entity Debugger** ac
4. Yeni entity'de `BuildingData` component'ini gor
5. Kaynak binasi ise `ResourceProducer` component'ini kontrol et
6. Ev ise `PopulationProvider` + `BuildingFoodCost` kontrol et

## Test Kontrol Listesi
- [ ] SO asset olusturulabiliyor mu? (Create menu)
- [ ] Inspector'da tum alanlar gorunuyor mu?
- [ ] BuildingGridManager.BuildingConfigs'e atanabiliyor mu?
- [ ] Play mode'da bina yerlestirince entity olusturuluyor mu?
