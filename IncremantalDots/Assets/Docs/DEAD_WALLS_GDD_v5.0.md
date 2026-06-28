# DEAD WALLS - Game Design Document v5.0

**Mobile Continuous Siege + Castle Interior Economy**

**Ana Sahne:** `Assets/Scenes/NewGameScene.unity`  
**Motor:** Unity 6, DOTS/ECS, Burst, Job System  
**Hedef Platform:** Mobile landscape ilk hedef; PC/editor playtest desteklenir  
**Guncel Tasarim Tarihi:** 2026-06-27

---

## 0. Dokuman Amaci

Bu dokuman, `NewGameScene` uzerinden gelistirilen guncel Dead Walls yonunun ana GDD kaynagidir.

Eski `DEAD_WALLS_GDD_v2.0`, `v3.0` ve `v4.0` dokumanlari tarihsel referans olarak kalir. Guncel yon artik eski "100 gun RTS town builder + roguelike kart" tasarimi degildir. Yeni yon, mobil landscape ekranda surekli akan bir kusatma oyunudur:

```text
Sol taraf: yasayan kale ici ekonomi
Orta/sag: dis alan, okcular, zombiler ve savunma
Sag panel: okcu satin alma / upgrade / tech progression
Ust HUD: kaynak, gun-gece durumu, savunma ve kill bilgisi
```

Bu dokumanin amaci:

- Alinan tasarim kararlarini tek yerde tutmak.
- Mevcut implementation ile hedef tasarim arasindaki farki net gostermek.
- Siradaki milestone'lar icin baglam saglamak.
- "Ne artik yok?" sorusunu acik cevaplamak.

---

## 1. Oyun Ozeti

### 1.1 Konsept

Dead Walls, mobil landscape ekranda oynanan bir kale kusatma savunma oyunudur. Oyuncu sol taraftaki kale ici ekonomi alaninda halkini kaynak islerine yonlendirir, sag tarafta ise cok sayida zombinin surlara baski kurmasini izler ve savunmasini buyutur.

Oyun, "wave baslat / wave bitti / hazirlik ekrani" hissinden uzaklasir. Hedef deneyim, surekli akan bir kusatmadir:

```text
Day -> Dusk -> Night -> Dawn -> Day
```

Gunduz oyuncu ekonomiyi daha rahat duzenler. Gunes batarken zombi baskisi artar. Gece en yogun savas yasanir. Safakla birlikte baski azalir, kaynak/nufus odulleri verilir ve dongu yeniden baslar.

### 1.2 Temel Bilgiler

| Alan | Detay |
|------|-------|
| Tur | Mobile castle defense + active economy + incremental siege |
| Perspektif | 2D izometrik / tilemap tabanli |
| Ana Sahne | `NewGameScene` |
| Ana Fantezi | Sol tarafta kale halkini buyut, sag tarafta bitmeyen zombi kusatmasina dayan |
| Ana Dusman Fikri | Cok sayida tek tip zombi, buyuk kitle baskisi |
| Kontrol | Mobil uyumlu tiklama/tap, panel ve buton agirlikli |
| Ekonomi | Wood, Stone, Iron, Food, Population |
| Savas | Otomatik hedef alan okcular, sinirsiz ok V1, kale HP katmanlari |
| Progression | Okcu satin alma, okcu type upgrade, tech unlock, worker artisi |
| Eski Kart Sistemi | Mobile ana loop icin kaldirildi / legacy |

### 1.3 Elevator Pitch

"Kalenin icindeki halkini kaynak islerine yonlendir. Disarida okcularini cogalt ve upgrade et. Gunes battikca zombi baskisi artar; gece boyunca surlarina dayanirlar. Oyun durmaz, kusatma bitmez. Her gunduz daha iyi hazirlan, her gece daha buyuk kalabaligi erit."

### 1.4 Tasarim Pillar'lari

1. **Cok Sayida Zombi**
   - Ana gorsel spektakl, tek tek ozel dusmanlardan once buyuk kalabaliktir.
   - DOTS/ECS kullaniminin ana nedeni budur.

2. **Yasayan Kale Ici Ekonomi**
   - Ekonomi soyut slider veya sadece sayi degildir.
   - Kaynak islerine atanan insanlar sahnede gorunur.
   - Oyuncu butona bastiginda ekonomide ve gorselde direkt geri bildirim alir.

