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
  - ZombiePrefab → Zombie_Surungun prefab'ini surukleCastle (Bos GameObject veya Quad)
  - ArrowPrefab → Arrow prefab'ini surukle

### Castle (Bos GameObject veya Quad)
- CastleAuthoring ekle
- Position: (-6, 0, 0)

### Archer_01 (Bos GameObject veya Quad)
- ArcherAuthoring ekle
- Position: (-5.5, 2, 0) — duvar uzerinde

## Prefab'lar (Assets/Prefabs/)
Her prefab'a ilgili Authoring component eklenmeli:
- Zombie_Surungun: ZombieAuthoring
- Arrow: ArrowAuthoring
- Archer: ArcherAuthoring (runtime'da okcu eklemek icin)
