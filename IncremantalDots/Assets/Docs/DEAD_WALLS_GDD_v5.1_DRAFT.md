# DEAD WALLS GDD v5.1 — DRAFT (Siege Pressure & Sinks)

> DURUM: TASLAK — owner/Codex onayi bekler. Bu dokuman v5.0'in YERINE GECMEZ;
> v5.0 uzerine "kod gercekligi + yeni kararlar" delta'sidir. Onaylaninca v5.0'a
> islenip DEAD_WALLS_GDD_v5.1.md olarak resmilestirilmelidir.
> Yazim: ASCII-only (CLAUDE.md kurali). Tarih: 2026-07-06.

---

## 1. v5.0'dan Bu Yana Kod Gercekligi (GDD'nin gerisinde kaldigi noktalar)

### 1.1 Tech Tree (v5.0: "2 dugumlu duz unlock" -> gerceklesen: dallanan dinamik agac)

v5.0 yalnizca sag panelde "Unlock Rapid Tech / Unlock Frost Tech" tanimliyordu.
Gerceklesen sistem cok daha genis ve ARTIK OTORITER TASARIM budur:

- SO-driven reveal grafi: `TechNodeDefinitionSO` + `TechTreeCatalogSO`
  (`Assets/ScriptableObject/MobileCastle/TechTree/`, 16 node).
- Kok `castle_heart` otomatik sahipli baslar; satin alma `RevealChildNodeIds`
  cocuklarini gorunur yapar. Kategori/tier YOK; graf tek dogruluk kaynagi.
