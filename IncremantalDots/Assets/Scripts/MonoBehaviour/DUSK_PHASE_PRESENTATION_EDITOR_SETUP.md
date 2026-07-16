# Dusk Phase Presentation Editor Setup

## Otomatik Onarim

1. `Assets/Scenes/NewGameScene.unity` sahnesini ac.
2. `Window > DeadWalls > Repair Dusk Presentation` komutunu calistir.
3. Tool sahneyi kaydeder ve asagidaki binding'leri idempotent bicimde kurar.

## Beklenen Binding'ler

`DayNightOverlayController`:

- `GlobalLight`: sahnedeki tek `Global Light 2D`
- `DayLightColor`: `(1.00, 0.93, 0.82)`
- `DuskLightColor`: `(1.00, 0.72, 0.47)`
- `NightLightColor`: `(0.46, 0.58, 0.94)`
- `LightMoveSpeed`: `2.5`

`AmbientAudioRoot/AmbientAudioController`:

- `DuskRiser`: `RPG3_WindMagicEpic_Cast01_P1.wav`
- `DuskRiserVolume`: `0.23`
- `DuskRiserPitch`: `0.90`

Runtime'da `AmbientAudioController` altinda olusan `PhaseTransition` AudioSource non-loop, 2D ve
`playOnAwake = false` olmalidir. Worker lantern icin sahneye yeni component eklenmez; SubScene'deki
mevcut worker material property akisi kullanilir.

## Manuel QA

1. Play Mode'a Day'de gir; worker lantern'lari kapali olmalidir.
2. Cycle'i Dusk baslangicina ilerlet; amber grading ve tek tension rise duyulmalidir.
3. Dusk ortasinda worker emissive lantern sinyali acik olmalidir.
4. Dusk sonunda global light soguk indigo hedefe yaklasmali ve Night girisinde renk sicramamalidir.
5. Ayni Dusk boyunca riser tekrar etmemelidir.
