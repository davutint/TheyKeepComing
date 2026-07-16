# Night Phase Presentation Editor Setup

## Hedefli Onarım

1. `Assets/Scenes/NewGameScene.unity` sahnesini aç.
2. `Window > DeadWalls > Repair Night Presentation` komutunu çalıştır.
3. Tool tek owner'ları bulur veya onarır, dört canonical pencere ışığını tilemap'ten üretir,
   Night audio/combat tuning'ini bağlar ve sahneyi kaydeder.

Komut idempotenttir: ikinci çalıştırmada duplicate root, Light2D, AudioSource veya feedback
bridge oluşturmaz.

## Beklenen Scene Sözleşmesi

`DayNightOverlayController`:

- Tek `Global Light 2D` binding'i.
- `NightLightColor = (0.46, 0.58, 0.94)`, `NightLightIntensity = 0.68`.
- `CastleWindowLights`: tam dört non-null Point Light 2D.
- Window color/intensity: `(1.00, 0.47, 0.12) / 0.82`.

`CastleNightWindowLights`:

- `WindowGlow_01..04`, Additive Point Light 2D.
- Inner/outer radius: `0.08 / 0.72`, falloff `0.85`.
- Position'lar `Wall A5_S` ve `Wall A5_N` tile merkezlerinden gelir.

`AmbientAudioRoot/AmbientAudioController`:

- `NightHordeLoop = RPG3_WindMagic_Drone01_DarkWindLoop.wav`.
- Runtime `NightHordeBed`: loop, 2D, playOnAwake false.
- Volume/fade: `0.18 / 0.40`.

`CombatFeedbackRoot/CombatFeedbackBridge`:

- Audio pool `16`, max SFX/frame `4`.
- Shoot/Night shoot min interval `0.075 / 0.12`.
- Salvo volume cap/pitch depth `0.62 / 0.08`.

## Manuel QA

1. Dusk'tan Night'a geç; global grading amber'den soğuk maviye kesintisiz akmalıdır.
2. Dört kale penceresi sıcak ve küçük ışık adaları olarak görünmelidir.
3. Zombie sayısı büyürken ayrı horde bed duyulmalı fakat ses seviyesi sınırsız büyümemelidir.
4. Çok sayıda okçu aynı anda ateş ettiğinde tek bir dolgun salvo duyulmalı; tekil ses yığını
   oluşmamalıdır.
5. HUD, Wall ve zombie silhouette'i Night paletinde okunur kalmalıdır.
