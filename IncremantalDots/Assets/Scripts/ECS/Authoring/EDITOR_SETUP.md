# Authoring - Editor Kurulum

## Sub Scene Kurulumu
1. GameScene.unity icinde bos bir GameObject olustur → "GameSubScene" adini ver
2. Inspector'da Add Component → Sub Scene
3. Sub Scene icine gir (cift tikla)

## Sub Scene Icerigi
Sub Scene icinde su GameObject'leri olustur:

### GameState (Bos GameObject)
- GameStateAuthoring ekle (Inspector'da degerleri ayarla)
- WaveConfigAuthoring ekle
  - EnemyCatalog → `Assets/ScriptableObject/MobileCastle/Enemies/EnemyCatalog.asset`
  - ZombiePrefab → yalniz catalog'u olmayan legacy sahneler icin fallback
  - ArrowPrefab → Arrow prefab'ini surukle

### Castle (Bos GameObject veya Quad)
- CastleAuthoring ekle
- Position: (-6, 0, 0)

### Archer_01 (Bos GameObject veya Quad)
- ArcherAuthoring ekle
- Position: (-5.5, 2, 0) — duvar uzerinde

## NewGameScene Mobile Castle SubScene Notu

`Window -> DeadWalls -> Mobile Castle Scene Setup` tool'u NewGameScene icin asagidaki authoring objelerini idempotent olarak kurar:

- `GameState`: GameStateAuthoring + WaveConfigAuthoring
- `CastleCore`: CastleAuthoring
- `MobileCastleConfig`: MobileCastleCombatAuthoring
- `BasicArcher_01`: ArcherAuthoring (`Type = Basic`, fire rate `1.5`, damage `10`, range `15`)

Tool ayrica `BasicZombie.asset` ve tek kayitli `EnemyCatalog.asset` dosyalarini olusturur/onarir; ayni catalog'u `WaveConfigAuthoring` ve `MobileCastleCombatAuthoring` alanlarina baglar.

MobileCastleCombatAuthoring varsa sistemler merkezi kale mode'da calisir; yoksa eski GameScene WallX akisi korunur. NewGameScene okcu yerlesimi main scene `Grid/outside` tilemapindeki dolu hucrelerden gelir.

`MobileCastleCombatAuthoring` ayrica day/night, reward, economy focus, wave director ve Castle Yard prep tuning degerlerini tasir. NewGameScene default Castle Yard degerleri: Fortify damage multiplier `0.70`, Rally duration `10`, Rally fire-rate multiplier `1.25`.

NewGameScene icin setup tool `GameStateAuthoring` kaynaklarini mobile economy'ye gore ayarlar:

- Initial Wood/Stone/Iron/Food: `150 / 80 / 45 / 150`
- Passive income Wood/Stone/Iron/Food: `90 / 50 / 30 / 75` per minute
- Initial Arrows: `200`

Okcu satin alma/upgrade davranisi SubScene authoring'de degil, ana scene `GameManager` + `MarketUI` drawer akisi tarafindadir.

## Prefab'lar (Assets/Prefabs/)
Her prefab'a ilgili Authoring component eklenmeli:
- Zombie_Surungun: ZombieAuthoring
- Arrow: ArrowAuthoring + SpriteSheetAuthoring; tint beyaz kalir, projectile tint runtime'da yazilir
- Archer: ArcherAuthoring + SpriteSheetAuthoring; tint beyaz kalir, Basic/Rapid/Frost tint runtime'da yazilir
