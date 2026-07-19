# Grave Essence Drop Architecture

## Urun Sozlesmesi

- Grave Essence yalniz mevcut run icinde Castle Heart icin kullanilir.
- Yeni run `0` bakiye ile baslar.
- Her gercek, stress-test disi dusman olumu bagimsiz bir drop roll'u atar.
- Production baseline `%10` ihtimal ve basarili roll basina `1` taban Grave Essence'tir.
- Drop otomatik toplanir; fiziksel pickup veya oyuncu tiklamasi gerekmez.

## Runtime Akisi

1. `ZombieDeathSystem` bir zombiyi ilk kez `Dead` durumuna gecirir.
2. `GraveEssenceDropUtility`, authored seed, mevcut spawn RNG state'i ve kill ordinal ile stateless roll hesaplar.
3. Basarili roll bir `GraveEssenceDropEvent` uretir.
4. `GameManager` event'leri tek frame'de toplar ve toplam taban miktari yalniz
   `GrantGraveEssence(long)` kapisina verir.
5. Meta Essence Gain yuzdesi ve exact fractional accumulator canonical grant transaction'inda uygulanir.

Bu ayrim, Burst ECS death path'ini Mono wallet/meta owner'indan ayirir. Soul her olumde `1` kalirken
Grave Essence yalniz basarili roll'larda uretilir.

## Determinizm ve Guvenlik

- Roll global mutable RNG kullanmaz; ayni seed/stream/kill ordinal ayni sonucu verir.
- Parallel death queue sirasi toplam drop sayisini degistirmez.
- `0` chance sistemi kapatir, `1` chance her uygun olumde drop verir.
- Stress-test mode drop uretmez.
- Game Over sonrasinda `GrantGraveEssence` reddeder; Restart bakiyeyi ve remainder'i sifirlar.

## Tuning Owner

Production chance ve miktar `DefaultDifficulty.asset` icindeki `DifficultyProfileSO` alanlaridir:

- `GraveEssenceDropChance = 0.10`
- `GraveEssencePerDrop = 1`

`Difficulty Tuner > Heart Runtime Contract` ayni alanlari duzenler ve beklenen ortalama
oldurme/drop cadence'ini gosterir. Heart node maliyetleri ayri `HeartNodeDefinitionSO` asset'lerinde kalir.
