# Resource Zones (Dogal Kaynak Noktalari) — Mimari Dokumani

## Genel Bakis
Dogal kaynak zone sistemi, kaynak binalarinin (Oduncu, Tas Ocagi, Maden) haritadaki ilgili dogal kaynaga yakin yerlestirilmesini zorunlu kilar. Tilemap tabanli zone depolama + proximity-based yerlestirme kontrolu kullanir.

## Dosyalar
- `Assets/Scripts/MonoBehaviour/BuildingGridManager.cs` — Zone cache, IsNearZone(), Show/HideResourceZones()
- `Assets/Scripts/MonoBehaviour/BuildingPlacementUI.cs` — Zone overlay gorunurluk toggle
- `Assets/Scripts/ScriptableObject/BuildingConfigSO.cs` — RequiredZone, ZoneProximityRadius field'lari
- `Assets/Scripts/ECS/Components/BuildingComponents.cs` — ResourcePointType enum

## ResourcePointType Enum

| Deger | Anlam | Gerektiren Bina |
|-------|-------|-----------------|
| None | Zone gereksinimi yok | Farm, House, Barracks, Fletcher, Blacksmith, WizardTower |
| Forest | Orman zone'u | Lumberjack (Oduncu) |
| Stone | Tas zone'u | Quarry (Tas Ocagi) |
| Iron | Demir zone'u | Mine (Maden) |

> ECS component degil, sadece enum — zone verisi MonoBehaviour grid cache'inde yasar.

## Zone Veri Akisi

```
Editor (Tilemap boyama)
  ↓
ResourceZoneTilemap (resource_zones layer)
  ↓
InitializeGrid() → _zoneGrid[,] cache (ResourcePointType array)
  ↓
CanPlace() → IsNearZone() kontrolu
  ↓
Yerlestirme izni / red
```

## Proximity Algoritmasi

`IsNearZone(zoneType, gridX, gridY, width, height, radius)`:

1. `zoneType == None` → hemen `true` (zone gerektirmeyen binalar)
2. Bina footprint'i (gridX,gridY → gridX+width, gridY+height) etrafinda `radius` kadar genisletilmis dikdortgen hesapla
3. Genisletilmis dikdortgeni grid sinirlarina clamp et
4. Tarama: ilk eslesen zone hucresinde `true` don
5. Hicbir eslesme yoksa `false` don

```
Ornek: 3x3 bina, radius=3

       ←── radius ──→
  ┌──────────────────────┐  ↑
  │                      │  radius
  │    ┌──────────┐      │  ↓
  │    │  BINA    │      │
  │    │  3x3     │      │
  │    └──────────┘      │
  │                      │
  └──────────────────────┘
  ← genisletilmis dikdortgen →
```

## Gorsel Overlay

- `ResourceZoneTilemap` uzerinde farkli tile asset'leri ile zone'lar boyanir
- Varsayilan: `TilemapRenderer.enabled = false` (gorunmez)
- Zone gerektiren bina secildiginde: `ShowResourceZones()` → renderer acilir
- Yerlestirme bittikten/iptal edildikten sonra: `HideResourceZones()` → renderer kapanir
- Zone gerektirmeyen bina secildiginde overlay acilmaz

## BuildingConfigSO Field'lari

| Alan | Tip | Varsayilan | Aciklama |
|------|-----|-----------|----------|
| RequiredZone | ResourcePointType | None | Hangi zone gerekli |
| ZoneProximityRadius | int | 3 | Kac hucre yakinlik yeterli |

## Restart Davranisi

Zone'lar kalici harita ozelligi — `_zoneGrid` ve `ResourceZoneTilemap` restart'ta temizlenmez. Sadece binalar (_grid, _entityGrid) sifirlanir.

## CanPlace Akisi (Guncel)

```
CanPlace(config, gridX, gridY)
  ├─ Grid sinir kontrolu
  ├─ Hucre bosluk kontrolu (_grid[x,y] == 0)
  ├─ Kaynak yeterlilik kontrolu (GameManager.Resources)
  └─ Zone yakinlik kontrolu (IsNearZone) ← YENi
```
