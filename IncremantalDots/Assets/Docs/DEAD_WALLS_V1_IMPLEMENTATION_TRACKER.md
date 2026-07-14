# Dead Walls V1 - Implementation Tracker

> **Amaç:** V1 Blueprint hedefi ile mevcut Unity projesi arasındaki farkı tek yerde tutmak; nerede kaldığımızı, sıradaki işi ve tamamlanma kanıtını kaybetmemek.
>
> **Tracker sürümü:** 2.0  
> **Son tam kapsam denetimi:** 2026-07-12  
> **Aktif paket:** Package D - Archers + Ammo
> **Aktif iş:** `DW-D-RETRAIN` - Basic → Rapid/Frost Tek Seferlik Retrain

---

## 1. Otorite ve Kullanım Kuralı

### Kaynak sırası

1. **Tasarım otoritesi:** [DEAD_WALLS_GAME_DESIGN_BLUEPRINT_v1.0.pdf](./DEAD_WALLS_GAME_DESIGN_BLUEPRINT_v1.0.pdf)
2. **Düzenlenebilir tasarım kaynağı:** [DEAD_WALLS_GAME_DESIGN_BLUEPRINT_v1.0.docx](./DEAD_WALLS_GAME_DESIGN_BLUEPRINT_v1.0.docx)
3. **Uygulama ve fark takibi:** Bu tracker.
4. **Mevcut runtime gerçeği:** Aktif scene, prefab, ScriptableObject ve kod sahipleri.
5. **Tarihsel bağlam:** Eski GDD, roadmap, master plan ve milestone belgeleri.

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
| Castle Heart | Run teknolojisinin procedural graph ekranı ve tek upgrade owner'ı |
| Council | Her 3 günde bir regular ve nadir emergency karar açan run yönetim katmanı |
| Meta | Yalnız ölümden sonra kalan sabit kalıcı upgrade listesi |
| Grave Essence | Yalnız run içi Heart node'larında harcanan ve ölümde silinen kaynak |
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

### Oyuncu eylemleri ve ritim

| Ritim | Oyuncu eylemi | Sistem |
|---|---|---|
| Sürekli | Kaynak ve Arrow stokunu izler | Üst HUD + ammo satın alma |
| Sık | Worker target ratio değiştirir | Farm/Lumberyard/Quarry/Mine |
| Sık | Archer alır veya retrain eder | Basic/Rapid/Frost, ortak 1000 cap |
| Dönemsel | Day/Dusk sırasında Wall onarır | Stone ile tek seferlik normal repair |
| Dönemsel | Council kararı verir | 3/6/9... + rare emergency |
| Dönemsel | Castle Heart node'u alır | Grave Essence + full pause |
| Taktik | Fireball/Rally/Emergency Repair kullanır | Alt orta cooldown barı |

### Canlı Unity/repo gerçeği - 2026-07-12

| Alan | Mevcut gerçek | Sonuç |
|---|---|---|
| Aktif scene | `Assets/Scenes/NewGameScene.unity`, Unity MCP'de loaded ve clean | Kanıtlandı |
| Kamera | Ortografik, size `8`, gameplay pan/zoom controller yok; `CameraShaker` var | Temel sabit kamera uyumlu |
| Oyun hızı | Oyuncu kontrollü x2/x4 veya offline progress owner'ı bulunmadı | Blueprint ile uyumlu; regression gerekli |
| Battlefield | Kale/duvar solda, spawn sağdaki `SpawnLineX` bandından geliyor | Temel kompozisyon uyumlu |
| Build placement | Aktif scene'de `BuildingPlacementUI` ve `BuildingGridManager` bağlı değil | Hazır bina yönüyle uyumlu |
| Cycle | `Day 30 / Dusk 5 / Night 20 / Dawn 5`; dört fazda pozitif spawn temposu | Uyumlu |
| Horde | Tek catalog prefabı; sabit stats; saved backlog; expandable bulk rent/return pool; Blood Moon dormant | 10K gate ve optimizasyon ölçüldü |
| Moat | Runtime flag kapalı; slow `1`, damage `0`; tech/meta catalog bağlantıları dormant | Uyumlu |
| Defense | Damage/Game Over aktif ve testli olarak tek Wall'a çekildi | `[x]` |
| Normal repair | Stone-only ve yalnız Day/Dusk | `[x]` |
| Save | Exact same-moment Continue; schema v6, minimum v3; purchased bed ve worker bina yatırım state'i exact | `[x]` |
| Economy | Worker üretimi, bed alımı ve dört hazır binanın capacity/efficiency yatırımları var; bed ve bina fiyat eğrileri `DefaultDifficulty.asset`/Difficulty Tuner üzerinden baked runtime tuning'e bağlı; V1 ana kaynaklarında pasif consumption yok | `[x]` |
| Population | House bed state + Wood purchase API + exact save var; Dawn isteği boş yatak ve Food/kişi bütçesiyle sınırlı, gerçek accepted count uygulanıyor, Food bir kez düşülüyor ve en fazla 15 temsili survivor sağdan Wall arkasına yürüyor | `[x]` |
| Workers | Kalıcı target ratio + actual/cap/idle state, +1/+10/+100/direct input, bağımsız bina capacity/efficiency seviyeleri, yeni nüfus auto-allocation, exact save, Low/Medium/High density ve allocation-senkronlu animation/cargo/lantern/delivery feedback var | `[x]` |
| Council | Curated/deterministic composer ve kart UI var; schedule chance/pity/cooldown | Package F altyapısı var, schedule yanlış |
| Archers | Basic/Rapid/Frost, instant buy ve population cost var | Kısmi uyum |
| Archer cap | `ArcherCapacityUtility` Basic/Rapid/Frost toplamını `1000` ile sınırlar; buy, merkezi spawn, Council, meta, restore ve legacy Barracks aynı guard'ı kullanır | `[x]` |
| Placement | `outside` tilemap merkezleri + küçük stack offset | 40x25 değil |
| Targeting | Her okçu bütün zombileri brute-force tarıyor | 1k x 10k blocker |
| Ammo | Config `UnlimitedArrows = true`; refill butonu bağlı değil | Package D uyumsuz |
| Tech/Heart | Sabit SO catalog + reveal graph + ana kaynak maliyeti | Generated Heart değil |
| Fireball | Dünya hedefli projectile/AoE ve cooldown çalışması mevcut | Korunacak temel |
| Rally | Wood/Food maliyetli prep purchase | Cooldown-only ability olmalı |
| Emergency Repair | Ayrı ability yok | Eksik |
| Meta | Ayrı JSON ve Game Over shop var; `StartingTechLevel` aktif | Kısmi uyum |
| HUD | CyclePanel, DAY/DUSK/NIGHT ve Horde Pressure mevcut; tek Wall runtime gizleme var | Package I polish gerekli |
| Tutorial | Aktif tutorial/onboarding sistemi bulunmadı | Package I eksik |
| Testler | EditMode `92/92`; PlayMode `22 pass + 1 explicit skip`; Standalone Player-targeted 10K `1/1` | Güncel değişiklikler full paketle testli |
| Telemetry | Spawn budget demanded/spawned/backlog telemetry mevcut; tam Blueprint event owner'ı eksik | Kısmi |

