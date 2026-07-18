# Dead Walls V1 - Implementation Tracker

> **Amaç:** V1 Blueprint hedefi ile mevcut Unity projesi arasındaki farkı tek yerde tutmak; nerede kaldığımızı, sıradaki işi ve tamamlanma kanıtını kaybetmemek.
>
> **Tracker sürümü:** 2.3
> **Son tam kapsam denetimi:** 2026-07-17
> **Aktif paket:** Post-Package V1 Closure - Owner Content, Narrative ve Tuning
> **Aktif iş:** `DW-V1-CLOSURE-10K-1K-NIGHT-VISUAL` - Combined 1K Archer + 10K Enemy + Active Projectile Night Visual Acceptance
> **İlerleme:** `441 / 442` tracker checkbox'ı tamamlandı - `%99,77`
> İlerleme hesabı bütün iş, kabul, DoD ve owner-kararı checkbox'larını kapsar; `[~]` tamamlanmış sayılmaz.
> **Council kapsam kararı:** Owner, 2026-07-15 tarihinde Emergency Council yolunu iptal etti. V1 Council yalnız Day `3/6/9...` regular toplantılarından oluşur.

---

## 1. Otorite ve Kullanım Kuralı

### Kaynak sırası

1. **Tasarım otoritesi:** [DEAD_WALLS_GAME_DESIGN_BLUEPRINT_v1.0.pdf](./DEAD_WALLS_GAME_DESIGN_BLUEPRINT_v1.0.pdf)
2. **Düzenlenebilir tasarım kaynağı:** [DEAD_WALLS_GAME_DESIGN_BLUEPRINT_v1.0.docx](./DEAD_WALLS_GAME_DESIGN_BLUEPRINT_v1.0.docx)
3. **Uygulama ve fark takibi:** Bu tracker.
4. **Mevcut runtime gerçeği:** Aktif scene, prefab, ScriptableObject ve kod sahipleri.
5. **Tarihsel bağlam:** Eski GDD, roadmap, master plan ve milestone belgeleri.

**Release doğrulama kanıtı:** [DEAD_WALLS_V1_RELEASE_VERIFICATION_REPORT.md](./DEAD_WALLS_V1_RELEASE_VERIFICATION_REPORT.md) full regression, save migration matrisi ve fresh long-run soak sonuçlarını tek audit kaydında tutar.

