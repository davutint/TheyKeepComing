# Tech Tree SO - Editor Setup

## Otomatik Kurulum (onerilen)

`Window > DeadWalls > Mobile Castle Scene Setup > Setup NewGameScene` calistir. Tool:

1. `Assets/ScriptableObject/MobileCastle/TechTree/` klasorunu kurar.
2. 13 default node asset'ini SADECE EKSIKSE olusturur (mevcut asset degerlerine dokunmaz):
   `CastleHeart, BasicArcher, BowTraining, RapidVolley, RapidArcher, WoodCamp, WorkerCamp,
   FoodStores, PopulationGrowth, WallReinforcement, RepairCrew, FrostArrows, FrostArcher`.
3. `TechTreeCatalog.asset`'i olusturur/merge eder — katalogda kullanicinin ekledigi
   EKSTRA node'lar KORUNUR, eksik default'lar eklenir; `RootNodeId` bossa `castle_heart` yazilir.
4. Katalogu `GameManager.techTreeCatalog` alanina baglar (SerializedObject ile).
5. `ValidateCatalog()` sorunlarini Console'a warning olarak basar.

Default agac (reveal iliskisi):

```
castle_heart (root, otomatik sahipli)
  +- basic_archer -> bow_training (MaxLevel 3), rapid_volley -> rapid_archer (Rapid unlock)
  +- wood_camp -> worker_camp, food_stores -> population_growth
  +- wall_reinforcement -> repair_crew
  +- frost_arrows -> frost_archer (Frost unlock)
```

Icon alanlari BILEREK bos birakilir; UI bas-harf placeholder gosterir. Sonradan
Inspector'dan sprite atanabilir, baska hicbir sey degistirmek gerekmez.

## Elle Kurulum / Yeni Node

Bkz. `TECH_TREE_SO_ARCHITECTURE.md > Yeni Tech Ekleme` (5 adim). Menu yollari:
- `Create > DeadWalls > Mobile Castle > Tech Node Definition`
- `Create > DeadWalls > Mobile Castle > Tech Tree Catalog`

## Dogrulama

- Play'e gir; HUD'daki `TECH` butonu paneli acar, root + ilk cocuklar gorunmeli.
- Node satin alindiginda cocuklari belirmeli; `rapid_archer`/`frost_archer` satin alimi
  sag drawer'daki Rapid/Frost satirlarini `LOCKED`'tan alinabilir duruma gecirmeli.
- `GameManager.Free Economy Test Mode` acikken maliyetler bypass edilir (hizli test).
- Restart sonrasi agac 5 gorunur node'a (root+4) ve config base degerlerine donmeli.
