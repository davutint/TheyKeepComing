# CastleYardPrepSystem - Editor Setup

## UI Import Beklentisi

Polish HUD export'u asagidaki isimleri icerirse `Mobile Castle Scene Setup` bunlari `MarketUI` uzerine baglar:

- `RepairButton`, `RepairCostText`, `RepairStatusText`
- `FortifyButton`, `FortifyCostText`, `FortifyStatusText`
- `RallyButton`, `RallyCostText`, `RallyStatusText`

Tool bu aksiyonlar icin yeni polish UI uretmez. Prefabda yoksa Fortify/Rally sessizce baglanmadan kalir.

## Runtime

- `Repair`, `Fortify` ve `Rally` sadece `DayPrep` sirasinda aktif olur.
- `Fortify`: sonraki gece savunma hasarini azaltir.
- `Rally`: sonraki gece acilis bolumunde okcu fire-rate bonusu verir.
- Sag archer drawer combat sirasinda kullanilmeye devam eder, fakat sadece archer buy/recruitment icindir.
- Archer upgrade ve tech unlock player-facing olarak sag drawer'da gosterilmez; ileride Tech Tree tarafina tasinacaktir.
