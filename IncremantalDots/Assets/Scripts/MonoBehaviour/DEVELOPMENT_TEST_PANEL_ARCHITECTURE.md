# Development Test Panel Architecture

## Amaç

`DevelopmentTestPanel`, combat presentation ve 10K ölçek incelemesini production Castle
Heart içeriğini, normal teknoloji ilerlemesini veya run save'i beklemeden yapar. Yalnız
`UNITY_EDITOR || DEVELOPMENT_BUILD` derlemelerinde bulunur; release oyuncu akışında type,
GameObject veya UI üretmez.

## Runtime sınırı

- `GameManager.DevelopmentTools`, Basic/Rapid/Frost erişimini, Fireball erişimini ve üç
  ability cooldown'unu yalnız mevcut Play Mode oturumu için hazırlar.
- Aynı aksiyon `freeEconomyTestMode` açar; test okçuları kaynak/population tüketmeden
  alınabilir. Shipping fiyatları ve incremental eğriler değişmez.
- Test oturumu aktifken `SaveRunSnapshot` açıkça reddedilir. Böylece geçici unlock, ücretsiz
  alım veya test hordesi exact run save'e yazılamaz.
- `2K / 5K / 10K` butonları yalnız bu exact preset'leri kabul eder. Mevcut aktif zombiler
  pool'a döner, tek aktif enemy catalog entry'si aynı `EnemyPoolRuntimeUtility` üzerinden
  yeniden rent edilir.
- Test grid'i mevcut `MobileCastleRenderDepth.UnitZ` ve authored enemy scale/stat'lerini
  kullanır. Yalnız `AttackDamage = 0` yapılır; Wall çökmeden Fireball, Frost, projectile ve
  hit presentation uzun süre incelenebilir.
- `StressTestMode` kullanılmaz. Böylece Day/Night grading ile combat VFX/SFX suppression'a
  girmez. Continuous spawn pending budget temizlenir ve spawn timer durdurulur.

## UI sahipliği

`DevelopmentTestPanel` sahne/prefab serialize etmez. `AfterSceneLoad` bootstrap'i gizli,
`DontSave` bir GameObject üretir. Game View sağ üstündeki panel:

- combat tech + free-buy hazırlığı,
- exact `2K / 5K / 10K` horde,
- cooldown reset,
- horde clear

aksiyonlarını sunar. Play Mode durdurulduğunda Unity bütün transient test state'ini geri
alır.

## Performans

Panel kapalı/boşta ECS query yapmaz. Büyük horde yalnız kullanıcı butona bastığında main
thread üzerinde pool rent ile bir kez kurulur. Steady-state combat sistemleri ve shader
batch sözleşmesi değişmez.
