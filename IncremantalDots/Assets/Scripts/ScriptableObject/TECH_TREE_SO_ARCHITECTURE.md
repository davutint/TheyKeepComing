# Tech Tree SO - Architecture

## Amac

Mobile castle modu icin SO-driven, dinamik genisleyen tech tree veri katmani.
Sabit kategori/tier/sort-order listesi BILEREK yoktur; agacin tek dogruluk kaynagi
node'lar arasi baglanti verisidir (`RevealChildNodeIds` + `PrerequisiteNodeIds`).
Yeni tech eklemek UI'yi yeniden tasarlamayi GEREKTIRMEZ.

## Dosyalar

### TechNodeDefinitionSO.cs

Tek bir tech node'un sabit datasi. Runtime durum (seviye/reveal) burada tutulmaz;
`GameManager` run-scoped state'inde yasar.

| Alan | Anlam |
|---|---|
| `Id` | Benzersiz string kimlik (orn. `wood_camp`). Baglantilar bu id ile kurulur |
| `Title` / `Description` | UI metinleri |
| `Icon` | `Sprite`, NULL OLABILIR — UI bas-harf placeholder gosterir, art uretilmez |
| `Cost` | `ResourceCost` (Wood/Stone/Iron/Food); free economy test mode bypass'i otomatik |
| `MaxLevel` | >1 ise node tekrar satin alinabilir; her seviyede Effects bir kez daha uygulanir (additif) |
| `PrerequisiteNodeIds` | Satin alma sarti: hepsi >=1 seviye sahipli olmali |
| `RevealChildNodeIds` | ILK satin almada gorunur olan cocuklar; UI baglanti cizgileri de bu iliskiden cizilir |
| `Effects` | `TechNodeEffect[]` — asagiya bak |

### TechNodeEffect (struct)

Alanlar `Type`'a gore yorumlanir: `Value` yuzdesel etkilerde oran (0.15 = +%15),
sayac etkilerde tam deger; `ArcherType` sadece unlock icin; `Resource` cap/production
hedefi (`Balanced` = tum kaynaklar).

| EffectType | Entegrasyon noktasi |
|---|---|
| `UnlockArcherType` | `GameManager._unlockedArcherTypes` (MALIYETSIZ icsel yol; legacy `UnlockArcherType()` cagrilmaz — o kendi maliyetini harcar, cift odeme olurdu) |
| `ModifyArcherDamagePercent` | `_techDamageMultiplier` -> `GetScaledArcherStats` -> `ApplyScaledStatsToArchers` (canli okculara aninda) |
| `ModifyArcherFireRatePercent` | `_techFireRateMultiplier` (ayni yol) |
| `IncreaseWorkerCap` | `MobileCastleCombatConfig.XxxWorkerCap` (ECS clamp + UI limiti ayni anda) |
| `IncreaseResourceProductionPercent` | `MobileCastleCombatConfig.XxxWorkerProductionPerMin` |
| `IncreasePopulationGrowth` | `MobileCastleCombatConfig.PopulationGrowthPerDayPrep` (cycle basi buyume) |
| `IncreaseDefenseMaxHpPercent` | Wall/Gate/CastleHP MaxHP; CurrentHP orani korunur |

BILINCLI OLMAYANLAR: `IncreasePopulationCapacity` (mobile modda Capacity hicbir seyi
sinirlamiyor, her frame ratchet'leniyor — no-op olurdu), `RepairEfficiency` (repair su an
ucretsiz + aninda tam dolum; olceklenecek sey yok). Sahte/gosteris effect'i eklenmez.

### TechTreeCatalogSO.cs

- `RootNodeId` (default `castle_heart`) + `Nodes[]`
- `GetNode(id)` (dictionary lazy cache), `GetRootNode()`, `FindRevealParent(id)`
- `ValidateCatalog()`: bos/duplicate id, bilinmeyen reveal/prereq hedefi, eksik root raporlar

## Runtime Kurallari (GameManager)

- Root oyun basinda OTOMATIK sahipli (level 1) + revealed baslar; ilk cocuklari gorunur
- Satin alma sarti: revealed + prerequisite'ler sahipli + !maxed + kaynak yeter
  (`CanBuyTechNode(node, out reason)`; reason = `WAIT/HIDDEN/MAX/LOCKED/NEED ...`)
- Satin alma ilk seviyede `RevealChildNodeIds`'i acar; effect'ler her seviyede uygulanir
- Config/defense etkileri BASE degerlerden yeniden hesaplanir (compound hatasi yok);
  base'ler ilk dokunusta cache'lenir, `RestartGame` hepsini base'e dondurur ve state'i sifirlar
- Persistence YOK; state run-scoped (`_unlockedArcherTypes` kalibi)

## Yeni Tech Ekleme (UI degisikligi GEREKMEZ)

1. `Assets/ScriptableObject/MobileCastle/TechTree/` altinda yeni `TechNodeDefinitionSO` asset'i olustur
   (Create > DeadWalls > Mobile Castle > Tech Node Definition).
2. `Id`, `Title`, `Cost`, `Effects` alanlarini doldur (Id benzersiz olmali).
3. Parent node'un `RevealChildNodeIds` listesine yeni Id'yi ekle
   (ve genelde yeni node'un `PrerequisiteNodeIds`'ine parent Id'sini yaz).
4. `TechTreeCatalog.asset`'in `Nodes` listesine yeni asset'i ekle.
5. `Icon` istege bagli, sonradan atanabilir (bos kalirsa bas-harf placeholder).

Setup tool (`MobileCastleSceneSetupWindow`) default node'lari yalnizca EKSIKSE seed eder;
katalogdaki ekstra node'lara ve mevcut asset degerlerine dokunmaz (merge-only).

## Iliskili

- Runtime state + effect uygulama: `MonoBehaviour/GameManager.cs` (Tech Tree bolumu)
- UI: `MonoBehaviour/TechTreeUI.cs` + `MonoBehaviour/TECH_TREE_UI_ARCHITECTURE.md`
- Seed/binding: `Editor/MobileCastleSceneSetupWindow.cs`
- Okcu unlock etiketi: `ArcherDefinitionSO.RequiredTechId` su an SADECE `TECH LOCKED`
  metin secimi icin okunur; gercek unlock `IsArcherTypeUnlocked` (tech effect'i yazar).
  Default asset'lerdeki `rapid_volley`/`frost_arrows` etiket degerleri gorseldir;
  unlock effect'leri `rapid_archer`/`frost_archer` node'larindadir.
