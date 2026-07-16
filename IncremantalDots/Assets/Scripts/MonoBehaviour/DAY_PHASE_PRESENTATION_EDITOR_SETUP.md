# Day Phase Presentation Editor Setup

## Scene Sozlesmesi

`Assets/Scenes/NewGameScene.unity` icinde:

- Tek `Global Light 2D` bulunur; `DayNightOverlayController.GlobalLight` buna baglidir.
- `Canvas/DayNightOverlay`, mevcut raycast-free fullscreen `Image` ve tek
  `DayNightOverlayController` component'ini tasir.
- Tek `AmbientAudioRoot/AmbientAudioController` bulunur. Runtime child AudioSource'lar
  `AmbientLoopA`, `AmbientLoopB`, `AmbientSting` ve `WorkerAmbience` olarak Awake'ta uretilir.
- `WorkerFoleyClips` sirasi Sawing Wood, Nail Wood, Blacksmithing ve Rock Impact'tir; dordu de
  null olmayan `AudioClip` asset referansidir.

## Hedefli Onarim

Unity Editor'de `NewGameScene` aktifken:

`Window -> DeadWalls -> Repair Day Presentation`

komutu calistirilir. Komut idempotent olarak tek global light'i warm Day varsayilanina getirir,
overlay controller binding/paletini normalize eder, mevcut ambient owner'a dort worker foley
asset'ini baglar ve sahneyi kaydeder. Prefab, SubScene veya baska sahne degistirmez.

## Dogrulama

- `DayPhasePresentationTests`: warm Day rengi/intensity, Dusk/Dawn continuity ve bounded worker
  cadence kurallarini dogrular.
- `WorkerAllocationPlayModeTests.DayPresentation_WarmLightKeepsProductionReadableAndWorkerAmbienceScalesWithWorkers`:
  gercek Day cycle, tek global light, current worker cap/ratio sisteminin settle ettigi authoritative
  Wood allocation'inin represented production strength'i, delivery pulse'u ve Day-only worker
  foley calimini dogrular.
- MCP Game View QA, 1920x1080 Day goruntusunde kale/tilemap/worker kontrastinin sicak fakat
  yikanmamis; resource strip production rate'lerinin okunur kaldigini kontrol eder.