---

## 4. Paket Sırası ve Anlık İlerleme

| Sıra | Paket | Durum | Sonraki pakete geçiş kapısı |
|---:|---|---|---|
| 1 | A - System Contracts | Tamamlandı | Reset/Continue deterministik; upkeep yok; tek Wall testli |
| 2 | B - Continuous Horde | Tamamlandı | Sabit stats, backlog/pool ve 10K ürün ölçümü tamamlandı |
| 3 | C - Economy + Population | Tamamlandı | Pasif drain yok; arrival tek Food öder; cap aşılmaz; fiyat tuning'i testli |
| 4 | D - Archers + Ammo | **Aktif** | 1.000 x 10.000; 40x25; Arrow truth çalışır |
| 5 | E - Castle Heart | Bekliyor | Aynı seed/load aynı valid graph'ı üretir |
| 6 | F - Council | Bekliyor | 3/6/9 bozulmaz; etkiler ana cap'leri bypass etmez |
| 7 | G - Active Abilities | Bekliyor | Kaynak tüketmez; Night repair sözleşmesi çalışır |
| 8 | H - Meta + Persistence | Bekliyor | Ölüm ödülü idempotent; force-close ölümü geri alamaz |
| 9 | I - Product Gate | Bekliyor | 10k scenario, tutorial ve temiz görsel inceleme |

> “A1/A2” resmî Blueprint paketi değildir. Resmî paketler A-I'dır; iş kimlikleri yalnız tracker içinde `DW-A-SAVE` gibi kullanılır.

---

## 5. Package A - System Contracts

### Mevcut oyun ile karşılaştırma

| Sözleşme | Mevcut oyun | Durum |
|---|---|---|
| Tek Wall truth | Damage, Game Over, authoring, repair ve save defense alanı Wall'a çekildi | `[x]` 19/19 EditMode |
| Run/meta ayrımı | Exact run `run_save.json`; kalıcı progression `meta_progress.json`; ölüm transaction'ı ayrı receipt | `[x]` |
| Exact Continue | Schema v6 aynı cycle/phase/timer, kaynak, spawn RNG, worker target/checkpoint, purchased bed ve worker bina yatırım state'ini restore ediyor; v3/v4/v5 migrate ediliyor | `[x]` EditMode + PlayMode |
| Otomatik save | Ana menüye dönmeden önce ve application quit sırasında exact snapshot alınıyor | `[x]` |
| Gönüllü reset yok | Aktif run sırasında Main Menu New Run ve Pause Restart kapalı; Game Over Restart yeni koşu başlatır | `[x]` |
| Upkeep yok | V1 ResourceTick consumption'ı yok sayıyor; population Food ve Fletcher Wood yolları castle loop'ta kapalı | `[x]` |
| Legacy Gate/Core disabled | Gate/Core data/UI referansları dormant; runtime damage, Game Over, repair, Council, save ve HUD tek Wall | `[x]` |
| Tuning owner | Difficulty alanları Profile, diğer baseline alanlar active SubScene Authoring, birleştirme tek resolver | `[x]` |
| Normal repair resource/phase | GameManager Stone-only cost üretiyor; Day/Dusk dışını gameplay ve UI seviyesinde kapatıyor | `[x]` |

### `DW-A-SAVE` - Tamamlandı: Exact Run Snapshot & Continue

- [x] Run save schema güncel `v6`, minimum `v3`; v2 Dawn checkpoint reddediliyor, v3 worker target, v3/v4 bed ve v3/v4/v5 worker bina yatırım state'iyle deterministik migrate ediliyor.
- [x] Gün/cycle index, aktif phase, exact cycle timer/progress ve spawn RNG state'i kaydediliyor.
- [x] Wood, Stone, Iron, Food, Arrow current ve kesirli accumulator state'i kaydediliyor; data-driven capacity tech/config'ten yeniden hesaplanıyor.
- [x] Population, bed/capacity, actual worker count, target ratio/cap/idle/checkpoint ve growth/event tekrar gate'leri capture/restore zincirinde kaydediliyor.
- [x] Basic/Rapid/Frost count, archer level ve ilgili run bonus state'i kaydediliyor.
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
- [x] Wood ile anlık arrow satın alma bu işin parçası olarak sahte biçimde kapatılmadı; aktif config hâlâ `UnlimitedArrows=true`, çözüm Package D'de açık.

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
| Basic retrain | Unlock ve buy var; Basic -> Rapid/Frost retrain owner'ı yok | `[!]` |
| Archer death yok | Archer HP/death combat yolu yok | `[~]` Regression gerekli |
| Upgrade yalnız Heart | Market'te type level ve upgrade butonları aktif | `[!]` |
| 40x25 placement | Tile center + stack offset; preview 96 | `[!]` |
| Nearest valid target | Brute-force ile nearest-to-archer uygulanıyor | `[~]` Davranış doğru, ölçek yanlış |
| Incoming damage reservation | Yok | `[!]` |
| Arrow -1/shot | Kod destekliyor fakat `UnlimitedArrows=true` bypass ediyor | `[!]` |
| Wood ile instant refill | Mevcut refill ücretsiz ve sabit target'a çekiyor; UI gizli | `[!]` |
| Fletcher yok | Legacy Fletcher kodu var; aktif mobile UI'da bağlı değil | `[~]` Leakage guard gerekli |

### Archer ve retrain işleri

- [x] Toplam Basic+Rapid+Frost cap'ini `1000` olarak tek owner'da uygula.
- [x] 1.001. satın alımı reddet; kaynak/population harcama.
- [x] Council, meta başlangıç bonusu ve restore spawn'ında aynı cap guard'ını kullan.
- [ ] Basic -> Rapid ve Basic -> Frost retrain işlemini tek seferlik maliyetle uygula.
- [ ] Retrain toplam archer ve population sayısını değiştirmesin.
- [ ] Type maliyetini aynı türün mevcut sayısına göre büyüt.
- [ ] Market'teki ayrı Basic/Rapid/Frost level ve upgrade akışını kaldır/disable et.
- [ ] Hasar, fire rate, range, Frost slow upgrade'lerini Heart effect pipeline'ına taşı.
- [ ] Archer death/individual HP yolunun eklenmesini regression ile engelle.

