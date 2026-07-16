# Archer Targeting - Mimari

## Amaç

`ArcherShootSystem`, 1.000 okçu ve 10.000 aktif düşman ölçeğinde bütün düşmanları
okçu başına tekrar taramaz. Aynı sistemin persistent coarse spatial map'i, read-only
query alias'ı ve frame-local incoming damage reservation map'i hedef seçiminin tek
runtime owner'ıdır.

Projectile pooling ayrı `ARROW_POOL_ARCHITECTURE.md`, bounded görsel temsil ise
`ARCHER_SALVO_PRESENTATION_ARCHITECTURE.md` sözleşmesinde tamamlanmıştır.

## Frame Akışı

1. Yaşayan `ZombieTag + ZombieStats + LocalTransform` entity'leri `2.0` world-unit
   cell size ile `NativeParallelMultiHashMap` içine paralel Burst job'da yazılır.
2. Mevcut `ArrowProjectile` entity'leri taranır. Target entity hâlâ aktifse ve
   `EnemyPoolMember.Generation` eşleşiyorsa okun hasarı target'ın incoming load'una
   eklenir.
3. Tek Burst `ArcherShootJob`, ateşe hazır okçuları deterministik entity/chunk
   sırasında işler.
4. Query, okçu hücresinden dışarı doğru halkalar gezer. Cell AABB'sinin minimum
   mesafesi mevcut en iyi mesafeden uzaksa hücre okunmaz.
5. `ZombieTag` disabled, `ZombieState.Dead`, `CurrentHP <= 0` veya enabled
   `DeathTimer` hedefleri reddedilir.
6. Range içindeki en yakın ve `reservedDamage < CurrentHP` olan hedef seçilir.
   Eşit mesafede düşük `Entity.Index`, ardından düşük `Entity.Version` kazanır.
7. Yeni ok pool'dan başarıyla rent edildikten sonra hasarı aynı reservation map'ine eklenir. Böylece aynı
   frame'deki sonraki okçular ölümcül hasarı zaten rezerve edilmiş hedefi atlar.
8. Başarılı rent sırası, gameplay projectile'ı silmeden bounded salvo temsilci scale'ını
   belirler; targeting ve reservation bu görsel karardan bağımsız kalır.

Range içindeki bütün hedefler ölümcül incoming damage ile doluysa okçu o frame
bekler. Uçuşta olan ok başka hedefe yönlendirilmez; pool generation uyuşmazlığında
mevcut projectile sistemleri deterministik pool return yapar.

## Ortak Policy

Basic, Rapid ve Frost için ayrı target branch'i yoktur. Üç tip aynı spatial query ve
reservation policy'sini kullanır; yalnız `ArcherUnit` içindeki damage, fire rate,
range ve Frost effect değerleri farklıdır.

## Container Yaşam Döngüsü

- Target map ve incoming damage map `Allocator.Persistent` ile `ArcherShootSystem`
  tarafından oluşturulur.
- V1 başlangıç kapasitesi `16.384` entity'dir.
- Aktif target sayısı kapasiteyi aşarsa iki container da `ceilpow2(targetCount)`
  kapasitesine güvenli dependency completion sonrasında büyür.
- Target map her frame yeniden kurulur; reservation map her frame temizlenip önce
  uçuşta olan oklardan seed edilir.
- `OnDestroy`, sistem dependency'sini tamamlayıp iki container'ı dispose eder.
- Target job'a yalnız `NativeParallelMultiHashMap.ReadOnly` alias'ı geçer.

Collision/Boundary broadphase'i eski `BuildSpatialHashSystem` çift buffer'ının
owner'ında kalır. Collision cell size `0.35`, archer targeting cell size `2.0`dır;
iki farklı yoğunluk ihtiyacı aynı map'e zorlanmaz.

## Ölçek Kanıtı

`HordeScalePlayModeTests` gerçek `NewGameScene` içinde Formation V1'in 1.000
pozisyonuna doğrudan ECS stress harness'iyle okçu yerleştirir; 10.000 pooled düşman,
projectile, HUD/feedback ve Fireball return ile birlikte hedefleme örnekler.
Benchmark okçuları run-state owner'ını bypass ettiği için bu test 1K archer save
kanıtı sayılmaz; 10K enemy Continue regresyonu ayrı korunur.

2026-07-16 bounded salvo sunumu sonrası Editor ölçümü:

- frame average: `9,77 ms`
- frame P95: `12,74 ms`
- main-thread average: `9,66 ms`
- sample sonu aktif pooled projectile: `745`
- ilk salvo: `1.000` gameplay projectile / `48` görünür temsilci
- aktif düşman: test boyunca `10.000`

Player/hardware frame pacing onayı Release Definition of Done içinde ayrı kapıdır.

## Testler

- `ArcherTargetingUtilityTests`: cell radius, cell AABB mesafesi, lethal saturation
  ve stable distance tie-break.
- `ArcherTargetingPlayModeTests`: Basic/Rapid/Frost aynı frame'de üç ölümcül oku
  üç ayrı hedefe dağıtır.
- `HordeScalePlayModeTests`: 1.000 okçu x 10.000 düşman birleşik ürün senaryosu ve
  telemetry; ilk salvoluk gameplay/visual temsil ayrımı.
