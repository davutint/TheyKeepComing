# Archer Recruitment SO Architecture

Mobile `Archer Recruitment` drawer artik okcu satirlarini hardcoded Basic/Rapid/Frost alanlarindan degil, `ArcherDefinitionSO` asset'lerinden besleyecek sekilde hazirlanir.

- `ArcherDefinitionSO`: tek bir okcu tipinin sabit datasidir. Id, display name, `ArcherType`, buy cost, population cost, base combat statlari, required tech id ve tint tutar.
- `ArcherRecruitmentCatalogSO`: UI'da listelenecek definition asset'lerini siralar.
- Runtime state bu asset'lerde tutulmaz. Count, unlock, afford, DPS, level ve satin alma sonucu `GameManager` tarafindan hesaplanir.
- Basic/Rapid/Frost toplam kapasitesi definition asset'lerinin alani degildir;
  `ArcherCapacityUtility.MaxTotalArchers = 1000` ortak V1 guardrail'idir.
- Sag drawer sadece recruitment yuzeyidir. Upgrade ve tech unlock kararlarinin ileride full-screen Tech Tree tarafina tasinmasi beklenir.

V1 katalog Basic/Rapid/Frost definition'larini icerir. Rapid/Frost definition'lari katalogda gorunebilir fakat tech unlock sistemi gelene kadar `GameManager.IsArcherTypeUnlocked` tarafindan kilitli kalir.