3. **Surekli Kusatma**
   - Oyun "Start Next Wave" butonuyla durup kalkmaz.
   - Gun/gece ritmi devam eder.
   - Zorluk zamanla ve gece faziyla artar.

4. **Okcu Savunma Fantazisi**
   - Okcular kalenin dis/duvar cevresindeki belirlenmis noktalara yerlesir.
   - Basic/Rapid/Frost gibi tipler davranis ve renk/tint ile ayrisir.
   - Oyuncu okcu alir, upgrade eder, tech acar.

5. **Mobil Okunabilirlik**
   - UI yogun olabilir ama okunabilir kalmalidir.
   - Ana aksiyonlar bir veya iki tap ile erisilebilir olmalidir.
   - Buyuk popup'lar ancak gercekten gerekli oldugunda kullanilir.

---

## 2. Ekran Kompozisyonu

### 2.1 Hedef Layout

Yeni hedef kompozisyon:

```text
+--------------------------------------------------------------+
| Resources                 Day/Dusk/Night      Archer Drawer  |
| Defense / Kills                                  Toggle      |
|                                                              |
| [Castle Interior Economy]      Open Field / Siege Combat     |
| [Workers + Resource Sites]     Archers / Arrows / Zombies    |
| [Tap Buttons]                                                 |
|                                                              |
+--------------------------------------------------------------+
```

### 2.2 Sol Taraf - Castle Interior Economy Area

Sol taraf, UI popup degil; oyun dunyasinin bir parcasi gibi duran kale ici ekonomi alanidir.

Bu alanda oyuncu sunlari gormelidir:

- Wood site
- Stone site
- Iron site
- Food site
- Her site etrafinda calisan kucuk citizen/worker gorselleri
- Kaynak atama butonlari
- Worker sayilari
- Resource production rate bilgisi
- Idle population bilgisi

Sol alan, oyuncuya "kalenin icini yonetiyorum" hissi vermelidir. Burasi bir spreadsheet degil, yasayan avlu/ekonomi sahnesidir.

### 2.3 Orta/Sag Taraf - Kusatma Alani

Orta ve sag taraf savas alanidir:

- Okcular kalenin dis/duvar bolgelerinde durur.
- Zombiler acik alandan savunmaya dogru gelir.
- Projectiles, hit feedback ve death feedback burada okunur.
- Zombi formasyonu veya lane yoktur.
- Ana gorsel hedef, yogun ama okunabilir bir kalabaliktir.

### 2.4 Sag Panel - Archer Progression

Sag panelin ana rolu:

- Basic/Rapid/Frost okcu satin alma
- Okcu type upgrade
- Rapid/Frost tech unlock
- DPS/level/count/cost bilgisi

Sag panel combat sirasinda kullanilabilir kalir. Oyun pause olmaz.

### 2.5 Ust HUD

Ust HUD sunlari gosterir:

- Wood, Stone, Iron, Food
- Population / idle population
- Arrows V1 icin `INF`
- Day/Dusk/Night state
- Kill count
- Defense Wall/Gate/Core HP

---

## 3. Core Loop

### 3.1 Guncel Hedef Loop

```text
1. Day fazi baslar.
2. Oyuncu sol kale ici alanda worker atar, kaynak ekonomisini buyutur.
3. Oyuncu sag panelden okcu alir, upgrade eder, tech acar.
4. Dusk fazinda spawn baskisi artmaya baslar.
5. Night fazinda zombi kalabaligi yogunlasir.
6. Okcular otomatik hedef alir ve savasir.
7. Zombi oldurdukce kaynak kazanilir.
8. Dawn fazinda baski azalir, gun odulu/nufus artisi verilir.
9. Yeni gun otomatik baslar.
```

### 3.2 Player-Facing Wave Karari

Eski "wave bitti, oyuncu Start Next Wave'e basar" akisi hedef tasarimdan cikarilir.

Kod icinde zorluk, gun numarasi veya spawn budget gibi sayaclar kalabilir; fakat oyuncu deneyimi sunu hissetmelidir:

