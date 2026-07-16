# Day Phase Presentation Architecture

## Owner Siniri

`DayNightOverlayController`, authoritative `ContinuousSiegeCycleData` fazini okuyup hem mevcut
UI atmosfer overlay'ini hem sahnedeki tek `Global Light 2D` rengini ve intensity'sini surer. Yeni
bir cycle, lighting manager veya ikinci global light kurulmaz.

`AmbientAudioController`, ayni faz verisini kullanarak gece loop'lari ile Day worker foley
ritmini tek audio owner'da toplar. Worker foley gameplay transaction'i, worker assignment'i veya
production tick'i uretmez; yalniz `GameManager.GetResourceWorkers(Balanced)` ile gercek aktif
worker sayisini okur.

## Warm Day Isigi

- Canonical Day hedefi `RGB(1.00, 0.93, 0.82)` ve intensity `1.08`'dir.
- Day boyunca hedef sabittir; kamera/tilemap kontrastini karartmadan hafif sicak renk verir.
- Dusk, Night ve iki-asamali Dawn hedefleri ayni global light'ta yumusak gecis tabani saglar.
- UI overlay alpha owner'i mevcut combat config'te kalir. Global light ve overlay ayni controller
  tarafindan `Time.unscaledDeltaTime` ile suruldugu icin pause sirasinda faz sunumu bozulmaz.

## Okunur Uretim

Worker uretim okunurlugu yeni billboard veya sayisal world UI ile kopyalanmaz. Mevcut
`WorkerLogisticsMovementSystem` / `SpriteSheet.shader` kontrati korunur:

- `_WorkerFeedback.w`: represented actual-worker agirligindan cargo okunurluk gucu.
- `_WorkerFeedback.x`: pickup sonrasinda gorunen resource cargo.
- `_WorkerFeedback.z`: hub tesliminde kisa delivery pulse.
- `_WorkerFeedback.y`: yalniz Dusk/Night lantern; Day'de sifir.

Warm Day light, bu mevcut cargo + delivery sinyalini zeminden ayirir. PlayMode regresyonu gercek
worker entity'sinde production strength ve teslim pulse'unun Day fazinda aktif kaldigini kanitlar.

## Worker Ambience

- Dört canonical foley klibi mevcut lisansli `Fantasy UI SFX - Lite Edition` paketinden gelir:
  sawing wood, nail wood, blacksmithing ve rock impact.
- Kaynak tek, 2D ve non-loop `WorkerAmbience` AudioSource'udur; worker basina AudioSource yoktur.
- Aktif worker sayisi logaritmik olarak `0..1` activity'ye map edilir. Bu deger ses seviyesini ve
  `5.2s -> 1.6s` bounded cadence'i surer; binlerce worker ses yogunlugunu sinirsiz artiramaz.
- Foley yalniz Day, en az bir aktif worker, yasayan run ve pause olmayan durumda calisir.
  Dusk/Night/Dawn, Game Over veya blocking pause aninda durur.
- Klip sirasi round-robin, pitch/cadence varyasyonu deterministiktir; ayni klibin rastgele spam'i
  ve test flakiness'i olusmaz. Son ses seviyesi `SoundSettings.AmbienceVolume` ile carpilir.

## Verification

- EditMode palette ve bounded worker-cadence kurallarini saf girdilerle kilitler.
- PlayMode gercek `NewGameScene` icinde tek global light binding'ini, warm Day hedefini, worker
  production material feedback'ini ve actual-worker-driven foley calimini birlikte dogrular.
