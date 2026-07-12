# Safak-Checkpoint Save/Load (M-E) - Mimari

## Amac

Owner karari (2026-07-08): SAFAK CHECKPOINT modeli — her DAWN'a giriste kosu otomatik
kaydedilir; oyun acilista ana menuden CONTINUE ile SON SAFAKTAN devam eder. Gece ortasinda
kapatilan oyunda o gunun savasi bastan baslar (bilincli kabul: safak dogal nefes anidir,
ECS dunyasinin tam fotografi cekilmez). Roguelite kurali: OLUM ve NEW RUN checkpoint'i siler.

## Tasarim ilkesi: yalniz recompute-EDILEMEYENI kaydet

Save dosyasi kucuk bir ekonomik ozettir; ne kadar az alan, o kadar az bozulma/versiyon riski.
- KAYDEDILIR: gun (CycleIndex), kaynaklar, nufus + isci dagilimi, tek Wall CurrentHP,
  okcu SAYILARI + tip yukseltme seviyeleri, tech SATIN-ALMA seviyeleri, level-up kart
  tier'lari + global bonuslar, council hafizasi (flags/recent/oneshot/pity/cooldown/SALT/
  cap bonuslari), economy focus, XP/kill sayaclari.
- KAYDEDILMEZ (recompute): tech carpanlari, reveal listesi, spell unlock/degerleri,
  meta bonuslari, Wall MaxHP, uretim oranlari, unlocked okcu tipleri, okcu
  POZISYONLARI (tilemap slot sirasina yeniden dizilir), zombiler/oklar, aktif council karti
  (deterministik seed'den ayni kart yeniden roll edilir), transient state'ler.

## Katmanlar

1. **RunPersistence.cs**: `RunSaveState` (JsonUtility; dict'ler List&lt;pair&gt; olarak —
   JsonUtility Dictionary serilestirmez) + `persistentDataPath/run_save.json` IO
   (MetaProgression kalibi). `HasSave` / `TryLoad` / `Save` / `Delete`.
2. **Kayit — GameManager.TrackDawnCheckpoint**: Update'te faz-kenari izler (GameOver-gecisi
   kalibi); Dawn'a giriste `SaveRunCheckpoint()` cache'lerden + private koleksiyonlardan
   snapshot cikarir. Kayit ani Dawn BASI: o gunun safak odulleri (pop growth) ECS'te zaten
   islenmis olur.
3. **Silme**: GameOver gecisinde (`CollectMetaRunResult` yani) + `UIManager.OnRestart`
   (NEW RUN) + `MainMenuUI.OnNewRun`.
4. **Restore — GameManager.TryRestoreRunFromCheckpoint** (MainMenu CONTINUE):
   a. `RestartGame()` — temiz taban (meta uygulanir, seed okcular, taze salt)
   b. tech: her (id, level) icin `GrantTechNodeLevelsFromMeta` — reveal + carpanlar +
      spell + config/defense aggregate'leri MALIYETSIZ yeniden kurulur
   c. council hafizasi (SALT dahil — kosu-ici RNG determinizmi) + `ApplyTechEconomyAggregates`
   d. kart tier'lari + okcu seviyeleri; okcu sayilari hedefe TAMAMLANIR (SpawnArcher;
      pozisyon tilemap slotundan otomatik) + `ApplyScaledStatsToArchers`
   e. ECS yazimlari: kaynaklar, focus, allocation (`LastPopulationGrowthCycle =
      savedCycleIndex+1` — cift safak odulu gate'i), PopulationState, cycle
      (`CycleIndex = saved+1`, Phase=Day — YENI GUNUN sabahi; kaydedilen gunun odulleri
      zaten verilmisti), GameStateData, CastleUpgrade
   f. Wall CurrentHP EN SON (MaxHP aggregate'lerden kurulduktan sonra; clamp'li)
   g. restore Dawn'i atladigi icin gunun council karti elle `TryRollCouncilEvent()`
5. **UI — AYRI ANA MENU SAHNESI (M-E v2, owner istegi)**: `Assets/Scenes/MainMenuScene.unity`
   (build index 0) — hafif sahne: kamera + Canvas + `MainMenuSceneUI`. Gorseller runtime
   uretilir (`MenuSpriteFactory`: gece gradyani + kanli ay/glow + rounded-rect 9-slice
   butonlar; Inspector'dan sprite atanarak override edilebilir) + DOTween giris animasyonlari.
   Kayit varsa "CONTINUE — DAY X" (X = savedCycleIndex+2). Secim `GameBootstrap.PendingAction`
   static'ine yazilir -> `SceneManager.LoadScene(NewGameScene)` -> oyun sahnesindeki
   `RunBootstrap` init sonrasi uygular (Continue=restore; NewRun=RestartGame — sahne gecisi
   ECS world'u YOK ETMEZ, onceki oturumun runtime entity'leri boylece temizlenir; None=
   editorde dogrudan acilis, dokunulmaz — bot akislari bozulmaz). Eski panel-menu KALDIRILDI
   (timeScale=0 acilis hack'i gitti). PauseMenuUI (sag ust II; RESUME/SETTINGS/NEW RUN/
   MAIN MENU — sahneye doner; GameOver'da acilmaz) ve SettingsUI oyun sahnesinde panel
   olarak kalir; settings paneli iki sahnede de `BuildSettingsPanel` ortak ureticisiyle kurulur.

## Dogrulama (2026-07-08, play)

Kosu sekillendirildi (kaynak/tech/duvar 217 HP/DAY 3) -> Dawn'da checkpoint yazildi ->
stop/play -> menu "CONTINUE — DAY 4" -> restore birebir: Wall 217/350, tech L1/L1,
FireballDamage 72 (recompute kaniti), okcu 4, salt ayni, SpellPanel acik. Pause/Resume
timeScale 0/1; settings slider'lari SoundSettings'e yazdi. Olumde kayit silindi.

## Tuzaklar

- Yeni kosu-durumu alani eklerken: RunSaveState + SaveRunCheckpoint + TryRestoreRun
  UCLUSUNE birden ekle (yoksa sessizce kaybolur). Recompute-edilebilirse HIC ekleme.
- Restore sirasi degistirilemez: tech -> aggregate -> spawn -> ECS yazimi -> CurrentHP.
- MainMenuUI save kontrolu CACHE'lidir (_saveChecked, acilista bir kez) — menu yalniz
  acilista goruldugu icin yeterli; menu baska anda gosterilecekse cache tazelenmeli.
- Editor botlari (LongRunSimulator/Tuner) timeScale'i kendileri yonetir; menu paneli
  onlari engellemez ama bot koşusu Dawn'larda checkpoint YAZAR (Logs kirliligi degil,
  run_save.json guncellenir) — bot sonrasi manuel oyunda CONTINUE bot kosusunu acabilir.
