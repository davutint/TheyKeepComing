# Development Test Panel Editor Setup

## Kullanım

1. `Assets/Scenes/NewGameScene.unity` sahnesini aç.
2. Play Mode'a gir.
3. Castle Heart ağacını test etmek için `F10` ile `DEVELOPMENT TESTS` panelini aç ve
   `GRANT 1M HEART ESSENCE` seç. Bu aksiyon node açmaz veya satın almaz.
4. Castle Heart'i aç; dört başlangıç teknolojisinden ilerleyerek node'ları normal butonla tek tek
   al ve her direct-child reveal/connector/animasyon geçişini incele.
5. Combat testi gerekiyorsa ayrıca `UNLOCK TEST COMBAT + FREE BUY` seç.
6. Alt ability barında Fireball'ın açık ve cooldown'un hazır olduğunu doğrula.
7. `SPAWN 2K`, `SPAWN 5K` veya `SPAWN 10K` ile önceki hordenin yerine exact test
   hordesini anında kur.
8. Archer drawer'da Rapid/Frost erişiminin açık, test alımlarının `FREE` olduğunu doğrula.
9. Fireball/Frost/hit feedback ve zombi zemin temasını incele.
10. Bittiğinde Play Mode'u durdur. Test state'ini ana menü/save yoluyla sürdürme.

## Güvenlik sözleşmesi

- Panel yalnız Editor veya Development Build'de derlenir.
- Test aktifken run save reddedilir; production save kirletilmez.
- Heart grant yalnız bakiyeyi artırır; reveal/purchase/graph state'ine doğrudan yazmaz.
- Test zombileri Wall'a hasar vermez, ancak normal hareket/death/pool ve combat feedback
  hatlarını kullanır.
- `CLEAR HORDE` yalnız aktif test zombilerini pool'a döndürür. Normal run'a tam dönüş için
  Play Mode durdurulur.

## Otomatik test

- `DeadWalls.Tests.DevelopmentTestRulesTests`
- `DeadWalls.Tests.DevelopmentTestPanelPlayModeTests`
- `DeadWalls.Tests.HordeReadabilityTests`
