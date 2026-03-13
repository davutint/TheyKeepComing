# Catapult Components — Mimari

## Genel Bakis
Mancinik sistemi icin ECS component tanimlari. Sur slotlarina yerlestirilen mancinik birimleri parabolik mermiler atar, AoE (alan hasari) uygular.

## Component'lar

### CatapultUnit
Mancinik birimini temsil eder. Sur slot'una yerlestirilir.

| Alan | Tip | Default | Aciklama |
|------|-----|---------|----------|
| Damage | float | 40f | Mermi hasari (ok hasarinin 4x'i) |
| SplashRadius | float | 2.0f | AoE yaricapi (~10-20 zombie vurur) |
| FireRate | float | 0.2f | Atis/sn (5sn/atis — yavas ama guclu) |
| FireTimer | float | 0f | Kalan bekleme suresi |
| Range | float | 25f | Menzil (okcu 15f, mancinik 25f) |
| StoneCostPerShot | int | 1 | Atis basina tas maliyeti |

### CatapultProjectile
Havadaki mancinik mermisini temsil eder. Parabolik yol izler.

| Alan | Tip | Default | Aciklama |
|------|-----|---------|----------|
| Damage | float | — | CatapultUnit'ten kopyalanir |
| SplashRadius | float | — | CatapultUnit'ten kopyalanir |
| StartPos | float3 | — | Mancinik pozisyonu (spawn ani) |
| TargetPos | float3 | — | Hedef zombie pozisyonu (spawn ani) |
| FlightDuration | float | 1.2f | Toplam ucus suresi |
| FlightTimer | float | 0f | Gecen ucus suresi |
| ArcHeight | float | 5.0f | Parabol yuksekligi |

### CatapultProjectileTag
Filtreleme icin bos tag component. `WithAll<CatapultProjectileTag>()` ile query'lerde kullanilir.

## Iliskiler
- `CatapultShootSystem` → CatapultUnit okur, CatapultProjectile spawn eder
- `CatapultProjectileMoveSystem` → CatapultProjectile gunceller (parabolik hareket)
- `CatapultProjectileHitSystem` → CatapultProjectile okur, AoE hasar uygular, entity siler
- `WallSlotManager` (MonoBehaviour) → CatapultUnit entity olusturur/siler

## Dosya Konumu
`Assets/Scripts/ECS/Components/CatapultComponents.cs`
