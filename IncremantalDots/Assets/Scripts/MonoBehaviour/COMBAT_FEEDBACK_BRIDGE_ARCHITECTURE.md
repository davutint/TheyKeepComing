# CombatFeedbackBridge - Mimari

## Amac

`CombatFeedbackBridge`, DOTS combat sistemlerinden gelen kisa omurlu feedback event'lerini GameObject tarafinda pooled VFX/SFX olarak oynatir. Gameplay projectile, damage ve hedefleme ECS tarafinda kalir; bu bridge sadece gorsel/ses juice katmanidir.

## Akis

- ECS sistemleri `CombatVfxEvent` ve `CombatSfxEvent` entity'leri uretir.
- `CombatFeedbackBridge`, `CombatFeedbackRoot` altinda bu event entity'lerini okur.
- Arrow/Frost hit VFX icin sprite flipbook pool kullanir; diger VFX icin prefab ParticleSystem pool kullanir ve event entity'sini siler.
- SFX icin AudioSource pool kullanir, type bazli rate limit uygular ve event entity'sini siler.

## V1 Event Kaynaklari

- `ArcherShootSystem`: `ArrowShoot` SFX. Shoot muzzle VFX V1'de kapali.
- `ArrowHitSystem`: Basic/Rapid icin `ArrowHit`, Frost icin `FrostHit`; hit VFX hedef pozisyonunda kisa sprite flipbook impact olarak oynar.
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
- `FX_Shoot_Ice_hit.prefab` normal Frost hit icin oynatilmaz; Frost V1'de ayni hit flipbook + slow tint ile okunur.

Castle hit V1'de ayrica prefab verilmezse `ArrowHitPrefab` ile fallback yapabilir. Owner daha sonra Inspector'dan ayri castle impact prefab'i atayabilir.

## Performans Notlari

- Stress mode'da `DisableInStressMode = true` ise event'ler temizlenir ama oynatilmaz.
- Hit flipbook pool varsayilan `1024`; pool bosalirsa en eski aktif flipbook recycle edilir.
- ParticleSystem pool type basina varsayilan `24`, frame basi maksimum particle oynatma `24`.
- SFX type bazli min interval ile kisilir; cok okcuda ses yigini olusmasi engellenir.
- Arrow shoot SFX icin `ArrowShootClips` doluysa random clip secilir; bos kalirsa eski `ArrowShootClip` fallback'i kullanilir.
- VFX world z degeri `MobileCastleRenderDepth.ProjectileZ` bandina normalize edilir.
- Hit flipbook world z degeri `MobileCastleRenderDepth.ProjectileZ` bandina normalize edilir.
