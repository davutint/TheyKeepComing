# ECS Components - Editor Kurulum

## Gereksinimler
- Unity 6 LTS
- com.unity.entities paketi kurulu olmali
- DeadWalls.asmdef referanslari dogru olmali

## Notlar
- Component'lar dogrudan kullanilmaz, Authoring + Baker uzerinden entity'lere eklenir.
- Singleton component'lar (GameStateData, WaveStateData) icin GameStateAuthoring kullanilir.
- Component degerlerini degistirmek icin ilgili Authoring component'inin Inspector degerlerini duzenleyin.