```text
Oyun durmuyor. Kusatma surekli devam ediyor.
```

Bu nedenle:

- `Start Next Wave` player-facing UI'da yoktur.
- `Wave Cleared` ana mesaj olarak kullanilmaz.
- Day/Night gecisleri otomatik akar.
- Hazirlik ekrani oyunu durdurmaz.

### 3.3 Fazlar

| Faz | Hedef His | Spawn | Oyuncu Aksiyonu |
|-----|-----------|-------|-----------------|
| Day | Nefes alma, ekonomi kurma | Yok veya cok dusuk | Worker ata, okcu al, upgrade et |
| Dusk | Tehdit yaklasiyor | Artan spawn | Son hazirliklar |
| Night | Yogun kusatma | Yuksek spawn | Savunma buyut, kaynak kararlarini surdur |
| Dawn | Baskinin azalmasi | Azalan spawn | Odul/nufus artisi, yeni gune gecis |

### 3.4 Eski Level-Up Kartlari

Mobile ana loop'ta level-up kartlari artik ana progression degildir.

Eski kartlar:

- `AddBasicArcher`
- `AddRapidArcher`
- `AddFrostArcher`
- `ArrowDamageUp`
- `FireRateUp`
- `RepairGate`

Bu kart sistemi, mobile core loop icin legacy kabul edilir. Okcu alma ve upgrade, sag Archer Progression panelinden yapilir.

---

## 4. Castle Interior Economy Area

### 4.1 Amac

Castle Interior Economy Area, ekonomiyi oyuncunun elinde ve gozunun onunde hissettiren alandir.

Eski slider tabanli fullscreen Castle Economy paneli ekonomik karari temsil ediyordu; fakat yeterince "yasayan kale" hissi vermiyordu. Yeni hedefte resource assignment sol ust resource bar altindaki `WorkerEconomyDrawerPanel` uzerinden her an erisilebilir, sahnede ise fiziksel villager lojistigiyle okunur.

Oyuncu kaynak butonuna bastiginda:

```text
1. Idle population kontrol edilir.
2. Kaynak worker sayisi artar.
3. Idle population azalir.
4. Ilgili resource site ile merkezi hub arasinda yuruyen yeni worker visual eklenir.
5. Resource production rate artar.
```

Tap progress yoktur. Her gecerli tap direkt assignment demektir.

### 4.2 Resource Site'lari

V1 site'lari:

| Site | Gorsel Tema | Uretim Rolu |
|------|-------------|-------------|
| WoodSite | Kutuk, agac, balta, odun yigini | Wood/min |
| StoneSite | Kaya, tas yigini, kazma | Stone/min |
| IronSite | Maden, cevher, ors, metal parcasi | Iron/min |
| FoodSite | Tarla, cuval, kazan, ambar | Food/min |

### 4.3 Scene Hierarchy Hedefi

Bu alan sahnede user-owned world visual olarak kurulmalidir. Kod, gorseli boyamaz veya tasimaz; sadece markerlari okur.

Onerilen hierarchy:

```text
CastleInteriorEconomyArea
  CastleWorkerHub
    VisualRoot
    DeliveryPoints
      Delivery_00
      Delivery_01
      Delivery_02
  WoodSite
    VisualRoot
    WorkerSpawnPoints
      Spawn_00
      Spawn_01
      Spawn_02
  StoneSite
    VisualRoot
    WorkerSpawnPoints
      Spawn_00
      Spawn_01
      Spawn_02
  IronSite
    VisualRoot
    WorkerSpawnPoints
      Spawn_00
      Spawn_01
      Spawn_02
  FoodSite
    VisualRoot
    WorkerSpawnPoints
      Spawn_00
      Spawn_01
      Spawn_02
```

`VisualRoot`, owner'in sahnede kurdugu dekor/gorsel objeleri icerir.  
`WorkerSpawnPoints`, kaynak pickup marker'laridir.  
`CastleWorkerHub/DeliveryPoints`, worker'larin kaynak teslim ettigi merkezi marker'lardir.

### 4.4 Worker Assignment Kurallari

V1 kurallari:

- Sol ust Worker Drawer icindeki her resource `+ WORKER` butonu bir worker assignment dener.
- Worker assignment DayPrep'e bagli degildir; continuous siege akisi icinde her zaman denenebilir.
- Idle population yoksa assignment basarisiz olur.
- Basarili assignment ilgili resource worker count'u +1 yapar.
- Worker visual ilgili site pickup noktasi ile hub delivery noktasi arasinda loop yapar.
- Worker production rate aninda guncellenir.
- Worker geri cekme V1 icin zorunlu degildir; sonraki milestone'da dusunulebilir.

### 4.4.1 Baslangic Defaultlari

V1 baslangic ekonomisi:

```text
Population Total: 24
Initial Workers: 16
Initial Archers: 4
Idle Population: 4

Wood Workers: 6
Food Workers: 5
Stone Workers: 3
Iron Workers: 2

Wood: 120
Food: 90
Stone: 60
Iron: 35
Arrows: INF
```

### 4.5 Worker Limitleri

Iki limit katmani olabilir:

1. **Population limit**
   - Idle population yoksa yeni worker atanamaz.

2. **Site limit**
   - V1'de istege baglidir.
   - Eger site limit yoksa spawn point'ler tukenince ayni noktalara kucuk deterministic offset uygulanabilir.
   - Eger site limit varsa UI `FULL` veya `MAX` gosterebilir.

Onerilen V1:

```text
Population limit aktif.
Site hard cap yok.
Spawn point biterse mini-offset ile tekrar kullan.
```

### 4.6 Worker Visual

Worker visual, ekonomi truth'unu temsil eden DOTS entity gorselidir. Gameplay truth yine `MobilePopulationAllocation` uzerindeki worker sayilaridir; ancak sahnede gorunen insanlar MonoBehaviour havuzu degil ECS/DOTS entity olarak spawn edilir.

V1 icin tek worker prefab yeterlidir:

```text
VillagerWorker.prefab
```

Kaynak asset:

```text
Assets/SmallScaleInt/Character creator - Fantasy/Created Spritesheets/Character_villager/Idle.png
```

Sonraki polish:

- Wood worker tint: kahverengi/sicak
- Stone worker tint: gri
- Iron worker tint: koyu gri/mavi
- Food worker tint: yesil/sari
- Kucuk walk/idle lojistik loop animasyonlari
- Kaynak noktasina gore farkli tool sprite'lari

### 4.7 Production Rate Onerisi

V1 tuning:

| Worker Tipi | Uretim |
|-------------|--------|
| Wood worker | +12 wood/min |
| Food worker | +10 food/min |
| Stone worker | +7 stone/min |
| Iron worker | +4 iron/min |

Iron daha yavas ama daha degerlidir. Food ve Wood daha hizli akar ve erken oyun temposunu tasir.

### 4.8 Fullscreen Castle Economy Panel'in Durumu

Mevcut fullscreen Castle Economy panel:

- Debug/legacy olarak kalabilir.
- Player-facing ana ekonomi araci olmaktan cikacaktir.
- Mevcut worker allocation kodu yeni worker site sistemine kaynak olabilir.
- Rare event UI'si ileride sol alanda daha kucuk "council event" olarak donusebilir.

---

## 5. Kaynak ve Nufus Sistemi

### 5.1 Kaynaklar

V1 kaynaklar:

| Kaynak | Rol |
|--------|-----|
| Wood | Basic/Rapid okcu, upgrade, genel erken ekonomi |
| Stone | Defense repair, Frost/stone economy, ileride wall upgrade |
| Iron | Rapid/Frost tech, ileri upgrade |
| Food | Nufus, worker ve okcu tempo kaynagi |

Gold yoktur. Coin economy su asamada eklenmeyecek.

### 5.2 Nufus

Population, hem worker hem okcu icin ayni havuzdan gelir.

V1:

- Total population gun/dongu ilerledikce artar.
- Idle population, yeni worker veya okcu almak icin kullanilir.
- Her okcu 1 population kullanir.
- Her worker 1 population kullanir.
- Population cap V1'de olmayabilir veya cok yuksek/internal tutulabilir.

### 5.3 Workerlar ve Okcular Arasindaki Gerilim

Ana ekonomi karari:

```text
Daha cok worker -> daha cok kaynak -> uzun vadeli buyume
Daha cok okcu -> daha cok savunma -> kisa vadeli hayatta kalma
```