### 40x25 formation işleri

- [ ] Kullanılacak tam 40 `outside` tile'ı data/version ile sabitle.
- [ ] Her tile için `tile coordinate + local slot index` seed'iyle 25 nokta üret.
- [ ] Noktaları izometrik diamond içinde güvenli inset ile örnekle.
- [ ] Minimum local mesafe uygula.
- [ ] Fill order'ı layer mantığıyla kur: önce 40 tile slot 0, sonra 40 tile slot 1...
- [ ] Save yalnız type count tutarken stable algorithm ile aynı formasyonu kur.
- [ ] Editor gizmo preview'u bütün 1000 noktayı göstersin.
- [ ] Formation algorithm version'ını save migration'a ekle.

### Targeting ve projectile işleri

- [ ] Mevcut spatial hash'i archer target query için uygun read-only query owner'ına dönüştür.
- [ ] Her okçu range içindeki yaşayan/death-state olmayan en yakın düşmanı seçsin.
- [ ] Basic/Rapid/Frost aynı target policy'yi kullansın.
- [x] Projectile target pool'a döner/yeniden rent edilirse generation mismatch ile deterministik cleanup uygula; retarget yapma.
- [ ] Incoming damage reservation/load ile overkill'i dağıt.
- [ ] Target search'ü Burst/job ölçeğinde 1k x 10k için ölç.
- [ ] Projectile instantiate/destroy churn'ünü pooling veya burst-safe lifetime yaklaşımıyla çöz.

### Ammo işleri

- [ ] `UnlimitedArrows` V1 active config'ini kaldır/false yap.
- [ ] Atılan gerçek projectile başına tam 1 Arrow düşür.
- [ ] Arrow `0` olduğunda okçuları durdur; refill sonrası aynı frame yeniden başlat.
- [ ] Wood maliyetli sabit oranlı refill paketleri ekle.
- [ ] Refill'i production queue olmadan anlık uygula.
- [ ] Current/Capacity, paket fiyatları ve `Buy Max` kontrolünü UI'da göster.
- [ ] Arrow capacity ve efficiency upgrade'lerini Heart/run purchase owner'ına bağla.
- [ ] Refill başına birim fiyatın sonsuza büyümesini engelle; ordu/fire rate doğal talebi yaratsın.
- [ ] Rapid'in yüksek fire rate'inin daha fazla Arrow tükettiğini test et.
- [ ] Legacy Fletcher/ArrowProduction V1 akışına sızmasın.

### Package D kabul kapısı

- [x] 1.001. archer alınamıyor.
- [ ] Retrain toplam sayıyı değiştirmiyor.
- [ ] 40 tile x 25 stable point save/load sonrası aynı.
- [ ] 1.000 archer x 10.000 enemy gerçek oyun senaryosu çalışıyor.
- [ ] Ammo truth ve refill davranışı korunuyor.

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
| Save | Sabit node level'ları save ediliyor | Generated graph, edge, hidden/reveal, locks ve version save edilmeli |
| Guarantees | Sabit catalog içeriğine bağlı | Her graph Rapid/Frost/Fireball reachable validation gerekli |
| Node türleri | Generic node/effect yapısı | Unlock/Repeatable/Evolution/Keystone semantiği eksik |
| Duplicate upgrades | Market archer level/upgrade butonları aktif | Heart tek teknoloji owner'ı olmalı |

### E1 - Data model ve catalog

- [ ] `HeartNodeDefinition` contract oluştur: id, tags, effects, rarity, depth range, repeatable, base cost, cost growth, conflicts.
- [ ] Node'ları `Unlock`, `Repeatable`, `Evolution`, `Keystone` türleriyle açıkça sınıflandır.
- [ ] Mevcut `TechNodeDefinitionSO` içeriklerini yeni contract'a migrate etme planı çıkar.
- [ ] `GeneratedRunGraph` contract oluştur: seed, graph version, node ids, edges, hidden/revealed, levels, locks.
- [ ] Source asset'lerin runtime state taşımamasını garanti et.
- [ ] Grave Essence run resource/state ve tek Heart spending owner'ını oluştur.
- [ ] Grave Essence'ın ölümde silinmesini run save matrisiyle güvenceye al.

### E2 - Graph üretimi

- [ ] Castle Heart merkez/root node'unu sabitle.
- [ ] Ordu, Savunma, Üretim ve Heart/Büyü yön pusulasını sabitle.
- [ ] Run seed ve dört yön iskeletini oluştur.
- [ ] Her ana yönün root'a bağlı olduğunu doğrula.
- [ ] Rapid, Frost ve Fireball guarantee node'larını izinli derinliğe yerleştir.
- [ ] Temel Wall/defense erişimini koru.
- [ ] Her ana yönde en az bir repeatable sink garanti et.
- [ ] Node havuzunu tag + rarity + depth kurallarıyla doldur.
- [ ] Duplicate node ve invalid prerequisite üretimini engelle.
- [ ] Edge ve kontrollü cross-link üret.
- [ ] Keystone çiftlerini yalnız birbirini kapatacak biçimde yerleştir.
- [ ] Normal node'un yanlışlıkla başka yolu lock etmesini engelle.
- [ ] Disconnected graph, dead core path ve unreachable guarantee durumunda validation reroll/fallback uygula.
- [ ] Graph tamamen run başlangıcında üretilsin; reveal anında RNG kullanma.
- [ ] Graph valid değilse run'ı sessizce broken state ile başlatma; açık hata/fallback üret.

### E3 - Reveal ve oyuncu bilgisi

- [ ] Başlangıçta Heart ve bağlı ilk seçenekleri tamamen göster.
- [ ] Uzak node'larda yalnız yön rengi/damarını göster; exact node'u gizle.
- [ ] Gizli node içeriğini run başında kesinleştir ve save'e yaz.
- [ ] İlk node alımında yalnız bağlı komşuları reveal et.
- [ ] Save-scum ile hidden graph reroll olmasını engelle.
- [ ] Oyuncunun gördüğü node'un effect ve gerçek sayısal sonucunu açıkça göster.
- [ ] Keystone görünür olduğunda karşıt seçimi ve kapanacak node'u açıkça işaretle.

### E4 - Node satın alma ve etkiler

