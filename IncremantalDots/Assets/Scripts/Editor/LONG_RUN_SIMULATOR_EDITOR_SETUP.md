# Long Run Simulator - Editor Setup

## Kosum Adimlari

1. `NewGameScene`'i ac, Play'e gir.
2. `Window > DeadWalls > Long Run Simulator` penceresini ac.
3. Time Scale (onerilen 3x) ve Hedef Gun (default 20; 0 = olene kadar) ayarla.
4. Coklu-kosu ortalamasi icin "GameOver'da yeni kosu" isaretle.
5. **Start** — durum satirinda gun/faz/kaynak/FPS canli akar.
6. Bitince (hedef gun / GameOver / Stop) CSV yolu pencerede gorunur:
   `Logs/LongRun/longrun_<timestamp>.csv`.

## Notlar

- Editor arka plandayken de kosar (Start, runInBackground'i acar).
- Bot yalniz GameManager public API'lerini kullanir; ECS state'ine elle yazmaz.
- Kosu sirasinda elle oynamak veriyi kirletir (bot + insan ayni anda harcama yapar) —
  olcum kosusunda dokunma.
- CSV her gun satirinda diske yazilir; kilitlenme olsa bile o ana kadarki veri durur.
