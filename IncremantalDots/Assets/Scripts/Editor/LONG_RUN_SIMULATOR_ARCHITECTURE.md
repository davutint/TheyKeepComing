# Long Run Simulator - Mimari (M-A olcum harness'i)

## Amac

DAY 1-20 uzun-kosu egrilerinin seklini gormek: kosu suresi, olum gunu, kaynak/uretim
egrileri, harcama dagilimi, canli zombi sayisi, FPS. Cikti M-B roguelite meta tasariminin
dogrudan girdisidir ("ortalama kosu N gun / M dakika"). Amac OPTIMAL oyun degildir —
bot "makul ortalama oyuncu"yu temsil eder; egri kirilmalari politikadan bagimsiz gorunur.

## Yapi

- `LongRunSimulatorWindow` (EditorWindow, `Window > DeadWalls > Long Run Simulator`):
  play moddayken `EditorApplication.update` uzerinden 0.25s gercek-zaman tick'iyle calisir.
- **Bot politikasi (tick basina):**
  1. Council karti varsa A (karsilanamiyorsa B) secilir.
  2. Savunma %60 altindaysa ve karsilanabiliyorsa REPAIR.
  3. Idle pop varken en dusuk doluluk oranli kaynaga worker (en fazla 2/tick).
  4. Alinabilir en ucuz gorunur tech node'u (1/tick).
  5. Okcu tavani (40) altindaysa Frost > Rapid > Basic tercihiyle 1 okcu.
- **Metrik satiri:** her SAFAK gecisinde (gun basina bir) CSV'ye yazilir; GameOver'da
  final satir. Kolonlar: gun, oyun-ici/gercek dakika, 4 kaynak stok + uretim/dk,
  nufus dagilimi, okcu sayisi, toplam tech seviyesi, canli zombi, Wall/Gate/Core %,
  FPS (frame-fark yontemi), kumulatif repair/tech/okcu islem sayilari ve maliyetleri.
- CSV: `Logs/LongRun/longrun_<timestamp>.csv` — her gun satirinda flush (kilitlenmeye dayanikli).
- Hedef gun (default 20) dolunca veya GameOver'da durur; "GameOver'da yeni kosu" acikca
  coklu-kosu ortalamasi toplanabilir (restart sonrasi timeScale=1 tuzagina karsi geri kurulur).

## Bilinen sinirlar / notlar

- Time.timeScale 1-5x: yuksek carpan dt'yi buyutur (fizik kabalasmasi); onerilen 3x.
- `Application.runInBackground = true` Start'ta garanti edilir (editor arka planda
  player loop durmasi — bilinen tuzak).
- FPS olcumu `Time.frameCount` farki / gercek-zaman farki iledir (EditorApplication.update
  dt'si oyun dt'si DEGILDIR).
- Okcu maliyeti katalog fiyati okunmadan adet olarak raporlanir (V1 sadeligi).
- Bot ECS'e yazmaz; yalniz GameManager public API'lerini cagirir (gercek oyuncu yollari).
