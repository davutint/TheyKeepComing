# Catapult Components — Editor Kurulumu

## Prefab Olusturma

### Catapult Prefab
1. Hierarchy > Create Empty → "CatapultPrefab" adini ver
2. SpriteRenderer ekle → mancinik sprite ata (placeholder olabilir)
3. **CatapultAuthoring** component'i ekle
   - Damage: 40
   - SplashRadius: 2
   - FireRate: 0.2
   - Range: 25
   - StoneCostPerShot: 1
4. Prefabs klasorune surukle

### CatapultProjectile Prefab
1. Hierarchy > Create Empty → "CatapultProjectilePrefab" adini ver
2. SpriteRenderer ekle → mermi/tas sprite ata (kucuk daire/kare)
3. **CatapultProjectileAuthoring** component'i ekle (parametre yok)
4. Prefabs klasorune surukle

## WaveConfigAuthoring'e Atama
1. Scene'deki WaveConfig GameObject'i sec
2. WaveConfigAuthoring inspector'inda:
   - **CatapultPrefab** → olusturdugun CatapultPrefab'i surukle
   - **CatapultProjectilePrefab** → olusturdugun CatapultProjectilePrefab'i surukle

## BuildingConfigSO Asset Olusturma
1. Assets/ScriptableObjects/ klasorunde sag tikla → Create > DeadWalls > Building Config
2. Isim: "BuildingConfig_Catapult"
3. Degerler:
   - Type: **Catapult**
   - DisplayName: "Mancinik"
   - IsWallSlotBuilding: **true** (ONEMLI!)
   - RequireBlacksmith: **true**
   - WoodCost: 30, StoneCost: 50, IronCost: 20
   - Diger uretim alanlari bos/sifir birakilabilir
4. BuildingGridManager.BuildingConfigs array'ine ekle

## Dogrulama
- Play mode'da bina menusunde "Mancinik" butonu gorunmeli
- Demirci yokken butona tiklaninca Console'da "Demirci binasi gerekli!" mesaji
- Demirci varken ghost preview en yakin bos slot'a snap etmeli
- Yerlestirme sonrasi CatapultUnit entity olusturulmali (Entity Debugger)
