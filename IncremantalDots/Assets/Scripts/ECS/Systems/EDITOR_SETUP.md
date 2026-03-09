# ECS Systems - Editor Kurulum

## Gereksinimler
- Tum Authoring component'lari Sub Scene icinde bake edilmis olmali
- GameStateAuthoring ve WaveConfigAuthoring Sub Scene icinde bulunmali
- Prefab referanslari (Zombie, Arrow) WaveConfigAuthoring'de atanmis olmali

## Debug
- Unity Editor → Window → Entities → Entity Debugger ile entity'leri goruntuleyebilirsiniz
- SystemAPI.GetSingleton hatalari genelde singleton entity'nin Sub Scene'de olusturulmamasindan kaynaklanir
- "RequireForUpdate" ile sistem sadece gerekli component varken calisir

## Play Mode Test Siralamasi
1. Sub Scene icinde GameState + Castle authoring oldugundan emin ol
2. Prefab'lar WaveConfigAuthoring'e atanmis olmali
3. Play'e bas — zombiler spawn olmali ve sola yurumelidir