- [ ] Bütün Heart satın alımlarını yalnız Grave Essence ile yap.
- [ ] Unlock node'u tek satın alma ile sistemi ve devam yolunu açsın.
- [ ] Repeatable node için `+1 / +10 / Buy Max` ekle.
- [ ] Evolution node'larını davranış değiştirici tek seferlik effect olarak uygula.
- [ ] Keystone seçiminin yalnız eş Keystone'u kapatmasını sağla.
- [ ] Damage/maliyet gibi büyük değerleri destekle.
- [ ] Fire rate, cooldown, slow ve range için soft-cap/diminishing return uygula.
- [ ] Soft-cap yüzünden node'u görünmez biçimde etkisizleştirme; kalan gerçek değeri UI'da göster.
- [ ] Archer damage/fire rate/range/Frost slow upgrade'lerini Heart effect pipeline'ına taşı.
- [ ] Wall Max HP, worker capacity/efficiency, Arrow capacity/efficiency ve ability upgrade'lerini aynı pipeline'a bağla.
- [ ] Fireball damage/radius/cooldown repeatable node'larını destekle.
- [ ] Burning ground/second blast gibi evolution'ları yalnız onaylı pool'dan üret.

### E5 - Heart ekranı ve pause

- [ ] HUD Castle Heart butonu full-screen graph açsın.
- [ ] Heart açıkken cycle timer dursun.
- [ ] Heart açıkken spawn ve movement/combat dursun.
- [ ] Heart açıkken worker production/allocation simulation dursun.
- [ ] Heart açıkken ability cooldown'ları dursun.
- [ ] Mouse drag pan ve wheel zoom yalnız Heart ekranında çalışsın.
- [ ] UI interaction, tooltip, buy/reveal ve focus davranışları unscaled UI zamanında çalışsın.
- [ ] Graph kapanınca önceki simulation state'i deterministik devam etsin.
- [ ] Market/Barracks archer upgrade ve direct unlock yüzeylerini kaldır/disable et.

### E6 - Save, migration ve test

- [ ] Seed, graph version, node ids, edge'ler, hidden/reveal, levels ve locks run save'e yazılsın.
- [ ] Continue source asset'ten yeniden zar atmasın; kaydedilmiş graph'ı kursun.
- [ ] Aynı seed + aynı catalog version aynı graph'ı üretsin.
- [ ] Catalog değişiminde eski run graph'ı sessizce başka graph'a map edilmesin.
- [ ] Rapid/Frost/Fireball unreachable graph testi ekle.
- [ ] Normal node accidental lock testi ekle.
- [ ] Keystone pair exclusion testi ekle.
- [ ] Hidden graph save/load testi ekle.
- [ ] Heart full-pause testi ekle.

### Package E kabul kapısı

- [ ] Aynı seed/load aynı graph.
- [ ] Rapid, Frost ve Fireball her run'da reachable.
- [ ] Hidden graph save-scum ile değişmiyor.
- [ ] Heart yalnız Grave Essence kullanıyor.
- [ ] Heart açıkken bütün simulation ve cooldown duruyor.
- [ ] Ayrı archer upgrade owner'ı kalmıyor.

---

## 10. Package F - Council

### Mevcut oyun ile karşılaştırma

| Sözleşme | Mevcut oyun | Durum |
|---|---|---|
| Council aktif core sistem | Composer, catalog, atoms ve UI mevcut | `[x]` Altyapı var |
| Curated/no runtime AI | Authored template/atom deterministik compose ediliyor | `[~]` Launch content review gerekli |
| 3/6/9 regular schedule | Daily chance + pity + cooldown | `[!]` |
| Rare emergency | Ayrı emergency type/trigger yok | `[!]` |
| Exact effects visible | İki option ve effect badge mevcut | `[~]` Tüm effects audit gerekli |
| Population guard | `AddPopulation` capacity/Food'u bypass ediyor | `[!]` |
| Archer guard | Free archer population ve 1000 cap'i bypass ediyor | `[!]` |
| Wall-only defense | Heal Wall'a yönlendirildi | `[~]` Test gerekli |
| Count-only night effect | Next-night spawn multiplier mevcut | `[~]` Stat effect leakage testi gerekli |
| Exact save | Flags/recent/cooldown kısmen save; active card/effects eksik | `[!]` |

### Yapılacaklar

- [ ] Regular Council'ı Day `3,6,9,12...` Dawn başlangıcında kesin tetikle.
- [ ] Chance/pity/cooldown'u regular schedule owner'ı olmaktan çıkar.
- [ ] Emergency Council için ayrı type ve owner-approved trigger listesi kullan.
- [ ] Emergency olayın regular day index'ini taşımamasını/sıfırlamamasını sağla.
- [ ] Her kartta tam sayısal iki seçenek ve karar süresi göster.
- [ ] Resource scarcity, production, Wall ve previous flags bağlamını koru.
- [ ] Aynı şablonun anlamsız tekrarını recent memory ile engelle.
- [ ] Yalnız editoryal onaylı flag chain'leri aç.
- [ ] Population gain'i available beds + one-time Food ile sınırla.
- [ ] Free archer gain'i idle population + common 1000 cap ile sınırla.
- [ ] Defense effect'lerini yalnız Wall current/max HP ile sınırla.
- [ ] Horde effect'lerini yalnız count/flow multiplier ile sınırla.
- [ ] Regular schedule, emergency state, chosen option, flags, recent templates ve active duration effects'i exact save et.
- [ ] Council'ın Heart currency/upgrade rolünü veya Meta rolünü devralmasını engelle.
- [ ] Launch template/atom listesi için owner review ve effect budget testi yap.

### Kabul kapısı

- [ ] Regular günler daima 3/6/9 düzeninde.
- [ ] Aradaki emergency regular schedule'ı değiştirmiyor.
- [ ] Hiçbir effect bed+Food, population, 1000 archer, Wall-only veya count-only guard'ını aşmıyor.
- [ ] Save/Continue aynı Council state'ini koruyor.

---

## 11. Package G - Active Abilities

### Mevcut oyun ile karşılaştırma

| Ability | Mevcut oyun | Blueprint hedefi |
|---|---|---|
| Fireball | World target, AoE projectile ve cooldown mevcut | Koru; Heart guarantee/evolution/save ekle |
| Rally | Wood+Food maliyetli prep purchase | Key `2`, global fire-rate buff, cooldown-only |
| Emergency Repair | Yok | Key `3`, Night Wall % heal, uzun cooldown |
| Fortify | Resource-cost prep etkisi mevcut | V1 üçlü ability barında yok; aktif rolü kaldır/ayır |
| Arrow Storm | Aktif bulunmadı | Eklenmeyecek |

### Yapılacaklar

