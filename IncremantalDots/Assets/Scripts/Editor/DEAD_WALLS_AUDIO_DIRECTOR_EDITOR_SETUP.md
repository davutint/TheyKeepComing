# Dead Walls Audio Director - Editor Setup

## Acma

Unity menusu: `Tools > Dead Walls > Audio Director`.

Pencere `Assets/Resources/DeadWallsAudioProfile.asset` profilini otomatik acar veya yoksa
olusturur. `LOAD CURATED DEFAULTS` incelenmis Dead Walls shortlist'ini tekrar yukler.

## Kullanim

1. Kategori override anahtarini ac/kapat ve eski scene sesi ile yeni profil sesini A/B test et.
2. Her clip satirindaki `PLAY` ile Editor icinde audition yap.
3. Array ailelerinde `+ ADD CLIP` veya `-` ile kontrollu varyasyon ailesini duzenle.
4. `STOP PREVIEW` acik preview'i durdurur; `PING PROFILE` asset'i Project penceresinde secer.
5. Degisiklikler profile aninda kaydedilir. Scene/prefab uzerinde ayri apply islemi gerekmez.

## Varsayilanlar

- Combat, Interface, Castle Heart ve Currency Arrival override'lari aciktir.
- Ambience ve Menu Music override'lari kapali gelir; music alanlari audition adayi olarak hazirdir.
- Zombie/Skeleton death klibi yoktur ve araca eklenmemelidir.
- Fireball cast/burn tail, Emergency Repair ve Rally alanlari audition candidate'tir; ilgili
  ability zamanlamasi icin ayri runtime route onaylanana kadar gameplay'de oynatilmaz.

## Dogrulama

- Console'da eksik asset path hatasi olmamalidir.
- `DeadWallsAudioProfileTests` profil ailelerini ve bounded currency mix politikasini dogrular.
- `CombatRewardFeedbackPlayModeTests` gercek Skeleton olumunun death SFX uretmedigini ve Soul
  ile Essence seslerinin ancak HUD ucusu tamamlandiginda oynadigini dogrular.
