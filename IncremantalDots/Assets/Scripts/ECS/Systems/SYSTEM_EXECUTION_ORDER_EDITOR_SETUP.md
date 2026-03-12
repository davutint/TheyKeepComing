# System Calisma Sirasi - Editor Kurulum

## Gereksinimler
- Tum Authoring component'lari Sub Scene icinde bake edilmis olmali
- GameStateAuthoring ve WaveConfigAuthoring Sub Scene icinde bulunmali
- Prefab referanslari (Zombie, Arrow) WaveConfigAuthoring'de atanmis olmali
- ZombieAuthoring prefab'inda CollisionRadius ve PhysicsDamping alanlari gorunmeli

## Fizik Sistemi Kontrol Listesi
1. ZombieAuthoring prefab'inda yeni alanlar:
   - CollisionRadius = 0.15
   - PhysicsDamping = 3.0
2. Spatial hash otomatik olusur — ek setup gerekmez

## Debug
- Unity Editor → Window → Entities → Entity Debugger ile entity'leri goruntuleyebilirsiniz
- SystemAPI.GetSingleton hatalari genelde singleton entity'nin Sub Scene'de olusturulmamasindan kaynaklanir
- "RequireForUpdate" ile sistem sadece gerekli component varken calisir
- PhysicsBody.Velocity sifirsa: ApplyMovementForceSystem kuvvet uygulamiyor olabilir
- Zombiler birbirine yapisiyor: CollisionRadius veya Damping degerlerini ayarla

## Play Mode Test Siralamasi
1. Sub Scene icinde GameState + Castle authoring oldugundan emin ol
2. Prefab'lar WaveConfigAuthoring'e atanmis olmali
3. ZombieAuthoring prefab'inda CollisionRadius ve PhysicsDamping gorunmeli
4. GameStateAuthoring'de kaynak baslangic degerleri ve test uretim hizlari ayarli olmali
5. HUDController'da WoodText, StoneText, IronText, FoodText atamalari yapilmis olmali
6. Play'e bas — zombiler fiziksel olarak birbirini itmeli
7. Duvar onunde organik yigilma olusmali
8. Zombi olunce bosluk dogal dolmali
9. Kaynak miktarlari HUD'da gorunmeli ve uretim hizina gore artmali