Bu gerilim oyunun temel stratejik kararidir.

### 5.4 Kill Reward ve Gun Odulu

Kill reward ve gun/dongu odulu tamamen kaldirilmaz; fakat ana gelir kaynagi worker production olmalidir.

Oneri:

- Kill reward kucuk kalir, savas hissini destekler.
- Dawn/day transition odulu orta buyuklukte olur.
- Worker economy ana ve kontrol edilebilir gelir kaynagidir.

---

## 6. Okcu ve Savunma Sistemi

### 6.1 Okcu Tipleri

| Tip | Rol | Davranis |
|-----|-----|----------|
| Basic | Dengeli DPS | Orta fire rate, orta damage |
| Rapid | Hizli DPS | Yuksek fire rate, dusuk damage |
| Frost | Kontrol | Dusuk damage, tek hedef slow |

### 6.2 Okcu Progression

Okcu progression sag panelden yapilir:

- Buy Basic
- Buy Rapid
- Buy Frost
- Upgrade Basic
- Upgrade Rapid
- Upgrade Frost
- Unlock Rapid Tech
- Unlock Frost Tech

Level-up kartlari okcu spawn etmez. Okcu ekonomisi resource + population ile ilerler.

### 6.3 Okcu Yerlesimi

Mevcut implementation:

- Okcular `Grid/outside` tilemapindeki dolu hucrelerde spawn olur.
- `outside` gameplay spawn kaynagidir.
- `inside`, `outside0`, `outside2` gorsel/sorting katmanlaridir.
- Ayni outside hucreleri tekrar kullanilabilir.
- Spawn point sayisi gameplay cap degildir.

Hedef kompozisyon sol kale / sag baski yonune kayarsa bu sistem korunabilir, fakat outside tilemap sol/orta savunma hattina gore yeniden cizilir.

### 6.4 Hedef Secimi

Okcular V1'de kendilerine en yakin uygun zombiyi hedef alir.

Bu davranis:

- Cok sayida zombi senaryosunda okunabilir ve basittir.
- RTS manuel hedefleme gerektirmez.
- Mobil kontrol karmasasini azaltir.

### 6.5 Oklar

V1 mobile loop:

- Oklar sinirsizdir (`INF`).
- Arrow refill player-facing UI'dan kaldirilir.
- Ok stok ekonomisi daha sonraki bir survival/tuning asamasinda geri gelebilir.

### 6.6 Savunma HP

Savunma katmanlari:

- Wall
- Gate
- Core/Castle

UI, tek `DEF 84%` text'iyle yetinmez. Wall/Gate/Core barlari gosterilmelidir.

---

## 7. Zombi Sistemi

### 7.1 V1 Zombi Felsefesi

Bu asamada enemy variety ana hedef degildir.

V1'in dusman vaadi:

```text
Tek tip zombi, cok sayida zombi.
```

Formasyon, lane, cephe sistemi veya ozel dusmanlar bu asamada hedef degildir.

### 7.2 Spawn Yonu

Mevcut sistem random 360 spawn kullanir. Yeni sol kale / sag baski kompozisyonuna gecildiginde spawn agirligi sag/distan gelebilir; fakat bu karar ayrica test edilmelidir.

V5 tasarim karari:

- Formasyon yok.
- Lane yok.
- Spawn direction oyunun kompozisyonuna gore tune edilebilir.
- Ana hedef zombi kalabaligi hissidir.

### 7.3 Zombi Hedefi

Zombiler savunma merkezine/kale attack radius'una ilerler. Attack radius'a girdiklerinde saldiri state'ine gecer ve savunmaya hasar uygular.

### 7.4 Future Enemy Variety

Sonraki asamalarda dusunulebilir:

- Hizli zombi
- Tank zombi
- Exploder
- Boss/titan
- Night modifier ile stat degisimi

Fakat bunlar V5 ana foundation icin zorunlu degildir.

---

## 8. Day / Dusk / Night / Dawn Sistemi

### 8.1 Neden Wave Stops Kaldiriliyor?

Mevcut day prep sistemi oynanabilir hale geldi; ancak hedef oyun hissi icin "wave bitti, bekleme basladi" yapisi fazla dur/kalk hissettirir.

