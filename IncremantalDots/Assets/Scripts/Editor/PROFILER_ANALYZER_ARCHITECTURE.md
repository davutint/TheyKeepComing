# Profiler Data Analyzer — Mimari

## Genel Bakis
`ProfilerDataAnalyzer.cs` Unity Editor'da calisan bir `EditorWindow` tabanli profiler analiz aracidir.
Unity Profiler `.raw` dosyalarini yukleyip kapsamli performans raporu uretir.

## Versiyon Gecmisi
- **V3:** Snapshot A/B karsilastirma, dosya tarayici, delta analizi
- **V4:** A/B/Both display mode, GC/Call metrik, User Code GC section
- **V5:** DeadWalls ECS/DOTS pipeline analizi, physics breakdown, ECS sistem bazli A/B karsilastirma
- **V6:** Worker thread job taramasi — Burst job surelerini worker thread'lerden yakalar, Job Avg/Max kolonlari

## Tab Yapisi (11 Tab)
| # | Tab | Aciklama |
|---|-----|----------|
| 0 | Overview | CPU/GPU/FPS ozeti, FPS dagilimi, GC ozet |
| 1 | Spikes | En yavas 20 frame (spike detection) |
| 2 | Functions | Tum fonksiyonlar (self time sirali, top 30) |
| 3 | Update | Update/LateUpdate/FixedUpdate breakdown |
| 4 | User Code | Assembly-CSharp.dll fonksiyonlari |
| 5 | Rendering | Draw call, batch, triangle istatistikleri |
| 6 | GC Analysis | GC allocator'lar, GC frame'ler, user/engine split |
| 7 | Call Chains | User Code → GC Producer zincirleri |
| 8 | Compare | A/B snapshot karsilastirma (delta analiz) |
| 9 | ECS Pipeline | DeadWalls ECS sistemleri tablo (grup bazli) |
| 10 | Physics | Physics pipeline breakdown, stacked bar, top frame'ler |

## Veri Akisi

```
.raw dosya
    → LoadProfile() (ProfilerDriver.LoadProfile)
    → AnalyzeFile()
        → ProcessFrame() (her frame icin)
            → TraverseHierarchy() (BFS, threadIndex=0, main thread)
                → FunctionProfileEntry kaydi
                → EcsSystemEntry kaydi (KNOWN_ECS_SYSTEMS eslesmesi)
                → EcsFrameData kaydi (per-frame timeline)
                → GcCallChainEntry kaydi
                → Sync point tespiti
            → ScanWorkerThreads() (threadIndex 1-63, worker thread'ler)
                → TraverseWorkerThread() (BFS, derinlik 4)
                    → ExtractJobName() ile job marker tespiti
                    → KNOWN_ECS_JOBS eslesmesi → parent sisteme job suresi ekleme
                    → RecordEcsJobFrameData() ile per-frame job timing kaydi
        → CacheAnalysisResults()
            → Siralama, gruplama, derived cache
    → AnalysisSnapshot
```

## ECS Entegrasyonu (V5)

### Marker Tespiti
- `ExtractSystemName()` profiler marker adlarindan bilinen ECS sistem adlarini cikarir
- Namespace prefix (`DeadWalls.`) ve suffix (`OnUpdate()`) otomatik temizlenir
- `KNOWN_ECS_SYSTEMS` dictionary'si ile eslesme yapilir

### Bilinen ECS Sistemleri
SimulationSystemGroup:
- WaveSpawnSystem, ZombieNavigationSystem, ApplyMovementForceSystem
- BuildSpatialHashSystem, PhysicsCollisionSystem, IntegrateSystem
- BoundarySystem, ZombieAttackSystem, ArcherShootSystem
- ArrowMoveSystem, ArrowHitSystem, ClickDamageSystem
- ZombieDeathSystem, ZombieAnimationStateSystem, DamageCleanupSystem

PresentationSystemGroup:
- SpriteAnimationSystem

### Physics Pipeline Sirasi
```
ApplyMovementForceSystem → BuildSpatialHashSystem → PhysicsCollisionSystem → IntegrateSystem → BoundarySystem
```

### Worker Thread Job Taramasi (V6)
- `ScanWorkerThreads()`: Her frame icin threadIndex 1-63 arasindaki worker thread'leri tarar
- Sadece `threadName` icinde "Worker" veya "Job" gecen thread'ler islenir
- `TraverseWorkerThread()`: BFS ile job hierarchy'sini tarar (max 4 seviye)
- `ExtractJobName()`: Marker adlarindan job struct adini cikarir (namespace/suffix temizleme)
- `KNOWN_ECS_JOBS` dictionary'si ile job → parent sistem eslesmesi yapilir
- Birden fazla worker'dan gelen ayni job sureleri toplanir (RecordEcsJobFrameData)

### Bilinen ECS Job'lari
| Job Struct | Parent System |
|------------|---------------|
| ApplyForceJob | ApplyMovementForceSystem |
| HashJob | BuildSpatialHashSystem |
| CollisionJob | PhysicsCollisionSystem |
| IntegrateJob | IntegrateSystem |
| BoundaryJob | BoundarySystem |
| NavSyncJob | ZombieNavigationSystem |

### Veri Yapilari
- `EcsSystemEntry`: Sistem bazli toplam/ort/max time, self time, GC, sync point bilgisi + job time (worker thread)
- `EcsFrameData`: Frame bazli her sistemin suresi, pipeline toplami + job timing'leri

### Sync Point Tespiti
- BFS sirasinda `nearestEcsSystem` takip edilir
- "Complete" veya "Sync Point" iceren marker'lar sync point olarak isaretlenir
- ECS tab'da "BLOCK" etiketi ile gosterilir

## Snapshot Mimarisi
- `AnalysisSnapshot`: Tek bir .raw dosyasinin tum analiz verisini tutar
- `_snapshotA` ve `_snapshotB`: A/B karsilastirma icin iki slot
- `_displaySnapshot`: Aktif goruntulenen snapshot (A/B/Both mode)
- `_activeSnapshot`: Display veya fallback olarak A

## Export
- Text raporu `GenerateTextReport()` ile uretilir
- ECS ve Physics section'lari raporda yer alir
- Varsayilan cikti: `C:\Users\PC\Desktop\PERFORMANS ANALIZI\profiler_report.txt`
