# Castle Heart Graph Generator - Editor Setup

## Production kurulumu oncesi

Bu paket generator altyapisini tamamlar fakat production Heart catalog asset'i
olusturmaz. Launch node icerikleri, Essence maliyetleri, rarity/depth dagilimi ve
Keystone trade-off ciftleri owner tarafindan onaylanmadan scene/runtime binding yapma.

## Definition asset'leri

Onayli her node icin:

1. `Assets > Create > DeadWalls > Castle Heart > Heart Node Definition` kullan.
2. Stable ve benzersiz `Id` ver; `castle_heart` kullanma.
3. `Type`, `Branch`, `Rarity`, `MinimumDepth` ve `MaximumDepth` alanlarini doldur.
4. Grave Essence maliyetlerini yalniz onayli tuning tablosundan gir.
5. Gameplay `Effects` ile node metninin ayni sonucu anlattigini kontrol et.
6. Repeatable sink ise `sink:repeatable` tag'ini ekle.

Guarantee node'lari catalog genelinde tam bir kez su tag'leri tasimalidir:

- Rapid: `guarantee:rapid`, Army, `UnlockArcherType/Rapid`.
- Frost: `guarantee:frost`, Army, `UnlockArcherType/Frost`.
- Fireball: `guarantee:fireball`, HeartMagic, `UnlockSpellcasting`.
- Wall: `guarantee:wall`, Defense, `ModifyWallMaxHpPercent`.

## Keystone cifti

Her iki node da `Type = Keystone` olmali. `ConflictNodeIds` dizisinde tam bir partner
bulunmali ve esleme simetrik yapilmalidir:

- A node'u yalniz B Id'sini tasir.
- B node'u yalniz A Id'sini tasir.
- Normal node'a conflict Id yazilmaz.

## Catalog asset'i

1. `Assets > Create > DeadWalls > Castle Heart > Heart Node Catalog` kullan.
2. `RootNodeId` degerini `castle_heart` olarak koru.
3. Yalniz onayli `HeartNodeDefinitionSO` asset'lerini `Nodes` listesine ekle.
4. Duplicate Id, duplicate/bos tag ve eksik Keystone partner birakma.
5. Her dort branch'te izinli depth araliginda en az bir `sink:repeatable` bulundur.

Catalog array sirasi sonucu degistirmez; generator node'lari stable Id'ye gore siralar.

## Runtime request

`HeartGraphGenerationRequest` degerleri kod tarafindan gizli default kabul etmez.
Runtime owner su alanlari acikca vermelidir:

- Run `Seed`.
- Minimum/maksimum branch depth.
- Maksimum cross-link.
- Keystone cift sayisi.
- Maksimum deterministic attempt.
- Standard ve Rare rarity agirliklari.

Bu tuning degerlerinin production owner'i onaylandiginda E3 run-start entegrasyonu
generator'i cagirir. `TryGenerate` false donerse null graph ile devam etme; acik hata
yuzeyi veya onayli fallback kullan. Reveal sirasinda generator'i tekrar cagirma.

## Dogrulama

EditMode hedefli test:

`DeadWalls.Tests.HeartGraphGeneratorTests`

Production catalog eklendiginde ayrica asset-bazli catalog validation ve ayni catalog
version + seed determinism regression testi eklenmelidir. E2 sentetik testleri launch
icerik review'unun yerine gecmez.

