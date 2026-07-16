# Dawn Phase Presentation Editor Setup

## Hedefli Onarım

1. `Assets/Scenes/NewGameScene.unity` sahnesini aç.
2. `Window > DeadWalls > Repair Dawn Presentation` komutunu çalıştır.
3. Tool cyan/altın grading, Night window fade binding'i, Dawn audio, ana gate tile binding'i ve
   `DawnGateGlow` ışığını onarıp sahneyi kaydeder.

Komut idempotenttir. İkinci çalıştırmada duplicate controller, root veya Light2D üretmez; kapıyı
canonical kapalı `Door C5_E` state'ine döndürür.

## Beklenen Scene Sözleşmesi

`DayNightOverlayController`:

- `DawnCyanLightColor = (0.48, 0.82, 1.00)`, intensity `0.84`.
- `DawnLightColor = (1.00, 0.80, 0.60)`, intensity `0.96`.
- Cyan/gold peak progress: `0.28 / 0.62`.
- Dört Night window light Dawn'ın ilk `%65` bölümünde söner.

`MobileCastleHudRoot/DawnRewardToastUI`:

- `GateTilemap = outside2`, `GateCell = (1, 0, 0)`.
- Closed/Open tile: `Door C5_E / Door C6_E`.
- Open delay/duration: `2.05 / 2.55` saniye.
- `GateGlow = DawnGatePresentationRoot/DawnGateGlow`.

`DawnGatePresentationRoot`:

- Tek child `DawnGateGlow`, Additive Point Light 2D.
- Inner/outer radius `0.10 / 1.05`, intensity tavanı `0.76`.
- Pozisyon doğrudan ana gate tile merkezinden gelir.

`AmbientAudioRoot/AmbientAudioController`:

- `DawnCue = RPG3_WindMagic_Buff03v2_Shorter.wav`.
- Volume/pitch `0.28 / 1.00`.
- Runtime mevcut tek `PhaseTransition` AudioSource'unu kullanır; ayrı kaynak oluşturmaz.

## Manuel QA

1. Night sonundan Dawn'a geç; dünya önce cyan'a, sonra altına ve Day sıcaklığına akmalıdır.
2. Accepted population pozitifse survivor'lar sağdan farklı lane'lerde yaklaşmalıdır.
3. Survivor'lar Wall'a varmadan ana portcullis yükselmeli, grup geçtikten sonra kapanmalıdır.
4. Gate çevresindeki tek altın glow açılış boyunca yumuşak yükselip sönmelidir.
5. Dawn girişinde tek, kısa yeni-gün nefesi duyulmalı; aynı Dawn'da tekrar etmemelidir.
6. Continue ile Dawn içinde yüklemede toast, kapı, survivor yürüyüşü ve cue yeniden başlamamalıdır.