Yeni hedef:

```text
Kusatma surekli akar.
Oyuncu oyunu durdurmadan ekonomiyi ve savunmayi yonetir.
```

### 8.2 Faz Sureleri

Baslangic tuning onerisi:

| Faz | Sure | Not |
|-----|------|-----|
| Day | 20-30 sn | Ekonomi agirlikli, dusuk spawn |
| Dusk | 8-12 sn | Spawn ramp-up |
| Night | 35-60 sn | Ana combat |
| Dawn | 8-12 sn | Spawn azalir, odul/nufus artisi |

Sureler mobile oturum hissine gore Inspector'dan tune edilmelidir.

### 8.3 Difficulty Scaling

Zorluk, player-facing wave bitti mantigi olmadan da olceklenir:

- Gun numarasi
- Toplam kill
- Current threat level
- Son gece clear hizi
- Savunmanin aldigi hasar

V1 icin basit gun numarasi scaling yeterlidir.

### 8.4 Reward Timing

Gun/dongu odulleri Dawn veya yeni Day basinda verilir:

- Population growth
- Kucuk resource bonus
- Rare event roll

Bu oduller "Wave Cleared" yerine "Dawn / New Day" hissiyle baglanir.

---

## 9. Event Sistemi

### 9.1 V1 Event Hedefi

Rare event'ler ekonomiye karakter katar ama ana akisi bozmaz.

V1:

- Event sansi dusuk olur.
- Eventler iki seceneklidir.
- Genelde ekonomiyle ilgilidir.
- Secilmezse sure sonunda expire olabilir.

### 9.2 Event Sunumu

Fullscreen popup yerine sol kale ici alaninda veya ust HUD yakininda compact event badge kullanilmalidir.

Oneri:

```text
Council Event
Travelers at the gate
[Take workers] [Take food]
```

### 9.3 Event Ornekleri

| Event | Secenek A | Secenek B |
|-------|-----------|-----------|
| Refugees | +Population | +Food cost penalty temporary |
| Broken Cart | +Wood | +Stone |
| Abandoned Mine | +Iron | temporary Iron production bonus |
| Harvest Offer | +Food | +Food worker efficiency |

---

## 10. UI / UX Tasarimi

### 10.1 UI Prensipleri

- English text kullanilir.
- Textler buyuk degil, kompakt ve okunur olur.
- Butonlar mobile tap icin yeterli boyutta olur.
- Resource iconlari anlamli olmalidir; arrow iconu wood/stone gibi alakasiz alanlarda kullanilmaz.
- UI gorseli dark medieval/gold stilini korur.
- Popup yerine oyunla entegre panel tercih edilir.

### 10.2 Sol Economy UI

Sol ekonomi alaninda her resource icin:

```text
WOOD
x12 workers
+144/min
[Assign]
```

Butona basilinca worker assignment denenir.

Idle population yoksa:

```text
NEED POP
```

### 10.3 Sag Archer UI

Sag panel:

- Archer row'lari
- Buy button
- Upgrade button
- Tech unlock buttonlari
- Cost / NEED feedback

Sag panel combat sirasinda acik/kapanabilir kalir.

### 10.4 Defense UI

Defense panel:

- Wall fill
- Gate fill
- Core fill
- Overall defense percent
- Hasar aldiginda kisa red flash

### 10.5 Day/Night UI

Wave text yerine yeni dil:

```text
DAY
DUSK
NIGHT
```

Cycle UI:

```text
Top: current phase
Bottom: DAY / DUSK / NIGHT labels
Progress slider: 60s cycle position
```

Eger kod icinde wave budget devam ediyorsa UI bunu "wave" olarak gostermek zorunda degildir.

---

## 11. Combat Feedback

### 11.1 SFX

Ok atis sesleri random bow clip listesinden secilir ve pitch varyasyonu uygulanir.

Hedef:

- Tekrarlayan tek ses hissini azaltmak.
- Rapid okcularda ses yigini yapmamak.
- Rate limit ile mobile mix'i temiz tutmak.

### 11.2 VFX

Normal hit VFX icin guncel guvenli yol:

