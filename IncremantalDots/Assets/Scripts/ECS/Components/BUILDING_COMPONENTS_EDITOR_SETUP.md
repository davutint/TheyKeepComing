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
| Required Zone | None |
| Zone Proximity Radius | 3 |
| Population Capacity | 0 |
| Food Cost Per Min | 0 |

**Ornek: Lumberjack (Oduncu)**
| Alan | Deger |
|------|-------|
| Type | Lumberjack |
| Display Name | Oduncu |
| Grid Width | 3 |
| Grid Height | 3 |
| Wood Cost | 20 |
| Produced Resource | Wood |
| Rate Per Worker Per Min | 5.0 |
| Max Workers | 5 |
| Required Zone | Forest |
| Zone Proximity Radius | 3 |

**Ornek: Quarry (Tas Ocagi)**
| Alan | Deger |
|------|-------|
| Type | Quarry |
| Display Name | Tas Ocagi |
| Required Zone | Stone |
| Zone Proximity Radius | 3 |

**Ornek: Mine (Maden)**
| Alan | Deger |
|------|-------|
| Type | Mine |
| Display Name | Maden |
| Required Zone | Iron |
| Zone Proximity Radius | 3 |

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
| Required Zone | None |
| Zone Proximity Radius | 3 |

> **Zone gerektiren binalar:** Lumberjack→Forest, Quarry→Stone, Mine→Iron. Diger tum binalarda `Required Zone = None`.

### Adim 3: BuildingGridManager'a Ata
1. Sahnedeki `BuildingGridManager` GameObject'ini sec
2. **Building Configs** array'ine SO asset'lerini surukle

### Adim 4: Askeri Bina SO'lari (M1.6)

**Kisla (Barracks)**
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
| Training Duration | 30 |
| Food Cost Per Archer | 20 |
| Wood Cost Per Archer | 10 |

**Fletcher (Ok Atolyesi)**
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
| Max Workers | 3 |
| Arrows Per Worker Per Min | 10 |
| Wood Cost Per Batch Per Min | 2 |

**Blacksmith (Demirci)**
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

> Blacksmith sadece BuildingData component'i tasir — baska component eklenmez. Upgrade mekanigi M2+ milestone'inda.

> Detay: `Systems/BARRACKS_TRAINING_SYSTEM_EDITOR_SETUP.md` ve `Systems/ARROW_PRODUCTION_SYSTEM_EDITOR_SETUP.md`

## Entity Debugger'da Dogrulama
1. Play mode'a gir
2. Bir bina yerlestir
3. **Window → Entities → Entity Debugger** ac
4. Yeni entity'de `BuildingData` component'ini gor
5. Kaynak binasi ise `ResourceProducer` component'ini kontrol et
6. Ev ise `PopulationProvider` + `BuildingFoodCost` kontrol et
7. Kisla ise `ArcherTrainer` component'ini kontrol et
8. Fletcher ise `ArrowProducer` + `ResourceProducer` component'lerini kontrol et
9. Blacksmith ise SADECE `BuildingData` oldugunu dogrula

## Test Kontrol Listesi
- [ ] SO asset olusturulabiliyor mu? (Create menu)
- [ ] Inspector'da tum alanlar gorunuyor mu?
- [ ] BuildingGridManager.BuildingConfigs'e atanabiliyor mu?
- [ ] Play mode'da bina yerlestirince entity olusturuluyor mu?
