# Simulation Speed and Pause - Architecture

## Amac

Oyuncu kosu sirasinda `1X`, `2X` veya `3X` hizi secebilir. Pause isteyen UI yuzeyleri
`Time.timeScale` degerini dogrudan yonetmez; merkezi lease sahibi secili kosu hizini ve ECS
simulation state'ini birlikte korur.

## Tek Otorite

- `SimulationSpeedUtility`: desteklenen kosu hizlarini `1`, `2`, `3` olarak tanimlar.
- `SimulationPauseService`: secili kosu hizinin ve aktif pause lease'lerinin tek runtime
  otoritesidir.
- `GameplayHUDToolkitUI.GameFlow.cs`: gun dongusu panelinin altindaki `1X / 2X / 3X`
  kontrollerini baglar ve aktif hizi gosterir.
- `GameplayHUDToolkitUI.Modals.cs`: aktif Council karari icin `CouncilDecision` lease'ini alir.

Yeni player-facing kod `Time.timeScale` yazmamalidir. Desteklenmeyen hizlar servis tarafindan
reddedilir. Run bootstrap ve tarihsel Game Over sunumu gibi mevcut sahipler ayri uyumluluk
sinirlaridir.

## Pause ve Hiz Geri Yukleme

Ilk pause lease'i acildiginda servis hem guncel kosu hizini hem de ECS
`SimulationSystemGroup.Enabled` state'ini yakalar. Lease sayisi sifira inene kadar:

- `Time.timeScale = 0` tutulur.
- ECS simulation group kapali tutulur.
- HUD secili hizi `PAUSED - NX` biciminde gosterir ve hiz butonlarini kilitler.

Son lease kapandiginda yakalanan ECS state'i ve secili kosu hizi aynen geri yuklenir. Bu nedenle
oyuncu `3X` hizindayken Council acilirsa karar sonrasi yeniden `3X` devam eder. Ic ice modal
lease'lerinde yalniz son lease simi devam ettirebilir.

## Council Sozlesmesi

- Aktif, cozulmemis Council karari production UI Toolkit HUD tarafindan pause lease'i alir.
- Uygulanabilir bir secenek basariyla commit edilince Council lease'i ayni aksiyonda birakilir.
- Gecersiz veya uygulanamayan secenek karti kapatmaz ve pause devam eder.
- Pause altinda faz zamani ilerlemedigi icin eski player-facing geri sayim yerine
  `GAME PAUSED - CHOOSE TO CONTINUE` mesaji kullanilir.
- Exact save Council payload'ini korur; secili hiz save semasina yeni alan eklemez.

## Test Sahipligi

- `HeartScreenPauseTests`: desteklenen hizlar, reddedilen hiz ve nested lease restore davranisi.
- `NoSpeedOfflineProgressContractTests`: player-facing hiz yaziminin merkezi serviste kalmasi.
- `GameplayHUDToolkitContractTests`: UXML/USS/controller binding ve Council pause copy'si.
- `CouncilRegularSchedulePlayModeTests`: gercek `NewGameScene` icinde `3X -> pause -> secim -> 3X`
  geri yukleme kontrati.