- [ ] Alt orta tek ability barı oluştur: Fireball, Rally, Emergency Repair.
- [ ] Fireball input'unu `1 + world area selection` yap.
- [ ] Rally input'unu `2` yap ve bütün okçulara kısa fire-rate boost uygula.
- [ ] Emergency Repair input'unu `3` yap ve yalnız Night sırasında Wall Max HP yüzdesi heal et.
- [ ] Üç ability'den Wood/Stone/Iron/Food/mana maliyetini kaldır.
- [ ] Ability kullanımını yalnız unlock + cooldown + phase/input guard ile sınırla.
- [ ] Fireball targeting sırasında UI click'lerini cast sayma.
- [ ] Fireball damage/radius/cooldown'u Heart node'larına bağla.
- [ ] Çıkış ability/spell içeriğini Fireball ile sınırla; yeni büyüleri yalnız ileride meta pool unlock yolu için data-driven bırak.
- [ ] Rally ve Emergency Repair cooldown state'ini exact save et.
- [ ] Night normal repair'i kapat; Stone harcanmamasını garanti et.
- [ ] Night başlangıcında açık repair drawer/input davranışını deterministik kapat.
- [ ] Normal repair paket formülünü tuning verisi yap: fixed HP, percent HP veya approved hybrid.
- [ ] Eksik HP başına Stone maliyeti ve day price multiplier tuning alanlarını tanımla.
- [ ] Emergency Repair yüzdesi ve cooldown tabanını tuning alanı yap.
- [ ] Wall `0 HP` ile aynı frame Emergency Repair gelirse Game Over kazansın.
- [ ] Fortify/Rally legacy prep purchase yollarının V1 ability sistemiyle çakışmasını kaldır.
- [ ] Arrow Storm ekleme.

### Kabul kapısı

- [ ] Ability'ler ana kaynak tüketmiyor.
- [ ] Night normal repair harcama yapmıyor.
- [ ] Emergency Repair ölümü geri çevirmiyor.
- [ ] Input ile UI aynı state'i gösteriyor.
- [ ] Cooldown save/load exact.

---

## 12. Package H - Meta + Persistence

### Mevcut oyun ile karşılaştırma

| Sözleşme | Mevcut oyun | Durum |
|---|---|---|
| Meta ayrı save | `MetaProgression` ayrı JSON kullanıyor | `[x]` Yapısal ayrım var |
| Death-only shop | `MetaProgressionUI` GameOver panelinde | `[~]` Voluntary reset yolları riskli |
| Reward inputs | Day+kills+record bonus | Nights/peak pop/record weighting eksik/tuning |
| Idempotent reward | GameOver transition bir kez çağırmayı varsayıyor; persistent receipt yok | `[!]` |
| Fixed upgrade list | Catalog var; mevcut effect listesi Blueprint ile tam eşleşmiyor | `[~]` |
| StartingTechLevel yok | Enum ve uygulama aktif | `[!]` |
| Meta graph'ı değiştirmez | StartingTechLevel graph'ı atlıyor; pool unlock contract yok | `[!]` |
| Tutorial flag | Aktif tutorial/meta flag bulunmadı | `[!]` |

### Hedef meta upgrade listesi

- [ ] Starting resources - Wood/Stone/Iron/Food; node açmaz.
- [ ] Starting Basic Archers - Rapid/Frost açmaz.
- [ ] Starting beds - run bed price curve'ünü silmez.
- [ ] Base Wall HP - Heart Wall node'larını değersizleştirmez.
- [ ] Worker production - küçük global multiplier; run capacity/efficiency devam eder.
- [ ] Arrow efficiency - ammo kararını yok etmez.
- [ ] Essence gain - graph sonucunu değiştirmez.
- [ ] Node pool unlock - yeni olası content ekler; mevcut run'a zorla enjekte etmez.

### Persistence işleri

- [ ] Run save ve meta save alanlarının açık schema sahiplerini ayır.
- [ ] Game Over'da run save içindeki tüm run state'i sil.
- [ ] Meta currency, upgrade levels, pool unlocks ve tutorial flag'lerini koru.
- [ ] Death reward'a unique run identity/receipt ekle.
- [ ] Process restart sonrası aynı ölümün ikinci reward yazmasını engelle.
- [ ] Force-close ile Game Over öncesi snapshot'a dönmeyi engelle.
- [ ] Aktif run sırasında meta satın alımını engelle.
- [ ] `StartingTechLevel` effect ve content'ini yeni modelden kaldır.
- [ ] Meta'nın generated graph edges/Keystone/result seçmesini engelle.
- [ ] Repeatable meta sink'lerde büyüyen maliyet; content unlock'ta tek sefer uygula.
- [ ] Eski Mobile save'i yeni run contract'a sessiz yanlış map etme.
- [ ] 10k enemy pozisyonlarını tek tek save etmek yerine perceptually faithful deterministik rebuild policy tanımla.

### Kabul kapısı

- [ ] Meta ödülü yalnız ölümde ve bir kez yazılıyor.
- [ ] Gönüllü reset/prestige yok.
- [ ] Force-close ölümü geri alamıyor.
- [ ] Meta mevcut run graph'ını geriye dönük değiştirmiyor.
- [ ] Migration guard eski save'i yanlış state'e çevirmiyor.

---

## 13. Package I - HUD, Onboarding ve Creative Polish

### I1 - HUD mevcut oyun karşılaştırması

| Blueprint hedefi | Mevcut canlı HUD | Durum |
|---|---|---|
| Tek minimal Wall bar | Runtime Wall-only yönü var; prefabda Gate/Core binding'leri hâlâ serialize | `[~]` |
| Minimal phase area | Büyük CyclePanel + DAY/DUSK/NIGHT labels | `[!]` |
| Forecast yok | HordePressurePanel aktif bağlı | `[!]` |
| Abilities alt orta | Fireball ayrı Spell panelinde; Rally drawer'da; Emergency yok | `[!]` |
| Workers/Housing alt sol | Worker drawer var; Housing owner yok | `[~]` |
| Archers/Heart alt sağ | Archer drawer ve Heart button var; yerleşim/polish doğrulanmalı | `[~]` |
| Tek drawer | Birden fazla controller bağımsız panel yönetiyor | `[~]` Exclusive owner testi yok |
| Council geçici kart | Geçici iki-option kart UI mevcut | `[~]` Schedule/effect sunumu güncellenecek |
| Fixed camera/ratio | Kamera sabit; ultrawide critical crop testi yok | `[~]` |

### HUD işleri

