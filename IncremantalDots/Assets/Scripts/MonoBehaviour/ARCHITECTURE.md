# MonoBehaviour Hybrid Layer - Mimari

## Genel Yapi
MonoBehaviour'lar ECS ile Unity UI arasinda kopru gorevi gorur.
World.DefaultGameObjectInjectionWorld.EntityManager uzerinden ECS verilerine erisir.

## Dosyalar

### GameManager.cs
- Singleton pattern
- Her frame ECS singleton'larini okur (GameStateData, WaveStateData, CastleHP vs.)
- Event'ler: OnGameOver, OnLevelUp, OnWaveChanged, OnGameStateChanged
- Upgrade uygulamak icin ApplyUpgrade(UpgradeType)
- RestartGame() ile oyunu sifirlar

### ClickDamageHandler.cs
- Mouse click'i alir
- Dunya koordinatina cevirir
- En yakin zombi entity'sini bulur (2 birim mesafe icinde)
- Dogrudan ZombieStats.CurrentHP'yi dusurur

### CameraSetup.cs
- Orthographic kamera ayari
- Size: 6, Position: (0, 0, -10)

## Veri Akisi
```
ECS Systems → Entity Data → GameManager.ReadECSData() → Events → UI Controllers
UI Input → GameManager.ApplyUpgrade() → EntityManager.SetComponentData → ECS
Mouse Click → ClickDamageHandler → EntityManager.SetComponentData → ECS
```
