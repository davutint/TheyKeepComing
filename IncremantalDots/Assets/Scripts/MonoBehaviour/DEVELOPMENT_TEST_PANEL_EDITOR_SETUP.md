# Development Test Panel Editor Setup

## Kullanım

1. `Assets/Scenes/NewGameScene.unity` sahnesini aç.
2. Play Mode'a gir.
3. `F10` ile `DEVELOPMENT TESTS` panelini aç ve `UNLOCK TEST COMBAT + FREE BUY` seç.
4. Alt ability barında Fireball'ın açık ve cooldown'un hazır olduğunu doğrula.
5. `SPAWN 2K`, `SPAWN 5K` veya `SPAWN 10K` ile önceki hordenin yerine exact test
   hordesini anında kur.
6. Archer drawer'da Rapid/Frost erişiminin açık, test alımlarının `FREE` olduğunu doğrula.
7. Fireball/Frost/hit feedback ve zombi zemin temasını incele.
8. Bittiğinde Play Mode'u durdur. Test state'ini ana menü/save yoluyla sürdürme.

## Güvenlik sözleşmesi

- Panel yalnız Editor veya Development Build'de derlenir.
- Test aktifken run save reddedilir; production save kirletilmez.
- Test zombileri Wall'a hasar vermez, ancak normal hareket/death/pool ve combat feedback
  hatlarını kullanır.
- `CLEAR HORDE` yalnız aktif test zombilerini pool'a döndürür. Normal run'a tam dönüş için
  Play Mode durdurulur.

## Otomatik test

- `DeadWalls.Tests.DevelopmentTestRulesTests`
- `DeadWalls.Tests.DevelopmentTestPanelPlayModeTests`
- `DeadWalls.Tests.HordeReadabilityTests`
