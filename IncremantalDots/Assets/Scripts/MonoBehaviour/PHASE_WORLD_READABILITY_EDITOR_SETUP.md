# Phase World Readability Editor Setup

## Hedefli Onarım

1. `Assets/Scenes/NewGameScene.unity` sahnesini aç.
2. `Window > DeadWalls > Repair Phase World Readability` komutunu çalıştır.
3. Tool sky/particle owner'ını, generated mote/material assetlerini, grading ve audio binding'lerini
   onarıp sahneyi kaydeder.

Komut idempotenttir. İkinci çalıştırmada duplicate `MomentVignetteUI`, particle root, material veya
texture oluşturmaz.

## Beklenen Scene Sözleşmesi

`AmbientAudioRoot/MomentVignetteUI`:

- `SkyCamera = Main Camera`, `SkyColorMoveSpeed = 2.2`.
- `AtmosphereParticles = PhaseAtmosphereParticles`.
- Day/Dusk/Night/Dawn emission `1.8 / 8 / 3 / 10`.
- `DawnPeak = 0`; ilk observation burst üretmez.

`AmbientAudioRoot/PhaseAtmosphereParticles`:

- Tek ParticleSystem, world simulation, box `29 x 17`, `maxParticles = 72`.
- Lifetime `4.5-7s`, size `0.035-0.12`, düşük hız/noise ve alpha fade.
- `Objects` sorting layer, order `40`.
- Material `Assets/Materials/PhaseAtmosphereParticles.mat`.

HUD:

- `CyclePhaseText`, `CycleDayLabelText`, `CycleDuskLabelText`, `CycleNightLabelText` inactive.
- Yalnız `CycleDayCounterText` ve Celestial Dial player-facing kalır.

## Manuel QA

1. Day -> Dusk: sky amberden indigoya akmalı, mote yoğunluğu kısa süre yükselmelidir.
2. Dusk -> Night: cold-blue sky/motes görünmeli; büyük NIGHT yazısı çıkmamalıdır.
3. Night -> Dawn: cyan sonra altın motes görünmeli; generic full-screen Dawn flash olmamalıdır.
4. Dawn -> Day: sky ve düşük yoğunluklu sıcak toz Day değerine dönmelidir.
5. HUD, Wall, horde ve kale particle alanının önünde okunur kalmalıdır.
6. Continue ile herhangi bir faza yüklemede edge burst tekrarlanmamalıdır.
