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
- Population growth: aktif `DefaultDifficulty.asset` kaynakli `+15` each completed Dawn/cycle;
  profile yoksa authoring fallback kullanilir
- Food per accepted arrival: aktif profile kaynakli `1`, ayni Dawn'da tek seferlik
- Initial House beds: authoring-owned `60`; satin alinmis yataklar run state'inde ayridir
- Worker production: aktif `DefaultDifficulty.asset` kaynakli Wood `8/min`, Stone `5.5/min`,
  Iron `4.9/min`, Food `7/min`; profile yoksa ayni authoring fallback'leri kullanilir
- Kill/wave reward multiplier while worker economy is active: `0.25`
- Economy event chance: `0.15`
- Event cooldown: `2` waves

## Kontrol Listesi

- `GameStateAuthoring.InitialPopulation` `60` olmali.
- `GameStateAuthoring.TestWorkers` `53`, `TestArchers` `4` olmali.
- `GameStateAuthoring.InitialCapacity` ve `MobileCastleCombatAuthoring.InitialBedCapacity` `60` olmali.
- `DefaultDifficulty.asset` request/Food `15 / 1`, bed curve `100W / 25 interval` olmali.
- `Window > DeadWalls > Difficulty Tuner > Population Runtime Contract` preview/live telemetry
  ayni Dawn butcesi ve +1/+10 bed fiyatini gostermeli.
- `MobileCastleCombatAuthoring.ArcherSlots` bos kalabilir; NewGameScene okcu yerlesimi main scene `Grid/outside` tilemapinden gelir.

Unity script compile islemi editor refresh ile yapilir; disaridan manuel compile komutu calistirma.
