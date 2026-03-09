# Prefab Olusturma - Editor Kurulum

## Zombie_Surungun Prefab
1. Hierarchy'de bos GameObject olustur → "Zombie_Surungun"
2. Add Component → Mesh Filter → Quad mesh sec
3. Add Component → Mesh Renderer → Material: URP Sprite-Unlit (yesil veya kirmizi renk)
4. Scale: (0.8, 0.8, 1)
5. Add Component → ZombieAuthoring (varsayilan degerler yeterli)
6. Assets/Prefabs/ klasorune surukle → Prefab olustur
7. Hierarchy'den sil

## Arrow Prefab
1. Hierarchy'de bos GameObject olustur → "Arrow"
2. Add Component → Mesh Filter → Quad mesh sec
3. Add Component → Mesh Renderer → Material: URP Sprite-Unlit (sari renk)
4. Scale: (0.3, 0.1, 1) — ince uzun ok seklinde
5. Add Component → ArrowAuthoring
6. Assets/Prefabs/ klasorune surukle
7. Hierarchy'den sil

## Archer Prefab
1. Hierarchy'de bos GameObject olustur → "Archer"
2. Add Component → Mesh Filter → Quad mesh sec
3. Add Component → Mesh Renderer → Material: URP Sprite-Unlit (mavi renk)
4. Scale: (0.6, 0.8, 1)
5. Add Component → ArcherAuthoring
6. Assets/Prefabs/ klasorune surukle
7. Hierarchy'den sil

## Onemli Not
- Prefab'larin Sub Scene icinde KULLANILMADAN once olusturulmasi gerekir
- WaveConfigAuthoring'e prefab referanslarini surukleyerek atama yapin
- Material olarak URP/Lit veya herhangi bir URP material kullanabilirsiniz
- Entities Graphics icin MeshFilter + MeshRenderer gerekli