- [ ] Gate/Core serialized binding ve görsel kalıntılarını active prefabdan temizle veya açık dormant guard koy.
- [ ] Üst kaynak HUD'ını kompakt tut.
- [ ] Üst ortada minimal phase alanı ayır.
- [ ] Büyük CyclePanel ve ham DAY/DUSK/NIGHT sunumunu owner-approved mockup ile değiştir.
- [ ] Horde forecast/pressure panelini kaldır.
- [ ] Fireball/Rally/Emergency Repair'ı alt orta tek cooldown barına taşı.
- [ ] Workers/Housing alt sol yerleşimini kur.
- [ ] Archers/Castle Heart alt sağ yerleşimini kur.
- [ ] Aynı anda yalnız bir management drawer açık olacak owner kur.
- [ ] Council kartında iki exact effect ve karar süresini göster.
- [ ] 16:9 ve ultrawide'da battlefield ve kritik UI crop testleri yap.

### I2 - İlk koşu onboarding

- [ ] İlk Day: worker ratio düğmesini pulse + tek satır metinle öğret.
- [ ] İlk kaynak yeterliliği: Basic Archer drawer highlight göster.
- [ ] İlk düşük ammo: ammo satırını highlight et; zorunlu popup açma.
- [ ] İlk kill/Essence: Heart butonunu pulse et; açılınca full pause öğret.
- [ ] İlk regular Council/Day 3: bedel ve iki exact sonucu öğret.
- [ ] İlk Wall hasarı sonrası Day: normal repair action'ı highlight et.
- [ ] İlk Night: unlock olan ability üzerinde key hint göster.
- [ ] Tutorial oyuncu adına kaynak harcamasın veya worker dağıtmasın.
- [ ] Sürekli modal pause zinciri kurma.
- [ ] İşlem prompt'tan önce yapılırsa adımı tamamlanmış say.
- [ ] Tutorial complete flag'i meta save'e yaz.
- [ ] Settings içinde tutorial reset ekle.
- [ ] Player-facing bütün tutorial metnini English yap.
- [ ] İkinci run'da tutorial'ın otomatik açılmadığını test et.

### I3 - Day/night görsel ve işitsel yön

- [ ] Day: sıcak ışık, okunur üretim, worker ambience.
- [ ] Dusk: amber -> indigo geçiş, fenerlerin yanması, tension riser.
- [ ] Night: soğuk ay, güçlü silhouette, pencere/ok salvosu, rate-limited yoğun mix.
- [ ] Dawn: cyan/altın kırılma, kapı/survivor gelişi, nefes/yeni gün cue.
- [ ] Faz geçişini büyük tam ekran yazı yerine grading, sky, particles ve audio ile okut.
- [ ] 10k horde için ground contrast, silhouette edge ve motion cadence koru.
- [ ] Hit VFX/SFX'i her düşmanda üretme; budget/rate limit uygula.
- [ ] Fireball ve Frost feedback'ini horde içinde kaybolmayacak hierarchy ile sun.
- [ ] Archer salvolarını tek tek projectile görsel kaosu yerine okunur toplu ritme çevir.
- [ ] Blood Moon görsel/audio/warning active bağlantılarını kaldır.

### Package I kabul kapısı

- [ ] İlk-run tutorial tamamlanıyor; ikinci run'da otomatik açılmıyor.
- [ ] Tek Wall bar ve minimal phase UI owner onayından geçiyor.
- [ ] 16:9/ultrawide temiz render.
- [ ] 10k horde okunabilir.
- [ ] Day/night lighting, audio ve combat feedback görsel/işitsel review'dan geçiyor.

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
| Heart | `TechNodeDefinitionSO`, `TechTreeCatalogSO`, `TechTreeUI` | Generator + Grave Essence + exact graph save |
| Council | `CouncilComposer`, `CouncilEventUI`, catalog | 3/6/9 + emergency + guarded effects |
| Meta | `MetaProgression` | Death-only fixed list + idempotent receipt |
| HUD | `MobileCastleHudRoot`, `HUDController` | Single Wall + minimal cycle + bottom abilities |

### Oluşturulacak/uyarlanacak contract'lar

- [x] `EnemyDefinition`: id, prefab, base stats, pool prewarm/expand, spawn weight.
- [ ] `RunDifficultyProfile`: BaseSpawn curve, phase multipliers, active cap, backlog policy.
- [ ] `HeartNodeDefinition`: tags, effects, rarity, depth, repeatable, cost growth, conflicts.
- [ ] `GeneratedRunGraph`: seed/version, node ids, edges, hidden/revealed, levels, locks.
- [ ] `WorkerAllocation`: four target ratios, actual counts, caps, idle population.
- [ ] `ArcherFormation`: 40 cells, 25 local points, algorithm version.
- [ ] `ActiveAbilityState`: unlocks, cooldown remaining, tuning multipliers.
- [ ] `CouncilRunState`: regular day index, emergency trigger, flags, recent templates, active effects.
- [ ] `MetaState`: currency, upgrade levels, pool unlocks, tutorial flags, death receipts.

### Teknik sınırlar

- [ ] Mevcut owner'lara paralel ikinci runtime sistem kurma; source owner'ı dönüştür.
- [ ] `MobileCastle*` isimlerini yalnız estetik için toplu rename etme.
- [ ] Definition asset ile runtime state'i birbirine karıştırma.
- [ ] Dormant legacy code'un active V1 owner'ına bağlanmasını açık review olmadan yapma.

---

## 15. Performans, Tuning ve Telemetry

### Performans sözleşmesi

| Alan | Mevcut durum | Gerekli iş |
|---|---|---|
| Enemy spawn/death | Prewarm/expand + rent/return; backlog bağımsız | 10K frame pacing ve allocation ölçümü |
| Archer target search | 1 archer x all enemies brute-force | Spatial query + target load |
| Projectiles | Instantiate/destroy | Pool/burst-safe lifetime |
| VFX/SFX | CombatFeedbackBridge pool ve bazı min interval'lar var | 10k budget/aggregation audit |
| Worker visuals | Low/Medium/High representative density; resource başına 32 visual cap | `[x]` |
| Save | Exact aktif combat snapshot var; inactive pool catalog'dan yeniden kurulur | 10K maksimum-state Continue ölçümü |

### Ölçüm senaryoları

