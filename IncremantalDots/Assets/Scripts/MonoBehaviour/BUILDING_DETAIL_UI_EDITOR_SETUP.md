# BuildingDetailUI — Editor Kurulum Rehberi

## Canvas Kurulumu

### 1. BuildingDetailUI GameObject Olustur
1. Sahnedeki Canvas'a sag tikla → Create Empty → `BuildingDetailUI` olarak adlandir
2. `BuildingDetailUI.cs` scriptini ekle

### 2. DetailPanel Olustur
Canvas altinda:
1. Create Empty → `DetailPanel` (veya Panel) — arka plan + layout
2. RectTransform: sag kenarda veya ortada konumlandir (ornek: 300x400 px)
3. Baslangicta **SetActive = false** (kod acip kapatir)

### 3. UI Elemanlari
DetailPanel altina sirasyla:

| GameObject | Tip | Inspector Notu |
|-----------|-----|---------------|
| `BuildingNameText` | TMP_Text | Font: Bold, Size: 24 |
| `ProductionText` | TMP_Text | Size: 18 |
| `WorkerSection` | Empty (parent) | Isci bolumu container |
| → `WorkersText` | TMP_Text | Size: 18 |
| → `IdleText` | TMP_Text | Size: 16, color: gray |
| → `RemoveWorkerButton` | Button + TMP_Text " - " | |
| → `AddWorkerButton` | Button + TMP_Text " + " | |
| `CapacitySection` | Empty (parent) | Ev kapasite container |
| → `CapacityText` | TMP_Text | Size: 18 |
| `DemolishButton` | Button + TMP_Text "Yik" | Color: kirmizi |
| `CloseButton` | Button + TMP_Text "Kapat" | |

### 4. Referanslari Bagla
BuildingDetailUI Inspector'unda:
- `DetailPanel` → DetailPanel objesi
- `BuildingNameText` → ilgili TMP_Text
- `ProductionText` → ilgili TMP_Text
- `WorkerSection` → isci bolumu parent
- `WorkersText`, `IdleText` → ilgili text'ler
- `AddWorkerButton`, `RemoveWorkerButton` → butonlar
- `CapacitySection` → kapasite bolumu parent
- `CapacityText` → ilgili text
- `DemolishButton`, `CloseButton` → butonlar

> **NOT:** Buton onClick listener'lari KOD tarafindan eklenir (Start'ta). Inspector'da manuel eklemeye GEREK YOK.

## Test Adimlari

1. **Binaya tikla:** Oduncu yerlestir → binanin herhangi hucresine tikla → detay paneli acilmali
2. **Isci ekle:** + butonu → Workers 0→1, uretim hizi guncellenmeli, HUD'da Workers guncellenmeli
3. **Isci cikar:** - butonu → Workers 1→0, uretim durmali
4. **Idle kontrol:** Idle 0 iken + butonu devre disi olmali
5. **Max kontrol:** MaxWorkers'a ulasinca + butonu devre disi
6. **Bina yik:** Yik butonu → grid serbest, entity silinmeli, %50 kaynak iade
7. **Ev detayi:** Ev'e tikla → isci bolumu gizli, kapasite gorunmeli
8. **Restart:** Restart sonrasi detay paneli kapanmali
9. **Placement sonrasi:** Bina yerlestir → detay paneli acilmamali (placement modundan cikilinca ayri tikla)
10. **UI tiklamasi:** HUD butonlarina tiklamak detay paneli acmamali (EventSystem kontrolu)
