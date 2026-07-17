# Mobile Economy Runtime Tuning - Editor Setup

1. `Window > DeadWalls > Difficulty Tuner` penceresini aç.
2. `DefaultDifficulty` profilini seç.
3. `Economy Runtime Contract` bölümünden worker base rate, CAP/EFF maliyeti, ortak growth
   ve EFF seviye yüzdesini düzenle. Bed/Arrow komşu alanları panelin altında korunur.
4. `APPLY` ile asset'i kaydet ve aktif authoring'e bağla.
5. Play Mode'da Apply, bake edilmiş `MobileEconomyPriceTuning` component'ini canlı yeniler
   ve worker rate aggregate'lerini yeni baseline'a yeniden uygular.

Inspector üzerinden doğrudan düzenlemek istersen:
`Assets/ScriptableObject/MobileCastle/Difficulty/DefaultDifficulty.asset` asset'ini seç ve
`Worker Economy` ve `Ekonomi Fiyat Egrileri` alanlarını kullan.

V1 default:

| Alan | Değer |
|---|---:|
| Bed base Wood | 100 |
| Bed growth interval | 25 owned bed |
| Worker base rate W/S/I/F | 8 / 5.5 / 4.9 / 7 per min |
| Worker CAP base | 100 Wood + 25 Iron |
| Worker EFF base | 150 Wood + 50 Iron |
| Worker EFF effect | additive %10 / level |
| Worker building growth | 1.35 |

Sıfır/negatif maliyet ve interval girilirse runtime en az `1` kullanır. Büyüme çarpanı
`1` altına inemez. Geçersiz veya pozitif olmayan EFF yüzdesi `%10` default'una döner.
Temsil edilemeyen maliyette UI `COST LIMIT` gösterir ve transaction reddedilir.
