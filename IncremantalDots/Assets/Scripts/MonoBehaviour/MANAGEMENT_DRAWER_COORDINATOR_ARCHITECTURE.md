# Management Drawer Coordinator - Mimari

## Kapsam

`ManagementDrawerCoordinatorUI`, continuous HUD'da ayni anda yalniz bir player-facing
management yuzeyinin acik kalmasini saglar. Koordine edilen uc yuzey:

- `WorkerEconomyDrawerUI`: Workers + Housing;
- `MarketUI`: Archer Recruitment;
- `ArrowSupplyUI`: Arrow stok/refill ve CAP/EFF yatirimi.

Arrow Supply girisi resource strip'teki pasif `ArrowChip` degil, alt-sag dock'taki ayri
`ArrowSupplyToggleButton` / `ARROW SUPPLY` kontroludur.

Castle Heart fullscreen modal, Council karti, Pause ve Game Over bu drawer grubuna dahil
degildir. Kendi modal/pause sahipliklerini korurlar.

## Sorumluluk siniri

Coordinator transaction, text refresh, animasyon veya gameplay verisi yazmaz. Her drawer
kendi controller'inda kalir. Bir controller acilmak istediginde `Claim()` cagirir;
coordinator diger iki yuzeyi kapatir ve `ActiveDrawer` kimligini gunceller. Aktif yuzey
kapaninca `Release()` kimligi `None` yapar.

Archer paneli slide animasyonu kullandigi icin baska bir drawer claim ettiginde
`SetDrawerOpen(false, true)` ile aninda kapanir. Boylece gecis frame'lerinde iki panel
ust uste kalmaz. Worker ve Arrow panelleri zaten active-state ile aninda kapanir.

## Runtime sahipligi

- Tek coordinator aktif `NewGameScene` icindeki `MobileCastleHudRoot` scene instance'inda bulunur.
- Generated HUD prefab yalniz presentation hiyerarsisidir; runtime controller eklenmez.
- Controller referanslari ayni GameObject uzerinden bir kez cozulur ve cache'lenir.
- Per-frame polling, hierarchy scan veya allocation yoktur; koordinasyon yalniz open/close event'inde calisir.

## Regression kapisi

`ManagementDrawerCoordinatorTests` uc claim sirasini, mutual exclusion'i ve `CloseAll()`
sonrasinda butun yuzeylerin kapanip active kimligin serbest kalmasini kilitler.
