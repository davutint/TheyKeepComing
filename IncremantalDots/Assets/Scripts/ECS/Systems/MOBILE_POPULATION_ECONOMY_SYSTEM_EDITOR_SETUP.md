# Mobile Population Economy System Editor Setup

## Gerekli Authoring

`MobilePopulationEconomySystem` icin `MobileCastleConfig` objesinde `MobileCastleCombatAuthoring` bulunmalidir. Baker su component'leri ayni mobile config entity'sine ekler:

- `MobileCastleCombatConfig`
- `MobilePopulationAllocation`
- `MobilePrepPauseState`
- `MobileEconomyEventState`

## Default Degerler

`Mobile Castle Scene Setup` tool'u mobile ekonomi icin su V1 defaultlarini yazar:

- Initial population: `60`
- Initial workers: Wood `20`, Stone `10`, Iron `8`, Food `15`
- Initial archers: `4`
- Worker caps: Wood `40`, Stone `30`, Iron `24`, Food `40`
- Population growth: `+15` each completed continuous siege cycle; legacy flow'da completed wave DayPrep
- Worker production: Wood `8/min`, Stone `5.5/min`, Iron `3.8/min`, Food `7/min`
- Kill/wave reward multiplier while worker economy is active: `0.25`
- Economy event chance: `0.15`
- Event cooldown: `2` waves

## Kontrol Listesi

- `GameStateAuthoring.InitialPopulation` `60` olmali.
- `GameStateAuthoring.TestWorkers` `53`, `TestArchers` `4` olmali.
- `GameStateAuthoring.InitialCapacity` mobile loop'ta internal high value kalabilir.
- `MobileCastleCombatAuthoring.ArcherSlots` bos kalabilir; NewGameScene okcu yerlesimi main scene `Grid/outside` tilemapinden gelir.

Unity script compile islemi editor refresh ile yapilir; disaridan manuel compile komutu calistirma.
