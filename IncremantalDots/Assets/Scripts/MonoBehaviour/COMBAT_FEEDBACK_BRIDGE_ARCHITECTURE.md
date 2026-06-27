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
