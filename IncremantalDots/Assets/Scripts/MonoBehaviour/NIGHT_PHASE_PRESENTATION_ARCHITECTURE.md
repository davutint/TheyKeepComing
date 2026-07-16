# Night Phase Presentation Architecture

## Sahiplik

Night sunumu yeni bir cycle, lighting manager veya combat sistemi kurmaz. Mevcut owner'lar
genişletilir:

- `DayNightOverlayController`: tek `Global Light 2D` ve dört kale pencere ışığı.
- `AmbientAudioController`: mevcut faz drone'u yanında tek, density-driven horde bed.
- `CombatFeedbackBridge`: mevcut ECS `CombatSfxEvent` akışında bounded salvo aggregation.

Gameplay spawn, okçu ateş temposu, projectile ve damage hesapları değişmez.

## Cold-Moon ve Silhouette

- Canonical Night global-light hedefi `RGB(0.46, 0.58, 0.94)`, intensity `0.68`'dir.
- Dusk'un ikinci yarısı aynı hedefe aktığı için Night girişinde renk sıçraması oluşmaz.
- Soğuk mavi ağırlık kale, Wall ve zombie kütlesini zeminden ayıran silhouette tabanıdır.
- Ayrı `10k ground contrast / silhouette edge / motion cadence` tracker işi bu pakette
  tamamlanmış sayılmaz; bu paket yalnız faz paleti ve bounded feedback owner'larını kurar.

## Kale Pencereleri

Scene setup, gerçek `NewGameScene` tilemap'lerinde yalnız `Wall A5_S` ve `Wall A5_N`
sprite'larını tarar. Bulunan dört benzersiz hücrenin merkezinde `CastleNightWindowLights`
altında dört küçük Additive Point Light 2D bulunur.

- Dusk `%18` sonrasında yanmaya başlar, `%72` noktasında tam güce ulaşır.
- Night boyunca warm `RGB(1.00, 0.47, 0.12)`, intensity `0.82` hedefinde kalır.
- Dawn'ın ilk `%65` bölümünde söner.
- Flicker tek controller'ın sabit dört elemanlı dizisinde çalışır; window başına Update veya
  yeni MonoBehaviour yoktur.

## Horde Bed

`AmbientAudioController`, Night sırasında `WaveState.ZombiesAlive` ve
`ContinuousSiegeCycleData.HordePressure01` değerlerini logaritmik `0..1` activity'ye çevirir.
Activity 10.000 zombide tavana ulaşır; ses seviyesi `NightHordeVolume = 0.18` ve
`SoundSettings.AmbienceVolume` ile sınırlandırılır. Tek 2D loop kullanılır; zombie veya chunk
başına AudioSource üretilmez.

## Archer Salvo Mix

`CombatFeedbackBridge`, aynı frame'deki `ArrowShoot` event'lerini tek cue'ya aggregate eder:

- Position: bütün event pozisyonlarının ortalaması.
- Volume: logaritmik yoğunluk artışı, `0.62` kesin tavan.
- Pitch: yoğunlukla en fazla `%8` düşüş.
- Night shoot rate-limit: en az `0.12s`.
- Global audio budget: frame başına en fazla `4` cue; sabit `16` AudioSource pool.

Bu yalnız ses karmaşasını azaltır. Ok projectile görsellerini salvo kümelerine dönüştüren ayrı
tracker maddesi ve per-hit VFX/SFX bütçe maddesi ileride açık kalır.

## Doğrulama

- EditMode: cold-moon paleti, pencere ignition envelope'u, 10k horde activity tavanı ve
  salvo volume/pitch sınırları.
- PlayMode: gerçek `NewGameScene` içindeki dört pencere ışığı, Night horde loop'u, 1.000
  ArrowShoot event'inin tek salvo cue olması, ikinci cue'nun Night rate-limit ile yutulması,
  sabit AudioSource sayısı ve frame budget.