- Fullscreen panel (`TechTreeUI`), pan/zoom (`TechTreeViewController`,
  Desktop tekerlek-zoom + orta tus pan / Mobile pinch + drag), DOTween juice
  (cizgi cizilme, node pop, toast, TECH butonu badge'i).
- Rapid/Frost unlock'lari `rapid_archer`/`frost_archer` NODE'larindan gelir;
  sag drawer recruitment-only kalir (v5.0 ilkesiyle uyumlu).
- Upgrade'ler drawer'a GERI DONMEZ; kalici guclenme tech tree'nin isidir.

### 1.2 Ekonomi sayilari (v5.0 tablosu eskidi)

| Deger | v5.0 | Kod (guncel) |
|---|---|---|
| Baslangic pop | 24 (16W+4A+4I) | 60 (53 worker + 4 archer) |
| Baslangic kaynak | 120W/90F/60S/35I | 160W/120F/80S/50I |
| Worker uretim/dk | 12W/10F/7S/4I | 8W/7F/5.5S/3.8I |
| Worker cap | tanimsiz | 40W/30S/24I/40F |
| Cycle odulu | "orta buyuklukte" | +15 pop (DAWN'da) |

Kimlik notu: oyun fiilen "bol kaynak + hizli buyume" incremental hissine
oturdu; v5.1 bunu KABUL eder ve dengeyi kaynak kisarak degil TEHDIT buyuterek
kurar (bkz. 2.1).

---

## 2. v5.1 Yeni Kararlar (bu draft'in getirdikleri — KODA GIRDI)

### 2.1 Kutle-odakli tehdit eskalasyonu (v5.0 8.3'un somutlasmasi)

Ilke: zorluk "sungerlesen zombi" degil "kalabaliklasan ekran" (pillar #1).

- Zombi HP LINEER: `ZombieBaseHP * (1 + (cycle-1) * ZombieHpGrowthPerCycle)`
  = 20 * (1 + 0.30/cycle). Cycle 10'da HP 74 (eski ustel w^1.2 formulu 317
  veriyordu — kaldirildi).
- Hasar: 5 + 0.5/cycle. Hiz: 0.85 + 0.04/cycle (degismedi).
- Batch cycle ile buyur: `SpawnBatchSize * fazIntensity * (1 + (cycle-1)*0.10)`,
  tavan `MaxSpawnBatch=12`. Tempo `MinSpawnInterval=0.35s`te doysa bile
  kalabalik artmaya devam eder.
- Performans tavani: `MaxAliveZombies=900` (stress testte 1500 dogrulanmisti).
- Erken oyun yumusatildi: `SpawnBatchSize 3->2`, `BaseSpawnInterval 0.8->0.95`
  (etkilesimsiz koside DAY 2 yikimi gozlendi; oyunculu kosi icin owner
  playtest'i gerekir — ACIK AYAR NOKTASI).
- Tum degerler `MobileCastleCombatAuthoring`/config'te (Inspector'dan tune edilir;
  eski hardcoded HP 20/dmg 5 tabanlari config'e tasindi).

### 2.2 Gelir/zorluk ayrismasi

`KillRewardWaveScale = 0` (eski 0.05): kill odulu cycle ile BUYUMEZ. Zorluk
artisi geliri otomatik sisiremez; ana gelir worker ekonomisidir (v5.0 5.2 ile
uyumlu). Istenirse Inspector'dan geri acilir.

### 2.3 Repair maliyeti + player-facing buton (YENI sink)

- v5.0'da repair akisi DayPrep'e bagliydi ve continuous'ta OLU idi; ayrica bedava
  + anlik tam dolumdu. Yeni kural: repair HER ZAMAN denenebilir, maliyet
  kayip-orantilidir: `ceil(RepairBase * kayipOrani)`, taban 120W/80S (tam kayipta).
- UI: `CastleDefensePanel` icinde `REPAIR` butonu + maliyet etiketi
  (`DefenseRepairUI`). "Tamire mi, tech'e mi, okcuya mi" ucgeni dogar.
- Tech: `repair_efficiency` node'u (-%20/seviye, MaxLevel 2) maliyeti dusurur
  (`ReduceRepairCostPercent` effect'i — v5.0'da iptal edilen RepairEfficiency
  artik anlamli).

### 2.4 Tekrarlanabilir tech sink'leri (agac tukenmez)

- `TechNodeDefinitionSO.CostGrowthPerLevel`: efektif maliyet
  `Cost * (1 + seviye * buyume)`.
- Yeni node'lar: `bow_mastery` (+%6 hasar/seviye, MaxLevel 20, maliyet buyume
  0.40) ve `volley_mastery` (+%5 atis hizi/seviye, MaxLevel 20, 0.40).
- Gec oyunda kaynaklarin HER ZAMAN gidecek bir yeri vardir; incremental kimlik
  ile tehdit egrisi burada bulusur.

### 2.5 DAWN fazi geri geldi (v5.0 3.3'teki 4-faz dongusu)

- 60s = DAY 22 / DUSK 8 / NIGHT 22 / DAWN 8. Dawn intensity 0.15 (spawn nefesi).
- Population growth (+15) artik DAWN BASINDA verilir ve GORUNUR:
  `DawnRewardToastUI` -> "DAWN — DAY n SURVIVED  +15 POP" (SiegeToastText).
- Gece karartmasi Dawn boyunca aydinliga lerp'lenir (DayNightOverlayController).
- Geriye uyumluluk: `SiegeDawnDuration=0` bake'lerde 3-faz davranis korunur.

### 2.6 Gun sayaci (skor hissi)

`CycleDayCounterText` ("DAY n") cycle panelinde. Sonsuz kusatmada oyuncunun
dogal hedefi "kac gun dayandim"dir; sayac bunu gorunur kilar. Ileride best-run
kaydi (yerel) dusunulebilir — V5.1 kapsaminda DEGIL.

---

## 3. Acik Sorular (v5.0 bolum 16'ya ek)

1. Etkilesimsiz baseline kac gun dayanmali? (su an hedef ~DAY 3-4; owner
   playtest'iyle SpawnBatchGrowthPerCycle/BaseSpawnInterval ayari gerekir)
2. GameOver sonrasi akis: sadece restart mi, "en iyi gun" gosterimi mi?
3. Mastery node maliyet buyumesi (0.40) uzun vadede dogru mu?
4. v5.0 acik sorulari (sol alan genisligi, worker geri cekme, sag-agirlikli
   spawn, event zamanlamasi) hala acik.

## 4. Degismeyen v5.0 Ilkeleri (teyit)

Tek tip zombi kutlesi; oyun durmaz (tech tree paneli dahil hicbir UI pause
etmez); gold yok; oklar sinirsiz; run/meta-progression yok; sol canli ekonomi
vizyonu (M5.1/M5.3) ve council events (M5.4) SIRADAKI isler olarak durur.