- `fanfx2_cure_small_red` sprite flipbook impact
- Pool prewarm
- Runtime instantiate yok
- Arrow/Frost normal hit particle path'i devre disi

Particle VFX ileride geri gelebilir, fakat final polish asamalarina birakilir.

### 11.3 Death Feedback

Zombie death feedback su an ana odak degildir. Oyunun loop ve ekonomi kimligi oturduktan sonra ele alinacak polish konusudur.

---

## 12. Teknik Mimari

### 12.1 ECS / MonoBehaviour Ayrimi

Combat ve simulation DOTS/ECS tarafinda kalir:

- Zombi spawn/movement/attack
- Okcu targeting/shooting
- Arrow movement/hit
- Resource tick
- Population/economy state
- Day/night state

Scene/UI/feedback MonoBehaviour tarafinda kalir:

- HUD
- Archer drawer
- Castle interior UI
- Worker visual placement
- Audio/VFX bridge
- Editor setup

### 12.2 Mobile Castle Config

`MobileCastleCombatConfig`, mobile mode'un varligini ve tuning degerlerini tasir.

Config yoksa legacy/non-mobile davranis korunabilir.

### 12.3 Render Depth Bands

Shader transparent yapilmamalidir. ECS sprite gorunurlugu icin mevcut opaque/geometry akis korunur.

Render sirasi world z-depth band ile cozulur:

| Band | Z |
|------|---|
| Back tilemaps | 0 |
| Units | -1 |
| Front occluders | -2 |
| Projectile / VFX | -2.5 veya daha onde |

Kamera `z = -10` oldugu icin daha negatif z daha onde kabul edilir.

### 12.4 Okcu Spawn Source

Okcular:

- `Grid/outside` tilemapinden spawn olur.
- Auto-ring artik mobile player-facing spawn sistemi degildir.
- `outside2` front occluder olarak kullanilir.

### 12.5 Worker Spawn Source

Workerlar:

- `CastleInteriorEconomyArea/*Site/WorkerSpawnPoints` markerlarini kaynak pickup noktasi olarak kullanir.
- `CastleInteriorEconomyArea/CastleWorkerHub/DeliveryPoints` markerlarini teslim merkezi olarak kullanir.
- DOTS villager entity'leri pickup ve delivery arasinda loop yaparak kaynak tasima feedback'i verir.
- Tilemap'e gomulmez.
- User-owned scene placement korunur.
- Setup tool sadece eksik root/marker olusturabilir; gorseli boyamaz.

---

## 13. Editor Tooling

### 13.1 Mobile Castle Scene Setup

Tool'un gorevi:

- NewGameScene icin gerekli manager/controller referanslarini baglamak.
- Mobile config defaultlarini yazmak.
- HUD/controller bindinglerini tamamlamak.
- CombatFeedbackBridge referanslarini baglamak.
- Outside tilemap placement controller'ini baglamak.
- Eksikse CastleInteriorEconomyArea site/hub marker skeleton'i olusturmak.

Tool'un yapmamasi gerekenler:

- User'in tilemaplerini silmek.
- User'in world visual'ini boyamak.
- Kale/orman/zemin gorselini otomatik degistirmek.
- UI mockup JSON'u kendi basina uretmek.

### 13.2 UI Importer Workflow

Yeni polish UI gerekiyorsa implementer dogrudan JSON uretmez.

Workflow:

1. Implementer gerekli UI isimlerini ve prompt'u owner'a verir.
2. Owner ayri Codex tabinda UI mockup/export uretir.
3. Unity repo tarafinda sadece runtime binding ve setup tool entegrasyonu yapilir.

---

## 14. Eski Kararlarin Durumu

| Eski Karar | Yeni Durum |
|------------|------------|
| 100 gun final wave | Legacy / su an hedef degil |
| Roguelike level-up kartlari | Mobile ana loop'tan cikarildi |
| Fullscreen Castle Economy panel | Legacy/debug; player-facing ana ekonomi sol alana tasinacak |
| Start Next Wave butonu | Player-facing olarak kaldirilacak |
| Arrow refill | Mobile V1'de yok; arrows `INF` |
| Auto-ring okcu placement | Kaldirildi; `Grid/outside` tilemap kullaniliyor |
| Formasyonlu zombi dalgalari | Hedef degil |
| Enemy variety | Ertelendi; once cok sayida tek tip zombi |
| Particle hit polish | Ertelendi; once loop ve ekonomi kimligi |

