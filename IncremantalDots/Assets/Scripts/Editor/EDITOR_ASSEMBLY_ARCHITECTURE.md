# Editor Assembly Boundary

## Amaç

`Assets/Scripts/DeadWalls.asmdef` üst klasörde bulunduğu için altındaki `Editor` klasörü tek başına Player derlemesinden dışlanmaz. `DeadWalls.Editor.asmdef`, bütün proje Editor araçlarını `DeadWalls.Editor` assembly'sine alır ve yalnız `Editor` platformunda derler.

## Sınır

- Runtime gameplay kodu `DeadWalls` assembly'sinde kalır.
- `EditorWindow`, `MenuItem`, `SerializedObject`, profiler analyzer ve scene/setup araçları `DeadWalls.Editor` assembly'sindedir.
- Editor assembly runtime `DeadWalls` tiplerini okuyabilir; runtime assembly Editor assembly'ye referans vermez.
- Player build içinde `UnityEditor` tipi veya Editor aracı bulunmaz.

Yeni Editor aracı `Assets/Scripts/Editor` altında tutulmalıdır. Runtime tarafından kullanılacak kod bu klasöre konmamalıdır.

## Menü Hiyerarşisi

- Bütün Dead Walls Editor araçları `Tools/Dead Walls` kökünün altında bulunur.
- Alt kategoriler `Audio`, `Balancing`, `Content`, `Maps`, `Profiling` ve `Setup & Repair` olarak sabittir.
- Yeni bir `[MenuItem]` yolu elle yazılmaz; `DeadWallsEditorMenuPaths` sabitlerinden oluşturulur.
- `Window/DeadWalls`, `Tools/DeadWalls` veya genel `Tools` altında ikinci bir proje köküne izin verilmez.

## Doğrulama

- Unity Editor script compilation: sıfır error.
- StandaloneWindows64 Player build veya Player-targeted PlayMode test derlemesi: `UnityEditor` type resolution hatası üretmemeli.
