# Phase World Readability Architecture

## Amaç ve Kaynak

`DEAD_WALLS_GAME_DESIGN_BLUEPRINT_v1.0` sayfa 27, fazların büyük tam ekran yazıyla değil
`color grading + sky + particles + audio` ile okunmasını kilitler. Bu paket yeni bir cycle veya
gameplay state owner'ı kurmaz; mevcut `ContinuousSiegeCycleData` tek truth kaynağıdır.

## Katman Sahipliği

- `DayNightOverlayController`: world grading, Global Light 2D ve kale pencere ışıkları.
- `MomentVignetteUI`: Main Camera sky rengi ve tek bounded atmosfer ParticleSystem.
- `AmbientAudioController`: Day worker ambience, Dusk riser, Night mix ve Dawn cue.
- `HUDController`: yalnız minimal `B - Celestial Dial`; büyük phase title ve ham label'lar kapalı.

`MomentVignetteUI` adı serialized geriye uyumluluk için korunur. Runtime sorumluluğu generic
full-screen flash üretmek değil, sky ve atmosfer parçacıklarını authoritative faza bağlamaktır.

## Sky ve Particle Eğrisi

- Day: düşük yoğunluklu sıcak toz ve nötr sıcak sky.
- Dusk: Day'den amber zirveye, oradan Night indigosuna geçer; particle oranı amberde yükselir.
- Night: cold-blue sky ve düşük yoğunluklu mavi motes.
- Dawn: Night'tan cyan'a, altına ve Day'e akar; cyan/altın motes kısa geçişi destekler.

Tek `PhaseAtmosphereParticles` sistemi `maxParticles = 72` ile sınırlıdır. Faz başına yeni
ParticleSystem veya emitter oluşturulmaz. Dusk/Night/Dawn kenarlarında sırasıyla `10/6/14`
parçacık burst edilir; sürekli emission da aynı 72 cap içinde kalır. Stress mode ve Game Over
yeni emission/burst üretmez.

## Tek-Sefer ve UI Sınırı

Scene load veya Continue sırasında görülen ilk faz transition sayılmaz. Bu yüzden ilk observation
particle burst veya Dawn flash üretmez. Canonical `DawnPeak = 0`; Dawn generic full-screen flash
yerine cyan/altın grading, sky, particles, gate/survivor ve audio ile okunur.

`CyclePhaseText`, `CycleDayLabelText`, `CycleDuskLabelText` ve `CycleNightLabelText` serialized
uyumluluk için bulunabilir fakat player-facing inactive kalır. Atmosfer objeleri raycast veya modal
UI üretmez.

## Asset Sözleşmesi

- `Assets/Art/Generated/phase_atmosphere_mote.png`: yumuşak radial alpha mote.
- `Assets/Materials/PhaseAtmosphereParticles.mat`: URP Particles/Unlit transparent material.
- `NewGameScene/AmbientAudioRoot/PhaseAtmosphereParticles`: tek scene ParticleSystem.