- [ ] 1.000 archer + 10.000 enemy + projectile peak + Night presentation.
- [x] Fireball 10K horde içinde aynı-frame lethal damage ve toplu pool return correctness geçti; optimize peak iki temiz koşuda `79,13-83,72 ms`.
- [ ] Arrow refill sonrası 1.000 archer yeniden ateş başlangıcı.
- [ ] Tam maksimum run state ana menü save/Continue; 10K enemy Player snapshot/Continue geçti (`4,24 MB`, `52,58 / 86,19 ms`), 1K archer Package D'yi bekliyor.
- [x] Düşük/orta/yüksek worker visual density geçişi; actual `12/60/1000/5000/0`, visual `12/24/32/32/0`.
- [ ] Target search frame spike ve allocation ölçümü.
- [ ] Long-run soak ve active cap/backlog saturation ölçümü.

### Tuning yüzeyleri

- [ ] Spawn: day curve, phase multiplier, backlog, active cap.
- [ ] Wall: base HP, repair Stone cost, repair amount, Emergency %, day multiplier.
- [ ] Economy: base rates, capacity cost, efficiency growth.
- [ ] Population: Food per arrival, bed curve, dawn count.
- [ ] Archers: base stats, cost growth, retrain cost, Arrow drain.
- [ ] Heart: Essence gain, node cost/growth, rarity/depth.
- [ ] Council: fixed cadence, emergency rarity, effect bands, repeat memory.
- [ ] Meta: reward weights, upgrade costs/effects.

### Telemetry event'leri

- [ ] `run_started`: meta levels, starting resources, graph seed/version.
- [ ] `phase_changed`: day, phase, alive enemies, spawn backlog.
- [ ] `resource_spent`: resource, amount, purchase type, resulting level/count.
- [ ] `archer_changed`: buy/retrain, type from/to, total cap usage.
- [ ] `heart_node_bought`: node, level, depth, cost, revealed children.
- [ ] `council_resolved`: day, regular/emergency, template, option/expired, effects, next-night delta.
- [ ] `ability_cast`: ability, phase, cooldown, targets/repair.
- [ ] `wall_repaired`: phase, Stone cost, HP before/after.
- [ ] `run_ended`: day, kills, peak enemies/pop, Wall damage timeline, meta reward.

---

## 16. QA Kabul Matrisi

| Alan | Test | Beklenen | Durum |
|---|---|---|---|
| Cycle | 60 sn full loop | 30/5/20/5; kesintisiz spawn | `[x]` |
| Horde | Active cap dolu | Talep backlog'a gider | `[x]` |
| Horde | Day 1 vs ileri gün stat | HP/damage/speed aynı | `[x]` |
| Wall | Night normal repair | Kapalı; Stone harcanmaz | `[x]` |
| Wall | HP 0 + same-frame repair | Game Over kazanır | `[x]` |
| Enemy catalog | V1 runtime bake + gerçek spawn | Tek `zombie_basic`; prefab/stat/scale tanımla eşleşir | `[x]` |
| Population | Food yetersiz dawn | Mevcut pop korunur; arrival sınırlı | `[x]` |
| Ammo | Arrow 0 / refill | Ateş durur / anında başlar | `[ ]` |
| Archers | 1.001. purchase | Reddedilir; harcama yok | `[x]` |
| Placement | 40 tile'da 1.000 archer | Her tile 25 stable point | `[ ]` |
| Targeting | Yoğun overkill | Incoming damage hedefleri dağıtır | `[ ]` |
| Heart | Invalid generated graph | Reroll/fallback veya açık hata | `[ ]` |
| Heart | Guarantee reachability | Rapid/Frost/Fireball reachable | `[ ]` |
| Heart | Full pause | Cycle/spawn/worker/cooldown durur | `[ ]` |
| Council | Day 3 + arada emergency | Regular schedule kaymaz | `[ ]` |
| Council | Guarded effects | Bed/Food, 1000, Wall-only, count-only | `[ ]` |
| Save | Menu çıkış / Continue | Aynı graph/phase/Wall/economy | `[ ]` |
| Death | Process restart | Meta bir kez; run geri gelmez | `[ ]` |
| HUD | 16:9 / ultrawide | Kritik UI ve dünya kırpılmaz | `[ ]` |
| Tutorial | İkinci run | Otomatik tekrar etmez | `[ ]` |
| Stress | 1k archer x 10k enemy | 10K enemy-only Player P95 `6,97 ms`; `535` draw call / `202` chunk ölçüldü, 1K archer ve projectile peak bekliyor | `[~]` |

### Mevcut test envanteri

- `[x]` EditMode: `92/92`; contract, compact save/migration v3-v6, ortak 1000 archer cap matematiği, worker target mutation/allocation, profile-driven bed ve bina fiyat tuning'i, bina CAP/EFF seviye-maliyet matematiği, representative density/weight, production feedback strength ve lantern phase rule, cycle, quantity-only, backlog, Moat isolation, enemy catalog ve pool kapsamı.
- `[x]` PlayMode: `22 pass + 1 explicit skip`; gerçek `NewGameScene`, 1000. archer kabulü ve 1001. buy/Council/restore reddi, baked ekonomi fiyat tuning'i, GameManager fiyat API'leri, worker drawer target/CAP/EFF kontrolleri, çift kaynak transaction'ı, bina exact Continue, Dawn survivor akışı, Low/Medium/High visual density, same-bucket exact weight, work/cargo/delivery/lantern state, worker arrival/cap overflow, Wall, cycle, backlog ve pool kapsamı. 10K profiler capture explicit targeted testtir.
- `[~]` Council schedule/guardrail ve 1k x 10k ürün senaryoları ilgili paketleri bekliyor; enemy pool churn contract testli.

---

## 17. Risk Register

| Risk | Mevcut erken sinyal | Mitigation / kill rule |
|---|---|---|
| 1k x 10k targeting collapse | ArcherShoot brute-force | Spatial query + target load; HP scaling'e kaçma |
| Projectile/VFX flood | Projectile destroy churn; hit yoğunluğu | Pool + budget + aggregation |
| Graph unreachable | Generator henüz yok | Validation + deterministic fallback |
| Meta runaway | Current reward kills/day ağırlıklı | Diminishing values + telemetry |
| Ammo click angaryası | Refill tasarımı henüz aktif değil | Paket/capacity/efficiency tune; auto-spend ekleme |
| HUD tekrar büyür | CyclePanel + HordePressure + çoklu drawers | Tek drawer + fixed layout + mockup gate |
| Legacy leakage | Gate/Core binding, Fletcher, Barracks training, direct upgrades | Source-owner audit + guard tests |
| Council generic/slop | Composer authored olsa da launch set review eksik | Human template review + effect budget |
| Unapproved lore/content | Narrative açık karar | Owner review olmadan canon/content ekleme |
| Save contract drift | Her yeni sistem ayrı field ekleyebilir | Merkezi schema/version/migration owner |

---

## 18. Release Definition of Done

