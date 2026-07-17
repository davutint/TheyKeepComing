# Death Receipt ve Kalıcı Ödül Transaction'ı - Editor Setup

## Kurulum

Bu sistem için Inspector, prefab veya scene binding kurulumu yoktur. Sahipler statik
`RunPersistence`, `MetaProgression` ve `AtomicJsonFile`; runtime başlangıç çağrıları
`GameManager` ile `MainMenuSceneUI` içindedir.

Unity Editor doğru proje instance'ına bağlıyken `NewGameScene` açık olmalı ve script compile
hatası bulunmamalıdır. Testler gerçek `Application.persistentDataPath` dosyalarını geçici olarak
yedekler ve teardown sırasında geri yükler.

## Disk dosyaları

Standalone Player ve Editor için ilgili `persistentDataPath` altında şu dosyalar oluşabilir:

- `run_save.json`: yaşayan koşunun exact snapshot'ı.
- `run_death_receipt.json`: tamamlanmamış ölüm transaction'ı.
- `meta_progress.json`: kalıcı meta state.
- Aynı adların `.tmp` uzantılı halleri: yalnız atomik write sırasında geçici dosya.

Test veya manuel QA sırasında bu dosyaları açık bir metin editöründe kilitli bırakma. Dosyaları
elle değiştirerek yapılan corrupt-marker testi bittikten sonra gerçek oyuncu kaydını geri yükle.

## Otomatik doğrulama

EditMode'da `RunPersistenceTests` ile `MetaTuningContractTests` çalıştır. V2 receipt'in
`PeakPopulation` ve exact `MetaRewardQuote` alanlarını round-trip/recovery boyunca koruması,
component toplamı bozuk quote'u reddetmesi ve aynı RunId'yi ikinci kez ödememesi gerekir.

PlayMode'da şu üç testi birlikte çalıştır:

- `SaveRunSnapshot_LethalEcsState_CannotRewriteContinueAfterDeath`
- `Continue_RestoresSameCyclePhaseTimerResourcesAndSpawnRng`
- `RuntimeDefense_IgnoresInjectedGateCore_AndEndsOnlyWhenWallDies`

Beklenen sonuç `3/3` pass ve Unity Console'da gerçek compile/runtime error olmamasıdır. Unity
Test Runner'ın `Saving results to: ...TestResults.xml` kaydı bazı MCP sürümlerinde `Exception`
tipinde sınıflandırılabilir; stack trace ve gerçek failure içermeyen bu satır test çıktısıdır.

## Manuel force-close kabulü

1. Yaşayan bir koşuda Main Menu'ye dön; Continue'nun aynı run'ı açtığını doğrula.
2. Yeni bir koşuda Wall'u `0 HP` durumuna getir.
3. Game Over frame'inde Player'ı kapatıp yeniden aç.
4. Ölen run için Continue bulunmamalı; Souls/TotalRuns yalnız bir kez artmış olmalı.
5. Uygulamayı tekrar kapatıp aç; aynı ölüm ikinci kez reward yazmamalı.

Hata simülasyonu için meta dosyasını kilitlemek gerçek kullanıcı verisini riske atabilir. Bu
senaryo otomatik testlerle kapsanır; manuel yapılacaksa önce üç authoritative dosyanın ve
`.tmp` dosyalarının yedeğini al.

## Sorun teşhisi

- Continue görünüyorsa: matching `RunId` taşıyan receipt/marker ile `TryLoad()` fail-closed
  kontrolünü ve `SaveRunSnapshot()` içindeki taze ECS death guard'ını denetle.
- Reward iki kez yazılıyorsa: meta dosyasındaki `RewardedRunIds` ve receipt `RunId` eşleşmesini
  denetle; yeni GUID üretimini ölüm transaction'ı içinde tekrarlama.
- Receipt silinmiyorsa: `meta_progress.json` write sonucunu kontrol et. Receipt'in meta durable
  olmadan temizlenmemesi beklenen güvenli davranıştır.
- `.tmp` kalıyorsa: authoritative dosyanın varlığını ve dosya kilidini kontrol et; load path'i
  orphan temp recovery çalıştırır.
