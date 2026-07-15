# Deterministic Combat Rebuild Policy - Mimari

## Amaç ve sınır

`RunSaveState v14`, 10.000 aktif zombie'nin dünya pozisyonunu ve aynı tekrar eden stat
alanlarını entity başına JSON'a yazmaz. Continue sözleşmesi iki katmana ayrılır:

- Koşu kimliği, gün/faz/timer, spawn budget ve RNG, kaynaklar, Wall, population,
  progression, Council, ability cooldown'ları ve aktif Fireball exact korunur.
- Büyük zombie alanı; toplam baskı, uzamsal yoğunluk, state, HP dağılımı, slow/death
  durumu ve ortalama combat değerleri korunarak perceptually faithful biçimde yeniden kurulur.

Bu sınır yalnız zombie world entity'leri içindir. Spawn stream'i ilerletilmez, reward üretilmez,
backlog değiştirilmez ve Wall hasarı yeniden oynatılmaz.

## Disk şeması

`RunSaveState.HasCombatRebuild`, aggregate payload'ın tek otoritesidir. `true` olduğunda
`CombatRebuildRunSaveState` şu veriyi taşır:

- policy version ve non-zero rebuild seed;
- exact toplam aktif zombie sayısı;
- snapshot dünya bounds'u;
- sabit `24 x 16` spatial grid ve `4` HP bandı;
- yalnız dolu bucket'lar.

Bucket key'i `X cell + Y cell + ZombieState + HP band + slow flag + death-timer flag`
bileşimidir. Her bucket exact count ile Z/scale, combat stat'ları, attack timer, slow,
physics velocity/force ve death timer ortalamalarını taşır. Bu nedenle 10K aynı özellikli
zombie 10K kayıt üretmez; alanın dolu yoğunluk hücrelerine çöker.

`ActiveZombies`, yalnız v3-v13 kayıtlarının exact fallback listesidir. Yeni v14 capture
bu listeyi boş bırakır. v13 -> v14 migration eski pozisyonları tahminle aggregate'e
çevirmez; ilk yeni save'e kadar legacy listeyi aynen korur.

## Deterministik rebuild

Rebuild seed; saved `SpawnRandomState`, cycle index, total kill ve active zombie count'tan
stable uint mixing ile üretilir ve payload'a yazılır. Bu seed gameplay spawn RNG stream'ini
tüketmez veya değiştirmez.

Bucket'lar key sırasına göre canonical sıralanır. Her bucket içindeki count, jitter'lı
stratified noktalarla kendi spatial cell alanına dağıtılır. Position üretimi yalnız saved
seed, bucket index ve item index'e bağlıdır. Aynı JSON snapshot her restore'da aynı position
multiset'ini üretir; entity/pool kimliği sonuç üzerinde etkili değildir.

## Projectile sınırı

Aktif Arrow pozisyon, damage, type, slow ve remaining lifetime alanlarını korur. Exact
zombie entity index'i yerine hedef zombie'nin canonical bucket index'i yazılır. Restore,
Arrow ordinal ve saved seed ile o bucket içindeki deterministik bir hedefe bağlanır. Böylece
aktif mermi kaybolmaz, fakat artık var olmayan entity kimliği veya tekil zombie pozisyonu
save'e geri sızmaz.

Aktif Fireball projectile oyuncunun doğrudan ability aksiyonudur ve exact DTO olarak
korunmaya devam eder.

## Validation ve fail-closed davranış

`CombatRebuildUtility.IsValid` Continue ve Save öncesinde birlikte şunları doğrular:

- desteklenen policy version ve non-zero seed;
- finite, pozitif spatial bounds;
- cell/HP band limitleri;
- her bucket key/count ve finite runtime payload;
- bucket count toplamının `TotalZombies` ile exact eşitliği.

Discriminator `true` iken payload geçersizse save yazılmaz veya Continue açılmaz. Sistem
bozuk aggregate'i sıfır horde ya da legacy liste gibi yorumlamaz.

## 10K kabul kanıtı

`HordeScalePlayModeTests.HordeScale_10K_WithHudFeedbackPoolFireballAndContinue_ProducesTelemetry`
üç temiz ölçümü:

- active zombie: `10.000`;
- rebuild bucket: `372-375`;
- v14 snapshot: `165.957-227.597 B` (aktif Arrow sayısı `10-273`);
- save: `31,42-32,93 ms`;
- restore: `75,09-213,32 ms`; bu yükleme-path ölçümü aktif projectile ve Editor
  koşullarıyla değişken olduğu için frame-time kazancı olarak yorumlanmaz;
- aynı snapshot için iki restore position fingerprint'i: eşit.

Önceki entity-başına v13 ölçümü `4.240.003 B` ve `52,58 / 86,19 ms` idi. Aynı 10K
senaryosunda v14 toplam disk payload'ı aktif projectile varyansına rağmen `%94,63-%96,09`
küçüldü. Runtime kabul ayrıca exact backlog `777`, active count `10.000` ve pool total'ın
restore boyunca değişmediğini doğrular.

## Değişiklik kuralı

Grid boyutu, HP bandı, key semantiği, seed üretimi veya bucket payload anlamı değişirse
`CombatRebuildUtility.CurrentPolicyVersion` artırılmalıdır. Disk DTO alanı değişirse ayrıca
`RunSaveState.CurrentVersion` ve açık migration zinciri güncellenmelidir. Eski policy için
sessiz varsayım veya random fallback eklenmez.
