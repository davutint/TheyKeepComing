# Mobile Economy Price Tuning - Editor Setup

1. `Window > DeadWalls > Difficulty Tuner` penceresini aç.
2. `DefaultDifficulty` profilini seç.
3. `Ekonomi Fiyat Eğrileri` bölümünden bed ve worker bina maliyetlerini düzenle.
4. `APPLY` ile asset'i kaydet ve aktif authoring'e bağla.
5. Play Mode'da Apply, bake edilmiş `MobileEconomyPriceTuning` component'ini canlı yeniler.

Inspector üzerinden doğrudan düzenlemek istersen:
`Assets/ScriptableObject/MobileCastle/Difficulty/DefaultDifficulty.asset` asset'ini seç ve
`Ekonomi Fiyat Egrileri` alanlarını kullan.

V1 default:

| Alan | Değer |
|---|---:|
| Bed base Wood | 100 |
| Bed growth interval | 25 owned bed |
| Worker CAP base | 100 Wood + 25 Iron |
| Worker EFF base | 150 Wood + 50 Iron |
| Worker building growth | 1.35 |

Sıfır/negatif maliyet ve interval girilirse runtime en az `1` kullanır. Büyüme çarpanı
`1` altına inemez. Temsil edilemeyen maliyette UI `COST LIMIT` gösterir ve transaction reddedilir.
