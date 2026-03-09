# MonoBehaviour - Editor Kurulum

## Scene Kurulumu (GameScene)

### GameManager
1. Bos GameObject olustur → "GameManager"
2. GameManager component'ini ekle
3. DontDestroyOnLoad degil, scene'e bagli

### ClickDamageHandler
1. GameManager objesine veya ayri bir GameObject'e ekle
2. Ek ayar gerekmiyor

### Camera
1. Main Camera objesine CameraSetup ekle
2. Otomatik olarak orthographic ayarlari yapar
3. Manuel ayar: Orthographic Size = 6, Position = (0, 0, -10)

## Onemli
- Bu MonoBehaviour'lar ana scene'de bulunur (Sub Scene'de degil!)
- ECS entity'lerine erisim icin Sub Scene'in yuklu olmasi gerekir
- GameManager ilk birkaç frame'de initialization bekler