---

## 15. Milestone Plani

### M5.1 - Castle Interior Worker Sites v1

Hedef:

- Sol castle economy area marker hierarchy.
- Wood/Stone/Iron/Food site root'lari.
- Worker spawn point markerlari.
- Resource assign butonlari.
- Idle population kontrolu.
- Worker visual spawn.
- Worker count -> production rate baglantisi.
- Fullscreen slider economy player-facing akistan cikarilir.

Kabul:

- Worker Drawer'da Wood `+ WORKER` butonuna basinca idle pop varsa Wood worker artar.
- Yeni DOTS villager Wood pickup noktasi ile CastleWorkerHub delivery noktasi arasinda hareket eder.
- Wood/min artar.
- Idle pop yoksa buton disabled veya `NEED POP`.
- Ayni mantik Stone/Iron/Food icin calisir.

### M5.2 - Continuous Siege Loop v1

Hedef:

- Player-facing wave stop/start kaldirilir.
- Day/Dusk/Night/Dawn fazlari surekli akar.
- Spawn yogunlugu faza gore ramp-up/ramp-down yapar.
- Dawn/Day basinda population growth ve odul verilir.
- UI dili `Wave Cleared` yerine day/night phase diline doner.

Kabul:

- Oyun durmadan akar.
- Start Next Wave gerekmez ve gorunmez.
- Day'de ekonomi daha rahat, Night'ta baski daha yogun hissedilir.

### M5.3 - Left/Right Scene Composition v1

Hedef:

- Sol castle interior alaninin sahne kompozisyonu oturur.
- Orta/sag acik savas alani netlesir.
- Kamera, tilemap, spawn ve UI ayni kompozisyonu destekler.

Kabul:

- Ekranda sol ekonomi ve sag savas ayrimi net okunur.
- Merkezde buyuk bos kale hissi kalmaz.
- Zombi baskisi sag/orta acik alanda okunur.

### M5.4 - Siege Economy Events v1

Hedef:

- Rare event'ler sol ekonomi alanina baglanir.
- Eventler worker/resource kararlarini etkiler.
- Fullscreen popup yerine compact council/event badge kullanilir.

### M5.5 - Combat Juice v2

Hedef:

- Hit/death/castle damage feedback polish.
- Daha uygun particle veya flipbook alternatifleri.
- Stress/performance uyumlu VFX modu.

Bu milestone, loop ve ekonomi kimligi oturduktan sonra ele alinmalidir.

---

## 16. Acik Sorular

1. Sol ekonomi alaninin kesin ekran genisligi ne olacak? Oneri: %30-35.
2. Worker assignment geri alinabilecek mi, yoksa V1'de sadece ileri mi gidecek?
3. Site worker limitleri olacak mi, yoksa sadece population limit mi olacak?
4. Yeni sol/sag kompozisyonda zombiler full 360 mi kalacak, yoksa sag agirlikli spawn mi olacak?
5. Fullscreen Castle Economy panel tamamen silinecek mi, yoksa debug olarak mi kalacak?
6. Dawn odulu ne kadar buyuk olacak?
7. Rare eventler day basinda mi, belli araliklarla mi gelecek?

---

## 17. Guncel Karar Ozeti

- Ana gelistirme sahnesi `NewGameScene`.
- Guncel oyun yonu mobile continuous siege.
- Sol tarafta yasayan castle interior economy alanina gecilecek.
- Worker assignment butonla olacak; tap progress yok.
- Worker visual sadece feedback degil, ekonominin okunabilir gorselidir.
- Zombiler icin ana hedef cok sayi; formasyon yok.
- Wave stop/start player-facing olarak kaldirilacak.
- Okcular `Grid/outside` tilemapinden spawn olmaya devam edecek.
- Sag panel Archer Progression olarak kalacak.
- Fullscreen Castle Economy panel kademeli olarak legacy/debug'e cekilecek.
- Particle polish en son asamalara birakilacak.

---

*DEAD WALLS - GDD v5.0*  
*Guncel ana yon: Mobile Continuous Siege + Castle Interior Economy*
