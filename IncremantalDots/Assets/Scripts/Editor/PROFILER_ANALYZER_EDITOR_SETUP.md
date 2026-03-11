# Profiler Data Analyzer — Editor Setup

## Nasil Acilir
Unity Editor'da: **Tools > Analyze Profiler Data**

## Profiler Verisi Olusturma
1. Unity Profiler'i ac (Window > Analysis > Profiler)
2. Oyunu calistir ve performans verisi topla
3. Profiler'da **File > Save** ile `.raw` dosyasi olarak kaydet

## Kullanim

### Tek Snapshot Analizi
1. "Load A" butonuna tikla
2. `.raw` dosyasini sec
3. Tab'lar arasinda gezinerek verileri incele

### A/B Karsilastirma
1. "Load A" ile fix oncesi (before) `.raw` dosyasini yukle
2. "Load B" ile fix sonrasi (after) `.raw` dosyasini yukle
3. **Compare** tabina gec → delta analizi gorunur
4. Ust toolbar'daki A/B/A|B toggle ile diger tab'larda tek veya cift snapshot gorebilirsin

### ECS Pipeline Analizi (Tab 9)
- Tum DeadWalls ECS sistemleri SystemGroup bazli gruplanmis olarak listelenir
- Toplam, ortalama, max sure, self time, GC allocation ve sync point bilgisi gosterilir
- **Job Avg / Job Max** kolonlari: Worker thread'deki gercek Burst job surelerini gosterir
- Job suresi olmayan sistemler (main thread'de calisan) "-" olarak gosterilir
- Sync point olan sistemler turuncu "BLOCK" etiketi ile isaretlenir
- A/B Both modunda her iki snapshot icin ayri ayri gorunur

### Physics Breakdown (Tab 10)
- 5 physics pipeline sistemi (ApplyMovementForce → BuildSpatialHash → PhysicsCollision → Integrate → Boundary)
- Stacked bar chart ile oransal dagalim
- Detay tablosu: Ort, Max, **Job Avg**, **Job Max**, Toplam, %, Sync
- Top 10 en yavas pipeline frame'leri: Main thread + job timing detaylari
- Sync point uyarisi (ornegin BuildSpatialHashSystem .Complete() blogu)

### Export
- "Export TXT" butonu ile text raporu olustur
- Rapor tum tab verilerini, ECS ve Physics section'larini icerir
- Cikti: `C:\Users\PC\Desktop\PERFORMANS ANALIZI\profiler_report.txt`

## Tab Ozeti
| Tab | Ne Gosterir |
|-----|-------------|
| Overview | CPU/GPU/FPS ozet, FPS dagilimi |
| Spikes | En yavas 20 frame |
| Functions | Top 30 fonksiyon (self time) |
| Update | Update/LateUpdate/FixedUpdate breakdown |
| User Code | Assembly-CSharp.dll fonksiyonlari |
| Rendering | Draw call, batch, triangle stats |
| GC Analysis | GC allocator ve frame analizi |
| Call Chains | User Code → GC Producer zincirleri |
| Compare | A vs B delta karsilastirma |
| ECS Pipeline | Tum ECS sistem metrikleri |
| Physics | Physics pipeline stacked bar + detay |

## Yeni ECS Sistemi Ekleme
Yeni bir ECS sistemi eklendiginde `KNOWN_ECS_SYSTEMS` dictionary'sine eklenmesi gerekir:
```csharp
{ "YeniSistemAdi", "SimulationSystemGroup" },
```
Physics pipeline'a ait ise ayrica `PHYSICS_PIPELINE_SYSTEMS` dizisine de eklenmeli.

## Yeni Job Ekleme
Yeni bir IJobEntity tanimlayan sistem eklediginde `KNOWN_ECS_JOBS` dictionary'sine eklenmesi gerekir:
```csharp
{ "YeniJobStructAdi", "ParentSistemAdi" },
```
Bu sayede worker thread taramasi o job'un surelerini yakalar ve parent sisteme baglar.