**Owner referansı:** [Zero Stress King: Idle Defense](https://store.steampowered.com/app/4271160/Zero_Stress_King_Idle_Defense/) yalnız sürekli otomatik saldırı ve incremental büyüme için referanstır. Dead Walls gerçek ölüm, 60 saniyelik faz ritmi, worker ekonomisi, Council ve procedural Castle Heart ile ayrışır.

Bu tracker Blueprint'i değiştiremez. Blueprint ile kod çelişiyorsa hedef Blueprint'tir; proje o hedefe çekilir. Blueprint'te açık olmayan ürün kararı kod içinde varsayımla kapatılmaz, owner'a sorulur.

### Statüler

- `[x]` Mevcut oyun Blueprint ile uyumlu ve kanıtlandı.
- `[~]` İlgili sistem mevcut fakat sözleşme eksik veya runtime testi bekliyor.
- `[!]` Aktif oyun Blueprint ile çelişiyor.
- `[ ]` Sistem/iş henüz uygulanmadı.
- `[?]` Owner kararı veya mockup gerekiyor.

### Tamamlama kuralı

Bir iş ancak şu dört kayıt birlikte varsa `[x]` olur:

1. Kod/scene/prefab/SO değişikliği tamamlandı.
2. İlgili kabul kriteri karşılandı.
3. Uygun EditMode/PlayMode/runtime veya görsel doğrulama yapıldı.
4. Bu tracker'ın mevcut durum, checklist, gap ve çalışma günlüğü güncellendi.

---

## 2. Blueprint Kapsam Haritası

Bu tablo Blueprint'in hiçbir ana bölümünün tracker dışında kalmaması için kullanılır.

| Blueprint sayfaları | Konu | Tracker karşılığı |
|---|---|---|
| 1-3 | Belge otoritesi, ürün terimleri | Bölüm 1-3 |
| 4-5 | Ürün sözleşmesi, oyuncu rolü | Bölüm 3 |
| 6 | 60 saniyelik cycle | Package B |
| 7 | Run, ölüm ve kayıt | Package A + H |
| 8 | Sabit battlefield/kamera | Bölüm 3 + Package I |
| 9 | Tek prefab horde | Package B |
| 10 | Wall ve repair | Package A + G |
| 11-13 | Kaynak, worker, population, beds | Package C + D |
| 14 | Council | Package F |
| 15-18 | Archer, ammo, placement, targeting | Package D |
| 19-21 | Castle Heart ve procedural teknoloji graph'ı | Package E |
| 22 | Aktif yetenekler | Package G |
| 23-24 | Meta ve persistence | Package H |
| 25 | Minimal HUD | Package I |
| 26 | Onboarding | Package I |
| 27 | Görsel/işitsel yön | Package I |
| 28-29 | Teknik sahiplik ve data contract'ları | Bölüm 13 |
| 30 | Performans sözleşmesi | Bölüm 14 |
| 31 | Tuning ve telemetry | Bölüm 15 |
| 32-34 | Paket sırası ve kabul kapıları | Bölüm 4-12 |
| 35 | QA matrisi | Bölüm 16 |
| 36 | Risk register | Bölüm 17 |
| 37 | Release Definition of Done | Bölüm 18 |
| 38 | Kapsam dışı guardrail'ler | Bölüm 19 |
| 39 | Açık kararlar | Bölüm 20 |
| 40 | Repo evidence/owner kaynakları | Bölüm 21 |

---

## 3. Ürün Sözleşmesi ve Mevcut Oyun Özeti

### Ana terimler

| Terim | Tracker'daki kesin anlamı |
|---|---|
| Run | Wall ayaktayken süren ve yalnız Game Over ile sıfırlanan aktif koşu |
| Stand | Bir run'ın player-facing anlatı adı; ilk sabahtan Wall düşene kadar süren savunma |
| Steward | Oyuncunun canon rolü; savaşçı değil insan, kaynak, savunma ve teknoloji karar sahibi |
| Castle Heart | Run teknolojisinin procedural graph ekranı ve tek upgrade owner'ı |
| Council | Her 3 günde bir regular karar açan run yönetim ve risk katmanı |
| Meta | Yalnız ölümden sonra kalan sabit kalıcı upgrade listesi |
| Grave Essence | Yalnız run içi Heart node'larında harcanan ve ölümde silinen kaynak |
| Last Embers | Başarısız stand'den sonraki run'a taşınan kalıcı hafıza/meta para kimliği; literal ruh değildir |
| Spawn budget | Enemy statını büyütmeden sahaya çıkacak adet/akış baskısını yöneten değer |

### Kilitli ürün kimliği

- Sabit kale, tek cephe ve otomatik savaş.
- Tek enemy prefab; boss, elite veya varyant yok.
- 10.000 aktif düşman ve 1.000 toplam okçu hedefi.
- 60 saniyelik kesintisiz Day/Dusk/Night/Dawn döngüsü.
- Zorluk enemy stat şişmesinden değil, sayı ve akıştan gelir.
- Oyuncu işçi, kaynak, Wall, Arrow, Council, Heart ve cooldown yönetir.
- Roguelike değişkenlik düşman/lane seçiminden değil generated Castle Heart graph'ından gelir.
- Wall `0 HP` olduğunda run kesin olarak biter.
- PC/Steam etkileşim dili; reklam, IAP veya mobile interaction hedefi yok.
- Canon premise: dünya sona ermiştir fakat kuşatma sürer; Castle Heart Wall düştüğünde stand'i yeniden tutuşturur, yalnız Last Embers kalır. Heart'ın ve dead horde'un kökeni V1'de bilinçli olarak açıklanmaz.

### Oyuncu eylemleri ve ritim

| Ritim | Oyuncu eylemi | Sistem |
|---|---|---|
| Sürekli | Kaynak ve Arrow stokunu izler | Üst HUD + ammo satın alma |
| Sık | Worker target ratio değiştirir | Farm/Lumberyard/Quarry/Mine |
| Sık | Archer alır veya retrain eder | Basic/Rapid/Frost, ortak 1000 cap |
| Dönemsel | Day/Dusk sırasında Wall onarır | Stone ile tek seferlik normal repair |
| Dönemsel | Council kararı verir | 3/6/9... günlerinin Dawn başlangıcı |
| Dönemsel | Castle Heart node'u alır | Grave Essence + full pause |
| Taktik | Fireball/Rally/Emergency Repair kullanır | Alt orta cooldown barı |

### Canlı Unity/repo gerçeği - 2026-07-14

| Alan | Mevcut gerçek | Sonuç |
|---|---|---|
| Aktif scene | `Assets/Scenes/NewGameScene.unity` Unity MCP'de loaded; bu paket scene kaydetmedi, dış değişiklik modali in-memory hali korumak için Ignore edildi | Disk/in-memory scene uzlaşması sonraki scene yazımından önce kontrol edilmeli |
| Kamera | Ortografik, size `8`, gameplay pan/zoom controller yok; `CameraShaker` var | Temel sabit kamera uyumlu |
| Oyun hızı | Player runtime `1x`; pause `0`, Game Over sunumu `0.25x`; x2/x4 kontrolü, wall-clock owner'ı veya offline save alanı yok. Editor `LongRunSimulatorWindow` ayrı test aracıdır | `[x]` Source/save-schema/scene release guard + exact Continue regresyonu |
| Battlefield | Kale/duvar solda, spawn sağdaki `SpawnLineX` bandından geliyor | Temel kompozisyon uyumlu |
| Build placement | Aktif scene'de `BuildingPlacementUI` ve `BuildingGridManager` bağlı değil | Hazır bina yönüyle uyumlu |
| Cycle | Release guard ile exact `60 = 30/5/20/5`; dört fazda pozitif spawn temposu ve Day wrap'ında prep gap yok | Uyumlu |
| Horde | Tek production catalog/tanım/prefab ve ortak SubScene binding'i release guard ile kilitli; sabit stats; saved backlog; expandable bulk rent/return pool; Blood Moon dormant | 10K gate ve optimizasyon ölçüldü |
| Moat | Runtime flag kapalı; slow `1`, damage `0`; tech/meta catalog bağlantıları dormant | Uyumlu |
| Defense | Damage/Game Over aktif ve testli olarak tek Wall'a çekildi | `[x]` |
| Normal repair | Stone-only ve yalnız Day/Dusk | `[x]` |
| Save | Exact same-moment Continue; schema v17, minimum v3; purchased bed, worker bina yatırımı, Archer Formation V1, finite Arrow yatırımı, Grave Essence, exact Heart graph replay ve regular Council handled-day/active-card discriminator | `[x]` |
| Economy | Worker üretimi, bed alımı ve dört hazır binanın capacity/efficiency yatırımları var; bed ve bina fiyat eğrileri `DefaultDifficulty.asset`/Difficulty Tuner üzerinden baked runtime tuning'e bağlı; V1 ana kaynaklarında pasif consumption yok, Arrow başarılı projectile rent başına sürekli tükenen tek stok | `[x]` Runtime release guard |
| Population | House bed state + Wood purchase API + exact save var; Dawn isteği boş yatak ve Food/kişi bütçesiyle sınırlı, gerçek accepted count uygulanıyor, Food bir kez düşülüyor ve en fazla 15 temsili survivor sağdan Wall arkasına yürüyor; total/workers/archers/idle + base/purchased beds tek snapshot guard'ında exact | `[x]` |
| Workers | Kalıcı target ratio + actual/cap/idle state, +1/+10/+100/direct input, bağımsız bina capacity/efficiency seviyeleri, yeni nüfus auto-allocation, exact save, Low/Medium/High density ve allocation-senkronlu animation/cargo/lantern/delivery feedback var; dört actual count + dört ratio birleşik Continue guard'ında exact | `[x]` |
| Council | 9 launch template, 11 serialized atom (`cap_bonus` dormant), staged Day 3/6/9 havuzu, curated source-retirement/follow-up, exact kart ve fail-closed policy aktif | `[x]` 5.400-sample budget/token/content gate + exact save/guard doğrulandı |
| Archers | Basic/Rapid/Frost, instant buy, incremental type maliyeti, yerinde retrain, version'lı 40x25 formation, scalable target load ve pooled projectile lifetime var; damage/fire-rate/range/Frost slow progression'i yalnız Castle Heart effect pipeline'ında | `[x]` Combat + Heart progression owner'ı |
| Archer cap | `ArcherCapacityUtility` Basic/Rapid/Frost toplamını `1000` ile sınırlar; buy, merkezi spawn, Council, meta, restore ve legacy Barracks aynı guard'ı kullanır | `[x]` |
| Placement | Formation V1 asset'iyle sabit 40 `outside` tile x 25 seeded diamond nokta; layer-fill sıra, 1000 gizmo ve v9 Continue testli | `[x]` |
| Targeting | Persistent coarse spatial query + incoming damage reservation Burst job'ları aktif | `[x]` |
| Ammo | Finite stok; gerçek projectile başına `-1`; Wood ile anlık +1/+5/Buy Max refill; Wood+Iron CAP/EFF yatırımı; Current/Capacity HUD ve exact save v13 | `[x]` |
| Tech/Heart | Production v2 `37` node generated graph/reveal, Grave Essence-only purchase, actual effect adapter'i, hidden-safe fullscreen `HeartScreenUI`, full simulation pause ve schema v17 exact graph/effect replay aktif; legacy `TechTreeUI` ve direct archer level API'si aktif owner değil | `[x]` E1-E6 + launch content + archer progression cutover |
| Fireball | Dünya hedefli projectile/AoE ve cooldown çalışması mevcut | Korunacak temel |
| Rally | Wood/Food maliyetli prep purchase | Cooldown-only ability olmalı |
| Emergency Repair | Ayrı ability yok | Eksik |
| Meta | Ayrı JSON, durable ölüm kapılı shop, Blueprint exact 11-definition katalog, üstel repeatable sink'ler ve atomik tek-seferlik pool unlock aktif; player-facing `LAST EMBERS` presentation v2, gerçek icon asset'i ve scrollable polished death screen canonical katalogdan besleniyor | `[x]` Kimlik/copy/icon + teknik katalog/runtime sınırı tamamlandı; exact reward tuning ayrı |
| Narrative | `DEAD_WALLS_V1_NARRATIVE_PREMISE.md` Steward/stand/Heart rekindle/Last Embers canon'unu ve deliberate unknown sınırlarını kilitliyor. Main menu `THE WORLD ENDED. THE SIEGE DID NOT.` + `BEGIN THE STAND` kullanıyor; prologue modal/cutscene yok | `[x]` World pitch, opening copy, FullHD Game View ve scene/runtime/doc drift regresyonu tamamlandı |
| HUD | CyclePanel, DAY/DUSK/NIGHT ve Horde Pressure mevcut; tek Wall runtime gizleme var | Package I polish gerekli |
| Tutorial | İlk Day worker ratio, affordable Basic Archer, low ammo, Castle Heart, regular Council, Day repair ve ilk Night ability-key adımları non-modal pulse, 10 exact approved English hint ve durable meta flag ile aktif; accepted player action prompt'tan önce gelirse completion kaybolmuyor, yedi alt flag tamamlanınca `tutorial.v1.complete` meta save'e yazılıp legacy state backfill ediliyor. Pause ve ana menü Settings yüzeyindeki iki-tık onaylı reset altı exact approved English state metni kullanıp yedi adım + global flag'i tek atomik save'de temizliyor; run/meta yükseltmeleri korunuyor. Controller transaction/assignment/modal açma çağrısı yapmıyor ve blocking pause sırasında cue zinciri kurmuyor | `[x]` Package I ilk-run completion, second-run suppression, reset ve player-facing copy kabulünden geçti |
| Testler | Son full baseline: EditMode `209/209`, PlayMode `37 pass + 1 explicit profiler skip`; Package H meta katalog/regresyon EditMode `50/50`, run-start/Continue + death shop PlayMode `2/2`; Standalone Player-targeted 10K `1/1` | Fixed catalog, exponential cost, v13 migration, runtime effect ayrımı ve atomik pool purchase temiz; full baseline tarihsel olarak korunuyor |
| Telemetry | Spawn budget demanded/spawned/backlog telemetry mevcut; tam Blueprint event owner'ı eksik | Kısmi |

---

## 4. Paket Sırası ve Anlık İlerleme

| Sıra | Paket | Durum | Sonraki pakete geçiş kapısı |
|---:|---|---|---|
| 1 | A - System Contracts | Tamamlandı | Reset/Continue deterministik; upkeep yok; tek Wall testli |
| 2 | B - Continuous Horde | Tamamlandı | Sabit stats, backlog/pool ve 10K ürün ölçümü tamamlandı |
| 3 | C - Economy + Population | Tamamlandı | Pasif drain yok; arrival tek Food öder; cap aşılmaz; fiyat tuning'i testli |
| 4 | D - Archers + Ammo | Tamamlandı | 1.000 x 10.000 targeting/projectile ve Arrow truth çalışır |
| 5 | E - Castle Heart | Tamamlandı | Aynı seed/load aynı valid graph'ı üretir; production content owner gate ayrı |
| 6 | F - Council | Tamamlandı | 9 template staged launch set, curated repeat/follow-up, 5.400-sample budget ve full regression geçti |
| 7 | G - Active Abilities | Tamamlandı | Kaynak tüketmez; Night repair sözleşmesi ve exact cooldown save testli |
| 8 | H - Meta + Persistence | Tamamlandı | Ölüm ödülü idempotent; meta v3 fail-closed; fixed katalog ve incremental cost tamamlandı; deterministic 10K rebuild policy ayrı ürün kapısına taşındı |
| 9 | I - Product Gate | Tamamlandı | 10K scenario, tutorial ve owner görsel incelemesi geçti; kapsamlı audio asset polish'i owner kararıyla V1 kabulinin dışında |

> “A1/A2” resmî Blueprint paketi değildir. Resmî paketler A-I'dır; iş kimlikleri yalnız tracker içinde `DW-A-SAVE` gibi kullanılır.

---

## 5. Package A - System Contracts

### Mevcut oyun ile karşılaştırma

| Sözleşme | Mevcut oyun | Durum |
|---|---|---|
| Tek Wall truth | Damage, Game Over, authoring, repair ve save defense alanı Wall'a çekildi | `[x]` 19/19 EditMode |
| Run/meta ayrımı | Exact run `run_save.json`; kalıcı progression `meta_progress.json`; ölüm transaction'ı ayrı receipt | `[x]` |
| Exact Continue | Schema v17 aynı cycle/phase/timer, kaynak, spawn RNG, worker target/checkpoint, purchased bed, worker bina yatırımı, Archer Formation V1, finite Arrow ve sonraki run state'lerini restore ediyor; v3-v16 sıralı migrate ediliyor | `[x]` EditMode + PlayMode |
| Otomatik save | Ana menüye dönmeden önce ve application quit sırasında exact snapshot alınıyor | `[x]` |
| Gönüllü reset yok | Aktif run sırasında Main Menu New Run ve Pause Restart kapalı; Game Over Restart yeni koşu başlatır | `[x]` |
| Upkeep yok | V1 ResourceTick consumption'ı yok sayıyor; population Food ve Fletcher Wood yolları castle loop'ta kapalı | `[x]` |
| Legacy Gate/Core disabled | Gate/Core data/UI referansları dormant; runtime damage, Game Over, repair, Council, save ve HUD tek Wall | `[x]` |
| Tuning owner | Difficulty alanları Profile, diğer baseline alanlar active SubScene Authoring, birleştirme tek resolver | `[x]` |
| Normal repair resource/phase | GameManager Stone-only cost üretiyor; Day/Dusk dışını gameplay ve UI seviyesinde kapatıyor | `[x]` |

### `DW-A-SAVE` - Tamamlandı: Exact Run Snapshot & Continue

- [x] Run save schema güncel `v17`, minimum `v3`; v2 Dawn checkpoint reddediliyor, v3-v16 state'leri sıralı migrate ediliyor; zincir worker/bed/bina/formation/Arrow/Heart/Council/ability/meta/combat/telemetry/Fireball state'ini açık migration'larla koruyor ve v16 direct archer level listesini Heart state'i uydurmadan retired ediyor.
- [x] Gün/cycle index, aktif phase, exact cycle timer/progress ve spawn RNG state'i kaydediliyor.
- [x] Wood, Stone, Iron, Food, Arrow current ve kesirli accumulator state'i kaydediliyor; data-driven capacity tech/config'ten yeniden hesaplanıyor.
- [x] Population, bed/capacity, actual worker count, target ratio/cap/idle/checkpoint ve growth/event tekrar gate'leri capture/restore zincirinde kaydediliyor.
- [x] Basic/Rapid/Frost count, `ArcherFormationVersion`, exact Heart graph level'ları ve ilgili run bonus state'i kaydediliyor; individual world position kaydedilmiyor. `ArcherTypeLevels` yalnız v3-v16 JSON girdisi olarak okunup v17'de boş normalize ediliyor.
- [x] Wall current HP, Fireball cooldown/projectile, Rally ve Fortify state'i kaydediliyor.
- [x] Tech node level state'i kaydediliyor; generated graph reveal/effect aggregate'leri level state'inden yeniden kuruluyor.
- [x] Council flags, recent/one-shot memory, salt, cooldown, active event/options/effects ve süreli economy/horde effect state'i kaydediliyor.
- [x] Ana menüye dönmeden hemen önce save alınıyor; başarısız save sahne geçişini durduruyor.
- [x] Uygulama kapanırken canlı ve ölmemiş koşu güvenli biçimde kaydediliyor.
- [x] Continue sırasında `CycleIndex + 1`, zorunlu `Day`, timer `0` ve Council reroll davranışları kaldırıldı.
- [x] Aktif run varken gönüllü New Run/Restart yolları kapatıldı; Game Over sonrası Restart korunuyor.
- [x] Wall `0 HP` frame'inde death receipt önce yazılıyor ve run save Continue için geçersiz kılınıyor.
- [x] Run identity + death receipt + `RewardedRunIds` ile aynı ölümün meta ödülü idempotent hale getirildi.
- [x] Eski Dawn checkpoint metotları runtime akışından çıkarıldı; exact snapshot tek aktif save owner'ı.

### `DW-A-UPKEEP` - Tamamlandı: No Passive Main Resource Consumption

- [x] `ResourceConsumptionRate` V1 castle loop'ta `ResourceTickSystem` tarafından sıfır kabul ediliyor.
- [x] `PopulationTickSystem` pasif Food consumption yazmıyor.
- [x] `BuildingPopulationSystem`, V1'de serialize kalmış `BuildingFoodCost` değerlerini sıfırlıyor.
- [x] Legacy `ArrowProductionSystem`, `MobileCastleCombatConfig` bulunduğunda Fletcher arrow üretimi ve Wood consumption uygulamıyor.
- [x] Ana kaynakların yüksek bir consumption rate enjekte edildiğinde dahi azalmadığını gerçek `NewGameScene` PlayMode testi kanıtlıyor.
- [x] Bu işte Arrow ekonomisi sahte biçimde kapatılmadı; finite stok ve Wood ile anlık satın alma daha sonra `DW-D-AMMO` kapsamında gerçek owner'larıyla tamamlandı.

### `DW-A-REPAIR` - Tamamlandı: Stone-only Day/Dusk Repair

- [x] Bütün repair çağrı yolları `GameManager.CanRepairDefenseFull`, `GetRepairCost` ve `RepairDefenseFull` owner'ında birleşiyor.
- [x] Normal repair maliyetinden Wood kaldırıldı; eksik HP oranına göre yalnız Stone harcanıyor.
- [x] Normal repair yalnız Day ve Dusk phase'lerinde açık.
- [x] Night ve Dawn sırasında hem gameplay transaction'ı hem UI interactable state'i kapalı.
- [x] UI kilit metni `Day / Dusk only`; cost etiketi yalnız Stone gösteriyor.
- [x] Same-frame lethal damage, phase ve Stone-only transaction regression testleri geçti.

### `DW-A-TUNING` - Tamamlandı: Single Runtime Tuning Owner

- [x] Aktif runtime tuning alanlarının bake, live-apply ve aggregate yazma yolları çıkarıldı.
- [x] Difficulty baseline alanlarının owner'ı `DifficultyProfileSO` olarak kesinleştirildi.
- [x] Profile taşınmamış geometri, mode, cycle süreleri ve ekonomi baseline alanlarının owner'ı aktif SubScene `MobileCastleCombatAuthoring` olarak kesinleştirildi.
- [x] `MobileCastleCombatConfig` içerik kaynağı değil, bake edilmiş runtime çıktı olarak tanımlandı.
- [x] Tech/meta/Council değişiklikleri baseline üzerine effective aggregate katmanı olarak ayrıldı.
- [x] Baker ve Difficulty Tuner live-apply içindeki çift profile eşlemesi `MobileCastleTuningResolver` altında birleştirildi.
- [x] Difficulty curve ve SpecialNight sample üretimi Baker/Tuner için tek resolver metoduna çekildi.
- [x] Active SubScene/Profile farkları EditMode, gerçek bake edilmiş runtime config PlayMode testiyle doğrulandı.
- [x] Tuning owner sözleşmesi architecture ve editor setup belgelerine yazıldı.

### `DW-A-LEGACY` - Tamamlandı: Gate/Core Runtime Exclusion

- [x] Gate/Core component ve serialized UI referanslarının bütün aktif okuma/yazma yolları çıkarıldı.
- [x] `CastleAuthoring.Baker` yalnız Wall, WallXPosition ve CastleUpgradeData üretiyor; Gate/Core bake edilmiyor.
- [x] Damage ve Game Over kararının yalnız Wall üzerinden üretildiği runtime regression testiyle kilitlendi.
- [x] Repair, Council, tech/meta defense ve save/Continue yolları yalnız Wall kullanıyor.
- [x] Gate/Core HUD referansları serialize kalabilse de runtime'da kapalı.
- [x] Gate/Core `0 HP` enjekte edildiğinde oyun sürüyor; Wall öldüğünde Gate/Core tam can olsa dahi Game Over oluşuyor.
- [x] Legacy component/content migration uyumluluğu için dormant bırakıldı; aktif V1 davranışına sızması testle engellendi.
- [x] Package A kabul kapısı test kanıtlarıyla kapatıldı.

### Package A kalan işler

- [x] `ResourceConsumptionRate` içindeki aktif ana kaynak tüketimini V1 loop'unda sıfırla/devre dışı bırak.
- [x] `PopulationTickSystem` pasif Food tüketimini V1 loop'undan çıkar.
- [x] Legacy `ArrowProductionSystem`/Fletcher consumption yolunun V1'e sızmasını engelle.
- [x] Aktif tuning alanlarında tek owner belirle: config default, scene override ve profile önceliğini belgeleyip test et.
- [x] Gate/Core bileşenlerinin aktif damage, repair, Council, save ve HUD yollarına dönmesini engelle.
- [x] Normal repair maliyetinden Wood'u kaldır; Stone'u tek harcama kaynağı yap.
- [x] Normal repair'i yalnız Day/Dusk phase'lerinde aç.
- [x] Tek Wall EditMode testlerini Unity üzerinden çalıştır ve sonuç kaydet.
- [x] Wall `0 HP` + aynı frame repair testini ekle.
- [x] Save/Continue round-trip, migration ve ölüm invalidation testlerini ekle.

### Package A kabul kapısı

- [x] Restart/reset kuralları Blueprint ile uyumlu.
- [x] Continue aynı phase/timer/state ile deterministik.
- [x] Wood/Stone/Iron/Food pasif negatif akmıyor.
- [x] Wall `0 HP` aynı frame repair ile geri döndürülemiyor; Game Over geçişi tek transition gate'inden üretiliyor.
- [x] Gate/Core aktif ürün davranışına geri dönemiyor.

---

## 6. Package B - Continuous Horde

### `DW-B-CYCLE` - Tamamlandı: Blueprint Phase Rhythm

- [x] Aktif cycle süreleri `Day 30 / Dusk 5 / Night 20 / Dawn 5` oldu.
- [x] Dört fazın toplamı `60 saniye` olarak kilitlendi.
- [x] Profile/Authoring tuning owner sözleşmesi korunarak süreler active SubScene Authoring'de güncellendi.
- [x] Authoring default ve setup tool initializer değerleri aynı ritme çekildi.
- [x] Continue exact phase/timer davranışı yeni sürelerle çalışıyor.
- [x] Day, Dusk, Night ve Dawn fazlarının intensity değeri runtime testte sıfırın üzerinde.

### `DW-B-STATS` - Tamamlandı: Quantity-only Difficulty

- [x] `MobileWaveUtility` zombie HP'yi yalnız base HP'den yazıyor; cycle/day growth okumuyor.
- [x] Zombie damage yalnız base damage'dan yazılıyor; cycle growth okunmuyor.
- [x] Zombie speed yalnız base speed'den yazılıyor; wave growth okunmuyor.
- [x] Active Authoring/Profile growth değerleri `0` yapıldı.
- [x] Legacy growth ve HP curve alanları serialize uyumluluğu için kalabilse de V1 stat hesabında dormant.
- [x] Enemy count, batch ve spawn interval quantity pressure kanalları korunuyor.
- [x] Day 1 ile ileri cycle runtime karşılaştırmasında HP/damage/speed aynı; count artıyor ve interval daralıyor.

### `DW-B-SPECIAL` - Tamamlandı: Remove Special Nights

- [x] DefaultDifficulty içindeki Blood Moon/SpecialNights seed'i kaldırıldı.
- [x] Setup tool'un SpecialNights'i yeniden seed etme yolu kaldırıldı.
- [x] Tuning resolver bütün V1 sample'larında special multiplier'ı `1` yazıyor.
- [x] Runtime cycle system stale buffer multiplier'ını dahi okumuyor ve `IsBloodMoonNight=false` yazıyor.
- [x] Save/Continue legacy special-night flag'ini runtime'a geri yüklemiyor.
- [x] Warning, color, vignette ve audio dalları false runtime flag nedeniyle dormant.
- [x] SpecialNight content/schema gelecekte kullanım için korunuyor fakat V1 runtime'a bağlı değil.
- [x] Stale special multiplier enjekte edilen ileri gün testinde intensity bonusu ve warning oluşmadı.

### `DW-B-FLOW` - Tamamlandı: Spawn Budget & Backlog

- [x] Count/batch/interval quantity pressure matematiği `ContinuousSpawnBudgetUtility` altında toplandı.
- [x] Day curve/base interval ile phase multiplier'ı ayrı tutan `ContinuousSpawnBudgetData` kuruldu.
- [x] Dawn'daki düşük anlık intensity yalnız effective interval'i etkiliyor; yeni gün tabanına geri yazmıyor.
- [x] Active cap dolduğunda geçen her interval talebi explicit `PendingEnemies` backlog'una ekleniyor.
- [x] Backlog ve demanded/spawned telemetry sayaçları exact save/Continue state'ine eklendi.
- [x] Cap boşaldığında backlog'un `MaxSpawnBatch` ve alive capacity sınırlarıyla sahaya aktığı runtime test edildi.

### `DW-B-MOAT` - Tamamlandı: Dormant Moat Isolation

- [x] `MoatSystem -> ZombieSlow/ZombieStats`, authoring, tech aggregate ve meta grant zinciri çıkarıldı.
- [x] Baker, runtime aggregate ve reset yolları `disabled / slow 1 / damage 0` neutral sözleşmesine çekildi.
- [x] `moat_dig`, `moat_flame` ve `start_moat` assetleri silinmeden aktif tech/meta catalog'larından çıkarıldı.
- [x] Setup tool'un dormant Moat içeriğini yeniden seed/merge etmesi engellendi.
- [x] Stale `0.05 slow + 100000 DPS` değerlerinin zombie speed/HP/slow state'ini değiştiremediği runtime test edildi.

### `DW-B-ENEMY` - Tamamlandı: Single Enemy Catalog Contract

- [x] `EnemyDefinition` veri sözleşmesini oluştur: id, prefab, base stats ve pool metadata.
- [x] V1 active enemy catalog'ını yalnız mevcut tek zombie prefab ile seed et.
- [x] `WaveSpawnSystem` prefab/stat kaynağını catalog contract'ına bağla.
- [x] Spawn/UI kodunda enemy-type özel dallanma oluşmasını engelleyen validation testleri ekle.

### `DW-B-POOL` - Tamamlandı: Expandable Enemy Pool

- [x] Catalog `PoolPrewarm` metadata'sına göre inactive enemy entity rezervi oluştur.
- [x] Spawn talebini instantiate yerine pool rent'e bağla; rezerv biterse `PoolExpandBatch` kadar genişlet.
- [x] Ölüm cleanup'ını `DestroyEntity` yerine deterministik pool return akışına çevir.
- [x] Pool return sırasında target/projectile referansları ile zombie state/component verilerini güvenli sıfırla.
- [x] Exact Continue snapshot ve backlog davranışını pool kapasitesinden bağımsız tut.
- [x] Prewarm, expand, rent, return ve yoğun death churn için EditMode/PlayMode kanıtı ekle.

### `DW-B-SCALE` - Tamamlandı: 10K Horde Runtime Gate

- [x] Gerçek `NewGameScene`, HUD, VFX/SFX ve save/Continue açıkken 10.000 aktif enemy senaryosu kur.
- [x] Test-only/runtime tuning ile active cap ve backlog'u 10K ölçümüne kontrollü çıkar; release değerini ölçüm sonucu olmadan değiştirme.
- [x] Pool total/available/active, expansion, rent/return ve spawn backlog telemetry'sini senaryo raporuna bağla.
- [x] Frame pacing, main-thread spike, allocation ve Entities rendering darboğazlarını ölç.
- [x] Fireball aynı-frame çoklu death return ve maksimum-state Continue senaryolarını doğrula.
- [x] Sonuçlara göre Package B kabul kapısını kapat veya ölçülmüş blocker kaydet.

### `DW-B-SCALE-OPT` - Tamamlandı: 10K Player doğrulaması

- [x] `DamageCleanupSystem` return yolunu 10.000 tekil buffer/state erişimi yerine Burst-parallel reset + tek buffer/state commit kullanan bulk pool return'e çevir.
- [x] `126,42-131,95 ms` death/return peak değerini aynı benchmark ile yeniden ölç: iki temiz koşuda `79,13-83,72 ms`.
- [x] Steady-state allocation owner'larını yüklenebilir PlayMode RAW capture ile çıkar; proje kodunu yaklaşık `2.394 B/frame` değerinden `11,6 B/frame` değerine indir (`~%99,5`).
- [x] Render sayacını ürün ortamında doğrula: Player `535` draw call; Frame Debugger `21` üst seviye event, `HybridBatch` iç draw komutlarını topluyor; active archetype `202` chunk ve `50` entity kapasitesi.
- [x] Exact snapshot'ı compact JSON'a çevir; Player'da `4.240.003 B`, `52,58 ms` save ve `86,19 ms` restore ölç.
- [x] Enemy-only Player P95 `6,97 ms` ile 60 FPS kapısını geçse de birleşik Package D stresi tamamlanana kadar release `MaxAliveZombies = 900` değerini koru.

### Mevcut oyun ile karşılaştırma

| Sözleşme | Mevcut oyun | Durum |
|---|---|---|
| 60 saniye kesintisiz cycle | Continuous system ve dört faz toplamı 60 | `[x]` |
| 30/5/20/5 | Active SubScene, authoring default ve setup tool aynı | `[x]` |
| Spawn hiçbir fazda sıfır değil | Dört faz runtime testte pozitif intensity üretiyor | `[x]` |
| Enemy stats sabit | HP, damage ve speed bütün cycle'larda base değerde | `[x]` |
| Quantity-only difficulty | Count/batch/interval büyüyor; stat growth utility seviyesinde yok sayılıyor | `[x]` |
| Tek enemy prefab | `EnemyCatalog.asset` yalnız `zombie_basic` tanımını taşır; prefab ve base statlar aynı kayıttan spawn edilir | `[x]` |
| 10k expandable pool | Standalone 10K P95 `6,97 ms`, death peak `55,57 ms`, save/restore `52,58 / 86,19 ms`; `535` draw call ve `202 x 50` chunk topology ölçüldü; normal cap 900 | `[x]` Enemy-only Player kapısı geçti; 1K archer birleşik stresi Package D'de açık |
| Backlog kaybolmaz | Explicit saved budget state cap altında talep biriktiriyor ve kapasitede kontrollü boşaltıyor | `[x]` |
| Special night yok | SpecialNight schema dormant; runtime multiplier/flag/warning üretemiyor | `[x]` |

### Yapılacaklar

- [x] Faz sürelerini `Day 30 / Dusk 5 / Night 20 / Dawn 5` yap.
- [x] Düşman akışının dört fazda da sıfıra düşmediğini test et.
- [x] Zombie HP growth'ü kaldır.
- [x] Zombie damage growth'ü kaldır.
- [x] Zombie speed growth'ü kaldır.
- [x] Spawn count/budget için day curve ve phase multiplier owner'ı oluştur.
- [x] Dawn yoğunluğunu düşürürken yeni gün tabanının önceki güne geri dönmemesini sağla.
- [x] Blood Moon/SpecialNights active bağlantısını kaldır; dormant kalabilir.
- [x] Aktif `MoatSystem` combat etkisini V1 core loop'tan ayır; kod/content dormant kalabilir.
- [x] `EnemyDefinition` ve tek kayıtlı enemy catalog oluştur.
- [x] Enemy type özel dalları spawn/UI koduna eklemeden content genişleme sınırı kur.
- [x] Explicit spawn backlog state/policy kur ve save/telemetry'ye aç.
- [x] Enemy pool'u küçük prewarm + ihtiyaçla genişleme şeklinde kur.
- [x] Ölümde entity destroy yerine pool return uygula.
- [x] Active cap data-driven kalır; teknik cap dolduğunda talebi explicit backlog'da koru.
- [x] Gerçek oyun UI/VFX/save açıkken 10.000 enemy ölçüm senaryosu kur.

### Kabul kapısı

- [x] Day 1 ve ileri günlerde enemy HP/damage/speed aynıdır.
- [x] Gün baskısı yalnız count/budget/flow ile artar.
- [x] Cap doluyken talep backlog'a gider ve boşlukta sahaya çıkar.
- [x] Tek prefab catalog üzerinden çalışır.
- [x] Death churn pool ile yönetilir.
- [x] 10K death spike ve steady-state allocation blockerları çözülür.

---

## 7. Package C - Economy + Population - Tamamlandı

### Mevcut oyun ile karşılaştırma

| Sözleşme | Mevcut oyun | Durum |
|---|---|---|
| Dört hazır worker binası | Wood/Stone/Iron/Food prebuilt world alanları ve her kaynak için bağımsız CAP/EFF yatırım state'i Workers drawer'a bağlı | `[x]` |
| Target ratio | Actual count yanında dört kalıcı basis-point hedef, cap/idle mirror ve runtime API var | `[x]` |
| Yeni pop otomatik dağılır | Bed + Food bütçesinden kabul edilen pozitif population farkı target deficit + cap kuralıyla deterministik dağılıyor | `[x]` |
| +1/+10/+100/direct input | Drawer target share için ücretsiz/anlık +1%, +10%, +100% ve exact 0-100 input sunuyor | `[x]` |
| Worker world representation | Actual count Low/Medium/High eğriyle resource başına en fazla 32 visual'a çevriliyor | `[x]` |
| Worker feedback | Actual count visual weight'lere exact dağılıyor; Idle/Walk/Work/Celebrate, resource cargo, Dusk/Night lantern ve hub delivery pulse aynı DOTS pass'inde çalışıyor | `[x]` |
| Beds incremental/no hard max | `60` base + purchased House bed state; toplam sahipliği baz alan owner-onaylı quadratic Wood eğrisi, ardışık bulk fiyatı, int-safe transaction ve exact save var | `[x]` |
| Dawn survivor + one-time Food | Dawn isteği `15`; boş yatak ve `Food / 1` ile sınırlanıyor, gerçek accepted count uygulanıp toast'ta gösteriliyor ve `accepted × 1 Food` aynı Dawn'da yalnız bir kez düşüyor. En fazla `15` temsili survivor mevcut villager prefabıyla sağdan Wall arkasına yürüyor; worker üretimine karışmadan varışta temizleniyor | `[x]` |
| Mevcut pop pasif Food tüketmez | V1 castle loop'ta population Food/dk yazmıyor | `[x]` |
| Fiyatlar adet/seviyeyle büyür | Bed owned-capacity eğrisi yanında bina CAP/EFF maliyetleri Wood+Iron olarak `ceil(base × growth^level)` ile bağımsız büyüyor; `DifficultyProfileSO` + Difficulty Tuner tek tuning yüzeyi, baked `MobileEconomyPriceTuning` runtime owner'ı | `[x]` |

### Yapılacaklar

- [x] `WorkerAllocation` state'e dört target ratio, actual counts, caps ve idle population ekle.
- [x] Target ratio toplamını normalize eden deterministik kural tanımla.
- [x] Yeni population'ı target ratio'lara otomatik dağıt.
- [x] Bina cap'i dolduğunda fazlalığı Idle Population'da bırak.
- [x] Ücretsiz/anlık `+1 / +10 / +100 / direct input` kontrollerini ekle.
- [x] Worker world representation'ı sayısal truth'tan ayır; düşük/orta/yüksek temsilî density kur.
- [x] Worker animasyon, fener, taşıma ve üretim feedback'ini allocation ile senkron tut.
- [x] Houses için satın alınabilir bed capacity state'i kur.
- [x] Bed maliyetini sahip olunan capacity ile büyüt; hard max koyma.
- [x] Dawn survivor budget'ını bed boşluğu + Food bütçesiyle hesapla.
- [x] Her kabul edilen kişi için Food'u yalnız bir kez azalt.
- [x] Survivor'ları sağdan yürüyerek kaleye gelen görsel akışla temsil et.
- [x] Food yetersizliğinde mevcut popu azaltma; yalnız yeni arrival'ı sınırla.
- [x] Açlık, göç, population death ve üretim cezası ekleme.
- [x] Bina capacity ve efficiency satın alımlarını büyüyen maliyet eğrisine bağla.
- [x] Fiyat eğrilerini Inspector/SO tuning yüzeyi yap; int güvenlik sınırlarını koy.
- [x] Fletcher gameplay binası/worker'ı ve build placement'ı V1 akışına bağlama.

### Kabul kapısı

- [x] Ana kaynaklarda pasif drain yok.
- [x] Food `0` iken mevcut population değişmiyor.
- [x] Dawn arrival yalnız Food+bed kadar kabul ediliyor.
- [x] Capacity hiçbir zaman aşılmıyor.
- [x] Target ratio ve worker state save/load ile aynı kalıyor.
- [x] Büyük allocation 1:1 worker entity üretmiyor.
- [x] Aynı visual bucket içindeki actual değişim representation weight'e exact yansıyor; cargo/teslimat feedback'i ve Dusk/Night lantern state'i çalışıyor.
- [x] Her bina ve CAP/EFF seviyesi bağımsız; her alım Wood+Iron'ı tek transaction'da harcıyor.
- [x] CAP seviyesi `+10` slot, EFF seviyesi base worker üretimine additive `+10%` veriyor.
- [x] Sekiz bina yatırım seviyesi exact Continue sonrasında maliyet, cap ve üretim aggregate'iyle aynı kalıyor.
- [x] Bed ve worker bina fiyatları `DefaultDifficulty.asset`/Difficulty Tuner'dan bake ediliyor; invalid değerler sanitize, int dışı transaction reddediliyor.

---

## 8. Package D - Archers + Ammo

### Mevcut oyun ile karşılaştırma

| Sözleşme | Mevcut oyun | Durum |
|---|---|---|
| Basic/Rapid/Frost | Üç tip ve SO tanımları mevcut | `[x]` Yapısal olarak var |
| Instant buy + population cost | Satın alım anlık; definition `PopulationCost=1`; 1001. deneme transaction başlamadan reddediliyor | `[x]` |
| Common 1000 cap | `ArcherCapacityUtility` tek owner; `SpawnArcher` bütün aktif yolların son kapısı, drawer cap'te `MAX` gösteriyor | `[x]` |
| Basic retrain | `GameManager` bir Basic entity'yi tek seferlik kaynak maliyetiyle yerinde Rapid/Frost'a dönüştürüyor | `[x]` |
| Type bazlı incremental maliyet | Buy ve retrain maliyeti hedef türün mevcut sayısıyla SO-tunable ortak eğride büyüyor | `[x]` |
| Archer death yok | Archer HP/death combat yolu yok; 1K × 10K runtime testi archer count'un değişmediğini doğruluyor | `[x]` |
| Upgrade yalnız Heart | Ayrı type level/upgrade ve direct unlock kontrolleri player-facing kapalı; drawer yalnız buy + retrain | `[x]` Heart effect pipeline ayrı açık iş |
| 40x25 placement | Version'lı 40 canonical tile, tile+slot seeded 25 diamond nokta, minimum mesafe, layer-fill sıra, 1000 gizmo ve stable Continue | `[x]` |
| Nearest valid target | Persistent coarse grid + Burst job range içindeki yaşayan en yakın hedefi seçiyor | `[x]` |
| Incoming damage reservation | Uçuşta ve aynı frame üretilen ok damage'i target HP'ye karşı rezerve ediliyor | `[x]` |
| Arrow -1/shot | Başarılı pool rent'inden sonra tam `1`; pool yoksa stok harcanmıyor | `[x]` |
| Wood ile instant refill | Sabit oranlı +1/+5/Buy Max paketleri; stok aynı transaction'da artıyor, combat sonraki simulation tick'inde sürüyor | `[x]` |
| Fletcher yok | Legacy kod aktif V1 akışına bağlı değil; refill queue/worker olmadan GameManager transaction'ı | `[x]` |

### Archer ve retrain işleri

- [x] Toplam Basic+Rapid+Frost cap'ini `1000` olarak tek owner'da uygula.
- [x] 1.001. satın alımı reddet; kaynak/population harcama.
- [x] Council, meta başlangıç bonusu ve restore spawn'ında aynı cap guard'ını kullan.
- [x] Basic -> Rapid ve Basic -> Frost retrain işlemini tek seferlik maliyetle uygula.
- [x] Retrain toplam archer ve population sayısını değiştirmesin.
- [x] Type maliyetini aynı türün mevcut sayısına göre büyüt.
- [x] Market'teki ayrı Basic/Rapid/Frost level ve upgrade akışını kaldır/disable et.
- [x] Hasar, fire rate, range ve Frost slow progression'i Heart effect pipeline'ına taşındı. Direct type-level cost/upgrade API'si ve type-level stat çarpanları retired; mevcut/yeni okçu rebase'i ile non-compounding exact Continue testli.
- [x] Archer death/individual HP yolunun eklenmesini 1K × 10K runtime archer-count regression'ıyla engelle.

### 40x25 formation işleri

- [x] Kullanılacak tam 40 `outside` tile'ı data/version ile sabitle.
- [x] Her tile için `tile coordinate + local slot index` seed'iyle 25 nokta üret.
- [x] Noktaları izometrik diamond içinde güvenli inset ile örnekle.
- [x] Minimum local mesafe uygula.
- [x] Fill order'ı layer mantığıyla kur: önce 40 tile slot 0, sonra 40 tile slot 1...
- [x] Save yalnız type count tutarken stable algorithm ile aynı formasyonu kur.
- [x] Editor gizmo preview'u bütün 1000 noktayı göstersin.
- [x] Formation algorithm version'ını save migration'a ekle.

### Targeting ve projectile işleri

- [x] Mevcut spatial hash utility'sini archer target query için uygun read-only query owner'ına dönüştür.
- [x] Her okçu range içindeki yaşayan/death-state olmayan en yakın düşmanı seçsin.
- [x] Basic/Rapid/Frost aynı target policy'yi kullansın.
- [x] Projectile target pool'a döner/yeniden rent edilirse generation mismatch ile deterministik cleanup uygula; retarget yapma.
- [x] Incoming damage reservation/load ile overkill'i dağıt.
- [x] Target search'ü Burst/job ölçeğinde 1k x 10k için ölç.
- [x] Projectile instantiate/destroy churn'ünü pooling ve Burst-safe lifetime yaklaşımıyla çöz.

### Ammo işleri

- [x] `UnlimitedArrows` V1 active config'ini kaldır.
- [x] Atılan gerçek projectile başına tam 1 Arrow düşür; başarısız pool rent'inde stok harcama.
- [x] Arrow `0` olduğunda okçuları durdur; refill'i aynı transaction'da yaz ve combat'ı sonraki simulation tick'inde sürdür.
- [x] Wood maliyetli sabit oranlı +1/+5/Buy Max refill paketleri ekle.
- [x] Refill'i production queue olmadan anlık uygula.
- [x] Current/Capacity, paket fiyatları ve `Buy Max` kontrolünü UI'da göster.
- [x] Arrow capacity ve efficiency upgrade'lerini run purchase owner'ına bağla; ikisi de Wood+Iron kullansın.
- [x] Refill başına birim fiyatın satın alma sayısıyla büyümesini engelle; ordu/fire rate doğal talebi yaratsın.
- [x] Rapid'in yüksek fire rate'inin daha fazla Arrow tükettiğini test et.
- [x] Legacy Fletcher/ArrowProduction V1 akışına sızmasın.

### Package D kabul kapısı

- [x] 1.001. archer alınamıyor.
- [x] Retrain toplam sayıyı değiştirmiyor.
- [x] 40 tile x 25 stable point save/load sonrası aynı.
- [x] 1.000 archer x 10.000 enemy gerçek oyun senaryosu çalışıyor.
- [x] Ammo truth ve refill davranışı korunuyor.

---

## 9. Package E - Castle Heart / Teknoloji Ağacı

### Mevcut teknoloji sistemi - repo gerçeği

| Alan | Mevcut uygulama | Blueprint farkı |
|---|---|---|
| Definition | `TechNodeDefinitionSO` + `TechTreeCatalogSO` | Katalog korunabilir, generator input'u olmalı |
| Graph | Catalog içindeki sabit `RevealChildNodeIds` | Run başında generated node+edge graph gerekli |
| Root/reveal | Root otomatik level 1; child listesi reveal | Temel reveal fikri korunabilir |
| Maliyet | Wood/Stone/Iron/Food `ResourceCost` | Yalnız Grave Essence kullanılmalı |
| Repeatable | `MaxLevel` ve linear cost growth var | +1/+10/Buy Max ve soft-cap contract eksik |
| Effects | Archer unlock/stats, worker, Wall, moat, Fireball etkileri var | Effect pipeline genişletilip tek progression owner yapılmalı |
| UI | Fullscreen graph, runtime layout, pan/zoom controller var | Kullanışlı temel; hidden graph/branch compass/Keystone sunumu eksik |
| Pause | Panel açıkken oyun özellikle durmuyor | Heart bütün simulation/cycle/spawn/worker/cooldown'u durdurmalı |
| Save | Schema v11 exact graph, catalog version, edge, hidden/reveal, level ve lock state'ini kaydedip Continue'da effect replay ediyor | `[x]` |
| Guarantees | Sabit catalog içeriğine bağlı | Her graph Rapid/Frost/Fireball reachable validation gerekli |
| Node türleri | Generic node/effect yapısı | Unlock/Repeatable/Evolution/Keystone semantiği eksik |
| Duplicate upgrades | Market archer level/upgrade butonları aktif | Heart tek teknoloji owner'ı olmalı |

### E1 - Data model ve catalog

- [x] `HeartNodeDefinition` contract oluştur: id, tags, effects, rarity, depth range, repeatable, base cost, cost growth, conflicts.
- [x] Node'ları `Unlock`, `Repeatable`, `Evolution`, `Keystone` türleriyle açıkça sınıflandır.
- [x] Mevcut `TechNodeDefinitionSO` içeriklerini yeni contract'a migrate etme planı çıkar.
- [x] `GeneratedRunGraph` contract oluştur: seed, graph version, node ids, edges, hidden/revealed, levels, locks.
- [x] Source asset'lerin runtime state taşımamasını garanti et.
- [x] Grave Essence run resource/state ve tek Heart spending owner'ını oluştur.
- [x] Grave Essence'ın ölümde silinmesini run save matrisiyle güvenceye al.

### E2 - Graph üretimi

- [x] Castle Heart merkez/root node'unu sabitle.
- [x] Ordu, Savunma, Üretim ve Heart/Büyü yön pusulasını sabitle.
- [x] Run seed ve dört yön iskeletini oluştur.
- [x] Her ana yönün root'a bağlı olduğunu doğrula.
- [x] Rapid, Frost ve Fireball guarantee node'larını izinli derinliğe yerleştir.
- [x] Temel Wall/defense erişimini koru.
- [x] Her ana yönde en az bir repeatable sink garanti et.
- [x] Node havuzunu tag + rarity + depth kurallarıyla doldur.
- [x] Duplicate node ve invalid prerequisite üretimini engelle.
- [x] Edge ve kontrollü cross-link üret.
- [x] Keystone çiftlerini yalnız birbirini kapatacak biçimde yerleştir.
- [x] Normal node'un yanlışlıkla başka yolu lock etmesini engelle.
- [x] Disconnected graph, dead core path ve unreachable guarantee durumunda validation reroll/fallback uygula.
- [x] Graph tamamen run başlangıcında üretilsin; reveal anında RNG kullanma.
- [x] Graph valid değilse run'ı sessizce broken state ile başlatma; açık hata/fallback üret.

### E3 - Reveal ve oyuncu bilgisi

- [x] Başlangıçta Heart ve bağlı ilk seçenekleri tamamen gösteren idempotent reveal state geçişini kur.
- [x] Uzak node'larda yalnız yön rengi/damarını göster; exact node'u hidden-safe presentation contract'ında gizle ve gerçek prefabda VEILED slot olarak çiz.
- [x] Gizli node içeriği run başında kesinleşiyor; exact graph güncel schema v17'ye clone edilip Continue'da reroll olmadan restore ediliyor.
- [x] İlk satın alımda, `0 -> N` bulk geçişi dahil, yalnız outgoing bağlı komşuları reveal et.
- [x] Reveal anında RNG yok; save-scum karşıtı exact graph restore PlayMode round-trip ile doğrulandı.
- [x] Görünür node effect bilgisini production baseline/sink resolver'ından gerçek numeric current/after/delta olarak prefabda göster.
- [x] Görünür Keystone karşı başlık + kapanacak safe slot contract'ını gerçek conflict marker olarak çiz.

### E4 - Node satın alma ve etkiler

- [x] Yeni Heart purchase service yalnız `IHeartGraveEssenceWallet` kullanıyor; aktif UI legacy kaynak satın alımına fallback yapmıyor.
- [x] Unlock node tek satın alma ile level 1 olur ve outgoing devam yolunu reveal eder.
- [x] Repeatable node için exact `+1 / +10 / Buy Max` quote ve commit ekle.
- [x] Evolution tek seferlik authored effect'i uygular; launch numeric/role effect pool'u kilitlidir ve Fireball behavior katmanı Scorched Earth/Burning Ground ile Echoing Detonation/Second Blast'i canlı gameplay'e bağlar.
- [x] Keystone seçimi yalnız exact ve simetrik eş Keystone'u kapatır.
- [x] Maliyette `long`, effect value/raw/actual hesabında `double` ve açık overflow fail'i kullan.
- [x] Fire rate, cooldown, Frost slow, archer range ve spell radius için authored soft-cap/diminishing return uygula.
- [x] Resolver gerçek current/after/delta'yı üretip aktif Heart prefabında gösteriyor.
- [x] Archer damage/fire rate/range/Frost slow target/policy contract'ı `GameManager.HeartRuntime` baseline/sink adapter'ına bağlı.
- [x] Wall HP/repair, worker capacity/production/population ve Arrow capacity/efficiency target'ları live owner'lara bağlı; Arrow Heart bonusları paid level'lardan ayrı.
- [x] Fireball unlock/damage/radius/cooldown contract'ı live ability state'e bağlı.
- [x] Burning Ground ve Second Blast behavior contract'ları production v2 katalogda authored node'lara, aggregate ECS hasarına, ayrık VFX yönüne ve exact Continue state'ine bağlıdır; Split Shot dormant uyumluluk flag'i olarak kalır ve launch içeriğinde author edilmez.

### E5 - Heart ekranı ve pause

- [x] HUD Castle Heart butonu full-screen hidden-safe graph yüzeyini açsın; catalog yoksa açık content-gate hatası versin.
- [x] Heart açıkken cycle timer dursun.
- [x] Heart açıkken spawn ve movement/combat dursun.
- [x] Heart açıkken worker production/allocation simulation dursun.
- [x] Heart açıkken ability cooldown'ları dursun.
- [x] Mouse drag pan ve wheel zoom yalnız Heart ekranında çalışsın.
- [x] UI interaction, tooltip, buy/reveal ve focus davranışları unscaled UI zamanında çalışsın.
- [x] Graph kapanınca önceki time scale ve SimulationSystemGroup state'i deterministik devam etsin; nested pause owner'ları erken resume etmesin.
- [x] Market/Barracks archer upgrade ve direct unlock yüzeylerini kaldır/disable et.

### E6 - Save, migration ve test

- [x] Seed, graph version, catalog version, node ids, edge'ler, hidden/reveal, levels ve locks run save'e yazılıyor.
- [x] Continue source asset'ten yeniden zar atmıyor; kaydedilmiş graph'ı exact clone edip effect pipeline'ı replay ediyor.
- [x] Aynı seed + aynı catalog version byte-equivalent graph üretiyor.
- [x] Catalog version değişiminde eski run graph'ı sessizce başka graph'a map edilmiyor; preflight açık hata veriyor.
- [x] Rapid/Frost/Fireball unreachable graph testi generator validation suite'inde.
- [x] Normal node accidental lock testi restore validator ve purchase suite'inde.
- [x] Keystone pair exclusion testi generator/purchase/restore validation suite'inde.
- [x] Hidden graph save/load testi JSON round-trip, deep clone ve gerçek Continue PlayMode ile doğrulandı.
- [x] Heart full-pause testi nested time scale + SimulationSystemGroup state'iyle doğrulandı.

### Package E kabul kapısı

- [x] Aynı seed/load aynı graph.
- [x] Rapid, Frost ve Fireball her run'da reachable.
- [x] Hidden graph save-scum ile değişmiyor.
- [x] Heart yalnız Grave Essence kullanıyor.
- [x] Heart açıkken bütün simulation ve cooldown duruyor.
- [x] Ayrı archer upgrade owner'ı kalmıyor.

---

## 10. Package F - Council

### Mevcut oyun ile karşılaştırma

| Sözleşme | Mevcut oyun | Durum |
|---|---|---|
| Council aktif core sistem | Composer, catalog, atoms ve UI mevcut | `[x]` Altyapı var |
| Curated/no runtime AI | 9 authored template en az iki body varyantı, staged MinDay, explicit recipe, source-retirement/follow-up ve 5.400-sample budget/token gate'i taşıyor | `[x]` Launch content approved |
| 3/6/9 regular schedule | `CouncilRegularSchedule` Day 3'ten başlayarak her 3 günde bir; aynı gün tek açılış | `[x]` |
| Exact effects visible | İki seçenek aynı live quote owner'ından tam sayısal sonuç/maliyet, uygulanabilirlik ve Dawn+Day karar süresini gösteriyor | `[x]` |
| Context-aware selection | En kit/en bol kaynak stock/production dakikasından seçiliyor; template director iki option atomundan Wall bağlamını okuyor; recent template alternatif varken hard-exclude ediliyor | `[x]` |
| Role/content isolation | `CouncilContentPolicy` katalog, composed event, live karar ve Continue preflight'ta yalnız Council-owned run effect/recipe allowlist'ini kabul ediyor; Heart/Meta domain'leri yok | `[x]` |
| Population guard | Council gain boş yatak + Food bütçesiyle exact preflight edilir; stale/private payload clamp olur, accepted kişi başına Food bir kez harcanır ve capacity büyümez | `[x]` |
| Archer guard | Free archer exact idle population + ortak 1000 cap preflight'ından geçer; her spawn bir idle kişiyi Archer havuzuna taşır | `[x]` |
| Wall-only defense | Heal yalnız `WallSegment` current/max HP'ye gider; legacy Gate/Core değişmez | `[x]` |
| Count-only night effect | Council delta yalnız bounded next-night spawn multiplier yazar; zombie HP/damage/speed değişmez | `[x]` |
| Exact save | Flags/recent/one-shot/run salt, regular handled day, active payload, çözülmüş seçim flag'i ve temp-production/next-night multiplier+expiry state'i schema v11 capture/restore ile PlayMode testli | `[x]` |

### Yapılacaklar

- [x] Regular Council'ı Day `3,6,9,12...` Dawn başlangıcında kesin tetikle.
- [x] Chance/pity/cooldown'u regular schedule owner'ı olmaktan çıkar.
- [x] Her kartta tam sayısal iki seçenek ve karar süresi göster.
- [x] Resource scarcity, production, Wall ve previous flags bağlamını koru.
- [x] Aynı şablonun anlamsız tekrarını recent memory ile engelle.
- [x] Yalnız editoryal onaylı flag chain'leri aç.
- [x] Population gain'i available beds + one-time Food ile sınırla.
- [x] Free archer gain'i idle population + common 1000 cap ile sınırla.
- [x] Defense effect'lerini yalnız Wall current/max HP ile sınırla.
- [x] Horde effect'lerini yalnız count/flow multiplier ile sınırla.
- [x] Active kartı reroll etmeden; çözülmüş seçim flag/memory state'i ile temp-production/next-night duration state'ini exact Continue'da koru.
- [x] Council'ın Heart currency/upgrade rolünü veya Meta rolünü devralmasını engelle.
- [x] Launch template/atom listesi için owner review ve effect budget testi yap.

### Kabul kapısı

- [x] Regular günler daima 3/6/9 düzeninde.
- [x] Hiçbir effect bed+Food, population, 1000 archer, Wall-only veya count-only guard'ını aşmıyor.
- [x] Save/Continue aynı Council state'ini koruyor.

---

## 11. Package G - Active Abilities

### Mevcut oyun ile karşılaştırma

| Ability | Mevcut oyun | Blueprint hedefi |
|---|---|---|
| Fireball | Alt orta bar `[1]`; world target, AoE projectile, Heart damage/radius/cooldown ve exact save aktif | `[x]` Blueprint hedefiyle uyumlu |
| Rally | Alt orta bar `[2]`; cost-free global fire-rate buff ve cooldown aktif | `[x]` Blueprint hedefiyle uyumlu |
| Emergency Repair | Alt orta bar `[3]`; cost-free, Night-only Wall yüzde heal ve uzun cooldown aktif | `[x]` Blueprint hedefiyle uyumlu |
| Fortify | Resource-cost legacy prep etkisi ayrı; üçlü ability barında yok | `[x]` Aktif ability rolünden ayrıldı |
| Arrow Storm | Aktif bulunmadı | `[x]` V1'e eklenmedi |

### Yapılacaklar

- [x] Alt orta tek ability barı oluştur: Fireball, Rally, Emergency Repair.
- [x] Fireball input'unu `1 + world area selection` yap.
- [x] Rally input'unu `2` yap ve bütün okçulara kısa fire-rate boost uygula.
- [x] Emergency Repair input'unu `3` yap ve yalnız Night sırasında Wall Max HP yüzdesi heal et.
- [x] Üç ability'den Wood/Stone/Iron/Food/mana maliyetini kaldır.
- [x] Ability kullanımını yalnız unlock + cooldown + phase/input guard ile sınırla.
- [x] Fireball targeting sırasında UI click'lerini cast sayma.
- [x] Fireball damage/radius/cooldown'u Heart node'larına bağla.
- [x] Çıkış ability/spell içeriğini Fireball ile sınırla; yeni büyüleri yalnız ileride meta pool unlock yolu için data-driven bırak.
- [x] Rally ve Emergency Repair cooldown state'ini exact save et.
- [x] Night normal repair'i kapat; Stone harcanmamasını garanti et.
- [x] Night başlangıcında açık repair drawer/input davranışını deterministik kapat.
- [x] Normal repair paket formülünü tuning verisi yap: fixed HP, percent HP veya approved hybrid.
- [x] Eksik HP başına Stone maliyeti ve day price multiplier tuning alanlarını tanımla.
- [x] Emergency Repair yüzdesi ve cooldown tabanını tuning alanı yap.
- [x] Wall `0 HP` ile aynı frame Emergency Repair gelirse Game Over kazansın.
- [x] Fortify/Rally legacy prep purchase yollarının V1 ability sistemiyle çakışmasını kaldır.
- [x] Arrow Storm ekleme.

### Kabul kapısı

- [x] Ability'ler ana kaynak tüketmiyor.
- [x] Night normal repair harcama yapmıyor.
- [x] Emergency Repair ölümü geri çevirmiyor.
- [x] Input ile UI aynı state'i gösteriyor.
- [x] Cooldown save/load exact.

**Tamamlanma kanıtı (2026-07-15):** `ActiveAbilityRulesTests`,
`MobileCastleTuningResolverTests` ve `RunPersistenceTests` hedefli EditMode koşusunda
`22/22`; normal repair phase/Stone sözleşmesi ile exact cooldown/Wall Continue hedefli
PlayMode koşularında `2/2` geçti. MCP scene/prefab denetiminde tek scene
`SpellCastUI`, eksiksiz üçlü binding, aktif `AbilityBarPanel` ve sıfır legacy
`SpellUiRoot` doğrulandı.

---

## 12. Package H - Meta + Persistence

### Mevcut oyun ile karşılaştırma

| Sözleşme | Mevcut oyun | Durum |
|---|---|---|
| Meta ayrı save | `MetaProgression` canonical v3 JSON kullanıyor | `[x]` Currency/upgrade/pool/tutorial/receipt sahipleri ayrık |
| Death-only shop | UI ve transaction `GameManager` durable-death kapısından geçiyor; aktif run/Pause/Main Menu reset bypass'ı yok | `[x]` |
| Reward inputs | Production catalog-owned diminishing kill bands + day/night/peak-pop/record weights; exact quote death receipt v2'ye sabitlenir | `[x]` |
| Idempotent reward | Durable death receipt, `RewardedRunIds`, atomic write ve fail-closed Continue guard aktif | `[x]` |
| Fixed upgrade list | Exact 11-definition katalog Blueprint sırasını taşır; legacy Archer Damage kaldırıldı | `[x]` |
| StartingTechLevel yok | Enum, `TechNodeId`, runtime case ve dormant `Meta_start_moat.asset` kaldırıldı | `[x]` |
| Meta graph'ı değiştirmez | Fail-closed effect allowlist yalnız run-start/aggregate etkilerini kabul ediyor; pool Id storage mevcut graph'a yazmıyor | `[x]` |
| Tutorial flag | v3 `TutorialFlags` canonical state/API ile `tutorial.v1.worker_ratio` ve `tutorial.v1.basic_archer` consumer'ları aktif; kalan onboarding adımları Package I işi | `[~]` İlk iki consumer aktif |

### Hedef meta upgrade listesi

- [x] Starting resources - Wood/Stone/Iron/Food; node açmaz.
- [x] Starting Basic Archers - Rapid/Frost açmaz.
- [x] Starting beds - run bed price curve'ünü silmez.
- [x] Base Wall HP - Heart Wall node'larını değersizleştirmez.
- [x] Worker production - küçük global multiplier; run capacity/efficiency devam eder.
- [x] Arrow efficiency - ammo kararını yok etmez.
- [x] Essence gain - graph sonucunu değiştirmez.
- [x] Node pool unlock - stable future-pool Id'sini tek sefer açar; mevcut run'a zorla enjekte etmez. Gerçek node/evolution content'i ayrı owner onayıdır.

### Persistence işleri

- [x] Run save ve meta save alanlarının açık schema sahiplerini ayır.
- [x] Game Over'da run save içindeki tüm run state'i sil.
- [x] Meta currency, upgrade levels, pool unlocks ve tutorial flag'lerini koru.
- [x] Death reward'a unique run identity/receipt ekle.
- [x] Process restart sonrası aynı ölümün ikinci reward yazmasını engelle.
- [x] Force-close ile Game Over öncesi snapshot'a dönmeyi engelle.
- [x] Aktif run sırasında meta satın alımını engelle.
- [x] `StartingTechLevel` effect ve content'ini yeni modelden kaldır.
- [x] Meta'nın generated graph edges/Keystone/result seçmesini engelle.
- [x] Repeatable meta sink'lerde büyüyen maliyet; content unlock'ta tek sefer uygula.
- [x] Eski Mobile save'i yeni run contract'a sessiz yanlış map etme.
- [x] 10k enemy pozisyonlarını tek tek save etmek yerine perceptually faithful deterministik rebuild policy tanımla.
  - Kanıt: `RunSaveState v14`, `CombatRebuildUtility` policy v1, v13 legacy fallback ve üç temiz 10K deterministic fingerprint kabulü. `372-375` bucket; aktif Arrow'a bağlı `165.957-227.597 B`; save `31,42-32,93 ms`; restore `75,09-213,32 ms`.

### Kabul kapısı

- [x] Meta ödülü yalnız ölümde ve bir kez yazılıyor.
- [x] Gönüllü reset/prestige yok.
- [x] Force-close ölümü geri alamıyor.
- [x] Meta mevcut run graph'ını geriye dönük değiştirmiyor.
- [x] Migration guard eski save'i yanlış state'e çevirmiyor.

---

## 13. Package I - HUD, Onboarding ve Creative Polish

### I1 - HUD mevcut oyun karşılaştırması

| Blueprint hedefi | Mevcut canlı HUD | Durum |
|---|---|---|
| Tek minimal Wall bar | Aktif prefab, sahne binding'i ve HUDController yalnız Wall sunumu taşıyor | `[x]` |
| Minimal phase area | Owner-secili `B - Celestial Dial`: top-center gerçek pill `290 x 68`, yalnız `DAY N`, 44 segmentli `178 x 44` göksel yay, crescent/dawn glyph'leri ve hareketli phase-color marker/halo; A alternatifi karar dokümanında arşivli | `[x]` |
| Forecast yok | Aktif prefab, canlı HUD, `HUDController` ve setup binding sözleşmesi forecast/pressure yüzeyi taşımıyor; gameplay `HordePressure01` sinyali korunuyor | `[x]` |
| Abilities alt orta | Tek `496 x 90` bottom-center panelde Fireball/Rally/Emergency Repair; üç vertical cooldown overlay, tek `SpellCastUI`, legacy panel yok ve prefab guard testi aktif | `[x]` |
| Workers/Housing alt sol | Tek bottom-left `Workers + Housing` toggle/drawer yüzeyi; worker ratio ve bina CAP/EFF yanında limitsiz `+1/+10/+100 Beds` Wood alımı, canlı population/bed/free/purchased aynaları ve prefab guard testi var | `[x]` |
| Archers/Heart alt sağ | Sabit `ARCHERS` + `CASTLE HEART` bottom-right dock; drawer `540 x 350` olarak dock üstünde açılır, HUD başlangıcında kapalıdır ve Heart modal pause davranışını korur | `[x]` |
| Tek drawer | Scene-owned `ManagementDrawerCoordinatorUI`, Workers/Housing, Archers ve Arrow Supply yüzeylerinde mutual exclusion kuruyor; Castle Heart bağımsız modal kalıyor | `[x]` |
| Council geçici kart | Tek compact regular Council kartı; iki seçenek `CouncilOptionPresentationUtility` üzerinden live exact effect/bedel quote'u gösteriyor, karar penceresi Dawn + Day cycle süresine bağlı sayısal sayaç ve gerçekten azalan Filled bar taşıyor | `[x]` |
| Fixed camera/ratio | Kamera sabit; responsive HUD visual root, top-anchored defense panel ve 1920x1080 / 3440x1440 critical rect + battlefield framing guard'ları aktif | `[x]` |

### HUD işleri

- [x] Gate/Core serialized binding ve görsel kalıntılarını active prefabdan temizle veya açık dormant guard koy.
- [x] Üst kaynak HUD'ını kompakt tut.
- [x] Üst ortada minimal phase alanı ayır.
- [x] Büyük CyclePanel ve ham DAY/DUSK/NIGHT sunumunu owner-approved mockup ile değiştir.
- [x] Horde forecast/pressure panelini kaldır.
- [x] Fireball/Rally/Emergency Repair'ı alt orta tek cooldown barına taşı.
- [x] Workers/Housing alt sol yerleşimini kur.
- [x] Archers/Castle Heart alt sağ yerleşimini kur.
- [x] Aynı anda yalnız bir management drawer açık olacak owner kur.
- [x] Council kartında iki exact effect ve karar süresini göster.
- [x] 16:9 ve ultrawide'da battlefield ve kritik UI crop testleri yap.

### I2 - İlk koşu onboarding

- [x] İlk Day: worker ratio düğmesini pulse + tek satır metinle öğret.
- [x] İlk kaynak yeterliliği: Basic Archer drawer highlight göster.
- [x] İlk düşük ammo: ammo satırını highlight et; zorunlu popup açma.
- [x] İlk kill/Essence: Heart butonunu pulse et; açılınca full pause öğret.
- [x] İlk regular Council/Day 3: bedel ve iki exact sonucu öğret.
- [x] İlk Wall hasarı sonrası Day: normal repair action'ı highlight et.
- [x] İlk Night: unlock olan ability üzerinde key hint göster.
- [x] Tutorial oyuncu adına kaynak harcamasın veya worker dağıtmasın.
- [x] Sürekli modal pause zinciri kurma.
- [x] İşlem prompt'tan önce yapılırsa adımı tamamlanmış say.
- [x] Tutorial complete flag'i meta save'e yaz.
- [x] Settings içinde tutorial reset ekle.
- [x] Player-facing bütün tutorial metnini English yap.
- [x] İkinci run'da tutorial'ın otomatik açılmadığını test et.

### I3 - Day/night görsel ve işitsel yön

- [x] Day: sıcak ışık, okunur üretim, worker ambience.
- [x] Dusk: amber -> indigo geçiş, fenerlerin yanması, tension riser.
- [x] Night: soğuk ay, güçlü silhouette, pencere/ok salvosu, rate-limited yoğun mix.
- [x] Dawn: cyan/altın kırılma, kapı/survivor gelişi, nefes/yeni gün cue.
- [x] Faz geçişini büyük tam ekran yazı yerine grading, sky, particles ve audio ile okut.
- [x] 10k horde için ground contrast, silhouette edge ve motion cadence koru.
  - Kanıt: Vampire materyalinde ek pass/draw/entity üretmeyen muted-cold edge + küçük contact patch; pool generation tabanlı `15/15` frame ve `16/16` timer-band dağılımı; 1920x1080 Night QA; iki final 10K enemy + 1K archer benchmark koşusu `9,21-9,80 ms` ortalama, `10,35-10,71 ms` P95 ve `546` ortalama draw call ile geçti.
- [x] Presentation review için save-safe anlık combat unlock ve exact 2K/5K/10K test yüzeyi sağla.
  - Kanıt: Yalnız Editor/Development derlemesindeki sağ-üst test paneli Fireball/Rapid/Frost'u açıyor, üç ability cooldown'unu sıfırlıyor, recruitment'ı transient `FREE` yapıyor ve mevcut enemy catalog/pool ile exact `2.000 / 5.000 / 10.000` aktif zombi kuruyor. Test oturumunda run save fail-closed; Stress Mode kapalı olduğu için Day/Night grading ve combat VFX/SFX görünür kalıyor. Vampire contact patch materyal-owned `(0.50, 0.30, 0.075, 0.025)` atlas ayak bandına taşındı; entity transform/physics/movement değişmedi. EditMode `12/12`, exact preset PlayMode `1/1`, canlı `2K/10K` Game View QA, scene validation `0`, final Console `0 error`.
- [x] Hit VFX/SFX'i her düşmanda üretme; budget/rate limit uygula.
  - Kanıt: `ArrowHitSystem` aynı `0,75` world-unit hücredeki aynı hit türünü sabit
    `512` candidate map'inde tek örneğe indiriyor; her frame en fazla `24` hit VFX
    ve tür başına tek multiplicity taşıyan SFX event'i üretiyor. `CombatFeedbackBridge`
    ikinci katmanda `128` flipbook pool, `24/frame` playback budget ve `0,04s`
    rate-limit uyguluyor. EditMode `7/7`; yoğun `1000` gerçek ECS hit + bridge
    PlayMode `2/2`; ArrowPool regresyonu `1/1`; Night salvo regresyonu `1/1` geçti.
    Runtime QA `80 requested / 24 played / 56 dropped / 0 remaining event`, scene
    validation `0` ve final Console `0 error` ile doğrulandı.
- [x] Fireball ve Frost feedback'ini horde içinde kaybolmayacak hierarchy ile sun.
  - Kanıt: ordinary hit `Wall/12`, Frost pooled ring/hit `Wall/47-48`, Fireball
    projectile `Wall/219-220` ve blast/core/ring `Wall/230-232` contract'ına alındı.
    Fireball bütün sunum katmanları opaque enemy depth buffer'ının önündeki
    `MobileCastleRenderDepth.ProjectileZ` bandına normalize edildi; gameplay damage,
    radius, cooldown ve Frost slow/budget değerleri değişmedi. EditMode `3/3`, gerçek
    `10.000` pooled enemy hierarchy PlayMode `1/1`, dense hit + bridge budget + mevcut
    Fireball/Continue 10K regresyonları `3/3` geçti. `DW_I_SPELL_HIERARCHY_10K.png`
    görsel QA'sında cyan Frost ve sıcak Fireball ayrı okunur; scene validation `0` ve
    final Console `0 error`.
- [x] Archer salvolarını tek tek projectile görsel kaosu yerine okunur toplu ritme çevir.
  - Kanıt: `ArcherShootSystem` gerçek damage/target/ammo/pool projectile'larını aynen
    korurken `ArcherSalvoPresentationUtility` canlı okçu sayısından
    `ceil(count / 48)` stride üretir; 48 ve altındaki birliklerde bütün oklar görünür,
    1.000 okçuda tek salvo en fazla 48 temsilci okla sunulur. Global pool rent sequence
    ardışık salvolarda temsilci şeritlerini kaydırır; gizli oklar `ArrowTag` ile aktif
    kalıp hareket, hit, Frost, feedback, save ve return akışını sürdürür. Continue yoğun
    active-arrow snapshot'ını saved count + ordinal ile aynı bounded sözleşmeye alır.
    EditMode `3/3`; gerçek `10.000` enemy + `1.000` archer PlayMode `1/1`, ilk salvo
    `1.000 gameplay / 48 visual / stride 21`; targeting/ammo/pool/generation regresyonu
    `5/5`; `1920x1080` görsel QA; frame average `9,77 ms`, P95 `12,74 ms`, average
    draw call `544`; final Console `0 error`.
- [x] Blood Moon görsel/audio/warning active bağlantılarını kaldır.
  - Kanıt: `BloodMoonWarningUI` ve scene root'u silindi; HUD label/color, overlay tint,
    vignette flash, özel loop/sting ve setup yeniden-oluşturma dalları kaldırıldı. Dormant
    save/config alanları geriye uyumluluk için korundu. Live reflection `0` presentation
    field/type, scene validation `0` issue; EditMode `8/8`, PlayMode `5/5`, final normal
    Night Game View QA ve Console `0` gerçek error.

### Package I kabul kapısı

- [x] İlk-run tutorial tamamlanıyor; ikinci run'da otomatik açılmıyor.
  - Kanıt: Yedi zorunlu accepted-player-action step'i olmadan global
    `tutorial.v1.complete` yazılmıyor; son adım flag'i meta save'e durable kaydediyor ve legacy
    yedi-step save global flag'i backfill ediyor. Gerçek lethal first run + durable death
    transaction + `UIManager.OnRestart()` ikinci run zincirinde run Id yenileniyor, Day 1
    temiz başlıyor, sekiz tutorial flag'i meta reload sonrasında korunuyor ve bütün hint/pulse/
    cue yüzeyleri `120` frame kapalı kalıyor. EditMode `25/25`, PlayMode `13/13`; canlı sahnede
    tek controller, `0` eksik binding, scene validation `0` issue ve final Console `0` error.
- [x] Tek Wall bar ve minimal phase UI owner onayından geçiyor.
- [x] 16:9/ultrawide temiz render.
- [x] 10k horde okunabilir.
- [x] Day/night lighting ve combat feedback owner görsel review'dan geçiyor; kapsamlı audio asset polish'i owner kararıyla bu V1 kabul kapısının dışında.
  - Kanıt: Owner, Day/Night geçişlerini ve yoğun sürü sunumunu açıkça kabul etti. Exact 2K owner QA, yalnız blocker açısı/progress kurallarının yeterli olmadığını gösterdi: Fireball boşluğunun çevresindeki `Queued` zombiler duvara kadar uzanan geçerli fakat eğri bir blocker zinciri kuruyor, state `Queued` olduğu için bütün zincirin hareket kuvveti sıfırlanıyordu. `ApplyMovementForceSystem`, saldırı izni vermeden Moving ve Queued state'lerinde ileri crowd pressure'i koruyor; yalnız Attacking/Dead sabit kalıyor. Aynı exact 2K ölçümünde `r=2,2` Fireball sonrası 4,5 saniyedeki boşluk içi canlı sayı `53 -> 240`, Queued ortalama hız `0,003 -> 0,354` oldu. Sabit Day 10K koşusunda `785` ölüm sonrası aynı alana `753` canlı geri doldu, Queued ortalama hız `0,207` kaldı, Wall penetration `0` ve görünür çember yoktu. Utility EditMode `11/11`; kısa kuyruk PlayMode `2/2`; yeni exact 2K uçtan uca PlayMode `1/1`; final Console `0 error` ve Game View QA temiz.

---

## 14. Teknik Sahiplik ve Yeni Data Contract'ları

### Mevcut owner -> hedef sorumluluk

| Alan | Mevcut owner | Hedef sorumluluk |
|---|---|---|
| Cycle/spawn | `ContinuousSiegeCycleSystem`, `WaveSpawnSystem` | 60 sn fixed phases, quantity-only, backlog |
| Enemy data | `EnemyDefinitionSO` + tek kayıtlı `EnemyCatalogSO`; pool metadata bake ediliyor | Expandable runtime pool |
| Workers | `MobilePopulationEconomySystem`, GameManager worker visuals | Target ratios + caps + representative density |
| Archers | `GameManager`, `ArcherShootSystem` | Common 1000 cap + scalable target load |
| Placement | `MobileCastleArcherTilePlacement` | 40x25 stable local points + version |
| Heart | `GameManager.HeartRuntime` + `HeartScreenUI` generated graph/reveal/purchase/effect/pause ve exact v11 Continue replay owner'ı; legacy `TechTreeUI` aktif scene'den kaldırıldı; production v1 launch catalog/effect spec kilitli | Yalnız launch spec'te kayıtlı node/effect/cost içeriği; yeni behavior evolution ayrı versioned review ister |
| Council | `CouncilRegularSchedule`, `CouncilComposer`, `CouncilContentPolicy`, `CouncilOptionPresentationUtility`, `CouncilEventUI`, catalog | Exact 3/6/9, guarded effects, exact option/timer, curated context/memory ve Heart/Meta role boundary hazır; regular-only scope |
| Meta | `MetaProgression` | Death-only fixed list + idempotent receipt |
| HUD | `MobileCastleHudRoot`, `HUDController` | Single Wall + minimal cycle + bottom abilities |

### Oluşturulacak/uyarlanacak contract'lar

- [x] `EnemyDefinition`: id, prefab, base stats, pool prewarm/expand, spawn weight.
- [x] `RunDifficultyProfile`: `DifficultyProfileSO` BaseSpawn curve/phase/active-cap içerik owner'ı; `ContinuousSpawnBudgetUtility` sabit PreserveDemand backlog policy owner'ı; runtime telemetry ve exact Continue state'i `ContinuousSpawnBudgetData` üzerinde.
- [x] `HeartNodeDefinition`: tags, effects, rarity, depth, repeatable, cost growth, conflicts.
- [x] `GeneratedRunGraph`: seed/version, node ids, edges, hidden/revealed, levels, locks.
- [x] `HeartGraphPresentation`: safe slots, hidden redaction, resolved effect rows, Keystone conflict marker data.
- [x] `HeartPurchaseService` + `HeartEffectPipeline`: Grave Essence quote/commit, bulk fiyat, actual effect resolver ve Keystone exclusion.
- [x] `WorkerAllocation`: `MobilePopulationAllocation` dört target ratio/actual count/effective cap/idle ECS owner'ı; `WorkerAllocationUtility` normalize, yeni-arrival dağıtımı ve canonical idle matematik owner'ı; system/GameManager aynı sonucu tüketiyor.
- [x] `ArcherFormation`: `ArcherFormationV1.asset` sıralı 40 canonical hücrenin; `ArcherFormationUtility` version `1`, hücre başına 25 deterministic local nokta ve türetilmiş 1000 kapasitenin; `MobileCastleArcherTilePlacement` ise aktif `outside` tilemap world-space cache'inin tek owner'ı.
- [x] `ActiveAbilityState`: `GameManager` Fireball/Rally/Emergency unlock ve üç cooldown remaining state'inin; tech level + exact Heart graph Fireball modifier girdilerinin; `DifficultyProfileSO -> MobileCastleCombatConfig` Rally/Emergency tuning'inin; `CastleYardPrepState` aktif Rally effect state'inin mevcut tek owner zinciri. Türetilmiş unlock/modifier ikinci state olarak saklanmıyor, exact Continue'da girdilerden yeniden kuruluyor.
- [x] `CouncilRunState`: `CouncilRegularSchedule + GameManager` regular handled day, flag/chain memory, recent/one-shot list, non-zero run salt ve unresolved active kartın; `MobileEconomyEventState` çözülmüş production/next-night multiplier + expiry state'inin mevcut tek owner zinciri. V1 regular-only; Emergency state dalı yok. İlk regular meeting beklenmeden salt exact save'e commit ediliyor.
- [x] `MetaState`: currency, upgrade levels, pool unlocks, tutorial flags, death receipts.

### Teknik sınırlar

- [x] Mevcut owner'lara paralel ikinci runtime sistem kurma; source owner'ı dönüştür. Aktif `NewGameScene` owner matrisi ve temel ECS singleton state'leri `V1_RUNTIME_OWNER_BOUNDARY_ARCHITECTURE.md` ile tanımlı; PlayMode guard 12 scene owner + 14 ECS domain'ini tekil kilitliyor.
- [x] `MobileCastle*` isimlerini yalnız estetik için toplu rename etme. Önek mobil hedef değil, 158 kaynak/scene/prefab/asset dosyasına yayılan tarihsel serialized teknik sözleşmedir; işlevsel rename yalnız owner onayı, tam referans envanteri, compatibility migration planı ve regresyon doğrulamasıyla yapılabilir. Yeni tipler domain adı kullanabilir; player-facing metin teknik öneki göstermez.
- [x] Definition asset ile runtime state'i birbirine karıştırma.
- [x] Dormant legacy code'un active V1 owner'ına bağlanmasını açık review olmadan yapma. Dormant gameplay ile onaylı migration/compatibility yüzeyleri ayrıldı; dormant Gate/Core, Barracks trainer, Moat ve special-night state'i aktif V1 world'ünde fail-closed guard ile sıfıra/neutral değere kilitli. Yeniden wiring, refactor değil owner onaylı scope değişikliği kabul edilir.

---

## 15. Performans, Tuning ve Telemetry

### Performans sözleşmesi

| Alan | Mevcut durum | Gerekli iş |
|---|---|---|
| Enemy spawn/death | Prewarm/expand + rent/return; backlog bağımsız | 10K frame pacing ve allocation ölçümü |
| Archer target search | Persistent coarse grid + deterministic nearest unsaturated target | `[x]` Spatial query + target load |
| Projectiles | Enableable `ArrowTag`, 1024 prewarm + 256 batch expand, rent/return ve 5s Burst lifetime | `[x]` |
| VFX/SFX | CombatFeedbackBridge pool ve bazı min interval'lar var | 10k budget/aggregation audit |
| Worker visuals | Low/Medium/High representative density; resource başına 32 visual cap | `[x]` |
| Save | v17 exact-critical + deterministic combat rebuild; inactive pool catalog'dan yeniden kurulur | `[x]` 10K aggregate Continue ölçümü |

### Ölçüm senaryoları

- [~] 1.000 archer + 10.000 enemy + aktif projectile gerçek sahnede geçti; enemy-only 10K explicit Night görsel kabulü de tamamlandı. Aynı Night karesinde 1K archer + aktif projectile birleşik görsel kabulü bekliyor.
- [x] Fireball 10K horde içinde aynı-frame lethal damage ve toplu pool return correctness geçti; optimize peak iki temiz koşuda `79,13-83,72 ms`.
- [x] Arrow refill sonrası 1.000 archer yeniden ateş başlangıcı. Stok `0` iken 1.000 hazır Basic Archer rent üretmiyor; gerçek `TryBuyArrowRefill(10)` transaction'ı `1.000 Arrow / 250 Wood` yazıyor ve takip eden tek simulation tick'i tam `1.000` pooled projectile rent edip stoku `0` yapıyor. İki temiz Editor örneğinde pool expansion `0`, refill `0,010-0,600 ms`, restart main thread `22,210-23,158 ms`, wall-frame `24,327-50,327 ms`.
- [x] 10K enemy + canonical 1K Basic Archer v14 Continue doğrulandı. Önceki üç temiz enemy rebuild koşusuna ek olarak üretim restore/spawn owner'ıyla kurulan 1K okçu snapshot'a `Basic/Rapid/Frost = 1000/0/0` ve formation version `1` olarak girdi. Canonical koşuda `378` bucket, `369.097 B`, save `47,05 ms`; bütün okçular her restore öncesi silindikten sonra iki Continue `417,11/436,89 ms` içinde 10K enemy + 1K okçuyu yeniden kurdu. Enemy position ve archer type/formation fingerprint'leri iki turda da deterministik kaldı; backlog `777` korundu.
- [x] Düşük/orta/yüksek worker visual density geçişi; actual `12/60/1000/5000/0`, visual `12/24/32/32/0`.
- [x] Projectile pool sonrası birleşik 10K enemy + canonical 1K Basic Archer yükü isolated StandaloneWindows64 Development Player'da ölçüldü. Targeted Player test `1/1`; 120-frame capture `73.765.057 B` raw üretti ve analyzer `121` frame okudu. Frame average/P95/max `10,593/18,694/33,234 ms`; root GC average/max `10.437/12.020 B`, proje kodu GC `58.290 B toplam / 481 B/frame`. Named ECS system GC `0 B`, sync-point yok. `DamageApplySystem` `1,892 ms` ortalama ve `4,863 ms` max ile sürekli owner; tek en yüksek system sample'ı `ArcherShootSystem 17,444 ms`. Release cap artırılmadı; saturation kararı long-run soak'a bırakıldı.
- [x] Long-run soak ve active cap/backlog saturation ölçümü. Release `MaxAliveZombies = 900` değiştirilmeden, üretim `WaveSpawnSystem`/`ContinuousSpawnBudgetData`/enemy pool owner'larıyla 10.000 seed backlog + 1.000 canonical Basic Archer + gerçek projectile pipeline koşuldu. İki temiz explicit PlayMode turunun her birinde `360` warmup + `3.600` steady-state frame geçti; alive hiçbir frame `900` üstüne çıkmadı, cap doluyken backlog `9.228 -> 9.989/10.002` büyüdü ve enemy rent sabit kaldı. Yeni demand dondurulunca backlog `625/626` frame'de, tam `16/frame MaxSpawnBatch` sınırıyla sıfırlandı; demanded/spawned `10.889/10.889` ve `10.902/10.902` oldu. Enemy pool `1.024` created / `7` expansion, arrow pool `1.024` created / `0` expansion olarak warmup sonrası drift etmedi. Soak frame average/P95 `5,992/7,732 ms` ve `5,833/7,400 ms`; targeted test iki turda da `1/1` geçti. Release cap artırılmadı.

### Tuning yüzeyleri

- [x] Spawn: day curve, phase multiplier, backlog, active cap. `DifficultyProfileSO` BaseSpawn/Night gün eğrileri, dört faz çarpanı, `MaxSpawnBatch` backlog drain tavanı ve `MaxAliveZombies` active cap owner'ları `Difficulty Tuner > Spawn Runtime Contract` panelinde preview + canlı ECS telemetry ile tek yüzeyde görünür. Backlog politikası V1 `PreserveDemand` olarak read-only kalır; exact `PendingEnemies` cap dolumu ve kapasite açılınca drain davranışı 1 EditMode + 2 PlayMode hedef testinde geçti.
- [x] Wall: base HP, repair Stone cost, repair amount, Emergency %, day multiplier. `WallBaseHp`, normal repair paketi, `RepairStonePerMissingHp`, `RepairDayPriceMultiplier` ve Emergency heal/cooldown alanları `DifficultyProfileSO -> MobileCastleTuningResolver -> MobileCastleCombatConfig` owner zincirinde `Difficulty Tuner > Wall Runtime Contract` yüzeyine bağlandı; profile yoksa `CastleAuthoring.WallHP` fallback kalır. Baseline package preview ile gameplay aynı `SingleWallDefenseRules` Stone formülünü kullanır; live Apply HP oranını ve tech/meta/Heart aggregate'lerini korur. Default asset/resolver/formül/Emergency 5 EditMode, baked runtime/Stone transaction/live base HP 3 PlayMode testinde geçti.
- [x] Economy: base rates, capacity cost, efficiency growth. Wood/Stone/Iron/Food kisi basi
  production baseline'lari `8 / 5,5 / 4,9 / 7`, CAP `100W+25I`, EFF `150W+50I`, ortak fiyat
  buyumesi `1,35` ve seviye basi additive EFF `%10` tek
  `DifficultyProfileSO -> MobileCastleTuningResolver -> MobileCastleCombatConfig /
  MobileEconomyPriceTuning` owner zincirine alindi. `Difficulty Tuner > Economy Runtime Contract`
  ayni gameplay utility'leriyle level preview ve dort kaynak icin canli worker/cap,
  base/effective/total production, seviye/bonus/next-cost telemetry'si gosterir. Play Mode Apply,
  base rate degisiminde mevcut Tech/Meta/Heart ve bina aggregate'lerini yeniden fold eder. Owner,
  sanitize, iki-kaynak maliyet/effect matematikleri EditMode `8/8`; baked runtime, live rebase,
  satin alma+Continue ve runtime quote PlayMode `4/4` gecti; scene validation `0` issue,
  final Console `0 error`.
- [x] Population: Food per arrival, bed curve, dawn count. Dawn request `15` ve kabul edilen
  survivor başına tek seferlik `1 Food`, `DifficultyProfileSO -> MobileCastleTuningResolver ->
  MobileCastleCombatConfig` owner zincirine alındı; aynı isimli SubScene alanları profile yoksa
  fallback kalır. Initial bed `60` authoring-owned run baseline'ıdır. House bed `100W / 25 owned`
  quadratic eğrisi profile-owned `MobileEconomyPriceTuning` üzerinden devam eder. `Difficulty
  Tuner > Population Runtime Contract`, gerçek arrival/bed utility'leriyle current population,
  purchased beds ve Food preview'u; accepted/required Food, +1/+10 bed quote ve canlı next-Dawn/
  last-Dawn telemetry'si gösterir. Live Apply mevcut run bed state'ini sıfırlamadan request/Food
  config'ini ve bed curve tuning'ini günceller. Profile/fallback/sanitize, Dawn budget ve int-safe
  bed curve EditMode `18/18`; baked runtime, gerçek Dawn tek transaction, bed purchase+Continue ve
  runtime quote PlayMode `4/4` geçti; scene validation `0` issue, final Console `0 error`.
- [x] Archers: base stats, cost growth, retrain cost, Arrow drain. Basic/Rapid/Frost base
  combat ve recruitment tuning'i aktif catalog'daki üç `ArcherDefinitionSO` asset'inin tek
  definition owner'ı olarak korundu: `10 x 1,5 / R15`, `6 x 3 / R14`, `5 x 1,2 / R14`;
  buy/retrain base cost'lari ve ortak target-type count eğrisi `interval 25 / exponent 2`.
  `Difficulty Tuner > Archer Runtime Contract`, bu asset'leri profile'a kopyalamadan doğrudan
  düzenler ve `ArcherRecruitmentCostUtility` ile buy/retrain preview üretir. Finite Arrow
  capacity/refill/verim/CAP-EFF alanları aynı yüzeye taşındı fakat mevcut
  `DifficultyProfileSO -> MobileCastleTuningResolver -> MobileEconomyPriceTuning` owner zinciri
  değişmedi; paket/Buy Max/yatırım preview'ları gerçek `ArrowEconomyUtility` kullanır. Tüketim
  `ArcherShootSystem.ArrowCostPerSuccessfulProjectileRent = 1` read-only V1 kuralıdır ve yalnız
  başarılı pool rent'inden sonra uygulanır. Live telemetry effective type stat/count/DPS,
  teorik shot demand, ölçülen pool-rent Arrow/s, stok/capacity/verim ve yatırım fiyatlarını
  gösterir. Live Apply count/population/formation/type/fire timer state'ini koruyup mevcut
  okçuları aynı Heart/Tech/Meta katmanlarıyla yeni definition baseline'ına rebase eder. Canonical
  asset/cost/drain EditMode `11/11`; live rebase + retrain + finite Arrow PlayMode `5/5` geçti;
  1K refill örneği `1000` rent, `1000` Arrow, `0` pool expansion ve `30,413 ms` main thread verdi;
  Difficulty Tuner açıldı, scene validation `0` issue ve final Console `0 error`.
- [x] Heart: Essence gain, node cost/growth, rarity/depth. `Difficulty Tuner > Heart Runtime
  Contract`, canonical `GameManager`/`HeartNodeDefinitionSO` sahiplerini yeni bir runtime owner
  kurmadan tek yüzeyde gösterir. Gelecek run graph ayarları doğrudan `heartGraphSettings` üzerinden;
  catalog varsa rarity, minimum/maximum depth, base Grave Essence cost ve level growth doğrudan
  gerçek definition asset'lerinde düzenlenir. `+1/+10/Buy Max` preview'u production
  `HeartPurchasePricing`, seed graph preview'u production `HeartGraphGenerator` kullanır. Live
  telemetry hidden node kimliği sızdırmadan wallet/meta remainder ile aggregate node/edge/reveal/
  purchase/lock sayılarını verir. Bu tuning yalnız gelecekte üretilecek graph'ı etkiler; aktif veya
  Continue ile restore edilen exact graph reroll edilmez. Blueprint Essence drop yönünü tanımlasa
  da onaylı drop ihtimali/miktarı/cadence'i ve production node catalog'u yoktur; tuner bunları
  uydurmaz, açık `UNCONFIGURED` owner gate'i olarak gösterir ve legacy tech catalog'a fallback
  yapmaz. Contract guard EditMode `2/2`, Heart data/generator/purchase EditMode `36/36`, exact
  Continue PlayMode `1/1` geçti; Difficulty Tuner açıldı, scene validation `0` issue ve final
  Console `0 error / 0 warning`.
- [x] Council: fixed cadence, effect bands, repeat memory, decision timer. V1 regular schedule
  `CouncilRegularSchedule` owner'inda Day `3/6/9...` Dawn cadence'i olarak sabit kalir; Emergency
  Council yoktur ve legacy chance/pity/cooldown alanlari dormanttir. Composer icindeki gizli band
  sabitleri production `CouncilEventCatalogSO.EffectBands` owner'ina tasindi: multiplier
  `0.7/1.0/1.4`, weight `%35/%50/%15`, A/B budget tolerance `1.25`; varsayilan deterministic
  davranis korunur ve gecersiz katalog fail-closed'dur. `RecentTemplateMemory=3` dogrudan katalogda
  tune edilir; azaltildiginda `GameManager` recent listeyi bir sonraki scheduled compose'dan once ve
  secilen kart eklendikten sonra ayni sinira indirir. Karar suresi ayri state/tuning degildir;
  `CouncilDecisionWindowUtility` active cycle Dawn+Day owner'indan turetir ve Dusk'ta expire eder.
  `Difficulty Tuner > Council Runtime Contract`, katalog alanlarini profile'a kopyalamadan dogrudan
  duzenler; fixed cadence, SubScene cycle snapshot'i ve aggregate live memory/card/budget/timed-effect
  telemetry'sini gosterir. Production snapshot `5s Dawn + 30s Day = 35s`; Council EditMode `56/56`,
  regular runtime/Continue/memory PlayMode `8/8` gecti; Tuner gercek editor window olarak acildi,
  `NewGameScene` validation `0` issue ve final Console `0 error / 0 warning`.
- [x] Meta: reward weights, upgrade costs/effects. Production `MetaUpgradeCatalogSO`, diminishing
  kill bandlari `100/1000` ve agirliklari `1/0,25/0,05` ile day/night/peak-pop/record
  agirliklarinin `10/25/0,2/50` tek owner'idir. Quote component-floor + saturating toplamla
  hesaplanir; 10K/Day10/peak200/new-record ornegi `1.640 Souls` (`775/100/225/40/500`) verir.
  `RunDeathReceipt v2`, exact quote ve peak population'i journal'a sabitleyerek recovery'nin
  sonradan degisen tuning'i yeniden okumasini engeller; v1 receipt eski formuluyla explicit
  migration yolundadir. 11 permanent definition'in mevcut incremental cost/effect tablosu
  korunup exact testle kilitlendi: resource/yatak limitsiz exponential sink, Basic Archer `1000`
  cap, Wall `+%25`, production `+%15`, Arrow `+10`, Essence `+%50`, pool unlock tek sefer.
  `Difficulty Tuner > Meta Runtime Contract`, reward alanlari ile definition cost/growth/cap/effect
  degerlerini gercek catalog/asset sahibinden duzenler; ayni runtime utility'leriyle reward/level
  preview ve aggregate live Souls/lifetime/current quote/applied-effect telemetry'si gosterir.
  Aktif HUD `SoulCounterUI`, kill sayisini yanlis bicimde Souls diye gostermek yerine ayni exact
  quote'u `DEATH +N SOULS` olarak sunar.
  Targeted Meta+persistence EditMode `44/44`, targeted death/Continue/shop PlayMode `3/3`, full
  EditMode `345/345`, full PlayMode `72 pass + 2 explicit soak/profiler skip` gecti; production
  catalog `11 definition / 0 problem`, Tuner gercek editor window olarak acildi,
  `NewGameScene` validation `0` issue.

### Telemetry event'leri

- [x] `run_started`: meta levels, starting resources, graph seed/version. Provider-bagimsiz tek
  `GameplayTelemetry` cikisi kuruldu; event v1 envelope'u `EventName/SchemaVersion/RunId/PayloadJson`
  tasir ve external analytics target'i owner karari gelmeden secilmez. `GameManager`, canonical
  `MetaProgression` + production 11-definition Meta catalog, ECS `ResourceData/ArrowSupply/
  PopulationState` ve Heart runtime identity'sini yeni run tamamen kurulduktan sonra immutable
  snapshot'a cevirir. Production default emission `160/80/50/120`, `200/200 Arrow`, `60/60`
  population ve 11 exact Meta level verir. Launch Heart catalog henuz owner karari bekledigi icin
  event kaybolmaz; `configured=false/ready=false/version=0/seed=0` explicit unconfigured kimligi
  tasir ve catalog geldiginde ayni contract generated graph seed/version'ini otomatik okur.
  Main-menu pending action uygulanmadan emission yapilmaz; yeni `RestartGame` RunId basina bir kez
  emit eder, exact Continue restore edilen ayni RunId'yi ikinci `run_started` saymaz. Editor/
  Development log'u `[DW-TELEMETRY]` machine-readable envelope verir; Release log kapali fakat
  subscriber cikisi aktiftir. Contract EditMode `3/3`, yeni-run + Continue duplicate PlayMode
  `1/1`, ilgili Meta/Continue grubu `3/3` gecti.
- [x] `phase_changed`: day, phase, alive enemies, spawn backlog. Mevcut
  `GameplayTelemetry` envelope/subscriber siniri yeniden kullanildi; ikinci bus veya ECS state
  owner'i kurulmadı. `GameManager`, canonical `ContinuousSiegeCycleData.CycleIndex + 1/Phase`,
  `WaveStateData.ZombiesAlive` ve `ContinuousSpawnBudgetData.PendingEnemies` snapshot'ini her
  yeni `(RunId, Day, Phase)` kimliginde v1 payload olarak bir kez emit eder. Phase contract'i
  `day/dusk/night/dawn` degerleriyle sabittir; ayni phase icindeki alive/backlog degisimi duplicate
  event uretmez. Yeni run'da `run_started` basariyla emit edildikten sonra Day 1 gelir; exact
  Continue mevcut run/day/phase kimligini prime ederek tekrar saymaz. Contract EditMode `6/6`,
  canonical NewGameScene transition/Continue PlayMode `2/2`, full EditMode `351/351` ve full
  PlayMode `74 pass + 2 explicit profiler/soak skip` gecti.
- [x] `resource_spent`: resource, amount, purchase type, resulting level/count. Mevcut
  `GameplayTelemetry` envelope/subscriber sınırı v1 payload ile genişletildi; resource identity
  `wood/stone/iron/food/grave_essence/meta_currency`, purchase type ise active V1 transaction
  yüzeyleri için sabit machine-readable contract'tır. Normal Wall repair, Arrow refill/capacity/
  efficiency, bed capacity, dört worker binasının capacity/efficiency upgrade'leri, üç archer buy,
  Rapid/Frost retrain, Castle Heart node ve durable ölüm-sonrası Meta upgrade yalnız canonical commit
  başarıyla tamamlandıktan sonra event üretir. Bir purchase birden fazla kaynak harcıyorsa aynı
  resulting level/count ile Wood/Stone/Iron/Food sırasına göre kaynak başına ayrı kayıt çıkar.
  Reddedilen/rollback edilen transaction, `freeEconomyTestMode`, Council negatif effect'i, dawn Food
  kesintisi ve player-facing olmayan legacy purchase yolları event üretmez. Heart debit graph/effect
  commit'inden, Meta debit ise atomic disk save'den sonra yazılır. Contract EditMode `9/9`, gerçek
  bed + Wood/Iron worker purchase/rejection PlayMode `3/3`, full EditMode `354/354` ve full PlayMode
  `75 pass + 2 explicit profiler/soak skip` geçti.
- [x] `archer_changed`: buy/retrain, type from/to, total cap usage. Mevcut provider-independent
  `GameplayTelemetry` bus'i v1 payload ile genişletildi. Yalnız başarılı player transaction'ı
  canonical entity/state commit'i ve type-count sorgusu tamamlandıktan sonra `buy: none ->
  basic/rapid/frost` veya `retrain: basic -> rapid/frost` transition'ı ile post-commit toplam
  `1..1000` cap kullanımını yazar. Aynı buy/retrain transaction'ındaki `resource_spent` kayıtları
  önce gelir; free economy test modunda debit olmasa bile gerçek archer state değişimi event üretir.
  Locked/cap/population/resource guard'ında reddedilen, spawn/retrain rollback yapan işlemler ile
  Council/meta başlangıç bonusu ve exact Continue restore sıfır `archer_changed` üretir. Contract
  EditMode `12/12`, gerçek buy/retrain/rejected transaction PlayMode `4/4`, full EditMode `357/357`
  ve full PlayMode `76 pass + 2 explicit profiler/soak skip` geçti.
- [x] `heart_node_bought`: node, level, depth, cost, revealed children. Mevcut provider-independent
  `GameplayTelemetry` bus'i v1 payload ile genişletildi. Yalnız `HeartPurchaseService` Grave Essence
  spend, graph level, prepared effect, Keystone lock ve first-purchase reveal commit'lerini tamamen
  başarıyla tamamladığında canonical `NodeId`, post-commit `NewLevel`, satın alınan graph node'unun
  exact `Depth` değeri, bulk-safe toplam `Cost` ve gerçekten `Hidden -> Revealed` olan outgoing child
  sayısı yazılır. Hidden child Id'leri telemetry ile açığa çıkarılmaz. Aynı transaction'ın
  `resource_spent: grave_essence/heart_node` kaydı önce gelir; quote/evaluate, exact Continue replay,
  insufficient Essence, hidden/locked/root/unknown node, invalid graph/catalog, effect/spend rejection
  sıfır `heart_node_bought` üretir. Contract EditMode `15/15`, gerçek graph buy/reveal/rejected tekrar
  PlayMode `5/5`, full EditMode `360/360` ve temiz full PlayMode tekrarı `77 pass + 2 explicit
  profiler/soak skip` geçti.
- [x] `council_resolved`: day, template, option/expired, effects, next-night delta.
  `GameplayTelemetry` v1 payload'i regular-only Council kararini `Day`, `TemplateId`,
  `option_a/option_b/expired`, machine-readable effect listesi ve production guard'dan gecmis
  `NextNightDelta` ile yayar. Basarili secim effect/flag/active-clear commit'inden, expire ise
  active-clear commit'inden sonra tek event uretir. Rejected secim, bos tekrar expire ve cozulmus
  karar sonrasi exact Continue sifir duplicate uretir. Contract EditMode `18/18`, gercek
  `NewGameScene` secim/expire/Continue PlayMode `6/6`, full EditMode `363/363` ve full PlayMode
  `78 pass + 2 explicit profiler/soak skip` gecti.
- [x] `ability_cast`: ability, phase, cooldown, targets/repair. Provider-independent
  `GameplayTelemetry` bus'i Fireball/Rally/Emergency Repair v1 payload'iyle genişletildi. Yalnız
  canonical ability transaction'i commit edildikten sonra resolved ability identity, exact
  day/dusk/night/dawn phase'i ve post-commit cooldown remaining yazılır. Fireball projectile kabul
  anında asynchronous impact sonucu uydurmaz (`Targets=0`, `Repair=0`); Rally canonical toplam
  archer sayısını, Emergency Repair ise `Targets=1` ve Wall'a gerçekten eklenen HP farkını taşır.
  Locked/cooldown/active/pause/Game Over/off-phase/full/dead Wall rejection'ları ile exact Continue
  sıfır duplicate üretir. Contract EditMode `21/21`, gerçek `NewGameScene` üç ability/rejected/
  Continue PlayMode `7/7`, full EditMode `366/366` ve full PlayMode
  `79 pass + 2 explicit profiler/soak skip` geçti.
- [x] `wall_repaired`: phase, Stone cost, HP before/after. Provider-independent
  `GameplayTelemetry` bus'i normal Wall repair v1 payload'iyla genisletildi. Yalniz
  `RepairDefenseFull` Day/Dusk transaction'i exact Stone debit'i ve canonical `WallSegment` HP
  commit'ini tamamen basariyla tamamladiktan sonra `Phase`, `StoneCost`, `HpBefore`, `HpAfter`
  snapshot'i yayilir. Ayni transaction'in `resource_spent: stone/wall_repair` kaydi once,
  `wall_repaired` sonucu sonra gelir. Full/dead Wall, Game Over, Night/Dawn, yetersiz Stone ve
  `freeEconomyTestMode` sifir event uretir; Emergency Repair ile Council heal ikinci kez normal
  repair sayilmaz. Exact Continue tamamlanmis transaction'i tekrar yaymaz. Contract EditMode
  `24/24`, gercek `NewGameScene` repair/rejected/Continue PlayMode `8/8`, full EditMode `369/369`,
  full PlayMode `80 pass + 2 explicit profiler/soak skip` ve scene validation `0 issue` gecti.
- [x] `run_ended`: day, kills, peak enemies/pop, Wall damage timeline, meta reward ve final economy/combat snapshot'i.
  Provider-independent `GameplayTelemetry` bus'i final `run_ended` v2 summary'siyle genişletildi.
  Event yalnız durable death reward transaction'ı başarıyla finalize edildikten sonra gerçek `Day`,
  `Kills`, `PeakPopulation` ve receipt-owned `MetaReward` ile tek kez çıkar. Production spawn commit'leri
  `RunTelemetryData.PeakEnemies` high-water mark'ını günceller; development stress spawn'ları dışarıda
  kalır. V2 final upgrade-applied Wall MaxHP, Wood/Stone/Iron/Food, Arrow/current capacity, population/capacity/idle,
  Basic/Rapid/Frost count ve unspent Grave Essence snapshot'ını canonical death state'inden ekler;
  launch target asset'i bu event'leri yalnız designer review için kullanır ve automatic retuning yapmaz.
  `DamageApplySystem`, modifier sonrası gerçekten uygulanan Wall hasarını kronolojik day/phase
  bucket'larında toplar; per-hit telemetry/event üretmez. RunSave v17 exact Continue peak ve timeline'ı
  korur; v14->v15 migration eski save'lere geçmiş veri uydurmaz. Eksik Meta finalize, rejected payload ve aynı lethal
  transaction'ın tekrar işlenmesi sıfır duplicate üretir. Targeted EditMode `53/53`, gerçek Continue +
  durable death PlayMode `9/9`, full EditMode `375/375` ve full PlayMode
  `81 pass + 2 explicit profiler/soak skip` geçti.

---

## 16. QA Kabul Matrisi

| Alan | Test | Beklenen | Durum |
|---|---|---|---|
| Cycle | Production authoring + 60 sn full loop/wrap | 30/5/20/5; dört pozitif phase; yeni Day'e prep gap olmadan kesintisiz combat | `[x]` |
| Horde | Active cap dolu | Talep backlog'a gider | `[x]` |
| Horde | Production profile guard + Day 1 vs ileri gün runtime stat/quantity | HP/damage/speed aynı; count artar, interval daralır | `[x]` |
| Wall | Night normal repair | Kapalı; Stone harcanmaz | `[x]` |
| Wall | HP 0 + same-frame repair | Game Over kazanır | `[x]` |
| Enemy catalog | Production asset/authoring guard + V1 runtime bake + gerçek spawn | Tek `zombie_basic`; iki SubScene owner'ı aynı catalog/prefab'a bağlı; prefab/stat/scale tanımla eşleşir | `[x]` |
| Population | Food yetersiz dawn | Mevcut pop korunur; arrival sınırlı | `[x]` |
| Ammo | Arrow 0 / refill | Ateş durur / stok aynı transaction'da artar / sonraki simulation tick'inde atış sürer | `[x]` |
| Archers | 1.001. purchase | Reddedilir; harcama yok | `[x]` |
| Placement | 40 tile'da 1.000 archer | Her tile 25 stable point | `[x]` |
| Targeting | Yoğun overkill | Incoming damage hedefleri dağıtır | `[x]` |
| Heart | Invalid generated graph | Reroll/fallback veya açık hata | `[x]` |
| Heart | Source/runtime state + Grave Essence lifecycle | Asset runtime state taşımaz; Continue exact, Restart/ölüm siler | `[x]` |
| Heart | Guarantee reachability | Rapid/Frost/Fireball reachable | `[x]` |
| Heart | Full pause | Cycle/spawn/worker/cooldown durur | `[x]` |
| Council | Day 1-12 regular cadence | Yalnız Dawn 3/6/9/12; aynı gün tek kart | `[x]` |
| Council | Guarded effects | Bed/Food, 1000, Wall-only, count-only | `[x]` |
| Save | Menu çıkış / Continue | Aynı graph/phase/Wall/economy | `[x]` |
| Death | Process restart | Meta bir kez; run geri gelmez | `[x]` |
| HUD | 16:9 / ultrawide | Kritik UI ve dünya kırpılmaz | `[x]` |
| Tutorial | İkinci run | Gerçek lethal ilk run -> `UIManager.OnRestart()` yeni run kimliği; sekiz meta flag durable, 120 frame boyunca bütün cue'lar kapalı | `[x]` |
| Stress | 1k archer x 10k enemy | StandaloneWindows64 target-hardware penceresi, 1920x1080 Ultra ve Profiler instrumentation kapalı 180 warmup + 600 sample: average/P95/P99/max `7,665/13,890/14,058/27,722 ms`; `4/600` frame 16,667 ms üstü, `0/600` frame 33,333 ms üstü, en uzun seri `1`; exact `10.000 enemy / 1.000 canonical Basic Archer`, projectile peak `670`; explicit Player `1/1` | `[x]` |

### Mevcut test envanteri

- `[x]` EditMode: `408/408`; tek production `IsGameOver = true` writer'ı ve Wall-destroyed guard'ı, telemetry payload/envelope/identity guard'ları, run peak/timeline accumulator, eksiksiz v3-v17 migration matrisi, retired direct archer owner guard'ı, production Heart'ın dört archer stat progression kategorisi, archer transition/total-cap, Heart node/level/depth/cost/reveal, Meta/Last Embers/narrative/Council/launch tuning, finite Arrow, pool, targeting, Formation V1, economy, worker, cycle, quantity-only, backlog, Moat isolation ve enemy catalog kapsamı.
- `[x]` PlayMode envanteri: `89 pass + 2 explicit profiler/soak skip`; telemetry, death quote/Continue/meta shop, Council, production Heart graph/Continue, Fireball evolution, Arrow/pool/targeting, 1K archer x 10K enemy, Formation V1, archer cap/retrain, economy/worker, Wall, cycle ve backlog kapsamına ek olarak Heart damage/fire-rate/range/Frost slow satın alımının existing entity rebase, future spawn inheritance ve non-compounding exact Continue davranışını kilitler.
- `[x]` Player/hardware frame pacing: exact 10K enemy + canonical 1K Basic Archer explicit StandaloneWindows64 test `1/1`; instrumentation kapalı 600-frame JSON `accepted=true`, average/P95/P99 `7,665/13,890/14,058 ms`; ayrı `74.879.570 B` raw `120/120` frame analiz edildi.

---

## 17. Risk Register

| Risk | Mevcut erken sinyal | Mitigation / kill rule |
|---|---|---|
| 1k x 10k targeting collapse | StandaloneWindows64 600-frame average/P95/P99 `7,665/13,890/14,058 ms`; exact 10K/1K ve projectile combat kanıtı geçti | V1 target-hardware kapısı kapandı; release cap `900` ve soak saturation guard'ı korunur |
| Projectile/VFX flood | Projectile churn kapandı; hit event yoğunluğu açık | Pool tamam; VFX/SFX budget + aggregation |
| Graph unreachable | Generator henüz yok | Validation + deterministic fallback |
| Meta runaway | 10K quote artık `1.640`; kill katkısı üç azalan band kullanıyor | Tuner reward/cost preview + live aggregate; telemetry event'leriyle saha dağılımını izle |
| Ammo click angaryası | +1/+5/Buy Max, capacity ve efficiency aktif | Telemetry ile paket/verim tune et; Blueprint dışı auto-spend ekleme |
| HUD tekrar büyür | Forecast kaldırıldı; üç management yüzeyi ortak exclusive owner altında ve aynı anda yalnız biri açık | Fixed layout + mockup gate + presentation guard testleri |
| Legacy leakage | Gate/Core binding, Fletcher, Barracks training, direct upgrades | Source-owner audit + guard tests |
| Council generic/slop | Çözüldü: 9 template human-review, iki body varyantı, staged MinDay, distinct follow-up recipe ve honest transaction copy taşıyor | 5.400 compose budget/token/content regression gate |
| Unapproved lore/content | Narrative açık karar | Owner review olmadan canon/content ekleme |
| Save contract drift | Her yeni sistem ayrı field ekleyebilir | Merkezi schema/version/migration owner |

---

## 18. Release Definition of Done

- [x] Run yalnız Wall `0 HP` ile bitiyor; final wave/boss/ikinci fail phase yok.
  Production runtime scriptlerinde `GameStateData.IsGameOver = true` yazan tek owner
  `DamageApplySystem` ve tek guard `SingleWallDefenseRules.IsDestroyed(remainingWallHp)`. `GameManager`
  yalnız authoritative `false -> true` geçişini gözleyip death transaction/sunumu bir kez başlatıyor.
  Wave completion, cycle/day, horde pressure, enemy/boss ölümü veya başka fail phase terminal state
  yazmıyor. Source-owner EditMode guard'ı ile injected Gate/Core gerçek PlayMode regresyonu bu sınırı
  kilitliyor; targeted `12/12` EditMode, `1/1` PlayMode, full EditMode `376/376` ve full PlayMode
  `81 pass + 2 explicit profiler/soak skip` geçti.
- [x] Çıkış catalog'unda tek enemy prefab var.
  Unity MCP production asset denetiminde enemy klasöründe `1 EnemyCatalogSO + 1 EnemyDefinitionSO`
  bulundu; catalog tam bir `zombie_basic` kaydı ve `Zombie.prefab` içeriyor. Release guard,
  `MobileCastleCombatSubScene` içindeki tek `WaveConfigAuthoring` ile tek
  `MobileCastleCombatAuthoring` sahibinin aynı production catalog'a, legacy compatibility prefab
  alanının da aynı `Zombie.prefab` referansına bağlı olmasını zorunlu kılıyor. Targeted EditMode
  `4/4`, gerçek bake/spawn PlayMode `2/2`, full EditMode `377/377` ve full PlayMode
  `81 pass + 2 explicit profiler/soak skip` geçti; scene validation `0` issue ve final Console
  `0 error / 0 warning`.
- [x] Difficulty enemy stats değil adet/akış büyütüyor.
  Unity MCP production asset denetiminde difficulty klasöründe tek `DefaultDifficulty.asset` bulundu.
  Release guard, active SubScene'deki tek `MobileCastleCombatAuthoring` sahibinin bu profile ve geçerli
  enemy catalog'una bağlı olmasını; production profile/authoring HP, damage ve speed growth alanlarının
  neutral kalmasını zorunlu kılıyor. Aynı gerçek owner bileşimi Day 1 ve Day 50 için karşılaştırıldığında
  HP/damage/speed değişmiyor; extra enemy count, cycle batch growth, phase/day intensity ve daralan
  spawn interval quantity pressure üretmeye devam ediyor. Targeted EditMode `2/2`, gerçek runtime
  PlayMode `3/3`, full EditMode `378/378` ve full PlayMode
  `81 pass + 2 explicit profiler/soak skip` geçti; scene validation `0` issue ve final Console
  `0 error / 0 warning`.
- [x] 60 saniye cycle 30/5/20/5 ve kesintisiz.
  Production SubScene release guard'ı `ContinuousSiegeEnabled=true` ve exact
  `60 = Day 30 + Dusk 5 + Night 20 + Dawn 5` authoring sözleşmesini kilitliyor. Gerçek runtime
  testinde dört fazın tamamı pozitif spawn intensity üretti; cycle `60s` sınırını geçtiğinde
  `CycleIndex` bir arttı, timer yeni Day içine döndü ve `WaveStateData` araya DayPrep/wave-start
  beklemesi girmeden active combat halinde kaldı. Stale üç-faz `25/10/25` mimari metni güncel
  dört-faz sözleşmesiyle değiştirildi. Targeted EditMode `1/1`, gerçek phase/wrap PlayMode `2/2`,
  full EditMode `379/379` ve full PlayMode `81 pass + 2 explicit profiler/soak skip` geçti;
  scene validation `0` issue ve final Console `0 error / 0 warning`.
- [x] Speed-up/offline progress yok.
  Production runtime source guard'ı player-facing `Time.timeScale > 1`, `Time.fixedDeltaTime`
  yazıcısı, wall-clock API ve offline owner alanlarını yasaklıyor; yalnız normal `1x`, merkezi pause
  `0` ve Game Over sunumu `0.25x` sahipleri onaylı. Editor-only `LongRunSimulatorWindow` player
  runtime kapsamına girmiyor. `RunSaveState`, `MetaProgressState` ve `RunDeathReceipt` timestamp,
  last-login veya offline accrual alanı taşımıyor. Gerçek `NewGameScene` tek bound pause kontrolü
  taşıyor ve x2/x4/fast-forward kontrolü taşımıyor; exact Continue aynı cycle/phase/timer,
  kaynak ve spawn RNG state'ini geri yüklüyor. Targeted EditMode `3/3`, exact Continue PlayMode
  `1/1`, full EditMode `382/382` ve full PlayMode `81 pass + 2 explicit profiler/soak skip`
  geçti. Tam PlayMode ilk turunda 10K/1K stres testi son-frame projectile fotoğrafına bağlı kaldı;
  sample boyunca authoritative pool rent artışını ölçecek biçimde stabilize edildi ve hedefli
  turda `2.000` yeni rent / `1.000` peak aktif projectile doğrulandı. Scene validation `0` issue,
  final Console `0 error / 0 warning`.
- [x] Wood/Stone/Iron/Food pasif negatif akmıyor; Arrow tek sürekli tüketim.
  `ResourceTickSystem`, active `MobileCastleCombatConfig` loop'unda legacy
  `ResourceConsumptionRate` değerlerini hesap dışı bırakıyor; `ArrowProductionSystem` mobile
  config varken Fletcher üretimi veya Wood tüketimi yapmıyor. Gerçek production world'de
  `ArrowProducer = 0` ve legacy `ArcherTrainer = 0` doğrulandı. Yüksek negatif rate enjekte
  edilen gerçek combat tick'inde Wood/Stone/Iron/Food başlangıç stoklarının hiçbiri azalmadı ve
  dört consumption rate sıfırlandı; başarılı tek projectile rent'i yalnız Arrow stokunu
  `10 -> 9` düşürdü. Player purchase/repair ve kabul edilen Dawn nüfusunun bir defalık Food
  maliyeti committed transaction'dır; pasif/per-minute drain değildir. Targeted PlayMode `3/3`,
  full EditMode `382/382` ve full PlayMode `82 pass + 2 explicit profiler/soak skip` geçti;
  scene validation `0` issue ve final Console `0 error / 0 warning`.
- [x] Population, beds ve worker ratios exact save/load.
  Birleşik release guard aynı disk snapshot'ında `PopulationState` total/workers/archers/idle,
  population base/capacity, `MobileBedCapacityState` base/purchased, Wood/Stone/Iron/Food actual
  worker count, worker idle/last-observed ve dört target ratio'yu birlikte yazıyor. Runtime state
  farklı coherent değerlere mutate edildikten sonra Continue; actual archer entity sayısını koruyarak
  bütün alanları bire bir geri yükledi. Target ratio kontratı exact `2700/1900/1600/3800`, actual
  worker kontratı `23/17/11/25`, idle `17` ve purchased beds `15` ile doğrulandı. Targeted
  PlayMode `3/3`, full EditMode `382/382` ve full PlayMode
  `83 pass + 2 explicit profiler/soak skip` geçti; scene validation `0` issue ve final Console
  `0 error / 0 warning`.
- [x] Worker world feedback representative ve doğru.
  Gerçek `NewGameScene` birleşik release guard'ı dört resource'u aynı anda Low/Medium/High
  density örnekleriyle kurdu: actual `12 / 60 / 101 / 1000`, temsili
  `12 / 24 / 27 / 32`. Toplam `95` visual entity'nin resource içi index'i unique,
  temsil ağırlığı kendi actual grubuna exact eşit, logistics route'u geçerli, cargo rengi
  resource'a doğru, production feedback şiddeti represented weight ile aynı ve Night
  lantern sinyali aktif doğrulandı. Dünya feedback'i `MobilePopulationAllocation` actual
  truth'unu değiştirmedi; mevcut pickup work/carry, hub delivery pulse ve Day lantern-off
  regresyonları da korundu. İlk çoklu-filter Test Runner başlangıcı Unity Test Framework'ün
  kendi `PlayModeRunTask.cs:52` domain-reload hatasıyla test gövdesinden önce düştü; MCP'nin
  orphan job'u kendi `TestJobManager.ClearStuckJob()` recovery yolu ile temizlendi ve testler
  tekil gerçek job'larda geçti. Targeted PlayMode `3/3`, full EditMode `382/382`, full
  PlayMode `84 pass + 2 explicit profiler/soak skip`; scene validation `0` issue ve final
  Console `0 error / 0 warning`.
- [x] Basic/Rapid/Frost toplam 1000 cap.
- [x] 40x25 stable formation.
- [x] Arrow Wood ile anında alınır; Fletcher/queue yok.
- [x] Castle Heart generated, validated, saved ve yalnız Grave Essence kullanıyor.
  Canonical production v1 katalog, eski `TechTreeCatalogSO` asset'lerini değiştirmeyen idempotent
  migration builder'ı ile `35` node olarak üretildi ve `NewGameScene` `GameManager` sahibine bağlandı.
  Dört branch, dört Keystone trade-off çifti, dört repeatable sink ve `15` Evolution node içeriyor;
  `64` deterministic seed sweep'inde generator validation temiz. Player-facing graph branch damarları,
  rarity/owned/locked chrome'u, okunabilir gerçek numeric sonuçlar ve run-scope metinleriyle polish edildi.
  Launch içerik otoritesi `DEAD_WALLS_V1_CASTLE_HEART_LAUNCH_CATALOG.md` içinde bütün `35` node'un
  effect/cost/depth/legacy provenance değerlerini kilitliyor. `18` yararlı legacy fikir tekil provenance
  mapping'i taşırken root/Basic/Moat kapsamı migrate edilmiyor; node kimliği/effect/cost değişmediği için
  exact graph uyumluluğu korunarak catalog version `1` bırakıldı. Node kartları branch/type hiyerarşisi,
  node-specific production/archer/repair ikonları, açık `ESSENCE` dili ve türe özel satın alma fiilleriyle
  ikinci FullHD polish turundan geçti.
  Purchase transaction'ı yalnız canonical `GraveEssence` wallet'ını kullanıyor; mevcut exact graph save/
  Continue ve Grave Essence reset sözleşmeleri tam PlayMode regresyonunda korundu. Targeted production
  EditMode `3/3`, targeted production PlayMode `1/1`, full EditMode `385/385`, full PlayMode
  `85 pass + 2 explicit profiler/soak skip`; scene validation `0` issue, final Console
  `0 error / 0 warning`.
- [x] Council 3/6/9 regular schedule'ı koruyor ve aynı gün yalnız bir kart açıyor.
- [x] Council yalnız approved template/effect pool kullanıyor.
- [x] Council ana guardrail'leri bypass etmiyor.
- [x] Fireball/Rally/Emergency Repair bottom-center cooldown barında.
- [x] Meta yalnız ölümde bir kez reward veriyor; voluntary reset yok.
- [x] HUD tek Wall barı ve owner-approved minimal phase UI kullanıyor.
- [x] İlk-run tutorial tamamlanıyor; sonraki run'da tekrarlamıyor.
- [x] 1k archer + 10k enemy target hardware frame pacing kabul edildi.
- [x] EditMode/PlayMode, save migration ve long-run soak raporları temiz.
- [x] Day/night lighting, horde mix, Wall bar, phase UI ve combat feedback owner görsel incelemeden geçti.

---

## 19. Değiştirilemez Guardrail'ler

- Boss/miniboss/elite/enemy variant yok.
- Blood Moon veya sabit special-night schedule yok.
- Enemy scout/forecast yok.
- Enemy lane/front selection yok.
- Build grid/building placement yok.
- Fletcher production yok.
- Archer death/individual HP yok.
- Gate/Core HP yok.
- Arrow Storm yok.
- Voluntary reset/prestige button yok.
- Offline income/offline death yok.
- Separate archer upgrade panel yok.
- Mobile ads/rewarded/IAP yok.
- Council dormant değildir; active core run sistemidir.
- Dormant Moat/Blood Moon/legacy wave kodu, owner onayı olmadan active V1 loop'a bağlanamaz.

---

## 20. Owner Kararı Bekleyen Açık Konular

Bu maddeler kod içinde varsayımla kapatılmaz. Önce mockup/spec, sonra owner kararı, sonra implementation.

- [x] Faz göstergesi: Owner `B - Celestial Dial` yönünü seçti; uygulandı, görsel eşleşme turundan geçti ve `A - Horizon Ribbon` geri dönüş alternatifi karar dokümanında arşivlendi.
- [x] Meta currency adı, ikon ve death-screen copy: `LAST EMBERS / EMBERS`, `last_embers` stable presentation Id, amber cracked-ember icon ve polished `THE WALL HAS FALLEN` screen seti.
- [x] Narrative premise/world pitch/opening copy: Steward rolü, stand/Heart rekindle/Last Embers canon'u, deliberate unknown guardrail'leri ve modal eklemeyen `THE WORLD ENDED. THE SIEGE DID NOT.` main-menu açılışı kilitlendi.
- [x] Launch Heart node catalog ve effect specs: production v1 `35` node / `4` branch / `4` Keystone pair / `4` repeatable sink exact effect-cost-depth tablolarıyla kilitlendi; `18` legacy fikir provenance-only migrate edildi, root/Basic/Moat dışarıda bırakıldı ve player-facing graph ikinci FullHD polish turundan geçti.
- [x] Council launch template/atom listesi + tekrar/bütçe testi.
- [x] En az 3 Keystone trade-off çifti: launch catalog'undaki dört owner-approved çift (`Heavy Draw / Storm Cadence`, `Bastion Doctrine / Salvage Doctrine`, `Deep Stores / Relentless Shifts`, `Inferno Heart / Chronomancer Heart`) gerçek iki-kart run commitment fork'u olarak kilitlendi. Çiftler birlikte reveal olur; seçim yalnız exact partner'ı kalıcı kilitler, iki tarafın continuation edge'leri korunur ve eski exact save'lerdeki tek-taraf görünürlük catalog-aware normalize edilir. Castle Heart sunumu çiftleri branch yönüne göre yan yana, altın fork bağlantısı ve `CHOOSE ONE · RUN COMMITMENT` etiketiyle gösterir.
- [x] Fireball için 2-3 evolution spec ve VFX yönü: launch'ta Scorched Earth/Burning Ground ile Echoing Detonation/Second Blast kilitlendi; davranış, katalog, aggregate VFX ve exact Continue sözleşmeleri uygulandı.
- [x] Exact spawn/economy/combat/meta tuning curves ve telemetry target'ları: production Difficulty,
  üç Archer ve Meta catalog değerleri tek launch authority dokümanında kilitlendi. Provider-independent
  `V1LaunchTelemetryTargets.asset`, 19 inclusive band/cohort/minimum-sample/source-event tanımı ve
  `58fc60a0...f8059dd3` fingerprint'iyle oluşturuldu. `run_ended v2` final economy/combat snapshot'i
  housing, Arrow, archer mix ve run-currency hedeflerini ölçülebilir yaptı; Difficulty Tuner paneli
  bantları read-only/polished gösterir. Bant dışı ölçüm automatic retuning değil designer review'dur.
- [x] Day/night audio mix map ve kapsamlı asset polish'i, owner kararıyla ses paketi import edildikten sonraki final polish turuna ertelendi ve mevcut V1 görsel kabul kapısından çıkarıldı.

---

## 21. Denetlenen Mevcut Kaynak Sahipleri

| Kaynak | Denetlenen gerçek |
|---|---|
| `NewGameScene.unity` + `MobileCastleCombatSubScene.unity` | Kamera, HUD, active authoring, cycle ve combat values |
| `MobileCastleHudRoot` live components | Celestial Dial CyclePanel, compact resource strip, Wall-only defense, bottom-center ability bar ve management drawers; Horde forecast ile Gate/Core yüzeyleri yok |
| `MobileCastleCombatAuthoring.cs` + `DefaultDifficulty.asset` + `BasicZombie.asset` | 30/5/20/5, quantity curves, 900 cap ve ekonomi fiyat baseline'ları; enemy base statları catalog-owned |
| `GameManager.cs` | Save/restore, repair, worker bina CAP/EFF alımı ve economy aggregate'i, ortak archer spawn/cap guard'ı, archer buy/retrain, Heart archer stat rebase'i, Council, Fireball ve meta bridge; direct archer type-level upgrade owner'ı yok |
| `RunPersistence.cs` | Exact schema v17; minimum v3, compact snapshot, Heart graph, regular Council state, üç active-ability cooldown'ı, run telemetry accumulator'ları ve Fireball evolution pending/timer/tick state'i; açık v3->v17 migration zinciri ve retired archer level normalization'ı |
| `ContinuousSiegeCycleSystem.cs` | Phase/intensity ve Blood Moon application |
| `WaveSpawnSystem.cs` + `EnemyPoolRuntimeUtility.cs` | Tek catalog prefab/stat, cap/backlog ve expandable pool rent |
| `DamageApplySystem.cs` + `SingleWallDefenseRules.cs` | Production runtime'da tek `IsGameOver = true` writer'ı; yalnız `WallSegment` sıfır HP guard'ı terminal state üretir, final wave/boss/ikinci fail owner'ı yoktur |
| `DamageCleanupSystem.cs` | Reward sonrası enemy pool return |
| `ResourceTickSystem.cs` + `PopulationTickSystem.cs` | V1 castle loop'ta ana kaynak ve population için pasif consumption yok |
| `MobileEconomyPriceTuning` + `MobileCastleTuningResolver.cs` + `DifficultyTunerWindow.cs` | Profile-owned bed base/interval ve worker CAP/EFF Wood/Iron base + ortak growth değerlerini sanitize edip Baker/live Apply ile tek runtime component'e taşır |
| `V1LaunchTelemetryTargetsSO.cs` + `V1LaunchTelemetryTargets.asset` | Spawn/Economy/Combat/Council/Meta için 19 provider-independent launch review bandı, minimum cohort, canonical source event ve SHA-256 contract fingerprint'i; runtime tuning'i otomatik değiştirmez |
| `MobilePopulationEconomySystem.cs` + `MobilePopulationArrivalUtility.cs` + `WorkerAllocationUtility.cs` + `WorkerVisualRepresentationUtility.cs` + `MobileBedCapacityUtility.cs` + `SurvivorArrivalVisualSystem.cs` | Target ratio auto-allocation/cap overflow, temsili worker density ve exact weight; tuning-driven purchased bed state ve owned-capacity fiyat eğrisi; Dawn accepted growth + tek Food transaction'ı; mevcut VillagerWorker prefabıyla sağdan Wall arkasına yürüyen, en fazla 15 entity'lik transient arrival sunumu hazır |
| `MobileWorkerBuildingUpgradeUtility.cs` + `MobileWorkerBuildingUpgradeState` | Dört kaynak için bağımsız CAP/EFF seviyeleri; tuning-driven Wood+Iron base maliyetleri ve ortak exponential growth, `+10` cap, additive `+10%` base üretim ve int-safe maliyet reddi |
| `ArcherCapacityUtility.cs` + `GameManager.SpawnArcher` + `MarketUI.cs` | Basic/Rapid/Frost ortak `1000` entity cap'i; buy öncesi ve merkezi spawn guard'ı, Council/meta/restore sınırı, `ARMY CAP/MAX` feedback'i |
| `WorkerLogisticsMovementSystem.cs` + `SpriteSheet.shader` + `Villager.mat` | Ayrı Idle/Walk/Work/Celebrate atlas seçimi; resource cargo, Dusk/Night lantern ve weight-scaled hub delivery pulse |
| `ArcherFormationV1.asset` + `ArcherFormationUtility.cs` + `MobileCastleArcherTilePlacement.cs` | Version'lı exact 40 tile; tile+slot seeded 25 diamond nokta, minimum mesafe, layer-fill 1000 kapasite ve tam gizmo preview |
| `ArcherShootSystem.cs` + `ArcherTargetingUtility.cs` | Persistent coarse spatial nearest query, stable tie-break, uçuşta/yeni ok incoming damage reservation; başarılı projectile başına finite stoktan tam `1` tüketim |
| `ArrowEconomyUtility.cs` + `GameManager.cs` + `ArrowSupplyUI.cs` | Sabit oranlı Wood refill, kısmi kapasite, Buy Max, Wood+Iron CAP/EFF yatırımı, Current/Capacity HUD ve exact save v8 runtime owner'ları |
| `HeartPurchaseService.cs` + `HeartEffectPipeline.cs` + `GameManager.HeartRuntime.cs` | Grave Essence-only +1/+10/Buy Max transaction, bulk-safe long fiyat, double actual effect, authored soft-cap, exact Keystone partner lock ve live archer/Wall/worker/Arrow/Fireball adapter'i |
| `TechNodeDefinitionSO.cs` + `TechTreeCatalogSO.cs` | Sabit catalog/reveal/cost/effect model |
| `HeartScreenUI.cs` + `SimulationPauseService.cs` + `TechTreeViewController.cs` | Hidden-safe fullscreen generated graph, compass layout, GE quote/effect/Keystone UI, pan/zoom ve nested exact full-simulation pause |
| `TechTreeUI.cs` | Legacy sabit catalog UI; aktif NewGameScene owner'ı değil |
| `CouncilRegularSchedule.cs` + `CouncilComposer.cs` + `CouncilContentPolicy.cs` + `CouncilEventUI.cs` | Exact Day 3/6/9 cadence + curated deterministic card infrastructure + fail-closed Heart/Meta role/content boundary |
| `MetaRewardTuning.cs` + `MetaProgression.cs` + `MetaUpgradeSO.cs` | Catalog-owned diminishing death reward, receipt-fixed exact quote, separate meta save ve audited permanent costs/effects |
| `CombatFeedbackBridge.cs` | VFX/audio pools ve rate limiting altyapısı |
| `Assets/Tests/EditMode` + `Assets/Tests/PlayMode` | V1 contract ve gerçek scene/runtime regression testleri |

---

## 22. Tracker Güncelleme Protokolü

1. Her işe başlamadan `Aktif paket` ve `Aktif iş` güncellenir.
2. İlgili mevcut owner yeniden okunur; tracker'daki current truth bayatsa düzeltilir.
3. İş kapsamı ilgili package checklist'inden seçilir; paket dışı ek özellik alınmaz.
4. Uygulama ve doğrulama tamamlandığında checkbox/statü güncellenir.
5. Yeni fark bulunursa ilgili package comparison tablosuna ve gerekiyorsa risk register'a eklenir.
6. Owner kararı gereken konu Bölüm 20'ye eklenir; varsayımla kodlanmaz.
7. İş kapanınca aşağıdaki çalışma günlüğüne kayıt düşülür.

---

## 23. Çalışma Günlüğü

| Tarih | İş | Sonuç | Doğrulama |
|---|---|---|---|
| 2026-07-12 | Tracker 2.0 tam kapsam yeniden yazımı | 40 sayfalık Blueprint, canlı Unity scene/prefab ve repo owner'ları aynı takip belgesinde eşlendi | PDF text audit + Unity MCP + read-only code/SO/scene audit |
| 2026-07-12 | V1 Blueprint/project gap audit | Package A'nın kısmen tamamlandığı; sıradaki işin exact save/Continue olduğu belirlendi | PDF, code, subscene ve Unity MCP |
| 2026-07-12 | Tek Wall runtime dönüşümü | Damage, Game Over, authoring, repair, save defense alanı ve runtime HUD Wall'a çekildi | Statik kod denetimi; Unity testleri henüz çalıştırılmadı |
| 2026-07-12 | `DW-A-SAVE` exact run snapshot | Schema v3, exact cycle/wave/RNG/combat/Council/ability snapshot, Main Menu ve app-quit save, aynı-an Continue, ölüm receipt'i ve idempotent meta ödülü tamamlandı | Unity compile: 0 error; EditMode 18/18; exact Continue PlayMode 1/1 |
| 2026-07-12 | `DW-A-UPKEEP` pasif tüketim kaldırma | Population Food ve Fletcher Wood rate'leri V1 castle loop'ta kapatıldı; ResourceTick seviyesinde savunma sınırı eklendi | Unity compile: 0 error; EditMode 18/18; PlayMode 2/2 |
| 2026-07-12 | `DW-A-REPAIR` Stone-only phase gate | Normal repair yalnız Day/Dusk ve yalnız Stone olacak şekilde tek GameManager owner'ında düzeltildi; UI kilit metinleri eşlendi | Unity compile: 0 error; EditMode 19/19; PlayMode 3/3 |
| 2026-07-12 | `DW-A-TUNING` runtime tuning owner | Profile/Authoring precedence tek resolver'a çekildi; Baker ve live tuner aynı mapping/sample kodunu kullanıyor | Unity compile: 0 error; EditMode 22/22; PlayMode 4/4 |
| 2026-07-12 | `DW-A-LEGACY` Gate/Core exclusion | Legacy Gate/Core data ve UI referansları dormant tutuldu; bütün aktif sonuç yolları tek Wall'a kilitlendi | Unity compile: 0 error; targeted EditMode 2/2; targeted PlayMode 1/1 |
| 2026-07-12 | `DW-B-CYCLE` Blueprint phase rhythm | Active cycle ve initializer değerleri Day 30 / Dusk 5 / Night 20 / Dawn 5 olarak eşlendi | Unity compile: 0 error; EditMode 24/24; PlayMode 6/6 |
| 2026-07-12 | `DW-B-STATS` quantity-only difficulty | Enemy HP/damage/speed progression utility seviyesinde kaldırıldı; baskı count/batch/interval kanallarında kaldı | Unity compile: 0 error; EditMode 25/25; PlayMode 7/7 |
| 2026-07-12 | `DW-B-SPECIAL` special nights removal | Blood Moon seed, multiplier, flag restore ve runtime warning zinciri V1'de dormant hale getirildi | Unity compile: 0 error; EditMode 25/25; PlayMode 8/8 |
| 2026-07-12 | `DW-B-FLOW` spawn budget & backlog | Day tabanı/phase multiplier ayrıldı; cap altındaki her interval explicit saved backlog'a dönüştü ve kontrollü drain edildi | Unity compile: 0 error; EditMode 28/28; PlayMode 9/9 |
| 2026-07-12 | `DW-B-MOAT` dormant moat isolation | Moat slow/damage, tech ve meta yolları V1 core loop'tan ayrıldı; legacy content silinmeden catalog dışında ve runtime-neutral tutuldu | Unity compile: 0 error; EditMode 30/30; PlayMode 10/10 |
| 2026-07-12 | `DW-B-ENEMY` single enemy catalog contract | `zombie_basic` için tek catalog/definition owner kuruldu; prefab, base stat, XP ve pool metadata bake edilip type branch olmadan gerçek spawn'a bağlandı | Unity compile: 0 error; EditMode 33/33; PlayMode 11/11 |
| 2026-07-12 | `DW-B-POOL` expandable enemy pool | Catalog prewarm/expand metadata gerçek inactive rezerve bağlandı; spawn rent, ölüm return, Continue reuse ve projectile generation guard tamamlandı | Unity compile: 0 error; EditMode 34/34; PlayMode 12/12 |
| 2026-07-12 | `DW-B-SCALE` 10K runtime gate | Gerçek NewGameScene'de 10K pool, HUD/feedback, profiler telemetry, Fireball toplu return ve Continue doğrulandı; `126,42-131,95 ms` death peak ve `~20,7 KB/frame` GC blocker kaydedildi | Targeted PlayMode 1/1; full EditMode 34/34; full PlayMode 13/13 |
| 2026-07-13 | `DW-B-SCALE-OPT` death/allocation pass | Pool return Burst-parallel reset + bulk commit oldu; death SFX ve death-animation ECB fan-out kaldırıldı; HUD/market/worker UI steady allocation owner'ları cache'lendi | 10K P95 `10,55-11,16 ms`; death peak `79,13-83,72 ms`; project allocation `11,6 B/frame`; EditMode 34/34; PlayMode 13 pass + 1 explicit skip |
| 2026-07-13 | `DW-B-SCALE-OPT` Player/save/render gate | Exact snapshot compact JSON oldu; Editor-only sanılan draw sayısı Player'da doğrulandı ve archetype topology çıkarıldı; Editor araçları Player assembly'sinden ayrıldı | Player P95 `6,97 ms`; `535` draw call; `202` chunk x `50`; save/restore `52,58 / 86,19 ms`; snapshot `4.240.003 B`; EditMode 35/35; PlayMode 13+1; Player-targeted 1/1 |
| 2026-07-13 | `DW-C-ALLOC` persistent worker targets | Actual count + dört target ratio + cap/idle/checkpoint state'i kuruldu; yalnız yeni nüfus deterministik dağılıyor, cap overflow idle kalıyor; schema v4 ve v3 migration eklendi | Unity compile: 0 error; EditMode 40/40; full PlayMode 15/15 |
| 2026-07-13 | `DW-C-TARGET-UI` worker target controls | Worker drawer +1%/+10%/+100% ve exact 0-100 input ile target share owner'ına bağlandı; diğer hedefler deterministik ölçekleniyor, mevcut actual worker değişmiyor; generated HUD prefabı ve scene bindingleri yenilendi | Unity compile: 0 error; EditMode 43/43; PlayMode 15 pass + 1 explicit skip; game-view visual QA |
| 2026-07-13 | `DW-C-VIS-DENSITY` representative worker visuals | Actual worker truth save/production state'inde korundu; Low `1-12` 1:1, Medium `13-60` seyreltilmiş, High `61+` daha güçlü seyreltilmiş ve resource başına `32` visual cap uygulandı; GameManager yalnız temsil sayısı değişince sync ediyor | Unity compile: 0 error; EditMode 58/58; PlayMode 16 pass + 1 explicit skip; başlangıç 53 actual -> 45 visual game-view QA |
| 2026-07-14 | `DW-C-WORKER-FEEDBACK` allocation-synced worker feedback | Ayrı Idle/Walk/Work/Celebrate atlasları gerçek route state'ine bağlandı; actual count visual weight'lere exact dağıtıldı; küçük resource cargo, Dusk/Night lantern ve weight-scaled hub delivery pulse tek DOTS shader pass'inde eklendi | Unity compile: 0 error; EditMode 61/61; PlayMode 17 pass + 1 explicit skip; Day/Night game-view QA |
| 2026-07-14 | `DW-C-BEDS` purchased House bed state | `60` base + run içinde satın alınmış yatak state'i, int-safe uncapped utility, geçici sabit `100 Wood/yatak` transaction API'si ve exact save `v5` kuruldu; v3/v4 migration mevcut nüfusu koruyor. Dawn arrival ve legacy `999999` capacity bağlantısı sonraki işlerde açık bırakıldı | Unity compile: 0 error; EditMode 65/65; PlayMode 18 pass + 1 explicit skip; Unity console 0 error |
| 2026-07-14 | `DW-C-BED-COST` owned-capacity bed curve | Owner-onaylı `ceil(100 × (1 + max(0, ToplamYatak - 60) / 25)^2)` Wood eğrisi kuruldu; bulk alım ardışık fiyatları topluyor, base/meta yataklar sahipliğe dahil oluyor ve temsil edilemeyen int transaction taşırılmadan reddediliyor. Gameplay hard max eklenmedi; generic Inspector/SO tuning ayrı iş olarak açık | Unity compile: 0 error; EditMode 69/69; PlayMode 18 pass + 1 explicit skip; Unity console 0 error |
| 2026-07-14 | `DW-C-DAWN-BUDGET` bed + Food survivor budget | Dawn isteği `15`, toplam yatak boşluğu ve owner-onaylı `1 Food/survivor` bütçesinin minimumuyla sınırlandı; gerçek accepted count population/auto-allocation/toast akışına bağlandı, mobile `999999` capacity aynası kaldırıldı. Food bu pakette bilerek harcanmadı; sıradaki iş `DW-C-FOOD-SPEND` | Unity compile: 0 error; EditMode 74/74; PlayMode 19 pass + 1 explicit skip; targeted EditMode 5/5; targeted PlayMode 1/1; Unity console 0 error |
| 2026-07-14 | `DW-C-FOOD-SPEND` one-time arrival Food transaction | Dawn bütçesinin `RequiredFood = accepted × 1` sonucu population artışıyla aynı ECS transaction'ında `ResourceData.Food` stokundan düşüldü; persistent cycle/wave marker'ları aynı Dawn ve exact Continue sonrasında çift harcamayı engelliyor | Unity compile: 0 error; targeted PlayMode 2/2; EditMode 74/74; PlayMode 19 pass + 1 explicit skip; Unity console 0 error |
| 2026-07-14 | `DW-C-ARRIVAL-VISUAL` Dawn survivor walk-in | Gerçek accepted count mevcut `VillagerWorker` prefabıyla en fazla 15 transient görsele exact temsil edildi; survivor'lar sağ battlefield'daki farklı lane/gecikmelerden Wall arkasına yürüyor, resource worker lojistiğine girmiyor, varışta temizleniyor ve exact Continue tamamlanmış Dawn'ı yeniden oynatmıyor | Targeted EditMode 3/3; targeted PlayMode 2/2; full EditMode 77/77; full PlayMode 19 pass + 1 explicit skip; gerçek NewGameScene Game View 15-survivor QA; Unity console 0 error |
| 2026-07-14 | `DW-C-BUILDING-SCALE` independent worker building investments | Dört hazır worker binasının CAP/EFF seviyeleri bağımsız state'e bağlandı; her alım Wood+Iron harcıyor, owner-onaylı base maliyetler `ceil(base × 1.35^level)` ile büyüyor, CAP `+10` slot ve EFF base üretime additive `+10%` veriyor. Workers drawer'da sekiz buton, int-safe cost limit ve exact save v6 tamamlandı | Targeted EditMode 13/13; targeted PlayMode 2/2; full EditMode 82/82; full PlayMode 20 pass + 1 explicit skip; 1280×720 gerçek Game View QA; Unity console 0 error |
| 2026-07-14 | `DW-C-TUNING-SURFACE` profile-driven economy price curves | Bed base/interval ve worker CAP/EFF Wood/Iron base + ortak growth `DifficultyProfileSO`/Difficulty Tuner'a taşındı; Baker/live Apply tek `MobileEconomyPriceTuning` owner'ını yazıyor, GameManager bütün fiyat API'lerinde bunu tüketiyor. Onaylı default'lar korundu; invalid değerler sanitize, int dışı alımlar reddediliyor; Package C kapatıldı | Targeted EditMode 17/17; targeted PlayMode 2/2; full EditMode 85/85; full PlayMode 21 pass + 1 explicit skip; DefaultDifficulty live asset audit; Unity console 0 error |
| 2026-07-14 | `DW-D-ARCHER-CAP` common 1000 archer guard | Basic/Rapid/Frost toplamı tek `ArcherCapacityUtility` owner'ında `1000` ile sınırlandı; satın alma harcamadan önce, bütün aktif yollar merkezi spawn'da tekrar kontrol ediliyor. Council/meta/restore aşırı miktarları cap'te duruyor, dormant Barracks rezerve slotlarla guard'ı bypass edemiyor; drawer `ARMY CAP/MAX` gösteriyor | Targeted EditMode 7/7; targeted PlayMode 1/1; full EditMode 92/92; full PlayMode 22 pass + 1 explicit skip; Unity console 0 error |
| 2026-07-14 | `DW-D-RETRAIN` Basic -> Rapid/Frost retrain + type cost curve | Unlock edilmiş Rapid/Frost satırında tek bir Basic entity yeni tip/stat/tint ile yerinde dönüştürülüyor; entity, toplam archer, population/cap ve transform korunuyor. Buy ve retrain fiyatı hedef tür sayısına göre `ceil(base × (1 + count / interval)^exponent)` ile büyüyor; base/interval/exponent archer SO datasında. Dynamic HUD template'ine ayrı retrain kontrolü ve idempotent prefab repair eklendi; ayrı type upgrade/direct unlock yüzeyi kapalı kaldı | Targeted EditMode 2/2; targeted PlayMode 1/1; full EditMode 94/94; full PlayMode 23 pass + 1 explicit skip; Unity console 0 error |
| 2026-07-14 | `DW-D-FORMATION` version'lı 40 x 25 stable archer placement | `ArcherFormationV1.asset` exact 40 canonical `outside` tile'ı taşır; her tile'da coordinate+slot seeded best-candidate algoritma 25 inset diamond noktayı minimum mesafeyle üretir, global sıra layer-fill olur. Scene binding/setup repair ve tam 1000 gizmo eklendi; exact save v7 yalnız formation version + type count tutar, v6 migration ve Continue aynı noktaları yeniden kurar. Tam test turunda bulunan 10K sıra bağımlılığı gameplay değiştirilmeden test önkoşul/cleanup izolasyonuyla kapatıldı | Unity compile: 0 error; targeted EditMode 4/4; targeted Formation PlayMode 1/1; full EditMode 98/98; full PlayMode 24 pass + 1 explicit skip; Unity console 0 error |
| 2026-07-14 | `DW-D-TARGETING` spatial nearest-target + incoming load | `ArcherShootSystem` persistent `2.0` cell target grid'ini Burst-parallel kuruyor; tek deterministic Burst shoot job yaşayan/death-state olmayan nearest unsaturated target'ı seçiyor. Uçuşta ve aynı frame yaratılan ok damage'i target HP'ye rezerve ediliyor; Basic/Rapid/Frost aynı policy'de, pool generation mismatch retarget yapmıyor. Gerçek Formation V1 1K archer + pooled 10K enemy birleşik ürün testi geçti | Targeted EditMode 4/4; targeted targeting PlayMode 1/1; 1K×10K PlayMode 1/1, Editor P95 `9,66 ms`; full EditMode 102/102; full PlayMode 26/26; Unity console 0 error |
| 2026-07-14 | `DW-D-PROJECTILE` arrow pool + Burst-safe lifetime | Ok atışındaki per-projectile instantiate/destroy kaldırıldı; enableable `ArrowTag`, `1024` prewarm, `256` batch expand, deferred return ve `5s` Burst lifetime kuruldu. İsabet/timeout/invalid target/generation mismatch tek pool return yolunda, Continue kalan lifetime'ı exact restore ediyor ve restart aktif okları rezerve döndürüyor | Unity compile: 0 error; targeted EditMode 2/2; targeted projectile/targeting/stale-generation PlayMode 3/3; 1K×10K pool telemetry `1536 total / 3000 rent / 2895 return`, P95 `12,50 ms`; full EditMode 103/103; full PlayMode 26 pass + 1 explicit skip; explicit profiler 1/1; Unity console 0 error |
| 2026-07-14 | `DW-D-AMMO` finite Arrow supply + instant refill | Unlimited bypass kaldırıldı; başarılı pooled projectile tam `1 Arrow` tüketiyor, stok `0` iken atış duruyor. Wood ile sabit oranlı +1/+5/Buy Max refill, kısmi dolumda israf etmeyen fiyat, Wood+Iron CAP/EFF yatırımları, profile tuning, compact tek satır HUD ve exact save v8 kuruldu. Legacy Fletcher/queue aktif akış dışında kaldı; 10K fixture finite stokla güncellendi | Unity compile: 0 error; targeted EditMode 16/16; targeted ammo/targeting PlayMode 3/3; 10K targeted 1/1; full EditMode 109/109; full PlayMode 28 pass + 1 explicit profiler skip; Game View QA; Unity console 0 error |
| 2026-07-14 | `DW-E-DATA` Heart data model + run-only Grave Essence | `HeartNodeDefinitionSO` dört Blueprint node tipi, tags/effects/rarity/depth/cost/conflict verisini source-only taşır; `GeneratedRunGraph` seed/version/node/edge/reveal/level/lock state'ini asset referanssız tanımlar. Grave Essence ayrı ECS singleton, tek Heart harcama kapısı ve exact save v9'a bağlandı; v8 migration `0`, Restart ve ölüm silme matrisi testlendi. Legacy tech graph/purchase bu pakette değiştirilmedi | Unity compile: 0 error; targeted EditMode 20/20; targeted PlayMode 1/1; full EditMode 119/119; full PlayMode 30/30; Unity console 0 error |
| 2026-07-14 | `DW-E-GRAPH` deterministic Castle Heart graph generator | `HeartNodeCatalogSO` authored havuzu, stable seed/attempt RNG kullanan dört yön generator ve fail-closed validator eklendi. Rapid/Frost/Fireball/Wall guarantee'leri, branch repeatable sink'leri, rarity/depth filler, forward cross-link ve tam Keystone çiftleri sentetik catalog testleriyle kilitlendi; reveal anında RNG yok. Owner onayı bekleyen production node/maliyet/Keystone içeriği üretilmedi; legacy runtime değiştirilmedi | Unity compile: 0 error; targeted EditMode 9/9; full EditMode 128/128; full PlayMode 29 pass + 1 explicit profiler skip; Unity console 0 error |
| 2026-07-14 | `DW-E-REVEAL` hidden graph reveal + player information core | `HeartGraphRevealService` root komşularını initial reveal ediyor ve yalnız ilk `0 -> N` satın alımında outgoing komşuları açıyor; reveal anında RNG yok. `HeartGraphPresentationBuilder` hidden node Id/title/effect bilgisini safe branch/depth slotlarına redakte ediyor, numeric effect için E4 resolver'ını zorunlu tutuyor ve görünür Keystone karşı başlık/kapanacak slot bilgisini internal partner Id'sini sızdırmadan; pre/post-purchase lock durumuyla taşıyor. Exact graph save E6, gerçek numeric resolver E4 ve prefab rendering E5'e açık bırakıldı; legacy runtime değiştirilmedi | Unity compile: 0 error; targeted EditMode 8/8; full EditMode 136/136; full PlayMode 29 pass + 1 explicit profiler skip; Unity console 0 error |
| 2026-07-14 | `DW-E-PURCHASE` Grave Essence purchase + actual effect pipeline | `HeartPurchaseService` graph/catalog/visibility/lock/type preflight'inden sonra yalnız GameManager'ın Grave Essence kapısını kullanıyor; exact +1/+10/Buy Max maliyeti arithmetic-series + binary search ile hesaplıyor. Unlock/repeatable/evolution/Keystone state geçişleri, exact partner exclusion ve ilk bulk reveal tek commit'te. `HeartEffectPipeline` long/double büyük değer, actual baseline, archer/Wall/worker/Arrow/Fireball target'ları, authored soft-cap ve current/after/delta resolver'ını aynı raw state'ten üretiyor. Production catalog veya balance değeri eklenmedi; live sink/UI E5, exact replay E6 | Targeted EditMode 14/14; full EditMode 150/150; ilk full PlayMode'da 10K projectile assertion bir kez flake etti, targeted 1/1 ve full rerun 29 pass + 1 explicit profiler skip; Unity console 0 error |
| 2026-07-14 | `DW-E-UI` Castle Heart screen + full simulation pause | `HeartScreenUI` hidden-safe generated graph presentation'ını Army/Defense/Production/Heart-Magic compass layout, actual current/after/delta, exact GE quote, Keystone conflict ve `+1/+10/MAX` ile aktif prefab/sahneye bağladı. `GameManager.HeartRuntime` live archer/Wall/worker/Arrow/Fireball baseline/sink adapter'ını kurdu; Arrow Heart bonusları paid level'lardan ayrıldı. Lease tabanlı `SimulationPauseService` time scale ve DOTS `SimulationSystemGroup` state'ini nested owner'larla exact durdurup geri yüklüyor; `PauseMenuUI` aynı owner'a taşındı. Aktif HUD'dan legacy `TechTreeUI` kaldırıldı; Heart paneli override-sorted modal Canvas olarak HUD canvas'larının üstüne alındı. Production catalog/balance/Evolution içeriği owner onayı olmadan üretilmedi ve null catalog açık hata veriyor | Unity compile: 0 error; targeted EditMode 8/8; full EditMode 158/158; full PlayMode 29 pass + 1 explicit profiler skip; active scene HeartScreenUI 1 / TechTreeUI 0; Game View modal QA sırasında `Time.timeScale = 0`, `SimulationSystemGroup.Enabled = false`; Unity console 0 error |
| 2026-07-14 | `DW-E-SAVE` exact Castle Heart graph persistence + deterministic Continue replay | `RunSaveState` v10'a çıkarıldı; `HasHeartGraph` discriminator'ı, graph/catalog version, seed, node/edge, hidden/reveal, level ve Keystone lock state'i exact JSON'a bağlandı. `HeartGraphPersistenceUtility` deep clone, structural/runtime validation, catalog mismatch fail-closed ve deferred effect replay kurdu. Continue source catalog'dan reroll etmiyor; Arrow current final effective capacity ile tek kez clamp ediliyor. v9 eksik graph uydurmadan null-state migrate ediyor. Production catalog/content üretilmedi | Unity compile: 0 error; targeted EditMode 25/25; targeted exact Continue PlayMode 2/2; full EditMode 162/162; full PlayMode 30 pass + 1 explicit profiler skip; Unity console 0 error |
| 2026-07-15 | `DW-F-SCHEDULE` exact 3/6/9 regular Council cadence | `CouncilRegularSchedule` regular kartı yalnız Dawn Day `3,6,9,12...` ve aynı gün tek kez açan owner oldu; chance/pity/cooldown regular akıştan çıkarıldı, legacy catalog alanları yalnız serialized uyumluluk için gizlendi. Save schema v11'e çıktı; `LastRegularCouncilDay` ile `HasActiveCouncilEvent` discriminator'ı exact state'e bağlandı. v10 migration yalnız gerçekten üretilmiş event'i handled sayıyor; chance fail scheduled kartı yutmuyor. Emergency type/trigger/content üretilmedi ve owner gate açık bırakıldı | Unity compile: 0 error; targeted EditMode 36/36; targeted NewGameScene PlayMode 1/1; full EditMode 185/185; full PlayMode 31 pass + 1 explicit profiler skip; Unity console 0 error |
| 2026-07-15 | `DW-F-GUARDS` canonical Council effect guardrails | `CouncilEffectGuardUtility` population, free archer ve next-night count sınırlarının saf owner'ı oldu. Council population gain boş yatak + Food ile sınırlandı, accepted kişi başına Food tek transaction'da harcandı ve capacity büyütme bypass'ı kaldırıldı. Free archer gerçek idle population tüketiyor ve ortak 1000 cap'i koruyor. Defense yalnız Wall'a, horde etkisi yalnız bounded count multiplier'a yazıyor; exact kart sonucu tam uygulanamıyorsa seçenek preflight'ta kilitleniyor. Emergency trigger listesi Blueprint gereği owner gate'inde açık kaldı | Unity compile: 0 error; targeted EditMode 3/3; targeted NewGameScene PlayMode 1/1; full EditMode 188/188; full PlayMode 32 pass + 1 explicit profiler skip; Unity console 0 error |
| 2026-07-15 | `DW-F-EXACT` live exact Council option quote + authoritative decision window | `CouncilOptionPresentationUtility` iki seçeneğin authored eylem başlığını live ECS/resource/population/archer/Wall context'iyle birleştirerek tam sayısal sonuç, tek seferlik maliyet ve uygulanabilirlik sebebini aynı quote'tan üretiyor. UI aynı quote ile label ve interactability'yi yeniliyor; Dawn+Day karar süresi wall-clock yerine `ContinuousSiegeCycleData` üzerinden `CouncilTimerText` ve fill'e bağlandı. Generated HUD prefabı, aktif sahne binding'i, font/material ve idempotent repair menüsü tamamlandı | Unity compile: 0 error; targeted EditMode 6/6; targeted NewGameScene PlayMode 1/1; full EditMode 194/194; full PlayMode 32 pass + 1 explicit profiler skip; Unity console 0 error |
| 2026-07-15 | `DW-F-CONTEXT` context-aware Council selection + curated memory chains | `CouncilComposer` resource scarcity/abundance'ı stock-per-production dakikasından koruyor; template director artık iki option'ın authored atomlarını okuyarak B-tarafı Wall heal bağlamını da ağırlığa katıyor. Recent template, fresh alternatif varsa havuzdan tamamen çıkarılıyor; bütün uygun adaylar recent ise scheduled kart için deterministik fallback kullanılıyor. `CouncilEventCatalogSO.CuratedChains`, source template + branch + flag + target dörtlüsünü merkezi allowlist yaptı; composer ve `GameManager` onaysız chain'i fail-closed reddediyor. Production catalog'a yalnız mevcut Refugees ve Merchant follow-up bağları merge edildi; yeni içerik üretilmedi | Unity compile: 0 error; targeted Council EditMode 14/14; targeted Council PlayMode 2/2; full EditMode 201/201; full PlayMode 33 pass + 1 explicit profiler skip; Unity console 0 error |
| 2026-07-15 | `DW-F-BOUNDARY` Council role isolation + content ownership gate | `CouncilContentPolicy` yalnız run resources/production/worker cap/population/free Basic archer/Wall/count effect domain'lerini allowlist yaptı; Heart Grave Essence/node upgrade ve Meta progression Council rolü dışında kaldı. Catalog atom/contrast/branch recipe validation, composer fail-closed gate, live quote/choice preflight ve active Council Continue preflight aynı policy'ye bağlandı. Production 9 template/11 atom teknik gate'ten geçiyor ancak launch content review owner kararı beklemeye devam ediyor; Emergency content üretilmedi | Unity compile: 0 error; targeted Council EditMode 18/18; targeted Council/Continue PlayMode 4/4; full EditMode 205/205; full PlayMode 35 pass + 1 explicit profiler skip; Unity console 0 error |
| 2026-07-15 | `DW-F-SCOPE` regular-only Council authority cleanup | Owner kararıyla Emergency Council V1 kapsamından tamamen çıkarıldı; ikinci meeting type, trigger veya rarity yolu eklenmedi. Blueprint DOCX/PDF, Council architecture/setup sözleşmeleri ve tracker yalnız Day `3/6/9...` regular Council'ı otorite kabul edecek biçimde güncellendi. Dört iptal edilmiş açık checkbox kaldırıldığı için tracker paydası `445 -> 441` değişti; sıradaki iş `DW-F-SAVE` oldu | Blueprint structural scan: 0 cancelled-path hit; PDF 40/40 page render QA clean; tracker count `288/441`; runtime davranış değişikliği yok |
| 2026-07-15 | `DW-F-SAVE` regular Council exact state + Continue audit | `RunSaveState`/`SaveRunSnapshot`/`TryRestoreRunFromCheckpoint` alanları capture-restore simetrisiyle denetlendi. Active composed payload, regular handled day, run salt, flags, recent/one-shot memory ve Council cap state'i; çözülmüş branch flag'i ile temp-production/next-night multiplier+expiry state'i exact Continue PlayMode akışında kilitlendi. Aktif kart reroll edilmeden dönüyor, çözülmüş kart yeniden açılmıyor ve future Day 6 regular schedule devam ediyor. Gerçek kayıt sözleşmesiyle çelişen çağrısız legacy Dawn checkpoint hook'u kaldırıldı; yeni Council içeriği üretilmedi | Unity compile: 0 error; targeted EditMode 1/1; targeted PlayMode 2/2; full EditMode 205/205; full PlayMode 37 pass + 1 explicit profiler skip; Unity console 0 error; tracker `290/441` |
| 2026-07-15 | `DW-F-CONTENT` regular Council launch content + effect budget | Production 9 template human-review ile staged Day 3 ekonomi / Day 6 population-savunma / Day 9 gece riski havuzuna ayrıldı. Quarry'nin bedava permanent worker-cap ödülü 2 günlük production boost'a çevrildi; `cap_bonus` compatibility için serialized kaldı fakat launch recipe gate'inden çıkarıldı. Strange Bonfires normal production-scaled loot'a sabitlendi; Refugees/Merchant transaction metinleri gerçek effect ile eşlendi. Curated source event'ler tetikleyici branch'e kadar tekrar edebilir, flag sonrası kosuda emekli olur. Follow-up'lar `DefenseVsProduction` ve `ResourceVsPopulation` recipe'leriyle ana cache kartından ayrıldı; bütün template'ler en az iki authored body varyantı taşıyor | Unity compile: 0 error; production 5.400 compose budget/token/content gate; targeted Council EditMode 22/22; full EditMode 209/209; full PlayMode 37 pass + 1 explicit profiler skip; expected fail-closed test logları dışında Unity console 0 error; tracker `295/441` |
| 2026-07-15 | `DW-H-DEATH-RECEIPT` death-only durable reward transaction | Ölüm akışı journal-first yapıldı: same-volume temp + flush + replace kullanan `AtomicJsonFile`, unique `RunDeathReceipt`, `RewardedRunIds` idempotency ve meta durable olmadan receipt silmeme sözleşmesi kuruldu. Matching veya corrupt receipt marker yaşayan snapshot'ı fail-closed geçersiz kılıyor; `SaveRunSnapshot()` taze ECS lethal state'ini capture öncesi işleyerek application quit sırasında ölü koşunun yeniden yazılmasını engelliyor. GameManager/MainMenu startup recovery process restart sonrasında yarım transaction'ı tamamlıyor | Unity compile: 0 error; targeted EditMode 18/18; targeted PlayMode 3/3; Unity console'da gerçek error 0; tracker `325/441` |
| 2026-07-15 | `DW-H-META-SCHEMA` canonical meta v3 schema + migration guard | `MetaProgressState v3`, Souls/istatistik/upgrades/son 128 reward receipt yanında stable `UnlockedPoolIds` ve `TutorialFlags` sahiplerini kurdu. v1 ve v2 açık zincirle currency, upgrade ve receipt kaybetmeden v3'e taşınıyor; unknown upgrade Id'leri korunuyor. Future version veya corrupt JSON artık boş state'e sessiz map edilip üzerine yazılmıyor: `LoadStatus` ve `CanPersist` bütün meta mutation/reward write yollarını fail-closed kilitliyor. Pool/tutorial mutation'ları ile upgrade purchase disk write başarısızsa in-memory rollback yapıyor. Run v3-v12 explicit migration guard'ı ayrıca denetlenerek stale tracker satırı kapatıldı; önceki death receipt process-restart test satırı kanıtıyla eşlendi | Unity compile: 0 error; new schema EditMode 7/7; related EditMode regression 20/20; targeted PlayMode 2/2; Unity Console'da gerçek error 0; tracker `329/441` |
| 2026-07-15 | `DW-H-META-BOUNDARY` death-only shop + run graph isolation | Meta purchase otoritesi `GameManager`a taşındı; aktif koşu, toplanmamış/durable olmayan ölüm ve fail-closed meta state satın alamıyor. Canonical catalog asset identity aynı Id'li spoof definition'ı reddediyor; persistence mutation API'si internal kaldı. `MetaUpgradePolicy` yalnız run-start/aggregate effect'lerini allowlist yaptı; `StartingTechLevel`, `TechNodeId`, runtime case ve dormant `Meta_start_moat.asset` kaldırıldı. Exact Continue'un saved tech replay'i `RestoreSavedTechNodeLevels` adıyla run-save sahibinde korundu; pool unlock state'i mevcut generated graph'a yazmıyor | Unity compile: 0 error; new boundary EditMode 5/5; schema+dormancy regression 9/9; death purchase PlayMode 1/1; exact Continue/death/Heart graph PlayMode regression 3/3; Unity Console 0 error; tracker `335/441` |
| 2026-07-15 | `DW-H-META-CATALOG` fixed launch catalog + incremental cost curves | Blueprint sabit listesi dört starting resource, Basic-only Archer, limitsiz starting beds, Wall HP, worker production, Arrow efficiency, Essence gain ve future node-pool unlock olarak 11 canonical assete çekildi; eski `Meta_archer_damage` effect/asset/runtime yolu kaldırıldı. Kaynak/yatak sink'leri `MaxLevel=0` limitsiz, bütün fiyatlar üstel ve saturating; Basic Archer ortak 1000 cap'ini koruyor. Arrow meta verimi paid/Heart level'dan ayrıldı, Essence yüzdesinin kesirli payı exact save v13'e eklendi, Continue başlangıç bonuslarını çift uygulamadan derived etkileri yeniden kuruyor. Node-pool seviyesi/Id'si/Souls aynı atomik transaction'da tek kez commit ediliyor | Unity compile: 0 error; targeted EditMode `50/50`; targeted PlayMode `2/2`; Unity Console gerçek error 0; catalog validation 0 problem; tracker `344/441` |
| 2026-07-15 | `DW-I-HUD-LEGACY` active HUD Gate/Core cleanup | Aktif HUD prefabındaki Gate/Core text, track ve fill objeleri kaldırıldı; `HUDController` ile scene setup binding sözleşmesi Wall-only hale getirildi. Connected `NewGameScene` instance'ındaki eski serialize referanslar temizlendi; Wall text/fill bağları korundu ve eski runtime hide guard'ına ihtiyaç kalmadı | Unity compile: 0 error / 0 warning; targeted EditMode 3/3; targeted PlayMode 1/1; MCP prefab ve canlı sahne denetiminde Gate/Core obje 0, Wall bindingleri sağlam; tracker `346/441` |
| 2026-07-15 | `DW-I-HUD-RESOURCES` compact top resource strip | Üst soldaki altı kartlık resource alanı `700 x 84` yerine `560 x 48` tek şeride, chip'ler `84 x 42` ölçüsüne çekildi. Wood/Stone/Iron/Food değerleri signed `/m` rate ile tek satıra alındı; altı label kaynak bazlı hafif renk kodu aldı. Population ve finite Arrow aynı şeritte kaldı; `ArrowChip` ammo panel toggle sahipliğini korudu | Unity compile: 0 error; targeted EditMode 2/2; MCP runtime binding audit: `ResourceBar 560 x 48`, single-line values, Arrow toggle `false -> true -> false`; 1920x1080 Game View görsel QA temiz; tracker `347/441` |
| 2026-07-15 | `DW-I-HUD-PHASE-AREA` minimal top-center phase area | Aktif HUD prefabındaki `384 x 106` phase panel top-center anchor'lı `340 x 78` sabit slota çekildi; day counter, phase title, `280 x 10` progress track, canlı fill/marker ve üç phase label binding'i korundu. Ham `DAY / DUSK / NIGHT` görsel dili sonraki owner-approved polish işine bırakıldı | Unity compile: 0 error; targeted EditMode 1/1; MCP runtime audit: `340 x 78`, top-center anchor, fill/marker senkron; 1920x1080 Game View görsel QA temiz; tracker `348/441` |
| 2026-07-15 | `DW-I-HUD-PHASE-POLISH` owner-approved Celestial Dial | Owner A/B/C mockup turunda `B - Celestial Dial` yönünü seçti. İlk işlevsel geçişten sonra owner görsel eşleşmenin yetersiz olduğunu belirtti; aktif HUD referans B oranlarına göre `290 x 68` gerçek pill gövde/flat kapak silueti, `DAY N` sayacı, 44 segmentli `178 x 44` renk yayı, crescent/dawn glyph'leri, küçük parlak orb ve düşük-alpha halo ile yeniden işlendi. Referansta olmayan divider ile büyük phase başlığı, ham DAY/DUSK/NIGHT label satırı ve linear fill player-facing kapatıldı. `A - Horizon Ribbon` karşılaştırma görseli ve geri dönüş prosedürüyle `DW_I_PHASE_HUD_PRESENTATION_DECISION.md` içinde arşivlendi | Unity compile: 0 error; targeted EditMode 2/2; MCP runtime audit `290x68`, `178x44`, divider inactive, halo `0.22`, marker motion clean; 1920x1080 Game View B-parity QA temiz; tracker `351/441` |
| 2026-07-15 | `DW-I-HUD-FORECAST` remove Horde forecast/pressure surface | Aktif HUD prefabındaki `HordePressurePanel` ve beş child yüzeyi Prefab Stage içinde kaldırıldı. `HUDController` serialized alanları, runtime hide guard'ı ve scene setup arama/başlık yolları silindi; mimari/setup dokümanları forecast'siz sözleşmeye çekildi. `HordePressure01` spawn, save ve gameplay yoğunluk sinyali olarak korunarak UI scope ile gameplay scope ayrıldı. `NewGameScene` serializer spillover'ı temiz `HEAD` sürümüne döndürüldü; sahne task sonunda değişmeden ve dirty olmadan kaldı | Unity compile: 0 error; new EditMode 2/2, related HUD EditMode 9/9; MCP Edit/Play audit: forecast object 0, controller field 0, CyclePanel/Wall/AbilityBar sağlam; Play Mode Console 0 error; tracker `352/441` |
| 2026-07-15 | `DW-I-HUD-ABILITIES` bottom-center ability bar audit + regression guard | Package G'de zaten tamamlanmış aktif sistem Package I sunum hedefiyle yeniden denetlendi. Aktif prefabın tek `496 x 90` bottom-center `AbilityBarPanel` taşıdığı; Fireball, Rally ve Emergency Repair slotlarının çakışmadan soldan sağa dizildiği; her slotta raycast kapalı vertical cooldown overlay bulunduğu doğrulandı. Canlı sahnede tek `SpellCastUI`, eksiksiz altı button/fill binding ve sıfır legacy `SpellUiRoot`/`SpellPanel` var. Yeniden UI üretmek yerine bu gerçek `HudAbilityBarPresentationTests` ile kilitlendi; architecture/setup dokümanlarına exact geometri ve fill contract'ı eklendi | Unity compile: 0 error; new presentation EditMode 2/2, presentation + ability rules EditMode 5/5; MCP Play audit: Fireball `45s`, Rally `60s`, Repair `120s` duration ve üç fill `0.5`; 1920x1080 overlay QA bottom-center temiz; final Console 0 error; tracker `354/441` |
| 2026-07-15 | `DW-I-HUD-WORKERS-HOUSING` bottom-left Workers + Housing surface | Aktif Worker drawer ayrı bir Housing controller/panel üretmeden ortak `Workers + Housing` yüzeyine dönüştürüldü. Toggle bottom-left `(24,28)` / `206x56`, panel bottom-left `(24,160)` / `980x382` oldu; panel canlı ability content'inin `17 px` üstünde kalıyor. Housing satırı population/total beds, free beds ve purchased beds aynalarını; toplam sahipliğe göre büyüyen exact Wood maliyetli `+1/+10/+100 Beds` alımlarını gösteriyor. Sahnedeki tek `WorkerEconomyDrawerUI` yeni kontrolleri isim sözleşmesiyle runtime çözüyor; aktif prefab doğrudan Prefab Stage'de güncellendi, `NewGameScene` değişmedi | Unity compile: 0 error; new presentation/binding EditMode 2/2; presentation + bed utility EditMode 10/10; exact bed purchase/Continue PlayMode 1/1; MCP Play audit: tek controller, altı Housing binding, drawer toggle `false -> true`, 1920x1080 görsel QA temiz; final Console 0 error; tracker `355/441` |
| 2026-07-15 | `DW-I-HUD-ARCHERS-HEART` bottom-right Archers + Castle Heart surface | Mevcut `MarketUI` ve `HeartScreenUI` owner'ları korunarak `DrawerToggleButton` kayan panelin dışına çıkarıldı. Sabit bottom-right dock'ta `ARCHERS` `(-190,28)` / `156x56`, `CASTLE HEART` `(-24,28)` / `156x56`; Archer drawer üstlerinde `(-24,160)` / `540x350` oldu. HUD ve yeni run açılışında drawer kapalı başlıyor; legacy `OpenOnWaveCompleted` davranışı korunuyor. Aktif prefab doğrudan Prefab Stage'de güncellendi, setup tool aynı geometriyi idempotent normalize ediyor ve `NewGameScene` içerik diff'i bırakılmadı | Unity compile: 0 error; new presentation/behavior EditMode 3/3; Ability/Workers/Heart regression EditMode 12/12; MCP Play audit: tek `MarketUI`, tek `HeartScreenUI`, drawer click `closed -> open`, Heart pause `1 -> 0 -> 1`; 1920x1080 kapalı/açık görsel QA temiz; final Console 0 error; tracker `356/441` |
| 2026-07-15 | `DW-I-HUD-EXCLUSIVE-DRAWER` single management drawer owner | Scene-owned `ManagementDrawerCoordinatorUI`, mevcut presentation ve transaction owner'larını değiştirmeden Workers/Housing, Archer Recruitment ve Arrow Supply yüzeylerini mutual exclusion altında topladı. Yeni bir yüzey açıldığında diğer ikisi anında kapanıyor; aktif owner kapanınca state `None` oluyor. Castle Heart fullscreen modal olduğu için coordinator kapsamı dışında ve mevcut pause sözleşmesini koruyor. Scene setup tool coordinator'ı idempotent ekliyor; aktif `NewGameScene` yalnız bu component override'ını taşıyor | Unity compile: 0 error; targeted + related EditMode 15/15; MCP Play audit: owner sayıları `1/1/1/1`, akış `None > WorkersHousing > ArcherRecruitment > ArrowSupply > WorkersHousing`, her adımda yalnız tek yüzey açık; 1920x1080 görsel QA temiz; Play/exit Console 0 error; tracker `357/441` |
| 2026-07-15 | `DW-I-HUD-COUNCIL-CARD` exact regular Council card + live decision fill | Mevcut regular-only Council sunum sahibi korunarak iki option'ın authored eylem başlığı, live exact effect/maliyet quote'u ve uygulanabilirlik durumu tek compact kartta doğrulandı. `CouncilTimerFill` hatalı Sliced tipten Horizontal/Left Filled tipe geçirildi; Dawn + Day karar penceresinin sayısal metniyle aynı authoritative cycle değerinden gerçekten azalıyor. Scene setup repair aynı fill sözleşmesini idempotent normalize ediyor; yeni Council sistemi veya içerik üretilmedi. Prefab geometri, exact quote yüzeyleri, font/rich-text ve timer binding'i yeni guard testiyle kilitlendi | Unity compile: 0 error; targeted EditMode 29/29; targeted Council PlayMode 6/6; MCP live audit: `35s / 1.0000` -> `15s / 0.4286`, type `Filled`, iki exact option aktif; 1920x1080 Game View QA temiz; final Console 0 error; tracker `358/441` |
| 2026-07-16 | `DW-I-HUD-RATIO-CROP` responsive 16:9 + ultrawide framing | Generated HUD prefabının sabit `1920 x 1080` iç görsel root'u parent sanal canvas'ına stretch edildi. `CastleDefensePanel`, 16:9 konumunu koruyan top-center anchor'a alınarak kısa ultrawide sanal yükseklikte Celestial Dial ile çakışması giderildi. Scene setup tool iki sözleşmeyi prefab ve scene instance'ında idempotent onarıyor. Sabit kamera, kale/frontline görünürlüğü ve `SpawnLineX = 27` gizli doğum hattı otomatik guard ile kilitlendi | Unity compile: 0 error; new aspect/framing EditMode 4/4; related HUD presentation EditMode 17/17; MCP live audit 1920x1080 + 3440x1440: 13 kritik rect ekran içinde, Archer drawer açıkken taşma yok, Cycle/Defense overlap false; iki çözünürlükte görsel QA temiz; aktif scene dirty false; final Console 0 error; tracker `360/441` |
| 2026-07-16 | `DW-I-ONBOARD-WORKER-RATIO` first-day worker ratio cue | Scene-owned `FirstRunOnboardingUI`, ilk Day boyunca worker drawer kapalıyken ana toggle'ı, açıkken gerçek Wood `+10` ratio kontrolünü non-modal pulse ile işaretliyor ve `ADJUST A WORKER TARGET RATIO.` metnini gösteriyor. Yalnız başarılı gerçek player ratio action'ı `tutorial.v1.worker_ratio` durable meta flag'ini yazıyor; tutorial kaynak harcamıyor, worker atamıyor ve drawer'ı oyuncu adına açmıyor. Presentation aktif HUD prefabında, runtime state owner'ı tek scene controller'da tutuldu; setup/repair yolu idempotent hale getirildi | Unity compile: 0 error; onboarding/HUD/meta EditMode `15/15`; onboarding + worker drawer PlayMode `2/2`; MCP prefab/scene audit: presentation `1/1/1/1`, prefab controller `0`, scene controller `1`, tüm binding'ler dolu ve scene dirty false; 1920x1080 kapalı/açık drawer görsel QA temiz; final Console 0 error; tracker `361/441` |
| 2026-07-16 | `DW-I-ONBOARD-ARCHER-HIGHLIGHT` first-affordable Basic Archer cue | `FirstRunOnboardingUI`, authoritative `GameManager.CanBuyArcher(Basic)` ilk kez true olduğunda shared non-modal hint/pulse sunumunu devralıyor. Archer drawer kapalıyken sabit `ARCHERS` toggle'ı, açıkken runtime-generated Basic row'un gerçek `BUY` butonu işaretleniyor; metin `RECRUIT A BASIC ARCHER.`. Yalnız başarılı `MarketUI` satın alımı `tutorial.v1.basic_archer` durable flag'ini yazıyor; başarısız, locked veya cap-blocked tıklama tamamlanma sayılmıyor. Worker ratio tamamlanınca affordable Basic adımı aynı shared yüzeyi kesintisiz devralabiliyor; tutorial drawer açmıyor veya transaction üretmiyor | Unity compile: 0 error; onboarding/HUD/meta EditMode `19/19`; worker-ratio + Basic onboarding PlayMode `2/2`; related retrain/worker drawer PlayMode `2/2`; MCP prefab/scene audit: shared presentation tekil, prefab controller `0`, scene controller `1`, Worker/Market/presentation binding'leri dolu ve scene dirty false; 1920x1080 kapalı/açık Archer drawer görsel QA temiz; final Console 0 error; tracker `362/441` |
| 2026-07-16 | `DW-I-ONBOARD-AMMO-HIGHLIGHT` first low-ammo Arrow row cue | `FirstRunOnboardingUI`, finite Arrow stoku effective kapasitenin inclusive `%25` veya altına ilk kez indiğinde shared non-modal sunumla üst resource strip'teki gerçek `ArrowChip` satırını işaretliyor; `AmmoPurchasePanel` oyuncu adına açılmıyor ve metin `RESTOCK YOUR ARROWS.`. Yalnız başarılı player-facing `+1`, `+5` veya `Buy Max` refill sonrasında `ArrowSupplyUI.ArrowRefillPurchasedByPlayer` yayılıyor ve `tutorial.v1.low_ammo` durable flag'i yazılıyor; başarısız refill ile CAP/EFF yatırımı tamamlanma sayılmıyor. Önceki onboarding adımları sunum önceliğini koruyor, prompt'tan önce yapılan başarılı refill adımı yine tamamlıyor | Unity compile: 0 error; onboarding/meta/ammo EditMode `20/20`; worker-ratio + Basic + low-ammo + finite ammo PlayMode `5/5`; MCP audit: scene controller `1`, ArrowSupplyUI `1`, shared hint/pulse `1/1`, AmmoSupply/ArrowChip binding'leri dolu; 1920x1080 görsel QA'da Arrow row pulse, English hint ve panel closed doğrulandı; final Console 0 error; tracker `363/441` |
| 2026-07-16 | `DW-I-ONBOARD-HEART-HIGHLIGHT` first Essence-funded Castle Heart entry cue | `FirstRunOnboardingUI`, kill/drop oranı uydurmadan authoritative ilk `GraveEssenceAmount > 0` bakiyesini gözlüyor ve alt-sağ dock'taki gerçek `CastleHeartOpenButton` kontrolünü `OPEN THE CASTLE HEART.` metniyle pulse ediyor; panel oyuncu adına açılmıyor. `HeartScreenUI`, yalnız gerçek button/Escape akışında `HeartOpenedByPlayer` / `HeartClosedByPlayer` event'lerini yayıyor. Açılışta mevcut `SimulationPauseService` lease'i `Time.timeScale = 0` ve `SimulationSystemGroup.Enabled = false` sözleşmesini korurken pulse kapanıyor; `THE CASTLE HEART FULLY PAUSES THE BATTLE.` hint'i raycast'siz nested Canvas order `260` ile Heart modal `200` üstünde görünüyor. `tutorial.v1.heart` durable flag'i yalnız oyuncu Heart'i kapattıktan sonra yazılıyor; programmatic open/close tamamlanma sayılmıyor | Unity compile: 0 error; FirstRun onboarding EditMode `5/5`; Heart pause regression EditMode `8/8`; Worker/onboarding PlayMode `9/9`; MCP scene audit: tek `FirstRunOnboardingUI`, tek `HeartScreenUI`, `CastleHeart` binding'i dolu ve scene dirty false; 1920x1080 giriş pulse + full-pause modal görsel QA temiz; final Console 0 error; tracker `364/441` |
| 2026-07-16 | `DW-I-ONBOARD-COUNCIL-EXACT` first regular Council exact-choice cue | `FirstRunOnboardingUI`, regular Council kartı gerçek oyuncu seçimine açıldığında tek bir branch'i öne çıkarmadan bütün `CouncilEventPanel` rect'ini non-modal pulse ediyor ve `COMPARE BOTH EXACT OUTCOMES AND THEIR COSTS.` metnini gösteriyor. İki option'ın sayısal sonucu, bedeli ve uygulanabilirliği mevcut `CouncilOptionPresentationUtility` live quote owner'ında kaldı. Tutorial Council açmıyor, seçim yapmıyor, timer/pause/resource transaction'ına dokunmuyor; yalnız başarılı gerçek option button commit'i `CouncilChoiceCommittedByPlayer` yayıp `tutorial.v1.council` durable flag'ini yazıyor. Dusk expire'i ve kartın yalnız açılması completion sayılmıyor; süreli Council cue'su Heart pause dışındaki opportunistic prompt'lara geçici sunum önceliği taşıyor | Unity compile: 0 error; onboarding + exact quote EditMode `12/12`; full Council schedule/choice/Continue PlayMode `6/6`; MCP scene audit: tek `FirstRunOnboardingUI`, tek `CouncilEventUI`, `Council` binding'i dolu ve scene dirty false; 1920x1080 full-card pulse + hint görsel QA temiz; final Console 0 error; tracker `365/441` |
| 2026-07-16 | `DW-I-ONBOARD-REPAIR-HIGHLIGHT` first Daytime Wall repair cue | `FirstRunOnboardingUI`, continuous cycle gercek Day phase'indeyken yasayan Wall `%99,5` altina dustugunde top-center savunma panelindeki gercek `DefenseRepairButton` kontrolunu `REPAIR THE WALL DURING THE DAY.` metniyle pulse ediyor. Stone affordability prompt kapisi degil; oyuncu authoritative maliyeti disabled buton yaninda gorebiliyor. Tutorial Wall HP, Stone veya phase yazmiyor. Yalniz `DefenseRepairUI` butonundan baslayip `GameManager.RepairDefenseFull()` tarafindan basariyla commit edilen action `NormalRepairCommittedByPlayer` yayip `tutorial.v1.repair` durable flag'ini yaziyor; basarisiz veya programmatic repair tamamlanma sayilmiyor. Scene reload ilk frame'inde gizli hedefe cue acilmasini engelleyen active-hierarchy kapisi eklendi | Unity compile: 0 error; onboarding EditMode `7/7`; yeni repair onboarding PlayMode `1/1`; onboarding + normal repair regresyon PlayMode `6/6`; MCP scene audit: tek `FirstRunOnboardingUI`, tek `DefenseRepairUI`, `NormalRepair` binding'i dolu ve scene dirty false; 1920x1080 Game View QA temiz; final Console 0 error; tracker `366/441` |
| 2026-07-16 | `DW-I-ONBOARD-ABILITY-KEY-HINT` first Night ready-ability keyboard cue | `FirstRunOnboardingUI`, ilk kosunun ilk Night phase'inde gercek ability barindaki ilk hazir slotu `[1] Fireball -> [2] Rally -> [3] Emergency Repair` onceligiyle cozer; mevcut ilk kosuda kilitli Fireball ve full Wall nedeniyle `[2] Rally` pulse olur. Dynamic English key copy bottom-center `0,170` konumunda slotlardan ayrik okunur. Yalniz kabul edilmis `SpellCastUI` keyboard yolu `AbilityHotkeyAcceptedByPlayer` yayip `tutorial.v1.ability_key` durable flag'ini yazar; locked/cooldown reddi, mouse button ve programmatic gameplay cagrisi completion sayilmaz. Tutorial ability kullanmaz, cooldown/state yazmaz veya resource harcamaz | Unity compile: 0 error; FirstRun onboarding EditMode `8/8`; yeni key-hint PlayMode `1/1`; onboarding + exact ability Continue regresyon PlayMode `7/7`; MCP scene audit: tek `FirstRunOnboardingUI`, tek `SpellCastUI`, `Abilities` binding'i dolu ve scene dirty false; 1920x1080 Game View QA ilk `0,122` overlap'ini yakalayip final `0,170` yerlesimini temiz dogruladi; final Console 0 error; tracker `367/441` |
| 2026-07-16 | `DW-I-ONBOARD-NO-AUTO-TRANSACTION` global transaction-free onboarding invariant | `FirstRunOnboardingUI` bütün onboarding adımlarında yalnız authoritative state okuyan, gerçek player-action event'lerini dinleyen, shared hint/pulse sunan ve tutorial completion flag'i yazan presentation owner olarak sınırlandı. EditMode source guard; archer/ammo satın alma, resource harcama, worker ratio/assignment, Council seçimi, Wall repair, ability kullanımı, ECS write ve programmatic UI açma çağrılarını controller kaynak kodunda yasaklıyor. PlayMode global invariant worker ratio, Basic Archer, low ammo, Castle Heart, regular Council, Day repair ve ilk Night ability-key cue'larını sırayla gösterip `ResourceData`, Arrow, Essence, population, gerçek worker count/target/cap/idle, beds ve worker-building state'inin değişmediğini; drawer/modal/choice/ability completion'ın oyuncu inputu olmadan ilerlemediğini kanıtlıyor. Basic Archer ve ammo transaction testleri de doğal resource tick ile player işlemini karıştırmamak için maliyeti senkron transaction sınırında ölçüyor | Unity compile: 0 error; FirstRun onboarding EditMode `9/9`; onboarding + global invariant PlayMode `8/8`; MCP active scene `NewGameScene`, scene dirty false; final Console 0 error; tracker `368/441` |
| 2026-07-16 | `DW-I-ONBOARD-NO-MODAL-CHAIN` blocking-pause suppression + exact Heart lease handoff | `FirstRunOnboardingUI`, herhangi bir blocking pause sırasında bütün onboarding cue'larını bastırıyor; tek allowlist istisnası oyuncunun ilk kez açtığı Heart modalı üstünde mevcut Heart pause dersinin görünmeye devam etmesi. Tutorial pause lease almıyor, modal açmıyor ve Heart açıkken Council/repair/ability gibi yeni bir cue zincirlemiyor. Heart kapanınca mevcut tek lease bırakılıyor, simülasyon exact state'e dönüyor ve sıradaki uygun cue aynı shared yüzeyde non-modal olarak devam ediyor. Source guard pause lease/enforce ve programmatic modal açma çağrılarını yasaklıyor; saf blocking-pause kuralı ve gerçek Heart -> Day repair geçişi regresyonlarla kilitli | Unity compile: 0 error; FirstRun onboarding EditMode `11/11`; Heart pause EditMode `8/8`; yeni Heart-close handoff PlayMode `1/1`; ilgili onboarding/Council PlayMode `5/5`; MCP active scene `NewGameScene`, scene dirty false; final Console 0 error; tracker `369/441` |
| 2026-07-16 | `DW-I-ONBOARD-PREEMPTIVE-COMPLETE` prompt-independent accepted action completion | Worker ratio, Basic Archer, Arrow refill, regular Council choice, normal repair ve ability hotkey completion handler'larının active cue/hint/pulse state'ine bakmadan yalnız owner'larının başarılı player-action event'ini durable flag'e çevirdiği global source guard ile kilitlendi. Heart'in iki aşamalı open/close akışı aynı sözleşmeye çekildi: oyuncu ilk Grave Essence gelmeden Heart'i açarsa mevcut full-pause dersi yine gösteriliyor, gerçek close event'i flag'i yazıyor ve Essence daha sonra geldiğinde giriş prompt'i tekrar açılmıyor. Başarısız veya programmatic işlemler event yaymadığı için completion sayılmıyor; prompt eligibility yalnız sunum sahibi olarak kaldı | Unity compile: 0 error; FirstRun onboarding EditMode `12/12`; yeni preemptive Heart PlayMode `1/1`; bütün onboarding + Council regresyon PlayMode `10/10`; MCP active scene `NewGameScene`, scene dirty false; final Console 0 error; tracker `370/441` |
| 2026-07-16 | `DW-I-ONBOARD-COMPLETE-FLAG` global tutorial completion + legacy backfill | Stable `tutorial.v1.complete` Id'si yeni schema alanı açmadan mevcut `MetaProgressState v3.TutorialFlags` listesine eklendi. Saf completion kuralı worker ratio, Basic Archer, low ammo, Heart, regular Council, Day repair ve Night ability-key flag'lerinin yedisini de zorunlu tutuyor. Son accepted player action kendi durable alt flag'ini yazdıktan sonra global flag aynı run'da meta save'e kaydediliyor; eski save yedi alt flag'i taşıyıp global flag'i taşımıyorsa controller bunu durable backfill ediyor. Global flag sonraki framelerde eligibility/wallet sorgularını kısa devre edip hint/pulse sunumunu kapalı tutuyor; schema version bump yapılmadı | Unity compile: 0 error; FirstRun onboarding EditMode `13/13`; meta schema EditMode `7/7`; final-action + legacy-backfill PlayMode `2/2`; bütün onboarding + Council regresyon PlayMode `11/11`; MCP active scene `NewGameScene`, scene dirty false; final Console 0 error; tracker `371/441` |
| 2026-07-16 | `DW-I-ONBOARD-RESET-SETTING` two-confirmation Settings tutorial reset | Pause ve ana menüdeki ortak `SettingsUI` paneline `RESET TUTORIAL` kontrolü eklendi. İlk tıklama yalnız `CONFIRM RESET` durumunu kuruyor; ikinci tıklama yedi onboarding adımı ile `tutorial.v1.complete` flag'ini canonical sekizli olarak tek atomik meta save'de temizliyor. Run save, Souls, upgrade, pool unlock ve listede olmayan future tutorial flag'leri korunuyor; save başarısızsa önceki flag listesi geri yükleniyor. Reset pause altında yeni cue/modal zinciri açmıyor, Resume sonrasında ilk uygun cue yeniden başlıyor. İki sahne yalnız Settings panelini hedefleyen idempotent MCP repair yoluyla güncellendi | Unity compile: 0 error; tutorial/meta/iki-sahne Settings EditMode `24/24`; bütün onboarding + regular Council PlayMode `12/12`; MCP scene audit: tek `SettingsUI`, tek reset button/status, active `NewGameScene` dirty false; 1920x1080 Game View Settings yerleşimi temiz; final Console 0 error; tracker `372/441` |
| 2026-07-16 | `DW-I-ONBOARD-ENGLISH-COPY` exact player-facing English tutorial contract | Aktif player-facing tutorial yüzeyi source, generated HUD prefabı, NewGameScene, MainMenuScene ve gerçek runtime akışlarında denetlendi. Yedi onboarding step hint'i ile üç ability-key varyantının 10 exact approved English metni `FirstRunOnboardingUI` constant'larında; Settings reset default/confirm/success/failure durumlarının altı exact approved English metni `SettingsUI` constant'larında canonical hale getirildi. Scene setup inline alternatif copy üretmiyor; prefab ve iki sahnenin serialized başlangıç metinleri aynı constant'larla birebir. Türkçe kalan satırlar yalnız editor logu, yorum ve Inspector açıklaması olduğu için player-facing kapsama girmiyor | Unity compile: 0 error; exact copy + prefab + iki-sahne Settings EditMode `17/17`; bütün onboarding + regular Council runtime copy PlayMode `12/12`; MCP active scene `NewGameScene`, dirty false; final Console 0 error; tracker `373/441` |
| 2026-07-16 | `DW-I-ONBOARD-SECOND-RUN-SUPPRESS` real second-run tutorial suppression | Tamamlanmış tutorial'ın yedi step flag'i ile stable global flag'i ilk run snapshot'ından bağımsız meta state olarak korundu. Yeni PlayMode regresyonu ilk run'a gerçek living snapshot kurup lethal ECS state ile durable death receipt transaction'ını tamamlıyor; living Continue save'inin silindiğini ve gerçek `UIManager.OnRestart()` -> `GameManager.RestartGame()` yolunun farklı `CurrentRunId`, temiz Day 1 cycle ve tutorial için normalde eligible mobile worker economy ürettiğini kanıtlıyor. Meta reload sonrasında sekiz flag durable kalıyor; shared hint, pulse target ve sekiz onboarding cue state'i 120 frame boyunca kapalı. Yalnız iki-onaylı Settings reset tutorial'ı yeniden açabilir | Unity script validation: 0 error / 0 warning; targeted second-run PlayMode `1/1`; exact copy/reset EditMode `17/17`; bütün onboarding + regular Council PlayMode `13/13`; MCP active scene `NewGameScene`, dirty false; final Console 0 error; tracker `374/441` |
| 2026-07-16 | `DW-I-POLISH-DAY` warm Day light + readable production + bounded worker ambience | Mevcut `DayNightOverlayController`, paralel lighting manager kurmadan sahnedeki tek `Global Light 2D` owner'ina baglandi; canonical Day hedefi warm `RGB(1.00, 0.93, 0.82)` / intensity `1.08`, Dusk/Night/Dawn taban hedefleri ayni allocationsiz type-safe driver'da. URP 2D runtime assembly bagimliligi runtime/editor/PlayMode asmdef'lerinde explicit hale getirildi. Okunur production yeni world UI ile kopyalanmadi: gercek worker representation weight, cargo ve hub delivery pulse kontrati korundu. `AmbientAudioController`, current active worker sayisini logaritmik `0..1` activity'ye cevirip tek 2D `WorkerAmbience` source'unda Sawing Wood/Nail Wood/Blacksmithing/Rock Impact foley'lerini Day-only, pause/Game Over aware ve bounded `5.2s -> 1.6s` cadence ile caliyor; worker basina AudioSource veya unbounded spam yok. Hedefli idempotent scene repair ve architecture/setup belgeleri eklendi | Unity compile: 0 error; Day palette + worker representation EditMode `21/21`; yeni Day presentation PlayMode `1/1`; Day presentation + mevcut worker feedback PlayMode `2/2`; MCP scene audit: tek overlay/ambient/global light, 4/4 foley binding, scene dirty false; 1920x1080 Game View QA warm/okunur; final Console 0 error; tracker `375/441` |
| 2026-07-16 | `DW-I-POLISH-DUSK` amber-to-indigo Dusk + lantern ignition + bounded tension riser | `DayNightOverlayController` Dusk global-light eğrisi iki aşamalı hale getirildi: ilk `%45` warm Day'den canonical amber hedefe, kalan bölüm amber'den Night indigosuna akar; overlay'in mevcut normal/Blood Moon tint geçişi aynı authoritative phase progress'i kullanır ve Night girişindeki renk sıçramasını kaldırır. Yeni worker-light sistemi kurulmadı: mevcut `WorkerVisualRepresentationUtility` / `WorkerLogisticsMovementSystem` kontratı Dusk başlangıcında gerçek worker entity'lerinin `_WorkerFeedback.y` lantern sinyalini yakar. `AmbientAudioController` ayrı tek 2D non-loop `PhaseTransition` source'unda `RPG3_WindMagicEpic_Cast01_P1` klibini `0.23` volume / `0.90` pitch ile yalnız gerçek Day -> Dusk kenarında bir kez oynatır; one-shot source gain'i `1` olduğu için explicit cue mix'i gerçekten duyulur, polling aynı Dusk'ta tekrar etmez, scene load/Continue zaten Dusk veya Night ise ilk observation cue üretmez. Hedefli idempotent Dusk repair yolu ve architecture/setup belgeleri eklendi | Unity script validation: 0 error; Dusk palette + worker representation EditMode `21/21`; targeted Dusk PlayMode `1/1`; Day/Dusk/worker feedback regresyon PlayMode `3/3`; MCP scene audit: tek overlay/ambient, Global Light binding, canonical riser clip/volume/pitch, scene dirty false; 1920x1080 Dusk Game View QA grading/lantern/HUD okunur; final Console 0 error; tracker `376/441` |
| 2026-07-16 | `DW-I-POLISH-NIGHT` cold-moon silhouette + castle windows + bounded horde/salvo mix | `DayNightOverlayController`, sahnedeki tek Global Light 2D'nin Night hedefini cold-moon `RGB(0.46, 0.58, 0.94)` / `0.68` yaptı; Dusk `%18 -> %72` aralığında yanan, Night boyunca warm `RGB(1.00, 0.47, 0.12)` / `0.82` kalan ve Dawn'ın ilk `%65` bölümünde sönen dört pencere ışığını aynı owner'da bounded flicker ile sürüyor. `MobileCastleSceneSetupWindow`, gerçek `Wall A5_S/N` tile hücrelerini tarayıp tam dört `WindowGlow` Additive Point Light 2D'yi idempotent üretir; global-light aramaları Point ışıkları yanlış owner seçemez. `AmbientAudioController`, Night'ta authoritative pressure ve zombie sayısını logaritmik activity'ye çevirip 10.000 zombide tavana ulaşan tek 2D `NightHordeBed` loop'u kullanır. `CombatFeedbackBridge`, frame içindeki SFX event'lerini type bazında allocationsız aggregate eder; kritik cue önceliği, frame başı `4` playback bütçesi, Night `0.12s` shoot rate-limit'i ve `0.62` volume / `%8` pitch-depth ile aynı frame'deki 1.000 okçu atışını tek salvo cue'ya indirir; sabit `16` AudioSource pool büyümez. Architecture/setup belgeleri ve mevcut Dusk Night-palette referansı güncellendi; ayrı 10k ground-contrast, per-hit bütçe ve visual-salvo tracker maddeleri açık bırakıldı | Unity script validation: 0 error; Night palette + horde/salvo budget + worker representation EditMode `23/23`; targeted Night PlayMode `1/1`; Day/Dusk/Night/worker feedback regresyon PlayMode `4/4`; MCP scene audit: tek overlay/ambient/feedback/global light, tam `4` Point Light binding, canonical clip/tuning; scene validation `0` issue, dirty false; 1920x1080 Night Game View QA cold grading, warm windows, Wall/HUD/zombie silhouette okunur; final Console 0 error; tracker `377/441` |
| 2026-07-16 | `DW-I-POLISH-DAWN` cyan-to-gold break + survivor gate crossing + single new-day breath | `DayNightOverlayController`, Dawn'ı Night'tan cyan kırılmaya, oradan warm gold hedefe ve Day'e akan iki aşamalı Global Light/overlay eğrisine çevirdi; mevcut pencere ışıkları Dawn'ın ilk `%65` bölümünde söner. `DawnRewardToastUI`, kabul edilmiş gerçek survivor growth sonrasında `outside2` üzerindeki `(1,0,0)` kapı hücresini `Door C5_E` kapalı tile'ından `Door C6_E` açık tile'ına çevirir, tek `DawnGateGlow` ile bounded vurgu verir ve Dawn çıkışında kapıyı kapatır. Mevcut ECS survivor görselleri paralel bir sistem kurulmadan en fazla `15` temsilciyle, deterministic lane/delay ve gerçek accepted growth sayısıyla `5s` Dawn içinde kapının arkasına ulaşacak şekilde hızlandırıldı; Food/population transaction owner'ı değişmedi. `AmbientAudioController`, aynı `PhaseTransition` 2D source'unda yalnız gerçek Dawn kenarında bir kez new-day nefesi oynatır; scene load/Continue sırasında Dawn'ın ilk gözlemi toast, kapı veya cue tekrarına yol açmaz. Hedefli idempotent Dawn repair yolu, architecture/setup belgeleri ve regresyon testleri eklendi | Unity script validation: 0 error; Dawn palette + survivor route EditMode `9/9`; targeted Dawn PlayMode `2/2`; Day/Dusk/Night/Dawn regresyon PlayMode `5/5`; MCP scene audit: tek Dawn owner, exact `outside2`/`(1,0,0)`/C5-C6 tile binding, tek glow ve canonical cue; scene validation `0` issue, dirty false; 1920x1080 cyan/gold grading ve gate-detail Game View QA temiz; final Console 0 error; tracker `378/441` |
| 2026-07-16 | `DW-I-POLISH-PHASE-READ` layered sky + bounded atmosphere motes + silent large phase text | Blueprint sayfa 27'deki `grading + sky + particles + audio` sözleşmesi canlı sahneye bağlandı. Mevcut `MomentVignetteUI`, paralel cycle veya UI owner'ı kurmadan Main Camera sky rengini ve tek `PhaseAtmosphereParticles` sistemini authoritative `ContinuousSiegeCycleData` ile sürüyor. Day/Dusk/Night/Dawn sky ve mote renkleri mevcut grading checkpoint'leriyle aynı eğriyi izler; Dusk/Night/Dawn kenarları `10/6/14` bounded burst, sürekli emission ise tek `72` max-particle cap kullanır. Stress mode ve Game Over yeni emission/burst üretmez; ilk scene/Continue gözlemi transition sayılmaz. Canonical `DawnPeak = 0` ile generic full-screen Dawn flash kaldırıldı; `CyclePhaseText` ve üç ham phase label'ı inactive kalırken yalnız owner-onaylı Celestial Dial görünür. Setup tool radial mote texture'ını, URP transparent materialini ve tek scene emitter'ını idempotent üretir; architecture/editor-setup belgeleri eklendi | Unity script validation: 0 error; phase atmosphere + minimal HUD EditMode `8/8`; targeted phase-world PlayMode `1/1`; Day/Dusk/Night/iki Dawn/phase-world regresyon PlayMode `6/6`; MCP scene audit: tek owner/emitter, `Main Camera`, max `72`, exact material/texture, legacy phase text `4/4` inactive; scene validation `0` issue, dirty false; 1920x1080 Day/Dusk/Dawn cyan-gold Game View QA temiz; final Console 0 error; tracker `379/441` |
| 2026-07-16 | `DW-I-POLISH-SPELL-HIERARCHY` Fireball + Frost hierarchy above dense horde depth | `SpellFeedbackHierarchy`, ordinary/Frost/Fireball sunumunu tek `Wall` sorting contract'ında topladı. `CombatFeedbackBridge` mevcut `128` flipbook pool ve `24/frame` budget içinde Frost slotlarına `3.2x` cyan impact ile genişleyen pooled ring ekledi; ordinary hit küçük kaldı. `SpellCastUI`, Fireball projectile'a sabit aura, blast'a yeniden kullanılan sıcak core/ring katmanları ekledi. Fireball projectile, targeting indicator ve bütün blast render'ları `MobileCastleRenderDepth.ProjectileZ` bandına normalize edilerek opaque/depth-write enemy renderer'larının önüne taşındı; damage, radius, cooldown, Frost slow ve hit sampling değişmedi. Setup tool ve architecture/editor setup belgeleri güncellendi | Unity script validation: 0 error; hierarchy EditMode `3/3`; gerçek 10K pooled-enemy hierarchy PlayMode `1/1`; dense hit + bridge budget + Fireball/Continue 10K regresyon PlayMode `3/3`; `DW_I_SPELL_HIERARCHY_10K.png` görsel QA temiz; scene validation `0` issue; final Console `0 error`; tracker `383/441` |
| 2026-07-16 | `DW-I-POLISH-SALVO-RHYTHM` bounded visual representatives over full gameplay projectiles | `ArcherShootSystem`, her başarılı pool rent'inin monotonic sırasını ve canlı okçu sayısını `ArcherSalvoPresentationUtility` ile birleştirir. 48 ve altındaki birliklerde full visibility korunur; 1.000 okçuda stride `21` ile aynı salvonun yalnız `47-48` oku görünür, ardışık salvolar temsilci şeritlerini kaydırır. Scale `0` projectile'lar gameplay olarak aktif kalır; damage, target reservation, ammo, pool, Frost, hit feedback ve save truth azaltılmaz. Continue yoğun active-arrow listesine bounded dağılımı yeniden uygular. Targeting/pool mimarisi ve gerçek 10K testi güncellendi | Unity compile/Console `0 error`; utility EditMode `3/3`; gerçek `10.000` enemy + `1.000` archer PlayMode `1/1`, ilk salvo `1000 gameplay / 48 visual / stride 21`; targeting/ammo/pool/generation regresyon PlayMode `5/5`; `1920x1080` screenshot QA; frame average `9,77 ms`, P95 `12,74 ms`, draw call `544`; tracker `384/441` |
| 2026-07-16 | `DW-I-POLISH-BLOOD-MOON-REMOVAL` remove active special-night presentation wiring | `BloodMoonWarningUI` ve aktif `BloodMoonWarningRoot` tamamen silindi. `AmbientAudioController` yalnız canonical Night loop/horde bed kullanıyor; özel loop/sting kaynağı yok. `DayNightOverlayController`, `MomentVignetteUI` ve `HUDController` kırmızı tint/flash/label dallarını taşımıyor; scene setup bunları yeniden üretmiyor. `IsBloodMoonNight` ve `BloodMoonIntensityMult` yalnız save/config geriye uyumluluk alanları olarak dormant kaldı. Night polling regresyonu gerçek `0,2s` unscaled cadence'i explicit bekleyecek şekilde deterministikleştirildi | Unity compile gerçek error `0`; new removal + phase math EditMode `8/8`; stale special-night + Day/Dusk/Night/Dawn/phase-world PlayMode `5/5`; live reflection presentation field `0`, warning type/root `0`; scene validation `0` issue; 1280x720 normal Night Game View QA; tracker `385/441` |
| 2026-07-16 | `DW-I-ACCEPT-FIRST-RUN-TUTORIAL` first-run completion + second-run suppression acceptance | Mevcut onboarding implementasyonu acceptance seviyesinde yeniden denetlendi; runtime kodu veya UI yeniden yazılmadı. Yedi zorunlu gerçek oyuncu aksiyonu global durable completion flag'inin tek kapısıdır; legacy step-only meta state backfill olur. Gerçek first-run ölüm transaction'ı ve `UIManager.OnRestart()` ile başlayan yeni run, run kimliğini/cycle'ı sıfırlarken meta tutorial flag'lerini korur; ikinci run boyunca bütün cue yüzeyleri kapalıdır. Tutorial yalnız Settings'teki iki-onaylı explicit reset ile yeniden açılır | EditMode onboarding/meta/reset `25/25`; gerçek onboarding/Council/second-run PlayMode `13/13`; MCP scene audit: tek `FirstRunOnboardingUI`, `0` eksik binding; scene validation `0` issue; final Console `0` error; tracker `386/441` |
| 2026-07-16 | `DW-I-PRESENTATION-TEST-HARNESS` transient combat unlock + exact horde presets + grounded contact | Editor/Development-only Game View paneli Fireball/Rapid/Frost erişimini, hazır cooldown'ları ve transient ücretsiz recruitment'ı tek aksiyonda kuruyor; mevcut enemy catalog/pool ile exact `2K/5K/10K` horde butonları var. Test state'i run save'e yazılamıyor, test zombileri Wall'a hasar vermiyor ve Stress Mode kullanılmadığı için gerçek grading/VFX/SFX hattı açık kalıyor. Vampire shader temas patch'i hardcoded uzak konumdan materyal-owned atlas ayak bandına taşındı; gameplay transform/physics değişmedi | Unity compile `0 error`; EditMode `12/12`; PlayMode exact `2K/5K/10K` `1/1`; canlı 2K/10K Game View QA; scene validation `0`; final Console `0 error`; tracker kapsamı `441 -> 442`, ilerleme `387/442` |
| 2026-07-16 | `DW-I-ACCEPT-PRESENTATION-REVIEW` owner acceptance + horde cavity flow fix | Owner Day/Night ve yoğun sürü sunumunu kabul etti; kapsamlı audio asset polish'i V1 kabulinden çıkarıldı. Dairesel Fireball temizliği sonrasında kalan boşluğun nedeni, aynı ilerleme hizasındaki `Queued` zombilerin birbirini blocker sayarak karşılıklı kilitlemesiydi. `ZombieQueueFlowUtility`, yalnız hedefe daha yakın komşuyu blocker kabul edecek biçimde `BoundarySystem` owner'ına bağlandı; single-front, radial ve legacy akış korunurken yan/arka kuyruk deadlock'u kaldırıldı | Unity compile `0 error`; yön kuralı EditMode `4/4`; canlı iki-zombi MCP transaction'ı `x=4,850 -> x=0,847 / Attacking`; gerçek 10K dairesel strike `1.029` ölümden sonra eski alana üç saniyede `679` living refill; final Game View QA temiz. Formal PlayMode regresyonu eklendi fakat Unity Test Framework üç retry'da test başlamadan init timeout verdi; eşdeğer canlı MCP transaction'ı geçti |
| 2026-07-16 | `DW-I-QUEUE-CAVITY-FOLLOWUP` forward-lane horde refill after circular strikes | Önceki yalnız progress tabanlı düzeltmenin eksik kalan çapraz-teğet vakası hedeflendi. `ZombieQueueFlowUtility.CanBlockQueue`, single-front/legacy için `-X`, radial için kale merkezine yönelen eksen üzerinde ileri mesafeyi ve lateral mesafeyi karşılaştırır; yalnız `45 derece` ileri koridordaki Attacking/Queued komşu blocker olur. Fireball damage/radius/cooldown, Day/Night fazları, spawn yoğunlukları, temel hız ve doğrudan kuyruk davranışı değişmedi. Yeni çapraz-yay PlayMode regresyonu üç Queued zombinin açılan koridora dönmesini kilitler. Bu örnek 10K koşusunu geçti; owner'ın sonraki exact 2K kabulü, duvara bağlanan daha uzun eğri blocker zincirinde yalnız açı filtresinin yeterli olmadığını gösterdi ve nihai düzeltme bir sonraki kayda taşındı | Unity compile `0 error`; utility EditMode `7/7`; mevcut yan-komşu + yeni çapraz-yay PlayMode `2/2`; exact Day 10K canlı koşuda `994` ölüm sonrası `5,5s` hedef/çevre yoğunluğu `%99`; tracker kapsamı değişmedi: `392/442` |
| 2026-07-16 | `DW-I-QUEUE-CAVITY-2K` preserve forward pressure through queued horde | Exact 2K owner reprodüksiyonu kalan kök nedeni kanıtladı: Fireball çemberinin kenarındaki her Queued zombie gerçekten daha öndeki bir Queued/Attacking komşuya bağlanıyor ve zincir eğri biçimde Wall'a ulaşıyordu; bu nedenle blocker filtresini daha da daraltmak doğrudan kuyruk davranışını bozma riski taşıyordu. `ZombieQueueFlowUtility.ReceivesForwardPressure`, Moving ve Queued state'lerini fiziksel ileri baskı sahibi yaptı. `ApplyMovementForceSystem` Queued zombiye saldırı izni vermeden hedef kuvvetini uyguluyor; Attacking/Dead sabit kalıyor. Böylece geçerli blocker zinciri state semantiğini korurken arkadaki kalabalığın basıncı Fireball boşluğunu dolduruyor. Exact 2K uçtan uca regresyonu gerçek test paneli spawn'ı, gerçek `r=2,2` strike ve 4,5 saniyelik refill penceresini kilitliyor | Unity compile/Console `0 error`; utility EditMode `11/11`; yan-komşu + çapraz-yay PlayMode `2/2`; exact 2K PlayMode `1/1`; canlı exact 2K baseline/fix karşılaştırması: boşluk içi `53 -> 240`, Queued hız `0,003 -> 0,354`; sabit Day 10K: `785` ölüm, `753` living refill, Queued hız `0,207`, Wall penetration `0`; Game View QA'da görünür dairesel boşluk yok. Tracker kapsamı değişmedi: `392/442` |
| 2026-07-17 | `DW-V1-CONTRACT-RUN-DIFFICULTY` canonical run difficulty ownership | Yeni/paralel bir profile sistemi kurulmadı. Mevcut `DifficultyProfileSO`, BaseSpawn gün eğrisi, base/min interval, batch growth, beş phase multiplier ve normal-run active cap'in tek içerik owner'ı olarak kesinleştirildi. `MobileCastleTuningResolver` bake/live-apply için tek mapping owner'ı; `ContinuousSpawnBudgetUtility` ise designer seçeneği olmayan sabit PreserveDemand backlog politikasının matematik owner'ı. Cap doluyken talep saturating `long PendingEnemies` içinde korunur, exact save/Continue'a girer ve yalnız boş kapasite ile `MaxSpawnBatch` sınırında drain edilir. Development 2K/5K/10K override'ı production profile asset'ini değiştirmeyen transient istisna olarak belgelendi | Unity MCP live asset audit: `DefaultDifficulty`, BaseSpawn day `1/60 = 1/1`, phase `0,55/1/1,35/1,65/0,15`, active cap `900`, max batch `16`, interval `0,95/0,35`; contract + budget EditMode `8/8`; runtime profile + cap/backlog PlayMode `2/2`; final Console `0 error`; tracker `393/442` |
| 2026-07-17 | `DW-V1-CONTRACT-WORKER-ALLOCATION` canonical worker allocation ownership | Yeni/paralel allocation state kurulmadı. `MobilePopulationAllocation`, dört target ratio, actual worker sayısı, effective cap aynası ve idle mirror'ın tek ECS owner'ı olarak kesinleştirildi. `WorkerAllocationUtility`, exact `10.000` basis-point normalize, yalnız yeni arrival'ı deterministic dağıtma, actual worker toplamı ve canonical idle formülünü sahipleniyor. `MobilePopulationEconomySystem`, config aggregate cap'lerini aynalayıp actual/population sınırını uyguluyor; `PopulationState.Idle`, allocation idle mirror ve `GameManager.GetIdlePopulation()` artık aynı utility sonucunu tüketiyor. Target değişikliği existing worker'ı taşımıyor; cap overflow idle kalıyor; representative world visuals actual state'e yazmıyor. Worker save contract belgesi güncel v14 schema'ya getirildi | Unity MCP live NewGameScene audit: population `60`, archers `4`, actual workers `20/10/8/15` ve aggregate `53`, ratios `3774/1887/1509/2830 = 10.000`, caps `40/30/24/40`, allocation/population/derived idle `3/3/3`, checkpoint `60`; allocation EditMode `8/8`; arrival + drawer + exact Continue PlayMode `3/3`; final Console `0 error`; tracker `394/442` |
| 2026-07-17 | `DW-V1-CONTRACT-ARCHER-FORMATION` canonical versioned 40 x 25 ownership | Yeni/paralel formation sistemi kurulmadı ve runtime davranışı değiştirilmedi. `ArcherFormationV1.asset`, version `1` ile sıralı 40 canonical `outside` hücresinin ve örnekleme tuning'inin tek definition owner'ı; `ArcherFormationUtility`, hücre başına 25 coordinate+slot seeded deterministic nokta, layer-fill index mapping ve türetilmiş 1000 kapasitenin matematik owner'ı; `MobileCastleArcherTilePlacement` yalnız bu contract'i aktif tilemap eksenleriyle world-space cache'e dönüştüren scene bridge olarak kesinleştirildi. Exact save world position değil formation version + type count tutuyor; formation/save belgeleri güncel v14 şemasına çekildi ve gerçek asset sahipliği regresyonla kilitlendi | Unity MCP live asset/scene audit: `ArcherFormationV1`, version `1`, `40` tile, inset `0,18`, minimum distance `0,055`, attempts `128`, validation true; `Grid` binding aynı asset, `40` cell / `1000` capacity; contract + v6 migration EditMode `5/5`; exact cache rebuild + Continue PlayMode `1/1`; final Console `0 error`; tracker `395/442` |
| 2026-07-17 | `DW-V1-CONTRACT-ACTIVE-ABILITY-STATE` canonical derived active-ability ownership | Ayrı `ActiveAbilityState` ECS component'i veya paralel runtime store kurulmadı. `GameManager`, Fireball/Rally/Emergency unlock görünümünün ve üç cooldown remaining sayacının tek transaction owner'ı; Fireball unlock/damage/radius/cooldown modifier'ları tech node level'ları ile exact Heart graph'tan çözülüyor. Rally/Emergency base tuning'i `DifficultyProfileSO -> MobileCastleTuningResolver -> MobileCastleCombatConfig`, aktif Rally effect'i `CastleYardPrepState` sahibi. Exact save cooldown remaining + aktif effect'i doğrudan; unlock ve türetilmiş modifier'ları ise tech/Heart girdileri üzerinden yeniden kuruyor. İçerik asset'leri, v11 ability migration'ı, tech modifier rebuild'i ve Heart unlock replay'i regresyonlarla kilitlendi; ability architecture ile tracker save owner tablosu v14'e çekildi | Unity MCP live runtime audit: initial unlock `false/true/true`, cooldown `0/0/0`, Fireball `60 / 2,2 / 45s`, Rally `10s / 1,25x / 60s`, Emergency Repair `%20 / 120s`; profile/config eşleşti; contract+tuning+save/migration EditMode `11/11`; tech modifier + üç cooldown exact Continue PlayMode `1/1`; exact Heart Fireball unlock replay PlayMode `1/1` (ilk deneme test başlamadan init timeout, MCP recovery sonrası geçti); final Console `0 error`; tracker `396/442` |
| 2026-07-17 | `DW-V1-CONTRACT-COUNCIL-RUN-STATE` canonical regular-only Council ownership | Ayrı `CouncilRunState` veya ikinci meeting sistemi kurulmadı. `CouncilRegularSchedule + GameManager`, regular handled day, flag/chain memory, recent/one-shot hafıza, run salt ve unresolved aktif kartın tek owner zinciri; seçilmiş süreli production/next-night multiplier + expiry state'i mevcut `MobileEconomyEventState` sahibi. Exact save v14 iki katmanı aynen koruyor; unresolved kart production catalog preflight'ından geçmeden restore edilmiyor, resolved kart yeniden açılmıyor. Canlı audit, ilk meeting öncesi snapshot'ın salt'i `0` yazabildiğini gösterdi; `EnsureCouncilRunSalt` salt'i yeni run/ilk save/legacy-exact restore/GetCouncilSeed sınırlarında tek owner üzerinden non-zero commit edecek biçimde kapattı. Emergency meeting type/state/trigger yolu eklenmedi | Unity MCP pre-fix live audit: production catalog `9` template / `11` atom / recent memory `3`, cadence `3/3`, initial handled `-1`, flags/recent/one-shot `0/0/0`, no active card, neutral effects `1/0 + 1/0`, salt `0`; cadence/content/save migration EditMode `28/28`; first-meeting-before salt + unresolved payload/memory/handled-day + resolved timed-effects exact Continue PlayMode `3/3`; final Console `0 error`; tracker `397/442` |
| 2026-07-17 | `DW-V1-BOUNDARY-NO-PARALLEL-OWNER` active runtime owner boundary | Yeni manager, controller, ECS component veya system eklenmedi. Aktif V1 domain'leri için runtime state owner ile read/presentation bridge ayrımı `V1_RUNTIME_OWNER_BOUNDARY_ARCHITECTURE.md` içinde tek matriste kesinleştirildi. `GameManager` partial dosyalarının aynı sınıf olduğu, Mono read cache'lerinin ECS truth'ine paralel simulation owner olmadığı, ScriptableObject definition'larının runtime state sayılmadığı ve Development override'larının transient/save-dışı kaldığı sınırlandı. Gerçek scene ve ECS singleton matrisi PlayMode regresyonuyla kilitlendi; legacy dosyanın yalnız varlığı değil aktif runtime zincirine ikinci owner olarak bağlanması ihlal kabul ediliyor | Unity MCP live NewGameScene audit: GameManager/HUD/abilities/Council/Heart/cycle/audio/feedback/formation/onboarding/Arrow/repair scene owner'ları `12/12 = 1`; core ECS state sorguları `12/12 = 1`; genişletilmiş PlayMode guard `12` scene owner + `14` ECS domain `1/1`; Unity compile ve final Console `0 error`; tracker `398/442` |
| 2026-07-17 | `DW-V1-BOUNDARY-NO-COSMETIC-MOBILE-RENAME` serialized technical name stability | Runtime tipi, dosyası, scene'i, prefabı veya asset'i rename edilmedi; gameplay davranışı değiştirilmedi. `MobileCastle*` öneki mobil ürün hedefi değil, tarihsel serialized teknik sözleşme olarak sınırlandı. Mevcut isimler yalnız estetik için toplu değiştirilemez; işlevsel rename owner onayı, bütün referansların envanteri, GUID/type/field compatibility migration planı ve regresyon kanıtı gerektirir. Yeni tiplerin öneki sürdürme zorunluluğu yoktur; player-facing metin teknik adı göstermez | Repo audit: `7` aktif type declaration, `158` referans dosyası, mevcut migration attribute `0`; `V1_RUNTIME_OWNER_BOUNDARY_ARCHITECTURE.md` naming policy; Unity MCP `NewGameScene` validation `0` issue, final Console `0 error`; tracker `399/442` |
| 2026-07-17 | `DW-V1-BOUNDARY-DORMANT-LEGACY-REVIEW` fail-closed legacy wiring review gate | Legacy save migration/fallback/type alias compatibility yolları ile gerçekten dormant gameplay ayrıldı. Onaylı compatibility yalnız eski veriyi canonical owner'a tek yönlü taşıyabilir; yeni truth, transaction owner veya presentation üretemez. `GateComponent`, `CastleHP`, `ArcherTrainer`/Barracks eğitim zinciri, Moat gameplay ve special-night/Blood Moon davranışı owner kararı ile Blueprint/save/scene/prefab/regresyon review'u olmadan aktif zincire bağlanamaz. Mevcut owner PlayMode guard'ı bu dormant ECS state'lerini sıfır, Moat değerlerini neutral ve bütün special-night sample çarpanlarını `1` olarak kilitleyecek biçimde genişletildi | Unity script validation `0 error / 0 warning`; targeted active-owner + dormant-wiring PlayMode `1/1`; active world `Gate/Core/Trainer = 0/0/0`, Moat `false/1/0`, bütün special-night multiplier `1`; `NewGameScene` validation `0` issue; final Console `0 error`; tracker `400/442` |
| 2026-07-17 | `DW-V1-PERF-ARROW-REFILL-1K` measured pooled restart after finite-ammo refill | Mevcut `GameManager.TryBuyArrowRefill`, `ArrowSupply`, `ArcherShootSystem` ve projectile pool zinciri değiştirilmeden tek benchmarkta birleştirildi. 1.000 hazır Basic Archer stok `0` iken ateş etmiyor; 10 paketlik gerçek transaction `1.000 Arrow` için `250 Wood` harcıyor. Takip eden tek simulation tick'i tam `1.000` gameplay projectile rent ediyor, tam `1.000` Arrow tüketiyor ve prewarm pool'u genişletmiyor. Main-thread `<50 ms` ve wall-frame `<100 ms` Editor guard bütçeleri otomatik regresyona eklendi. Whole-frame GC sample test runner dahil olduğundan izole Player allocation kabulü sayılmıyor | Unity script validation `0 error / 0 warning`; targeted benchmark `1/1` ve full ArrowAmmo PlayMode `3/3`; iki temiz sample: refill `0,010-0,600 ms`, restart main `22,210-23,158 ms`, wall `24,327-50,327 ms`, whole-frame GC `29.622-30.094 B`, rent `1.000`, pool expansion `0`; bir ara koşu test başlamadan init timeout verdi, MCP recovery sonrası gerçek testler geçti; `NewGameScene` validation `0` issue; final Console `0 error`; tracker `401/442` |
| 2026-07-17 | `DW-V1-PERF-CONTINUE-10K-1K` canonical 10K horde + 1K archer Continue rebuild | Önceki direct-ECS stress archer kurulumu save kanıtı olmadığı için benchmark, üretim `GameManager.RestoreArcherCountsWithinCapacity` owner'ıyla canonical 1.000 Basic Archer kuracak biçimde kapatıldı. Population/allocation/bed state'i coherent exact-run değerlerine eşitlendi; v14 save payload'ı 1.000 Basic Archer, formation version `1`, 10.000 aggregate enemy rebuild ve backlog `777` taşıdı. Bütün okçular her tur öncesi silinerek iki bağımsız Continue yapıldı; iki turda da 10K enemy + 1K canonical archer yeniden kuruldu ve enemy/archer fingerprint'leri aynı kaldı. Runtime gameplay kodu değiştirilmedi | Unity compile/Console `0 error`; targeted canonical Continue PlayMode `1/1`; `378` rebuild bucket, snapshot `369.097 B`, save `47,05 ms`, restore `417,11/436,89 ms`, enemy/archer deterministic `True/True`, frame average/P95 `30,49/52,06 ms`; ilk deneme test başlamadan init timeout verdi, MCP recovery sonrası gerçek test geçti; tracker `402/442` |
| 2026-07-17 | `DW-V1-PERF-PLAYER-ALLOC-SPIKE` isolated combined-load Player ownership | Explicit profiler testi production pool ve canonical archer restore owner'ıyla 10.000 enemy + 1.000 Basic Archer kuracak, finite Arrow/projectile pipeline'ını çalıştıracak ve allocation callstack açık 120-frame raw yazacak biçimde genişletildi. Editor-only runner exact testi StandaloneWindows64 Development Player'a gönderiyor; analyzer insan-okur TXT ve makine-okur JSON üretiyor, bütün active DeadWalls marker biçimlerini ve ECS system/job marker'larını tanıyor. Test Framework build failure callback'i run metadata/status ile fail-closed bağlandı. Strict Player build'i bozan unused third-party prefab'daki tek missing legacy Pixel Perfect component temizlendi; corrupt Entities artifact SubScene reimport ile yeniden üretildi | Player-targeted test `1/1`; capture enemy/archer/projectile `10.000/1.000/6`, raw `73.765.057 B`, analyzer `121` frame; CPU average/P95/max `10,593/18,694/33,234 ms`; root GC average/max `10.437/12.020 B`; proje user-code GC `58.290 B / 481 B/frame`; ECS GC `0 B`, sync-point yok; `DamageApplySystem` avg/max `1,892/4,863 ms`, `ArcherShootSystem` max `17,444 ms`; script validation `0 error`, Unity compile `0 error`; tracker `403/442` |
| 2026-07-17 | `DW-V1-TUNING-HEART-SURFACE` canonical Heart tuning contract | Yeni/paralel bir Heart owner kurulmadı. `Difficulty Tuner > Heart Runtime Contract`, `GameManager` graph settings/wallet runtime owner'ını ve catalog varsa gerçek `HeartNodeDefinitionSO` rarity/depth/cost/growth definition'larını doğrudan düzenlenen tek yüzeyde birleştirdi. Fiyat ve graph preview'ları production `HeartPurchasePricing` ile `HeartGraphGenerator` kullanır; live telemetry hidden node kimliklerini açmadan aggregate state sunar. Değişiklikler yalnız gelecekte üretilecek graph'a uygulanır, aktif veya Continue exact graph'i reroll edilmez. Onaylı Essence drop sayıları ve production catalog bulunmadığından değer uydurulmadı; açık `UNCONFIGURED` owner gate'i ve fail-closed catalog sınırı korundu | Heart contract guard EditMode `2/2`; Heart data/generator/purchase EditMode `36/36`; exact graph Continue PlayMode `1/1`; Difficulty Tuner gerçek editor window olarak açıldı; `NewGameScene` validation `0` issue; final Console `0 error / 0 warning`; tracker `410/442` |
| 2026-07-17 | `DW-V1-TUNING-COUNCIL-SURFACE` canonical regular Council tuning contract | Yeni Council type, timer veya parallel state kurulmadı. Production catalog Small/Fair/Generous multiplier/weight, A/B budget tolerance ve recent memory'nin tek tuning owner'i oldu; eski composer dagilimi aynen korundu. Memory azaltimi scheduled compose oncesi ve secim sonrasi uygulanir. Tuner fixed Day `3/6/9...` cadence'i, regular-only/Emergency-absent siniri, SubScene'den okunan Dawn+Day karar penceresini ve aggregate runtime state'i tek yuzeyde gosterir | Council EditMode `56/56`; regular runtime/Continue/memory PlayMode `8/8`; Tuner snapshot `5s + 30s = 35s`; editor window acildi; `NewGameScene` validation `0` issue; final Console `0 error / 0 warning`; tracker `411/442` |
| 2026-07-17 | `DW-V1-TUNING-META-SURFACE` diminishing death reward + audited permanent economy | Production Meta catalog, uc azalan kill bandi ile day/night/peak-pop/record agirliklarinin tek owner'i oldu. Exact quote `RunDeathReceipt v2` icinde durable sabitlenir; recovery tuning drift'i yaratmaz, v1 legacy explicit kalir. Mevcut 11 definition'in exponential maliyet ve hafif-guc rolleri degistirilmeden exact testle kilitlendi. Tuner ayni catalog/definition assetlerini dogrudan duzenler, runtime preview ve aggregate telemetry sunar | Targeted EditMode `44/44`; targeted PlayMode `3/3`; full EditMode `345/345`; full PlayMode `72 pass + 2 explicit skip`; 10K quote `1.640`; catalog `11/0 problem`; editor window acildi; scene validation `0`; final Console `0 error / 0 warning`; tracker `412/442` |
| 2026-07-17 | `DW-V1-TELEMETRY-RUN-STARTED` provider-independent run identity event | Sonraki telemetry event'lerinin de kullanacagi tek `GameplayTelemetry` envelope/subscriber siniri kuruldu; external target owner karari bekler. Yeni run, 11 production Meta definition level'i, ECS baslangic kaynaklari/Arrow/population snapshot'i ve Heart catalog/graph kimligini v1 payload'a yazar. Unconfigured production Heart acik status olarak kalir; main-menu action uygulanmadan emit edilmez ve exact Continue ayni RunId'yi tekrar saymaz | Contract EditMode `3/3`; new-run/Continue PlayMode `1/1`; ilgili telemetry+Continue+Meta grubu `3/3`; full EditMode `348/348`; full PlayMode'da suite-order Dusk flake'i targeted `1/1` gecti; `NewGameScene` validation `0`, final Console `0 error / 0 warning`; tracker `413/442` |
| 2026-07-17 | `DW-V1-TELEMETRY-PHASE-CHANGED` canonical cycle/horde transition event | Mevcut provider-independent telemetry bus'i genisletildi. Her yeni `RunId + Day + Phase` kimligi, `ContinuousSiegeCycleData`, `WaveStateData.ZombiesAlive` ve `ContinuousSpawnBudgetData.PendingEnemies` canonical owner'larindan tek immutable snapshot uretir. Ayni phase icindeki horde sayisi degisimleri duplicate sayilmaz; yeni run event sirasi `run_started -> phase_changed` olarak kilitlidir ve exact Continue mevcut phase'i tekrar emit etmez | Contract EditMode `6/6`; transition/Continue PlayMode `2/2`; full EditMode `351/351`; full PlayMode `74 pass + 2 explicit skip`; `NewGameScene` validation `0`, final Console `0 error / 0 warning`; tracker `414/442` |
| 2026-07-17 | `DW-V1-TELEMETRY-RESOURCE-SPENT` canonical committed purchase debit event | Provider-independent telemetry bus'i `resource_spent` v1 ile genişletildi. Aktif run kaynakları, Grave Essence ve owner adı bekleyen Meta currency sabit machine identity kullanır. Bütün aktif V1 player purchase owner'ları post-commit seviyeyi veya sayıyı yazar; çok kaynaklı işlem kaynak başına ayrı event, rejected/rollback/free-test/automatic/Council/legacy yolları sıfır event üretir. Heart graph/effect ve Meta disk transaction sınırları debit'ten sonra doğrulanır | Contract EditMode `9/9`; gerçek bed + dual-resource worker + rejected purchase PlayMode `3/3`; full EditMode `354/354`; full PlayMode `75 pass + 2 explicit skip`; `NewGameScene` validation `0`, final Console `0 error / 0 warning`; tracker `415/442` |
| 2026-07-17 | `DW-V1-TELEMETRY-ARCHER-CHANGED` canonical player archer transaction event | Provider-independent telemetry bus'i `archer_changed` v1 ile genişletildi. Başarılı Basic/Rapid/Frost buy `none -> type`, başarılı Basic retrain ise `basic -> rapid/frost` transition'ı ve post-commit ortak 1000-cap kullanımını yazar. Event canonical type-count refresh'inden ve aynı transaction'ın kaynak kayıtlarından sonra çıkar; free economy state değişimini korurken rejected/rollback, Council/meta bonusu ve exact Continue restore yollarını dışarıda bırakır | Contract EditMode `12/12`; gerçek buy + retrain + rejected buy PlayMode `4/4`; full EditMode `357/357`; full PlayMode `76 pass + 2 explicit skip`; `NewGameScene` validation `0`, final Console `0 error / 0 warning`; tracker `416/442` |
| 2026-07-17 | `DW-V1-TELEMETRY-HEART-NODE-BOUGHT` canonical Castle Heart purchase event | Provider-independent telemetry bus'i `heart_node_bought` v1 ile genişletildi. `HeartPurchaseService` başarı sonucuna canonical purchased-node depth eklendi; GameManager yalnız Grave Essence, level, effect, lock ve reveal commit'i tamamlandıktan sonra node Id/post-level/depth/total-cost/revealed-child-count snapshot'ı yayıyor. `resource_spent` önce kalır; hidden child Id'leri redacted, quote/Continue/rejected yolları event dışıdır | Contract EditMode `15/15`; gerçek graph buy + first reveal + rejected repeat PlayMode `5/5`; full EditMode `360/360`; ilk full PlayMode turundaki unrelated 1K Arrow timing dalgalanması targeted `30,09 ms` ile geçti, temiz full tekrar `77 pass + 2 explicit skip`; `NewGameScene` validation `0`, final Console `0 error / 0 warning`; tracker `417/442` |
| 2026-07-17 | `DW-V1-TELEMETRY-COUNCIL-RESOLVED` canonical regular Council decision event | Provider-independent telemetry bus'i `council_resolved` v1 ile genisletildi. Regular Council Option A/B secimi effect apply, otomatik/curated flag ve active-clear commit'inden sonra; Dusk expire active-clear commit'inden sonra day/template/resolution/concrete effect/guardlanmis next-night delta snapshot'i yayiyor. Rejected secim, bos tekrar expire, UI sunumu ve cozulmus karar sonrasi exact Continue event disinda; Emergency Council yok | Contract EditMode `18/18`; gercek `NewGameScene` choice + expire + Continue PlayMode `6/6`; full EditMode `363/363`; full PlayMode `78 pass + 2 explicit profiler/soak skip`; `NewGameScene` validation `0` issue; tracker `418/442` |
| 2026-07-17 | `DW-V1-TELEMETRY-ABILITY-CAST` canonical active ability result event | Provider-independent telemetry bus'i `ability_cast` v1 ile genisletildi. Fireball/Rally/Emergency Repair yalniz canonical transaction commit'inden sonra ability/phase/resolved cooldown/result snapshot'i yayiyor. Fireball asynchronous impact oncesi speculative hit saymiyor; Rally canonical archer totalini, Emergency Repair gercek Wall HP farkini yaziyor. Rejected cooldown/active/phase/Wall yollari ve exact Continue event disinda; Fireball damage job'una sync-point veya per-zombie telemetry eklenmedi | Contract EditMode `21/21`; gercek `NewGameScene` uc ability + rejected tekrar + Continue PlayMode `7/7`; full EditMode `366/366`; full PlayMode `79 pass + 2 explicit profiler/soak skip`; scene validation `0` issue; final Console `0 error / 0 warning`; tracker `419/442` |
| 2026-07-17 | `DW-V1-TELEMETRY-RUN-ENDED` durable final run summary event | Provider-independent telemetry bus'i `run_ended` v1 ile genişletildi. Production spawn commit'leri peak enemy high-water mark'ını, modifier sonrası gerçek Wall hasarı day/phase bucket'larını besliyor; per-hit event ve development stress sayımı yok. Schema v15 exact Continue accumulator'ları koruyor. Event yalnız death receipt ve Meta persistence finalize edildikten sonra gerçek day/kills/peak enemy/peak population/Wall damage timeline/meta reward snapshot'ıyla bir kez çıkıyor | Targeted EditMode `53/53`; targeted PlayMode `9/9`; full EditMode `375/375`; full PlayMode `81 pass + 2 explicit profiler/soak skip`; `NewGameScene` validation `0` issue; final Console `0 error / 0 warning`; tracker `421/442` |
| 2026-07-17 | `DW-V1-DOD-WALL-ONLY-END` Wall-only terminal state contract | Runtime source audit'inde tek production `IsGameOver = true` writer'ının `DamageApplySystem` olduğu ve yalnız `SingleWallDefenseRules.IsDestroyed(remainingWallHp)` sonrasında yazdığı kilitlendi. `GameManager` terminal state üretmiyor; yalnız rising edge'i death transaction ve UI'ya taşıyor. Wave completion, cycle/day, horde pressure, enemy/boss ölümü, injected legacy Gate/Core ve ikinci fail phase koşuyu bitiremiyor | Targeted EditMode `12/12`; injected Gate/Core + lethal Wall PlayMode `1/1`; full EditMode `376/376`; full PlayMode `81 pass + 2 explicit profiler/soak skip`; `NewGameScene` validation `0` issue; final Console `0 error / 0 warning`; tracker `422/442` |
| 2026-07-17 | `DW-V1-DOD-SINGLE-ENEMY-CATALOG` single-enemy launch catalog contract | Production düşman klasörü tam bir `EnemyCatalogSO`, tam bir `EnemyDefinitionSO` ve tek `zombie_basic -> Zombie.prefab` kaydıyla sınırlandı. EditMode release guard'ı iki SubScene authoring sahibinin aynı production catalog'a, legacy compatibility alanının da aynı prefab'a bağlı olmasını kilitliyor. Runtime bake buffer'ı tek entry üretiyor; production spawn bu entry'nin prefab ve statlarını kullanıyor | Unity MCP asset audit `1 catalog / 1 definition`; targeted EditMode `4/4`; gerçek bake/spawn PlayMode `2/2`; full EditMode `377/377`; full PlayMode `81 pass + 2 explicit profiler/soak skip`; `NewGameScene` validation `0` issue; final Console `0 error / 0 warning`; tracker `423/442` |
| 2026-07-17 | `DW-V1-DOD-QUANTITY-ONLY-DIFFICULTY` quantity-only launch difficulty contract | Production difficulty klasörü tek `DefaultDifficulty.asset` profile'ıyla sınırlandı. EditMode release guard'ı active SubScene authoring/profile/catalog bağını, neutral HP/damage/speed growth değerlerini ve aktif count/batch/intensity/interval pressure kanallarını birlikte kilitliyor. Gerçek runtime Day 1 -> Day 50 geçişinde enemy statları sabit kalırken adet artıyor ve spawn interval daralıyor | Unity MCP asset audit `1 profile`; targeted EditMode `2/2`; gerçek profile/bake/advanced-cycle PlayMode `3/3`; full EditMode `378/378`; full PlayMode `81 pass + 2 explicit profiler/soak skip`; `NewGameScene` validation `0` issue; final Console `0 error / 0 warning`; tracker `424/442` |
| 2026-07-17 | `DW-V1-DOD-60S-CYCLE` exact continuous cycle launch contract | Production SubScene exact `ContinuousSiegeEnabled=true` ve `60 = Day 30 + Dusk 5 + Night 20 + Dawn 5` authoring sözleşmesiyle kilitlendi. Runtime wrap testi dört pozitif-intensity fazı, Day 1 -> Day 2 geçişini, cycle index/timer wrap'ını ve `WaveStateData` combat-active durumunun prep gap olmadan korunmasını doğruluyor. Eski üç-faz `25/10/25` mimari metni kaldırılarak güncel tek sözleşme bırakıldı | Targeted EditMode `1/1`; gerçek phase/wrap PlayMode `2/2`; full EditMode `379/379`; full PlayMode `81 pass + 2 explicit profiler/soak skip`; `NewGameScene` validation `0` issue; final Console `0 error / 0 warning`; tracker `425/442` |
| 2026-07-17 | `DW-V1-DOD-NO-SPEED-OFFLINE` fixed-speed online-only run contract | Production source release guard'ı player-facing `Time.timeScale > 1`, `Time.fixedDeltaTime`, wall-clock progression API ve offline owner alanlarını yasaklıyor; yalnız normal `1x`, merkezi pause `0` ve Game Over sunumu `0.25x` sahipleri onaylı. Run/meta/death save şemaları timestamp veya offline accrual alanı taşımıyor; gerçek scene pause sunuyor fakat x2/x4/fast-forward sunmuyor. Exact Continue kapalı süre uygulamadan aynı cycle/phase/timer, kaynak ve spawn RNG state'ini geri kuruyor. Tam regresyonda bulunan 10K/1K stres testi son-frame projectile flake'i, runtime değiştirilmeden sample içindeki authoritative pool rent artışını ölçerek stabilize edildi | Targeted EditMode `3/3`; exact Continue PlayMode `1/1`; stabilize 10K/1K PlayMode `1/1` (`2.000` sample rent, `1.000` peak projectile); full EditMode `382/382`; full PlayMode `81 pass + 2 explicit profiler/soak skip`; `NewGameScene` validation `0` issue; final Console `0 error / 0 warning`; tracker `426/442` |
| 2026-07-17 | `DW-V1-DOD-PASSIVE-RESOURCE-DRAIN` main-resource upkeep exclusion + finite Arrow combat drain | Active castle loop legacy `ResourceConsumptionRate` değerlerini etkisizleştiriyor; Fletcher `ArrowProductionSystem` mobile config varken çıkıyor. Production world `ArrowProducer` veya legacy `ArcherTrainer` taşımıyor. Gerçek combat release guard'ında yüksek negatif rate enjeksiyonu Wood/Stone/Iron/Food stoklarını azaltmadı; başarılı tek projectile rent'i yalnız finite Arrow stokunu `10 -> 9` düşürdü. Player purchase/repair ve accepted Dawn population Food maliyeti tek seferlik transaction olarak açıkça ayrıldı | Targeted PlayMode `3/3`; full EditMode `382/382`; full PlayMode `82 pass + 2 explicit profiler/soak skip`; `NewGameScene` validation `0` issue; final Console `0 error / 0 warning`; tracker `427/442` |
| 2026-07-17 | `DW-V1-DOD-EXACT-POPULATION-WORKERS` unified population/bed/worker exact Continue contract | Tek gerçek snapshot; Population total/workers/archers/idle ve base/capacity, bed base/purchased, dört actual worker count, worker idle/last-observed ve dört target ratio alanını birlikte yazdı. Runtime tuple tamamen değiştirildikten sonra Continue actual archer entity sayısını koruyarak bütün state'i exact geri yükledi; ratio `2700/1900/1600/3800`, worker `23/17/11/25`, idle `17`, purchased beds `15` kanıtı kullanıldı | Targeted PlayMode `3/3`; full EditMode `382/382`; full PlayMode `83 pass + 2 explicit profiler/soak skip`; `NewGameScene` validation `0` issue; final Console `0 error / 0 warning`; tracker `428/442` |
| 2026-07-17 | `DW-V1-DOD-WORKER-WORLD-FEEDBACK` representative worker world release contract | Mevcut allocation/representation owner zinciri değiştirilmedi. Birleşik gerçek-scene guard'ı Wood/Stone/Iron/Food için actual `12/60/101/1000` değerlerini temsili `12/24/27/32` entity'ye çevirdi; toplam `95` visual üzerinde unique index, exact represented weight, geçerli route, resource cargo rengi, weight-scaled feedback ve Night lantern doğrulandı. Presentation katmanı actual allocation truth'unu değiştirmedi | Targeted PlayMode `3/3`; full EditMode `382/382`; full PlayMode `84 pass + 2 explicit profiler/soak skip`; ilk çoklu-filter job test gövdesinden önce Unity Test Framework hatasıyla orphan kaldı, MCP internal recovery ile temizlenip tekil gerçek job'larda geçti; `NewGameScene` validation `0` issue; final Console `0 error / 0 warning`; tracker `429/442` |
| 2026-07-18 | `DW-V1-DOD-HEART-RUNTIME-CONTRACT` production Castle Heart catalog + polished generated graph | Eski `TechTreeCatalogSO` scope'u yerinde bırakıldı; canonical idempotent migration builder'ı production v1 `HeartNodeCatalogSO` ile `35` definition üretti ve `NewGameScene` `GameManager` sahibine bağladı. Dört branch, dört Keystone pair, dört repeatable sink, `15` Evolution node ve mevcut gerçek runtime adapter'larına bağlı effect'ler generator validation'dan geçti. Heart graph branch damarları, rarity/state chrome'u, player-facing exact value/cost/owned copy'si ve geniş node layout'uyla polish edildi. Purchase yalnız `GraveEssence` wallet'ından commit oluyor; graph save/Continue sözleşmesi değişmeden korundu | Production asset audit `35` node / `4` Keystone pair / `4` sink / `15` Evolution; deterministic seed sweep EditMode `3/3`; production scene PlayMode `1/1`; full EditMode `385/385`; full PlayMode `85 pass + 2 explicit profiler/soak skip`; ilk retry Unity Test Framework `PlayModeRunTask.cs:52` orphan job'u MCP internal recovery ile temizlendi; FullHD Game View visual QA; `NewGameScene` validation `0` issue; final Console `0 error / 0 warning`; tracker `430/442` |
| 2026-07-18 | `DW-V1-DOD-10K-1K-FRAME-PACING` target-hardware combined-load acceptance | Explicit Player fixture production enemy pool ile exact 10.000 enemy ve production `RestoreArcherCountsWithinCapacity` ile 1.000 canonical Basic Archer kuruyor. Normal run ammo kapasitesinin ölçümü erken kesmemesi için yalnız fixture Arrow stoku saturating kapasiteye alındı; release tuning değişmedi. Binary Profiler/callstack kapalı 180 warmup + 600-frame pencere hardware/quality/entity/combat kimliğiyle JSON yazıyor ve average/P95/P99 eşiklerini otomatik kabul ediyor. Ayrı 120-frame callstack raw yalnız owner analizi için korunuyor; Editor ve instrumented raw pacing sertifikası sayılmıyor | StandaloneWindows64 explicit `1/1`; i5-14400F + Arc B580, 1920x1080 Ultra; exact `10.000/1.000`, projectile peak `670`; instrumentation-free average/P95/P99/max `7,665/13,890/14,058/27,722 ms`, `4/600` frame 16,667 ms üstü, `0/600` frame 33,333 ms üstü; raw `74.879.570 B`, analyzer `120/120`; full EditMode `385/385`; full PlayMode `85 pass + 2 explicit profiler/soak skip`; `NewGameScene` validation `0` issue; final Console `0 error / 0 warning`; tracker `431/442` |
| 2026-07-18 | `DW-V1-DOD-RELEASE-TEST-REPORTS` full regression + migration matrix + fresh soak closure | Run save destek sınırı `v3-v15` kaynak/test matrisiyle denetlendi; eksik v7 finite-Arrow CAP/EFF `L0` migration regresyonu eklendi. RunPersistence + Meta v1/v2->v3 + legacy regular-Council grubu birlikte çalıştırıldı. Explicit release-cap soak güncel kodla yeniden alındı. İlk full PlayMode turundaki 2K Fireball test-order dalgalanmasının realtime bekleme sırasında yetersiz simulation update'inden geldiği kanıtlandı; fixture minimum queue/refill frame guard'ıyla stabilize edildi, gameplay değişmedi. Yeni `DEAD_WALLS_V1_RELEASE_VERIFICATION_REPORT.md` bütün kanıtı tek yerde toplar | Migration group `55/55`; fresh soak `1/1`, cap `900`, 3.600 frame average/P95/max `6,227/8,227/34,610 ms`, backlog `9.989 -> 0`, demanded/spawned `10.889/10.889`, Arrow rent/return `45.000/45.000`; stabilized 2K targeted `1/1`; full EditMode `386/386`; clean full PlayMode `85 pass + 2 explicit profiler/soak skip`; `NewGameScene` validation `0` issue; final Console `0 error / 0 warning`; tracker `432/442` |
| 2026-07-18 | `DW-V1-CONTENT-META-CURRENCY-IDENTITY` Last Embers catalog migration + death-screen polish | Generic player-facing `SOULS` kimliği `MetaUpgradeCatalogSO.Presentation v2` altında `LAST EMBERS / EMBERS` ve stable `last_embers` Id'sine taşındı. Generated transparent cracked-ember icon Sprite/Point/256 olarak production kataloga bağlandı. Migration reward settings, 11 stable upgrade asset'i, legacy `Souls` save alanı, bakiye, upgrade level/pool/receipt state'ini koruyor. Scene-owned Game Over ekranı `1120x880` dark rounded frame, run summary, reward bandı, iconlu bakiye, açıklamalı 68px satırlar, maskeli vertical ScrollRect ve ayrı next-run CTA ile yeniden kuruldu | Imagegen chroma-key + local alpha validation temiz; targeted EditMode `15/15` + final identity regression `2/2`; full EditMode `388/388`; full PlayMode `85 pass + 2 explicit profiler/soak skip`; FullHD Game View visual QA; scene validation `0` issue; final Console `0 error / 0 warning`; tracker `433/442` |
| 2026-07-18 | `DW-V1-CONTENT-NARRATIVE-PREMISE` canon premise + opening copy | Player role `Steward`, run'ın anlatı adı `stand`, Castle Heart'ın Wall düşüşünde stand'i yeniden tutuşturması ve yalnız `Last Embers` hafızasının kalması tek canon spec'te kilitlendi. Heart/dead origin, named kingdom/faction, chosen hero, boss/elite ve Council loop awareness deliberate unknown/kapsam dışı kaldı. Açılış uzun prologue modal'i yerine `DEAD WALLS` / `THE WORLD ENDED. THE SIEGE DID NOT.` / `BEGIN THE STAND` üçlüsüyle gerçek MainMenuScene'e bağlandı | Blueprint PDF page `4/5/39` visual audit; targeted narrative EditMode `2/2`; full EditMode `390/390`; full PlayMode `85 pass + 2 explicit profiler/soak skip`; FullHD MainMenu Play Mode visual QA; MainMenu repair menu + runtime/doc/scene drift guard; `MainMenuScene` ve `NewGameScene` validation `0` issue; `NewGameScene` aktif/clean; final Console `0 error / 0 warning`; tracker `434/442` |
| 2026-07-18 | `DW-V1-CONTENT-HEART-NODE-CATALOG` launch catalog lock + clean legacy provenance + graph polish | Production v1 catalog'un `35` stable node Id'si, branch/type dağılımı, effect/cost/depth/soft-cap değerleri ve garanti/sink sözleşmesi yeni launch authority dokümanında kilitlendi. Builder, yararlı `18` legacy TechTree fikrini yalnız provenance olarak yeni asset'lere yazar; eski kaynak maliyeti, prerequisite/reveal edge'i ve level state'i taşınmaz, system root/Basic/Moat bilinçli dışarıda kalır. Runtime effect/cost/Id semantiği değişmediği için catalog v1 ve exact graph save uyumluluğu korundu. Heart kartları doğru node-specific ikonlar, kesilmeyen branch eyebrow, açık `ESSENCE` maliyeti, `UNLOCK/DEEPEN/EVOLVE/COMMIT` fiilleri ve tekrarsız Keystone lock copy'siyle polish edildi | Production rebuild `35` canonical node / scene binding true; catalog fingerprint + classification + provenance + spec-sync targeted EditMode `6/6`; full EditMode `393/393`; full PlayMode `85 pass + 2 explicit profiler/soak skip`; FullHD root/initial, fully revealed graph ve Keystone card visual QA; `NewGameScene` validation `0` issue; final Console `0 error / 0 warning`; tracker `435/442` |
| 2026-07-18 | `DW-V1-CONTENT-KEYSTONE-TRADEOFFS` true-choice forks + four launch trade-offs | Dört branch'in owner-approved Keystone çiftleri gerçek paralel commitment fork'larına dönüştürüldü. Symmetric reveal, exact partner lock, ortak continuation reveal ve legacy exact-save görünürlük normalization sözleşmeleri catalog-aware runtime sahiplerine taşındı. Castle Heart grafiği fractional depth, branch yönüne göre iki-kart yerleşimi, altın pair/fork edge'i, dinamik chosen/locked copy'si ve tween lifecycle cleanup'ıyla polish edildi | Targeted EditMode `32/32`; Keystone + Dusk order PlayMode `2/2`; full EditMode `397/397`; full PlayMode `86 pass + 2 explicit profiler/soak skip`; FullHD true-choice overlay QA; `NewGameScene` validation `0` issue; targeted Keystone PlayMode sonrası DOTween `0` warning; tracker `436/442` |
| 2026-07-18 | `DW-V1-CONTENT-FIREBALL-EVOLUTIONS` behavior evolutions + aggregate VFX direction | Launch Fireball içeriği iki gerçek davranış evolution'ıyla kilitlendi. Scorched Earth ilk patlamadan sonra `5` saniye boyunca `1` saniye aralıklı, her biri base impact'in `%12`si olan `5` aggregate tick'i `%70` radius'ta uygular; Echoing Detonation `0,85` saniye sonra `%60` damage ve `%85` radius ile ikinci aggregate blast üretir. Production Heart catalog v2 `37` canonical node'a taşındı; v1 graph migration kimliği güncellerken node/reroll uydurmaz. Run save v16 projectile flag, pending strike, delayed blast ve burning-ground next tick/remaining tick state'ini exact korur. Scorched Earth zemin izi ile Echoing Detonation sıcak-altın blast ayrımı per-enemy VFX/event üretmeden uygulanır | Targeted EditMode `51/51`; evolution exactness + gerçek GameManager save/Continue PlayMode `2/2`; migration group `56/56`; full EditMode `400/400`; full PlayMode `88 pass + 2 explicit profiler/soak skip`; production catalog rebuild `37` node / scene binding true; `NewGameScene` validation `0` issue; final Console `0 error / 0 warning`; tracker `439/442` |
| 2026-07-18 | `DW-V1-CONTENT-EXACT-TUNING-CURVES` exact launch curves + telemetry targets | Mevcut production Difficulty/Archer/Meta değerleri spekülatif rebalance yapılmadan launch authority olarak kilitlendi. Yeni provider-independent target profile Spawn/Economy/Combat/Council/Meta için 19 measurable band, cohort, minimum sample ve canonical source event tanımlar; Difficulty Tuner bunları read-only polished panelde gösterir. `run_ended v2`, durable death sonrasında upgrade-applied Wall MaxHP, final resources, Arrow, population, archer mix ve unspent Essence snapshot'ını ekler. Band dışı ölçüm yalnız designer review tetikler; provider ve automatic retuning yoktur | Target contract EditMode `4/4`; telemetry EditMode `28/28`; gerçek `NewGameScene` telemetry PlayMode `9/9`; full EditMode `404/404`; full PlayMode `88 pass + 2 explicit profiler/soak skip`; target validation `19/19`, fingerprint `58fc60a0...f8059dd3`; scene validation `0` issue; final Console `0 error / 0 warning`; tracker `440/442` |
| 2026-07-18 | `DW-V1-CLOSURE-ARCHER-HEART-EFFECTS` Heart-owned archer stat progression | Mevcut production Heart effect adapter'ı yeni bir paralel sistem kurulmadan tek V1 owner olarak kapatıldı. Direct Basic/Rapid/Frost level cost/upgrade API'si, type-level damage/fire-rate ve Frost duration/slow çarpanları kaldırıldı; Market row'u sayısal `LV` yerine `HEART/TECH` sahipliğini gösteriyor. Production v2 katalog damage, fire rate, range ve Frost slow kategorilerini exact node/target contract'ıyla kilitliyor. Run save v17, v16 direct archer level listesini graph/node/Essence uydurmadan retired eder; exact Heart graph korunur. Heart satın alımı existing entity'leri rebase eder, future spawn aynı effective state'i alır ve Continue bonusu compound etmez | Targeted EditMode `14/14`; Heart/archer PlayMode `6/6`; full EditMode `408/408`; full PlayMode `89 pass + 2 explicit profiler/soak skip`; live production catalog `7` archer effect row / `0` validation error; `NewGameScene` binding true, scene validation `0` issue; final Console `0 error / 0 warning`; tracker `441/442` |
