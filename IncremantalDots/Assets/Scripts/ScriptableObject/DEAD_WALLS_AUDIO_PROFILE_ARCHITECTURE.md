# Dead Walls Audio Profile - Mimari

## Amac

`DeadWallsAudioProfileSO`, player-facing ses secimlerinin tek merkezi verisidir. Production
asset'i `Assets/Resources/DeadWallsAudioProfile.asset` yolundadir. Runtime owner'lar profile
scene veya prefab kopyasi tasimadan `Resources` uzerinden ulasir; kendi eski serialized
clip'lerini yalniz fallback olarak korur.

## Routing

- `CombatFeedbackBridge`: Arrow shoot ailesi, Arrow/Frost impact, Wall impact ailesi ve
  Fireball blast.
- `UiSoundFeedback`: click, confirm, denied ve game-over sting. Legacy uGUI dugmeleri pointer
  raycast'iyle; UI Toolkit dugmeleri document root'undaki `ClickEvent` route'uyla ayni merkezi
  `UiClickClip` ve SFX volume ayarini kullanir.
- `GameplayHUDToolkitUI`: Castle Heart research/reveal/denied/open ve Soul/Essence HUD varisi.
- `AmbientAudioController`: ambience override acilirsa night, dusk, dawn, horde ve worker foley.
- `MainMenuToolkitUI`: menu-music override acilirsa profil menu klibi.

Her kategori kendi override anahtarina sahiptir. Profil veya klip bulunamazsa mevcut scene
atamasi kullanilir. Bu sayede Audio Director icinden A/B karsilastirma yapilabilir.

## Skeleton ve Currency Karari

- Skeleton/zombie olum SFX'i yoktur. `CombatSfxType.ZombieDeath` serialized enum uyumlulugu
  icin korunur fakat producer event uretmez ve bridge bu tipe klip dondurmez.
- Soul ve Grave Essence gameplay bakiyesine olum aninda yazilir; ses bu transaction'i
  geciktirmez.
- Ses yalniz UI Toolkit flight hedef HUD anchor'ina vardiginda oynar.
- Ayni arrival penceresindeki miktarlar tek cue'ya toplanir. Volume ve pitch miktari lineer
  degil logaritmik izler ve kesin tavanda kalir. Soul ve Essence ayri kaynak/rate-limit kullanir.

## Curated Kaynaklar

- Ana yeni SFX kaynagi: `Gamemaster Audio - Pro Sound Collection v1.3 - 16bit 48k`.
- Korunan spesifik eski sesler: Arrow/Frost impact, Heart book/reveal/lock ve game-over sting.
- Biug menu/day/night parcalari profil icinde audition candidate olarak saklanir. Ambience veya
  music override owner oyun icinde dinleyip karar verene kadar varsayilan olarak kapali kalir.
- GameBurp OGG/WAV tekrarli katalog production profile'a eklenmez.

## Guvenlik

Profil runtime davranisi degil yalniz sunum secimi ve mix parametreleri tasir. ECS reward,
damage, drop chance, wallet ve save state profile bagli degildir. Unreferenced binlerce kaynak
ses build'e route edilmez; yalniz profil veya mevcut fallback tarafindan referanslanan klipler
production bagina girer.
