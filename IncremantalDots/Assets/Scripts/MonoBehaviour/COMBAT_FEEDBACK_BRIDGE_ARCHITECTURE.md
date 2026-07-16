# CombatFeedbackBridge - Mimari

## Amac

`CombatFeedbackBridge`, DOTS combat sistemlerinden gelen kisa omurlu feedback event'lerini GameObject tarafinda pooled VFX/SFX olarak oynatir. Gameplay projectile, damage ve hedefleme ECS tarafinda kalir; bu bridge sadece gorsel/ses juice katmanidir.

## Akis

- ECS sistemleri `CombatVfxEvent` ve `CombatSfxEvent` entity'leri uretir. Arrow hit akisi
  ham isabet basina event uretmez; `ArrowHitSystem` once `0.75` world-unit hucrelerde
  tur bazli spatial sample toplar.
- `CombatFeedbackBridge`, `CombatFeedbackRoot` altinda bu event entity'lerini okur.
- Arrow/Frost hit VFX icin sprite flipbook pool kullanir; Frost slotu ayrıca cyan,
  genişleyen pooled hierarchy ring'i taşır. Producer tarafindaki `24`
  event limitine ek olarak bridge tarafinda `24 / frame` global hit playback budget'i
  ve `0.04s` hit VFX rate-limit uygular. Diger VFX icin prefab ParticleSystem pool
  kullanir ve event entity'sini siler.
- SFX icin sabit AudioSource pool kullanir; bir frame'deki event'leri type bazinda aggregate eder,
  oncelik + frame budget + type rate-limit uygular ve event entity'lerini siler.

## V1 Event Kaynaklari

- `ArcherShootSystem`: `ArrowShoot` SFX. Shoot muzzle VFX V1'de kapali.
- `ArrowHitSystem`: Basic/Rapid icin `ArrowHit`, Frost icin `FrostHit` spatial
  candidate'i toplar. Bir frame'de en fazla `24` hit VFX event'i ve mevcut her hit
  turu icin tek `CombatSfxEvent` uretir; `Multiplicity` o cue'nun temsil ettigi
  spatial candidate sayisini tasir.
