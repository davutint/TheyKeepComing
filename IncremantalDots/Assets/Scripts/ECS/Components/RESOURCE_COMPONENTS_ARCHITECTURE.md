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
- `Accumulator` (float) — Kesirli birikim tamponu

Ok stogu singleton'u. ArrowProductionSystem Fletcher iscilerinin uretimini accumulator pattern ile biriktirir.
ArcherShootSystem ok atildiginda `Current -= 1` yapar. Current <= 0 ise ok atilamaz.
GameStateAuthoring Baker'i tarafindan eklenir — `InitialArrows` degeri baslangic stoku olarak yazilir.

## Veri Akisi
```
ResourceProductionRate ─┐
                        ├→ ResourceTickSystem → ResourceAccumulator → ResourceData
ResourceConsumptionRate ─┘                                               ↓
                                                              GameManager.Resources
                                                                        ↓
                                                              HUDController (gosterim)

ArrowProductionSystem → ArrowSupply.Accumulator → ArrowSupply.Current
                                                        ↓
                                              ArcherShootSystem (tuketim)
                                                        ↓
                                              HUDController.ArrowText (gosterim)
```

## Singleton Yerlesim
Tumu GameStateAuthoring Baker'i tarafindan ayni entity'ye eklenir.
Ek entity veya query olusturmaya gerek yok — `SystemAPI.GetSingletonRW<ResourceData>()` ile erisim.
ArrowSupply da ayni entity uzerinde — `SystemAPI.GetSingletonRW<ArrowSupply>()` ile erisim.
