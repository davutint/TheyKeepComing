# Archer Formation V1 - Mimari

## Oyuncu sözleşmesi

Okçular oyuncu tarafından tek tek yerleştirilmez. `NewGameScene` içindeki kalenin hazır
`Grid/outside` yüzeyine doğal, deterministik ve katmanlı bir düzende dağılır. Basic, Rapid
ve Frost aynı formasyonu paylaşır; toplam kapasite tam `1000` okçudur.

## Veri otoritesi

- Asset: `Assets/ScriptableObject/MobileCastle/Archers/ArcherFormationV1.asset`
- Tanım: `ArcherFormationDefinitionSO`
- Saf algoritma: `ArcherFormationUtility`
- Sahne köprüsü ve gizmo: `MobileCastleArcherTilePlacement`
- Spawn/save sahibi: `GameManager` + `RunPersistence`

Formation V1 tam `40` canonical `outside` hücresi kullanır. Hücreler merkezden dışa doğru
`0, +1, -1, +2, -2 ... -19, +20` sırasıyla `(c,c,0)` koordinatlarında sabittir. Asset
bu koordinatları ve örnekleme tuning'ini version ile birlikte taşır; sahnedeki tilemap bu
contract'i karşılamıyorsa placement geçersizdir.

### Contract sahipliği

| Contract alanı | Tek otorite | Runtime tüketicisi |
|---|---|---|
| 40 sıralı hücre | `ArcherFormationV1.asset` (`ArcherFormationDefinitionSO.TileCoordinates`) | `MobileCastleArcherTilePlacement` |
| Hücre başına 25 local nokta | `ArcherFormationUtility.SlotsPerTile` ve deterministic üretim algoritması | `MobileCastleArcherTilePlacement` |
| Algoritma sürümü | `ArcherFormationUtility.CurrentVersion` + asset `Version` | Placement cache, `GameManager`, `RunPersistence` |
| 1000 toplam kapasite | `RequiredTileCount * SlotsPerTile` türevi | Okçu cap'i, placement cache ve gizmo |

`ArcherFormationDefinitionSO` runtime state tutmaz. `MobileCastleArcherTilePlacement` yeni
bir formasyon tanımlamaz; asset ve utility contract'ini aktif `outside` tilemap üzerinde
world-space cache'e dönüştürür. Böylece 40, 25 ve sürüm değerleri farklı scene veya prefab
alanlarında tekrar author edilmez.

## 40 x 25 yerleşim

Her tile için `25` local slot üretilir. Seed; formation version, tile coordinate, local
slot ve candidate attempt değerlerinden oluşur. Best-candidate örnekleme noktaları
izometrik diamond içine yansıtır, güvenli inset uygular ve aynı tile içindeki noktalar
arasında minimum local mesafeyi korur.

Global sıra katman mantığıyla doldurulur:

1. İlk `40` okçu bütün tile'ların local slot `0` noktasına gider.
2. Sonraki `40` okçu bütün tile'ların local slot `1` noktasına gider.
3. Bu sıra local slot `24` tamamlanana kadar sürer.

Bu nedenle `globalIndex -> tileIndex = index % 40`, `localSlot = index / 40` eşlemesi
kullanılır. Tile merkezine yığılma veya spiral stack offset yoktur.

## World-space dönüşümü

Utility local diamond koordinatı üretir. `MobileCastleArcherTilePlacement`, aktif
isometric tilemap'in gerçek sağ ve üst yarı eksenlerini `CellToWorld` üzerinden çıkarır;
local noktayı tile merkezine bu iki eksenle taşır. Runtime Z değeri
`MobileCastleRenderDepth.UnitZ` (`-1`) kalır.

## Save ve Continue

Exact save güncel `v14` şemasında archer type count'ları yanında
`ArcherFormationVersion` değerini tutar;
1000 world position JSON'a yazılmaz. Restore önce kaydın version'ıyla aynı deterministik
cache'i kurar, mevcut başlangıç okçularını bu sıraya taşır ve sonra eksik type count'larını
tamamlar. Böylece Main Menu -> Continue aynı formation noktalarını yeniden üretir.

`v6 -> v7` migration, eski deterministic placement kayıtlarını Formation V1'e bağlar;
sonraki şema migration'ları Formation V1 sürümünü koruyarak güncel `v14` state'ine taşır.
V1 yalnız algoritma sürümü `1`i destekler. Save normalization eski veya boş formation
sürümünü current V1'e bağlar; placement API'si cache sürümüyle eşleşmeyen doğrudan
istekleri reddeder.

## Sınırlar

- Formation kapasitesi archer cap'inin yerine geçmez; ikisi de `1000` contract'ini korur.
- Tilemap içeriği runtime veya setup tool tarafından boyanmaz/değiştirilmez.
- Save'de individual archer position veya individual archer HP tutulmaz.
- Hedef seçimi, projectile ve ammo davranışı bu modülün sorumluluğu değildir.

## Doğrulama

- `ArcherFormationUtilityTests`: canonical asset'in 40 hücre + algoritma sürümü
  sahipliği, utility'nin 25 slot/1000 kapasitesi, deterministik diamond/inset/minimum
  mesafe ve layer-fill mapping.
- `ArcherFormationPlayModeTests`: gerçek `NewGameScene` üzerinde 40/1000 contract'i,
  benzersiz noktalar, cache rebuild ve exact Continue sonrası aynı formasyon.
- `RunPersistenceTests.TryLoad_Version6Snapshot_MigratesToFormationVersion1`: açık v6 -> v7
  migration kanıtı.
