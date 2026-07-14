# Resource Components - Mimari (M1.1)

## Genel Bakis
4 kaynak ekonomisinin (Ahsap, Tas, Demir, Yemek) veri katmani.
Tum resource component'lari GameState entity uzerinde singleton olarak tutulur.

## Dosya: ResourceComponents.cs

### ResourceData (IComponentData)
- `Wood`, `Stone`, `Iron`, `Food` (int)
- Mevcut kaynak miktarlari
- GameManager.Resources uzerinden MonoBehaviour tarafindan okunur

### ResourceProductionRate (IComponentData)
- `WoodPerMin`, `StonePerMin`, `IronPerMin`, `FoodPerMin` (float)
- Dakika basina uretim hizlari
- M1.1: Inspector'dan test degerleri, M1.4+ binalar runtime'da degistirecek

### ResourceConsumptionRate (IComponentData)
- `WoodPerMin`, `StonePerMin`, `IronPerMin`, `FoodPerMin` (float)
- Dakika basina tuketim hizlari
- M1.2'de nufus yemek tuketimi bu component'i kullanacak

### ResourceAccumulator (IComponentData)
- `Wood`, `Stone`, `Iron`, `Food` (float)
- Kesirli birikim tamponu — sadece ResourceTickSystem kullanir
- ±1.0 gecince ResourceData int'e transfer edilir
- Disaridan okunmaz/yazilmaz

### ArrowSupply (IComponentData — M1.6)
- `Current` (int) — Mevcut ok stoku
- `CapacityLevel` (int) — Run ici kapasite yatirim seviyesi
- `EfficiencyLevel` (int) — Run ici Arrow/Wood yatirim seviyesi
- `HeartCapacityBonus` (int) — Castle Heart'in paid level'dan ayri additive kapasite katkisi
- `HeartEfficiencyBonus` (int) — Castle Heart'in paid level'dan ayri additive Arrow/Wood katkisi
- `Accumulator` (float) — Legacy save/serialization uyumlulugu; V1 refill bunu kullanmaz

Ok stogu singleton'u. V1'de Fletcher/queue/pasif ok uretimi yoktur. `GameManager`,
`ArrowEconomyUtility` fiyat sonucuyla Wood refill veya Wood+Iron yatirim transaction'ini
aninda uygular. `ArcherShootSystem` yalniz pool rent'i basarili projectile icin
`Current -= 1` yapar. `Current <= 0` ise ok atilamaz.
GameStateAuthoring Baker'i tarafindan eklenir — `InitialArrows` degeri baslangic stoku olarak yazilir.
`ArrowEconomyUtility` effective capacity/efficiency hesabinda paid level ile Heart bonusunu
birlikte okur; yatirim fiyatini ve Inspector/player-facing paid level'i yalniz
`CapacityLevel`/`EfficiencyLevel` belirler.

### GraveEssence (IComponentData — Castle Heart E1)
- `Current` (`long`) — Yalniz mevcut run icindeki Castle Heart bakiyesi
- `ResourceData`'dan ayri tutulur; Wood/Stone/Iron/Food transaction'ina girmez
- Yalniz `GameManager.TrySpendGraveEssenceAtHeart()` harcama kapisindan azalir
- Exact save v10'da generated Heart graph ile birlikte korunur; Restart ve Game Over sonrasi `0` olur
- Meta progression state'ine yazilmaz

## Veri Akisi
```
ResourceProductionRate ─┐
                        ├→ ResourceTickSystem → ResourceAccumulator → ResourceData
ResourceConsumptionRate ─┘                                               ↓
                                                              GameManager.Resources
                                                                        ↓
                                                              HUDController (gosterim)

GameManager + ArrowEconomyUtility → Wood refill / Wood+Iron yatirimi → ArrowSupply
GameManager.HeartRuntime         → Heart CAP/EFF additive bonus ────────┘
                                                                         ↓
                                                           ArcherShootSystem (-1/shot)
                                                                         ↓
                                                  HUDController + ArrowSupplyUI
```

## Singleton Yerlesim
Tumu GameStateAuthoring Baker'i tarafindan ayni entity'ye eklenir.
Ek entity veya query olusturmaya gerek yok — `SystemAPI.GetSingletonRW<ResourceData>()` ile erisim.
ArrowSupply da ayni entity uzerinde — `SystemAPI.GetSingletonRW<ArrowSupply>()` ile erisim.
GraveEssence da ayni entity uzerindedir; kazanc kaynagi sonraki Heart/runtime paketine aittir.
