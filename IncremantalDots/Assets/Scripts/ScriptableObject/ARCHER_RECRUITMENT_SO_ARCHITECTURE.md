# Archer Recruitment SO Architecture

Mobile `Archer Recruitment` drawer artik okcu satirlarini hardcoded Basic/Rapid/Frost alanlarindan degil, `ArcherDefinitionSO` asset'lerinden besleyecek sekilde hazirlanir.

- `ArcherDefinitionSO`: tek bir okcu tipinin sabit datasidir. Id, display name, `ArcherType`, buy/retrain base cost, type-count growth tuning'i, population cost, base combat statlari, required tech id ve tint tutar.
- `ArcherRecruitmentCatalogSO`: UI'da listelenecek definition asset'lerini siralar.
- Runtime state bu asset'lerde tutulmaz. Count, unlock, afford, DPS, satin alma ve Basic retrain sonucu `GameManager` tarafindan hesaplanir.
- Basic/Rapid/Frost toplam kapasitesi definition asset'lerinin alani degildir;
  `ArcherCapacityUtility.MaxTotalArchers = 1000` ortak V1 guardrail'idir.
- Sag drawer recruitment ve Basic -> Rapid/Frost retrain yuzeyidir. Ayrı archer level/upgrade ve tech unlock kararlari burada bulunmaz; Castle Heart tek progression owner'idir.

Buy ve retrain maliyeti hedef türün transaction öncesindeki mevcut sayısına göre
`ArcherRecruitmentCostUtility` ile büyür. Retrain entity'yi yerinde dönüştürür; toplam
garnizon ve population değişmez. Ayrıntı için `ARCHER_RETRAIN_ARCHITECTURE.md`.

V1 katalog Basic/Rapid/Frost definition'larini icerir. Rapid/Frost definition'lari katalogda gorunebilir fakat tech unlock sistemi gelene kadar `GameManager.IsArcherTypeUnlocked` tarafindan kilitli kalir.
