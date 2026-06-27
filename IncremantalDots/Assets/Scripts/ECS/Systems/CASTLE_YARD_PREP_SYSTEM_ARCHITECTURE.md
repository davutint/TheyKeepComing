# CastleYardPrepSystem - Mimari

## Amac

`CastleYardPrepSystem`, mobile castle HUD'daki polish `Castle Yard` aksiyonlarindan gelen tek-gecelik buff state'ini yonetir. UI prefab/export davranis icermez; `MarketUI` butonlari `GameManager` API'lerine baglar.

## Akis

1. Oyuncu `DayPrep` sirasinda `Fortify` veya `Rally` kullanir.
2. `GameManager`, `CastleYardPrepState` singleton'ina aktif bonusu yazar.
3. `FortifyActive`, `DamageApplySystem` tarafindan okunur ve savunma hasarini azaltir.
4. `RallyTimer`, sadece `NightCombat` sirasinda azalir.
5. `ArcherShootSystem`, `RallyTimer > 0` iken fire-rate multiplier uygular.
6. Wave temizlenince `WaveSpawnSystem` tek-gecelik bonuslari sifirlar.

Stress mode bu akisi kullanmaz.
