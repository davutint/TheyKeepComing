# Meta Progression (Roguelite) - Mimari

## Amac

K2 karari: olum -> kalici ilerleme -> yeni kosu. Para birimi RUH: 1 oldurulen zombi = 1 Ruh
(+ yeni gun rekorunda `gun x 50` bonus). Ruh, olum ekrani magazasinda KALICI yukseltmelere
harcanir; yukseltmeler her kosunun BASINDA otomatik uygulanir. V1 odagi (owner karari):
baslangic ivmesi + hafif guc. Isim "RUH" placeholder'dir — `MetaProgression.CurrencyName`
tek sabitinden degisir.

## Katmanlar

1. **Kill sayaci (ECS):** `GameStateData.TotalKills` — `DamageCleanupSystem` olu temizliginde
   artirir; `RestartGame` GameStateData'yi yeniden yazarken sifirlanir.
2. **Kalici depo (`MetaProgression` static, MonoBehaviour/MetaProgression.cs):**
   `MetaProgressState` (Souls, TotalSoulsEarned, BestDay, TotalRuns, TotalKillsAllTime,
   Upgrades[id,level]) -> JSON @ `persistentDataPath/meta_progress.json` (JsonUtility).
   M-E save sisteminin ilk tuglasi. API: `AddRunResult(day, kills)` (kosu basina bir kez),
   `GetUpgradeLevel`, `TryBuyUpgrade`, `ResetAll` (yalniz debug).
3. **Katalog (SO):** `MetaUpgradeSO` (Id/Title/Cost merdiveni `Base*(1+seviye*Growth)`/
   MaxLevel/EffectType/ValuePerLevel) + `MetaUpgradeCatalogSO`. Seed (setup, merge-only):
   start_wood, start_food, start_archers, start_moat (moat_dig tech'i acik baslar),
   wall_hp (+%5), archer_damage (+%3), production (+%3).
4. **Kosu-basi uygulama (GameManager):** `ApplyMetaProgressionAtRunStart` — ilk init
   (`ApplyMobileInitialPrepIfNeeded`) ve her `RestartGame` sonunda; idempotent
   (`_metaAppliedThisRun`). Etki yollari MEVCUT kanallardan akar:
   - StartingResource -> AddResources (Balanced = 4'e bolunur)
   - StartingArchers -> SpawnArcher (population tuketmez)
   - StartingTechLevel -> `GrantTechNodeLevelsFromMeta` (maliyetsiz; reveal + effect dahil;
     ResetTechTreeState sonrasi yeniden verilir)
   - WallHpPercent -> `ApplyTechDefenseAggregates` carpanina `_metaWallHpPercent`
   - ArcherDamagePercent -> `GetScaledArcherStats`'a `_metaDamageMultiplier`
   - ProductionPercent -> `ApplyTechEconomyAggregates` uretim carpanina
5. **Kosu-sonu kazanim:** GameManager GameOver GECISINDE (`OnGameOver` firlamadan once)
   `CollectMetaRunResult` -> `LastRunResult` (UI okur). Bir kez (`_metaRunCollected`);
   restart bayragi temizler.
6. **UI (`MetaProgressionUI`, GameOverPanel uzerinde):** olumde ozet ("DAY X — N kill,
   +N RUH, YENI REKOR!") + bakiye + magaza satirlari (katalogdan klon, TechTree/Market
   kalibi). Satin alim aninda islenir; etkisi SONRAKI kosuda (restart uygular).
   GameOverPanel kod-uretimli oldugundan objeleri setup tool kurar (prefab degil) —
   isim sozlesmesi: MetaSummaryText / MetaSoulsText / MetaShopListRoot /
   MetaShopRowTemplate (RowTitleText / RowLevelText / RowCostText / RowBuyButton).

## Fiyat/olcek notu

M-A olcumu: DAY 6 olumu ~300-600 kill, DAY 15+ ~2-4k. Ilk yukseltme 150 Ruh (ilk kosudan
alinabilir — "olum bile kazandirdi" hissi ilk dakikada); merdivenler buyume katsayilariyla
uzun vadeye yayilir. Rekor bonusu (gun x 50) derinlik tesvikini korur.

## Tuzaklar / kurallar

- Meta yuzdeleri tech/council ile AYNI aggregate kanallarindan akar — dogrudan config/entity
  yazma YOK (her-frame-ezilme tuzagi).
- `AddRunResult` cagiran taraf GameOver-GECISINI izlemeli (her frame degil) — cift kazanim
  `_metaRunCollected` ile ayrica kilitli.
- JsonUtility Dictionary serilestirmez — Upgrades List<MetaUpgradeLevel> olarak tutulur.
- `MetaProgression.ResetAll` oyuncu-yuzeyine BAGLANMAZ (gercek ilerlemeyi siler; yalniz debug).