- `DamageApplySystem`: savunma hasari alindiginda `CastleHit`.
- `ZombieDeathSystem` (M-D): olum aninda `ZombieDeath` SFX (rate-limit 0.09s — kalabalik yigilmaz).
- `FireballStrikeSystem` (M-D): patlama aninda `FireballBlast` SFX (gorsel SpellCastUI'da).

## M-D His Katmani (2026-07-08)

- **SFX clip'leri** "RPG Magic Sound Effects Pack 3 [ELEMENTAL]" paketinden setup tool ile
  YALNIZ-BOSSA atanir (owner atamasi korunur): ZombieDeathClips (MONSTER_Hurt 1-2, random),
  FireballBlastClip (FireMagic_Explosion02), ArrowHitClip, FrostHitClip.
- **Kale hasar hissi:** `PlaySfx` icinde rate-limit'ten GECEN her CastleHit,
  `CameraShaker.Instance.AddTrauma` (trauma^2 Perlin offset, base pozisyon cache) +
  `DamageFlashUI.Instance.Flash` (tam-ekran kirmizi vuru; Canvas'in son sibling'i) tetikler.
  Ayar: `CastleHitShakeTrauma` / `CastleHitFlashEnabled`.
- **Ambiyans:** `AmbientAudioController` (AmbientAudioRoot; setup kurar) — Dusk+Night'ta gece
  drone'u (WindMagic_Drone01_LowSubtleLoop), kanli ay gecesinde DarkMagic_DroneUnderworld_Loop
  + Night'a giris aninda MONSTER_Roar01 sting'i; 2 kaynakli crossfade, GameOver'da susar.
- `_lastSfxTimes` dizisi enum'dan BUYUK tutulur (8 slot) — yeni SFX tipi eklerken tasma olmaz.

## Asset Kullanimi

`FX_Shoot_Arrow.prefab` demo/root projectile prefab'i runtime gameplay icin kullanilmaz. Normal arrow/frost hit icin `fanfx2_cure_small_red/spritesheet.png` flipbook'u kullanilir. ParticleSystem parca prefab'lari sadece opsiyonel/legacy ve castle fallback akisi icin elde tutulur:

- `FX_Shoot_Arrow_muzzle.prefab` su an otomatik baglanabilir ama V1 playback tarafinda kullanilmaz.
- `FX_Shoot_Arrow_hit.prefab` normal hit icin oynatilmaz; castle fallback olarak atanabilir.
- `FX_Shoot_Ice_hit.prefab` normal Frost hit icin oynatilmaz; Frost V1'de büyütülmüş cyan
  hit flipbook, pooled genişleyen ring ve persistent slow tint ile okunur.

Castle hit VFX prefab'i VARSAYILAN BOS (polish fix: eski ArrowHitPrefab fallback'i "duvara
ok saplanmasi" bug'i uretiyordu — kaldirildi; setup atanmis arrowHit'i de temizler).
Kale vurus hissi sarsinti+flash+ses ile verilir. Owner isterse Inspector'dan ayri bir
castle impact prefab'i atayabilir (yalniz-bossa kurali onu korur).

## Performans Notlari

- Stress mode'da `DisableInStressMode = true` ise event'ler temizlenir ama oynatilmaz.
- Hit flipbook pool varsayilan `128`; pool bosalirsa en eski aktif flipbook recycle edilir.
- `ArrowHitSystem`, sabit `512` candidate map'i icinde ayni `0.75` world-unit hucredeki
  ayni hit turunu tek ornege indirir. Basic/Rapid ve Frost birlikteyse `24` VFX slotunun
  en az `4` slotu mevcut her ture acik kalir; normal dengede Frost `8`, Arrow `16` slot alir.
- Bridge, producer disindaki event kaynaklarina karsi ikinci guvenlik kati olarak hit
  flipbook playback'ini frame basi `24` ve `0.04s` burst araligi ile sinirlar.
- Ordinary hit `Wall/12`, Frost ring/hit `Wall/47-48` kullanır. Her pool slotunun ring'i
  kurulumda bir kez oluşturulur; aktif enemy veya ham hit sayısı yeni GameObject üretmez.
- Ayrıntılı render sözleşmesi `SPELL_FEEDBACK_HIERARCHY_ARCHITECTURE.md` dosyasındadır.
- `CombatFeedbackBudgetTelemetryData`, son frame spatial candidate/emitted/dropped
  sayilarini ve run-toplamlarini ECS singleton olarak tutar. Bridge ayrica processed,
  played ve dropped hit VFX telemetrisini public read-only property'lerle sunar.
- ParticleSystem pool type basina varsayilan `24`, frame basi maksimum particle oynatma `24`.
- SFX playback frame basi en fazla `4` cue ile sinirlidir. Oncelik Fireball, Castle, Frost,
  ArrowShoot, ZombieDeath ve ArrowHit sirasidir; kritik cue'lar kalabalikta kaybolmaz.
- Bir frame'deki butun `ArrowShoot` event'leri ortalama world position'da tek salvo cue'ya
  donusturulur. Logaritmik gain `0.62` tavanda kalir; pitch en fazla `%8` alcalir.
- Shoot rate-limit Day/Dusk/Dawn icin `0.075s`, Night icin `0.12s` alt siniridir.
  Bin okcu yeni AudioSource veya bin ayri ses uretmez.
- Arrow shoot SFX icin `ArrowShootClips` doluysa random clip secilir; bos kalirsa eski `ArrowShootClip` fallback'i kullanilir.
- VFX world z degeri `MobileCastleRenderDepth.ProjectileZ` bandina normalize edilir.
- Hit flipbook world z degeri `MobileCastleRenderDepth.ProjectileZ` bandina normalize edilir.
