# Single Wall Defense - Mimari

## V1 sözleşmesi

Dead Walls V1 savunma ve koşu sonucu için tek otorite `WallSegment` component'idir. Wall `0 HP` olduğunda oyun biter; başka savunma katmanı sonucu erteleyemez veya Wall'u diriltemez.

## Bake sınırı

`CastleAuthoring.Baker` aktif castle entity'sine yalnız şunları ekler:

- `WallSegment`
- `WallXPosition`
- `CastleUpgradeData`

`GateComponent` ve `CastleHP` eski scene/entity migration uyumluluğu için data type olarak kalır fakat aktif V1 baker tarafından üretilmez.

`CastleAuthoring.GateHP` ve `CastleMaxHP` eski serialized scene verisinin kaybolmaması için hidden field olarak kalabilir; runtime davranış owner'ı değildir.

## Aktif yollar

- Damage: `DamageApplySystem` bütün zombie damage queue'sunu yalnız `WallSegment` üzerine uygular.
- Game Over: yalnız `SingleWallDefenseRules.IsDestroyed(Wall.CurrentHP)` sonucu üretir.
- Repair: `GameManager.RepairDefenseFull` ve `RepairWallToFull` yalnız Wall okur/yazar.
- Council heal: `HealWallByPercent` yalnız Wall okur/yazar ve yıkılmış Wall'u diriltemez.
- Tech/meta defense: yalnız Wall MaxHP baseline/aggregate kanalını kullanır.
- Save/Continue: `RunSaveState.WallCurrentHP` vardır; Gate/Core alanı yoktur.
- HUD: tek Wall fill/text gösterilir; serialize kalmış Gate/Core slider, text ve fill objeleri runtime'da kapatılır.

`UpgradeType.RepairGate` adı legacy enum/serialization uyumluluğu için kalır; görünen metin `Repair Wall`, uygulanan davranış `RepairWallToFull`dur. Mobile V1 level-up card akışı ayrıca player-facing değildir.

## Terminal state owner sınırı

Production runtime kaynaklarında `GameStateData.IsGameOver = true` yazan tek owner
`DamageApplySystem`dir. Bu yazma yalnız
`SingleWallDefenseRules.IsDestroyed(remainingWallHp)` guard'ından sonra gerçekleşir.
`GameManager`, authoritative `false -> true` geçişini izleyerek death transaction ve sunumu bir kez
başlatır; terminal state üretmez.

Bu nedenle wave tamamlanması, cycle/day sayısı, horde pressure, enemy/boss ölümü veya ikinci bir
fail phase koşuyu bitiremez. `GameManager.IsMobileFinalWavePressure()` legacy isimli bir HUD threat
color sinyalidir; `IsGameOver` yazmaz ve final-wave sonucu değildir.

## Doğrulama

- `SingleWallDefenseRulesTests`
- `LegacyDefenseExclusionTests.RunSaveSchema_ContainsWallState_ButNoGateOrCoreState`
- `LegacyDefenseExclusionTests.ActiveRuntime_HasSingleWallDestroyedGameOverWriter`
- `ExactRunContinuePlayModeTests.RuntimeDefense_IgnoresInjectedGateCore_AndEndsOnlyWhenWallDies`

PlayMode testi Gate/Core'u runtime entity'ye bilerek geri ekler. Gate/Core `0 HP` iken Wall canlıysa oyun sürer; Gate/Core tam can iken Wall lethal damage alırsa Game Over oluşur. Böylece dormant component'lerin gelecekte yanlışlıkla sonucu etkilemesi regression olarak yakalanır.

EditMode source-owner guard'ı runtime scriptlerinde ikinci bir `IsGameOver = true` yazıcısını
reddeder; tek writer'ın Wall-destroyed guard'ını ve `GameManager` rising-edge observer sınırını kilitler.
