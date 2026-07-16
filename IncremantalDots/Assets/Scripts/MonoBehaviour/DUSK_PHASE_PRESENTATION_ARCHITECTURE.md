# Dusk Phase Presentation Architecture

## Sahiplik

`DayNightOverlayController`, Dusk renk gecisinin tek presentation sahibidir. Authoritative
`ContinuousSiegeCycleData.PhaseProgress01` degerini iki parcali bir global-light egriye cevirir:

- `0.00 -> 0.45`: warm Day renginden amber `DuskLightColor` hedefine.
- `0.45 -> 1.00`: amber'den soguk `NightLightColor` indigosuna.

Overlay de ayni Dusk progress'i ile amber tint'ten tek canonical Night tint'ine akar.
Night basladiginda renk sicramasi olmaz; Dusk zaten Night paletinin baslangic hedefine ulasmistir.

## Worker Lantern

Yeni lantern sistemi veya worker basina light kurulmaz. Mevcut ECS sunum kontrati korunur:

- `WorkerVisualRepresentationUtility.ShouldUseLantern` yalniz Dusk ve Night icin true doner.
- `WorkerLogisticsMovementSystem`, gercek worker representation entity'lerine
  `WorkerLogisticsFeedbackState.LanternActive = 1` yazar.
- `_WorkerFeedback.y`, mevcut worker shader'inda lantern emissive sinyalidir.

Bu yol worker sayisindan bagimsiz, allocation-free ve mevcut 10k horde butcesinden ayridir.

## Tension Riser

`AmbientAudioController`, faz ambiyansinin tek runtime sahibidir. Controller Day'i ilk kez
observe ettikten sonra Dusk kenarini gordugunde tek bir 2D `PhaseTransition` AudioSource uzerinden
`DuskRiser` klibini oynatir. Polling ayni Dusk icinde tekrar oynatmaz. Save/Continue sahneyi zaten
Dusk veya Night'ta actiysa ilk observation transition sayilmaz; cue yeniden tetiklenmez.
Non-loop one-shot source factory gain'i `1`; duyulan seviye yalniz explicit cue volume ile
`SoundSettings.AmbienceVolume` carpimindan gelir. Loop crossfade source'lari `0` gain ile baslar.

Canonical clip mevcut lisansli SFX paketindeki
`Wind Magic/RPG3_WindMagicEpic_Cast01_P1.wav` dosyasidir. Dusuk `0.23` ambience volume ve
`0.90` pitch, cue'yu UI stinger'i veya buyu cast'i yerine Dusk gerilimi olarak mix'te tutar.

## Dogrulama Sozlesmesi

- EditMode: Dusk baslangici Day, `%45` noktasi amber, Dusk sonu Night indigo hedefidir.
- PlayMode: gercek cycle Day -> Dusk gecerken global light hedefi izler, worker lantern sinyali
  yanar ve riser sayaci yalniz bir kez artar.
- Scene repair: tek `AmbientAudioController`, tek `DayNightOverlayController`, tek Global Light 2D
  ve canonical Dusk clip binding'i korunur.
