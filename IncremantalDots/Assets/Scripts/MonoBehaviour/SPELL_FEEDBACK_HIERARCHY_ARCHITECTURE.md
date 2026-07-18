# Spell Feedback Hierarchy - Mimari

## Amaç

Fireball ve Frost feedback'i, 10.000 düşmanlık yoğun sahnede bile sıradan ok isabetlerinden ve enemy sprite gürültüsünden ayrılır. Bu katman yalnız sunumu değiştirir; Fireball hasarı/yarıçapı/cooldown'u ile Frost hasarı/slow süresi ve hit budget sözleşmesi değişmez.

## Sahiplik

- `SpellFeedbackHierarchy`: ortak renk, ölçek, fade ve sorting sözleşmesinin tek kod sahibi.
- `SpellCastUI`: Fireball projectile ve blast sunumunun GameObject sahibi.
- `CombatFeedbackBridge`: spatial-sampled Frost/Arrow hit flipbook havuzunun sunum sahibi.
- `FireballProjectileSystem`, `FireballStrikeSystem`, `ArrowHitSystem` ve `ZombieSlowTimerSystem`: gameplay sahipleri; hierarchy kodu bu sistemlere yeni davranış eklemez.

## Sorting Sözleşmesi

Tüm efektler `Wall` sorting layer'ında şu sabit sırayı kullanır:

| Görsel | Order | Rol |
|---|---:|---|
| Ordinary Arrow hit | 12 | Düşük öncelikli, küçük impact |
| Frost ring | 47 | Frost alanını taşıyan arka halka |
| Frost hit | 48 | Büyütülmüş cyan impact |
| Fireball projectile aura | 219 | Uçuş silueti |
| Fireball projectile | 220 | Animasyonlu ana mermi |
| Scorched Earth fill | 227 | Koyu ember zemin alanı |
| Scorched Earth ring | 228 | Alan sınırı ve pulse okunurluğu |
| Fireball blast | 230 | Animasyonlu ana patlama |
| Fireball blast core | 231 | Sıcak, yarı saydam vurgu diski |
| Fireball blast ring | 232 | En üstte genişleyen ateş halkası |

Fireball core ve ring ana blast sprite'ının üstündedir; böylece opaque veya yoğun ilk blast karesi hierarchy vurgusunu örtemez.
Fireball projectile, targeting indicator ve bütün blast katmanları ayrıca
`MobileCastleRenderDepth.ProjectileZ` bandına çekilir. Opaque/depth-write enemy renderer'ları
karşısında yalnız sorting order yeterli olmadığı için bu world-z sözleşmesi zorunludur.

## Frost Sunumu

- `ArrowHitSystem` mevcut spatial sampling ve `24/frame` üretim bütçesini korur.
- `CombatFeedbackBridge`, yalnız oynatılan Frost flipbook'larını normal hit ölçeğinin `3.2x` değerine çıkarır ve cyan tint uygular.
- Her pooled hit slotu tek bir `FrostHierarchyRing` child renderer taşır. Ring Frost için açılır, ordinary hit için kapalı kalır; yeni isabet başına GameObject oluşturulmaz.
- Ring yaşam süresi boyunca `1.05 -> 2.2` ölçeğinde genişler ve mevcut flipbook süresiyle birlikte solar.
- Persistent slow okunurluğu yine `ZombieSlowTimerSystem` tarafından enemy tint üzerinden sağlanır.

## Fireball Sunumu

- Projectile için ana flipbook ve bir adet pulse yapan sıcak aura kullanılır.
- Blast için ana flipbook, sıcak turuncu core ve genişleyen turuncu ring birlikte oynar.
- Projectile main/aura ve blast main/core/ring nesneleri tembel olarak bir kez oluşturulur ve sonraki cast'lerde yeniden kullanılır. Aktif düşman sayısıyla artan nesne veya allocation yoktur.
- Core ve ring mevcut Fireball hedefi ile yarıçapını kullanır; yalnız görsel çap çarpanları uygulanır. Gameplay AoE yarıçapı değişmez.
- `Echoing Detonation`, aynı blast main/core/ring setini `0.85s` sonra sıcak-altın palette yeniden
  oynatır; ikinci bir renderer havuzu veya düşman başına feedback üretmez.
- `Scorched Earth`, yaşayan alan başına yalnız bir fill ve bir ring renderer taşır. Fill pulse,
  ring rotation/fade yapar; renk ve çap doğrudan `FireballBurningGround` kalan süre/yarıçapından
  çözülür. Gameplay tick'i görsel katman tarafından üretilmez.

## Performans ve Doğrulama Sınırı

- Frost, mevcut `128` flipbook pool ve `24/frame` playback bütçesi içindedir.
- Base Fireball hierarchy sabit beş runtime renderer ile sınırlıdır. Scorched Earth aktifken tek
  alan için iki ek sabit renderer kullanılır; sayı düşman veya verilen hasar adediyle artmaz.
- `SpellFeedbackHierarchyTests` sıralama, renk, ölçek ve fade sözleşmesini doğrular.
- `SpellFeedbackHierarchyPlayModeTests.FireballEvolutions_ApplyExactAggregateDamageAndFixedGroundPresentation`
  iki behavior evolution'ın exact damage/lifecycle state'ini ve sabit ground sunumunu doğrular.
- `SpellFeedbackHierarchyPlayModeTests.FireballAndFrostHierarchy_RemainsOrderedInsideTenThousandEnemyHorde` gerçek `NewGameScene` içinde `10.000` pooled enemy, altı Frost ve altı ordinary hit ile Fireball projectile/blast katmanlarını doğrular ve `DW_I_SPELL_HIERARCHY_10K.png` çıktısını üretir.
