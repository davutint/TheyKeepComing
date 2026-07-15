# Castle Heart Reveal ve Player Information - Mimari

## Kapsam

`DW-E-REVEAL`, run basinda tamamen uretilmis `GeneratedRunGraph` uzerindeki visibility
gecislerini ve UI'nin tuketecegi hidden-safe presentation contract'ini kurar. Yeni node
secmez, graph edge'i uretmez ve hicbir reveal aksiyonunda RNG kullanmaz.

Bu paket legacy `TechTreeUI`yi yeni Heart runtime'ina gecirmez. Production Heart catalog
ve scene binding owner onayi bekledigi icin mevcut legacy satin alma/reveal akisi aktif
kalmaya devam eder.

## Visibility owner'i

`HeartGraphRevealService` visibility state'inin tek mutation owner'idir:

- `InitializeRunVisibility`: root'un depth `0`, level `1` oldugunu dogrular; root'u ve
  yalniz root'tan cikan edge hedeflerini reveal eder.
- `RevealAfterFirstPurchase`: source node'un gorunur ve satin alinmis oldugunu dogrular;
  `previousLevel == 0` iken outgoing komsulari reveal eder. `0 -> 10` gibi bulk ilk
  alimlar da ayni kurala dahildir.
- Repeatable node'un `previousLevel > 0` olan sonraki transaction'lari yeni node reveal etmez.
- Hidden veya level `0` node reveal kaynagi olamaz.
- Cross-link, generator tarafindan onceden uretilmis normal bir outgoing edge oldugu icin
  bagli komsu olarak reveal edilebilir.

Servis idempotent'tir. Ayni initialization veya first-purchase olayi tekrar oynatilirsa
zaten gorunur node'lar yeniden eklenmez. Servis graph icerigini degistirmez; yalniz
`GeneratedHeartNodeState.Visibility` alanini gunceller.

## Hidden-safe presentation

`HeartGraphPresentationBuilder`, internal graph state ile Heart UI arasindaki redaction
siniridir. UI dogrudan `GeneratedRunGraph` veya catalog okumamalidir.

Her graph node'u icin content-independent bir safe slot uretilir:

- Root: `heart:root`.
- Diger node'lar: `<branch>:<depth>`, ornegin `army:2`.

Hidden node presentation'i yalniz su bilgileri tasir:

- Safe slot.
- Branch.
- Depth.
- Damar/edge topology'si.
- Gorunur bir Keystone tarafindan hedefleniyorsa conflict marker.

Hidden node'da internal node Id, title, description, icon, type, rarity, level, lock ve
effect listesi bos/null kalir. Presentation edge'leri de internal node Id yerine yalniz
safe slot Id'leri tasir. Boylece UI veya tooltip kodu remote exact node'u yanlislikla
ifsa edemez.

Root ve `Revealed` node presentation'i authored title/description/icon/type/rarity,
runtime level/lock ve player-facing effect satirlarini alabilir.

## Gercek effect sonucu contract'i

Blueprint, oyuncunun gordugu numeric effect'in ham authored degeri yerine gercek mevcut
ve satin alim sonrasi sonucunu bilmesini zorunlu tutar. Bu nedenle presentation builder
numeric effect'i kendi basina formatlamaz.

`IHeartEffectValueResolver`, E4 `HeartEffectPipeline` tarafindan uygulanir.
Resolver su degerleri birlikte dondurur:

- Player-facing label.
- Mevcut deger.
- Satin alim sonrasi gercek deger.
- Gercek delta.

Numeric effect gorunur olup resolver yoksa veya eksik sonuc dondururse builder fail-closed
hata verir ve node'u `EffectInformationComplete = false` isaretler. UI bu state'te sahte
veya tahmini sayi gostermemelidir.

Pipeline; runtime baseline, raw Heart investment ve soft-cap policy'sinden current/after/delta
uretir. Production baseline/sink adapter'lari E5 runtime cutover'i ile baglanacaktir.

Unlock/Evolution davranis effect'leri numeric soft-cap gerektirmedigi icin contract
tarafindan dogrudan cozulur: archer unlock, spellcasting unlock, split shot ve burning
ground.

## Keystone presentation istisnasi

Remote exact-node gizliliginin Blueprint'teki acik istisnasi gorunur Keystone'dur.
Gorunur Keystone presentation'i:

- Karsi secimin player-facing basligini.
- Kapanacak safe slot'u.
- Karsi secimin su anda revealed olup olmadigini.
- Satin alimda lock uygulanacagini veya secim yapildiysa hedefin zaten bu Keystone
  tarafindan kilitlendigini.
- Gorunur source Keystone'un karsi secim tarafindan kilitlenmis olup olmadigini.

tasir. Internal partner node Id'si presentation'a verilmez. Partner hidden kalabilir;
safe slot `IsKeystoneConflictTarget` ile isaretlenir. Gercek lock mutation'i E4
purchase pipeline'inin sorumlulugudur.

## Persistence ve UI siniri

- Graph node listesi ve hidden icerik E2'de run basinda kesinlesir.
- E3 reveal servisinde yeni RNG yoktur.
- `GeneratedRunGraph` exact save/load baglantisi guncel schema v11 ile aktiftir; Continue saved
  visibility state'ini genisletmeden oldugu gibi kurar.
- Numeric effect'in effective runtime hesabini E4 resolver'i saglar; production runtime
  adapter binding'i E5'te tamamlanir.
- Prefabda branch damari, hidden slot, tooltip ve Keystone conflict cizimi E5 Heart UI
  cutover'inda yapilir.

Persistence, numeric bilgi ve gercek UI sunumu E3-E6 owner paketleriyle canli runtime'a
baglidir.

## Test kapsami

`HeartGraphRevealTests` su contract'lari kanitlar:

- Initial root komsulari reveal, remote node'lar hidden.
- Initialization idempotency.
- Ilk satin alimda, bulk `0 -> N` dahil, yalniz outgoing komsular ve controlled cross-link reveal.
- Sonraki repeatable level'da reveal yok.
- Hidden node Id/title/effect redaction ve safe-slot edge topology.
- Numeric resolver yoksa fail-closed; resolver varsa gercek current/after/delta satiri.
- Gorunur Keystone'un partner basligi, safe conflict slotu ve pre/post-purchase lock durumu.
- Hidden veya satin alinmamis node'un reveal kaynagi olamamasi.
