# DeadWalls — Claude Code Proje Kurallari

## Proje Hakkinda
- **Motor:** Unity 6 LTS + DOTS/ECS + ProjectDawn.Navigation
- **Namespace:** DeadWalls
- **Tur:** Zombie tower defense
- **Dil:** C# (Unity ECS pattern)

## Klasor Yapisi
```
Assets/Scripts/
  ECS/
    Components/    — IComponentData struct'lari
    Authoring/     — MonoBehaviour → ECS baker'lar
    Systems/       — ISystem struct'lari (gameplay logic)
    Physics/       — Custom 2D circle physics pipeline
  MonoBehaviour/   — Klasik Unity script'leri
```

## Kurallar

### MD Dokumantasyonu (ZORUNLU)
- Her yeni sistem veya klasor olusturuldiginda o klasore **ARCHITECTURE** ve **EDITOR_SETUP** md dosyalari yazilmali
- Mevcut klasorlere yeni dosya eklendiginde mevcut md dosyalari guncellenmeli
- MD dosya isimleri SPESIFIK olmali — neye tikladigini bilebilecek sekilde
  - Ornek: `PHYSICS_PIPELINE_ARCHITECTURE.md`, `SPATIAL_HASH_GRID_ARCHITECTURE.md`
  - Yanlis: `README.md`, `NOTES.md`

### Kod Stili
- Namespace: her zaman `DeadWalls`
- ECS Systems: `partial struct` + `ISystem`, job'lar `[BurstCompile]` ile
- Static field erisimi olan ISystem struct'lardan `[BurstCompile]` kaldirilir, job'da kalir
- Turkce yorum yazilir (degisken/fonksiyon adlari Ingilizce)

### ECS Pattern
- Component: sade data, logic yok
- System: tek sorumluluk, `IJobEntity` ile parallel
- `ComponentLookup` ile komsu okuma: `[ReadOnly] [NativeDisableContainerSafetyRestriction]`
- Spatial hash: `BuildSpatialHashSystem.ReadMap` / `WriteMap` (double-buffered NativeParallelMultiHashMap)

## Mimari Ozet

### Physics Pipeline (her frame sirasi)
```
ApplyMovementForceSystem  → Hedefe dogru kuvvet
BuildSpatialHashSystem    → Pozisyonlari hash grid'e yaz
PhysicsCollisionSystem    → Circle-circle carpisma + pozisyon duzeltme
IntegrateSystem           → velocity += force*dt, position += velocity*dt
BoundarySystem            → Duvar bariyeri, domino queuing, state transitions
```

### Zombie State Akisi
```
Moving ──→ Attacking    (duvara ulasti)
Moving ──→ Queued       (domino: Attacking/Queued komsusuna cakisti)
Queued ──→ Moving       (blocker gitti)
Queued ──→ Attacking    (duvara ulasti)
  *    ──→ Dead         (HP <= 0)
```
- **Moving:** Yuruyor, kuvvet uygulanir, walk animasyonu
- **Attacking:** Duvarda, saldirir, hit animasyonu (kirmizi)
- **Queued:** Durmus ama saldirmiyor, walk animasyonu, kuvvet yok
- **Dead:** Olum animasyonu, DeathTimer ile entity silinir

### Mevcut MD Dosyalari
- `Components/` — ARCHITECTURE.md, EDITOR_SETUP.md
- `Systems/` — ARCHITECTURE.md, EDITOR_SETUP.md, ZOMBIE_NAVIGATION_*, SPRITE_ANIMATION_*
- `Physics/` — PHYSICS_PIPELINE_ARCHITECTURE.md, PHYSICS_EDITOR_SETUP.md, SPATIAL_HASH_GRID_ARCHITECTURE.md, COLLISION_RESPONSE_ARCHITECTURE.md