- [ ] Run yalnız Wall `0 HP` ile bitiyor; final wave/boss/ikinci fail phase yok.
- [ ] Çıkış catalog'unda tek enemy prefab var.
- [ ] Difficulty enemy stats değil adet/akış büyütüyor.
- [ ] 60 saniye cycle 30/5/20/5 ve kesintisiz.
- [ ] Speed-up/offline progress yok.
- [ ] Wood/Stone/Iron/Food pasif negatif akmıyor; Arrow tek sürekli tüketim.
- [ ] Population, beds ve worker ratios exact save/load.
- [ ] Worker world feedback representative ve doğru.
- [x] Basic/Rapid/Frost toplam 1000 cap.
- [ ] 40x25 stable formation.
- [ ] Arrow Wood ile anında alınır; Fletcher/queue yok.
- [ ] Castle Heart generated, validated, saved ve yalnız Grave Essence kullanıyor.
- [ ] Council 3/6/9 ve rare emergency schedule'ı bozmuyor.
- [ ] Council yalnız approved template/effect pool kullanıyor.
- [ ] Council ana guardrail'leri bypass etmiyor.
- [ ] Fireball/Rally/Emergency Repair bottom-center cooldown barında.
- [ ] Meta yalnız ölümde bir kez reward veriyor; voluntary reset yok.
- [ ] HUD tek Wall barı ve owner-approved minimal phase UI kullanıyor.
- [ ] İlk-run tutorial tamamlanıyor; sonraki run'da tekrarlamıyor.
- [ ] 1k archer + 10k enemy target hardware frame pacing kabul edildi.
- [ ] EditMode/PlayMode, save migration ve long-run soak raporları temiz.
- [ ] Day/night lighting, horde mix, Wall bar, phase UI ve combat feedback görsel incelemeden geçti.

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

- [ ] Faz göstergesi: 2-3 minimal HUD mockup + motion örneği.
- [ ] Meta currency adı, ikon ve death-screen copy.
- [ ] Narrative premise/world pitch/opening copy.
- [ ] Launch Heart node catalog ve effect specs.
- [ ] Emergency Council trigger listesi + rarity tuning.
- [ ] Council launch template/atom listesi + tekrar/bütçe testi.
- [ ] En az 3 Keystone trade-off çifti.
- [ ] Fireball için 2-3 evolution spec ve VFX yönü.
- [ ] Exact spawn/economy/combat/meta tuning curves ve telemetry target'ları.
- [ ] Day/night audio mix map ve rate-limit budget'ları.

---

## 21. Denetlenen Mevcut Kaynak Sahipleri

| Kaynak | Denetlenen gerçek |
|---|---|
| `NewGameScene.unity` + `MobileCastleCombatSubScene.unity` | Kamera, HUD, active authoring, cycle ve combat values |
| `MobileCastleHudRoot` live components | CyclePanel, HordePressure, Gate/Core bindings, drawers, worker CAP/EFF ve archer upgrade kontrolleri |
| `MobileCastleCombatAuthoring.cs` + `DefaultDifficulty.asset` + `BasicZombie.asset` | 30/5/20/5, quantity curves, 900 cap ve ekonomi fiyat baseline'ları; enemy base statları catalog-owned |
| `GameManager.cs` | Save/restore, repair, worker bina CAP/EFF alımı ve economy aggregate'i, ortak archer spawn/cap guard'ı, archer buy/upgrade, Council, Fireball, meta bridge |
| `RunPersistence.cs` | Exact schema v6; minimum v3, worker target/checkpoint + bed + worker bina yatırım migration'ı ve compact snapshot |
| `ContinuousSiegeCycleSystem.cs` | Phase/intensity ve Blood Moon application |
| `WaveSpawnSystem.cs` + `EnemyPoolRuntimeUtility.cs` | Tek catalog prefab/stat, cap/backlog ve expandable pool rent |
| `DamageCleanupSystem.cs` | Reward sonrası enemy pool return |
| `ResourceTickSystem.cs` + `PopulationTickSystem.cs` | V1 castle loop'ta ana kaynak ve population için pasif consumption yok |
| `MobileEconomyPriceTuning` + `MobileCastleTuningResolver.cs` + `DifficultyTunerWindow.cs` | Profile-owned bed base/interval ve worker CAP/EFF Wood/Iron base + ortak growth değerlerini sanitize edip Baker/live Apply ile tek runtime component'e taşır |
| `MobilePopulationEconomySystem.cs` + `MobilePopulationArrivalUtility.cs` + `WorkerAllocationUtility.cs` + `WorkerVisualRepresentationUtility.cs` + `MobileBedCapacityUtility.cs` + `SurvivorArrivalVisualSystem.cs` | Target ratio auto-allocation/cap overflow, temsili worker density ve exact weight; tuning-driven purchased bed state ve owned-capacity fiyat eğrisi; Dawn accepted growth + tek Food transaction'ı; mevcut VillagerWorker prefabıyla sağdan Wall arkasına yürüyen, en fazla 15 entity'lik transient arrival sunumu hazır |
| `MobileWorkerBuildingUpgradeUtility.cs` + `MobileWorkerBuildingUpgradeState` | Dört kaynak için bağımsız CAP/EFF seviyeleri; tuning-driven Wood+Iron base maliyetleri ve ortak exponential growth, `+10` cap, additive `+10%` base üretim ve int-safe maliyet reddi |
| `ArcherCapacityUtility.cs` + `GameManager.SpawnArcher` + `MarketUI.cs` | Basic/Rapid/Frost ortak `1000` entity cap'i; buy öncesi ve merkezi spawn guard'ı, Council/meta/restore sınırı, `ARMY CAP/MAX` feedback'i |
| `WorkerLogisticsMovementSystem.cs` + `SpriteSheet.shader` + `Villager.mat` | Ayrı Idle/Walk/Work/Celebrate atlas seçimi; resource cargo, Dusk/Night lantern ve weight-scaled hub delivery pulse |
| `MobileCastleArcherTilePlacement.cs` | Tile center + stack offset, preview 96 |
| `ArcherShootSystem.cs` | Brute-force nearest target ve unlimited ammo bypass |
| `TechNodeDefinitionSO.cs` + `TechTreeCatalogSO.cs` | Sabit catalog/reveal/cost/effect model |
| `TechTreeUI.cs` + `TechTreeViewController.cs` | Fullscreen graph, pan/zoom, simulation'ın durmaması |
| `CouncilComposer.cs` + `CouncilEventUI.cs` | Curated deterministic card infrastructure |
| `MetaProgression.cs` + `MetaUpgradeSO.cs` | Separate meta save, current reward/effects |
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
