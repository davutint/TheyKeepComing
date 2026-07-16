# Dawn Phase Presentation Architecture

## Sahiplik

Dawn sunumu yeni bir cycle veya population sistemi kurmaz. Mevcut owner'lar birlikte çalışır:

- `DayNightOverlayController`: Night -> cyan -> altın -> Day global grading ve overlay.
- `MobilePopulationEconomySystem`: yatak ve Food sınırlarına göre gerçek accepted population transaction'ı.
- `GameManager` + `SurvivorArrivalVisualSystem`: mevcut `VillagerWorker` prefabıyla geçici survivor yürüyüşü.
- `DawnRewardToastUI`: gerçek accepted sayıyı gösteren toast ve ana portcullis tile sunumu.
- `AmbientAudioController`: gerçek Dawn faz kenarında tek 2D nefes/yeni-gün cue'su.

Food ve population yalnız ECS transaction'ında değişir. Kapı, ışık, toast, ses ve transient
villager'lar gameplay truth'u değildir.

## Cyan-Altın Kırılma

`DayNightOverlayController`, Dawn progress'ini üç parçaya böler:

1. `0.00 -> 0.28`: cold-moon Night renginden cyan sabah ışığına.
2. `0.28 -> 0.62`: cyan'dan altın-pembe Dawn ışığına.
3. `0.62 -> 1.00`: altından okunur warm Day ışığına.

Overlay aynı cyan/altın sırasını izlerken alpha Night değerinden Day değerine iner. Night
pencere ışıkları Dawn'ın ilk `%65` bölümünde söner. Böylece faz değişimi büyük tam ekran yazıya
bağımlı olmadan dünya ışığında okunur; genel faz-okunurluğu tracker maddesi ayrıca açık kalır.

## Survivor ve Ana Kapı

Accepted survivor görselleri Wall'ın `9.5` unit sağında, en fazla `15` entity olarak doğar ve
`3.0+` hızla Wall'ın `0.8` unit arkasına yürür. Deterministik lane, satır ve start-delay farkları
korunur; en geç görsel canonical `5s` Dawn süresi bitmeden hedefe ulaşır.

`DawnRewardToastUI`, `NewGameScene/outside2` tilemap'indeki tek `Door C5_E` hücresini sahiplenir:

- Gerçek accepted sayı pozitifse survivor yaklaşımına göre `2.05s` sonra `Door C6_E` ile açar.
- Kapıyı `2.55s` açık tutar, ardından `Door C5_E` ile kesin olarak kapatır.
- Aynı aralıkta tek Additive `DawnGateGlow` altın ışık envelope'u üretir.
- Faz Dawn'dan çıkarsa veya component kapanırsa kapı hemen kapalı state'e döner.

Building door tile'ları taranmaz; setup yalnız `Door C5_E/C6_E` sprite yolunu ve tam bir aday
sözleşmesini kabul eder.

## Tek-Sefer ve Continue Sınırı

`DawnRewardToastUI` ile `AmbientAudioController` ilk gördükleri fazı transition saymaz. Bu nedenle
scene load veya Continue doğrudan Dawn'a gelirse toast, kapı ve new-day cue tekrar oynatılmaz.
Gerçek Day/Dusk/Night -> Dawn kenarında ise her owner bir kez çalışır; aynı Dawn içindeki polling
ikinci sunum veya ikinci ses üretmez.

## Doğrulama

- EditMode: cyan/gold ışık ve overlay checkpoint'leri; tüm 15 survivor rotasının `5s` altında
  tamamlanması.
- PlayMode: gerçek accepted population/Food transaction'ı, üç transient survivor, tek toast,
  tek gate-open, gerçek `Door C5_E -> C6_E` tile swap, gate glow ve tek 2D Dawn cue.
- Continue: saved transaction marker'ı survivor entity'sini yeniden üretmez; ilk-faz gözlemi
  presentation edge'i değildir.
