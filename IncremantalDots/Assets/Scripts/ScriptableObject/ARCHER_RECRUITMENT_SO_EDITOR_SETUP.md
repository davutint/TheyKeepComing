# Archer Recruitment SO Editor Setup

`Window > DeadWalls > Mobile Castle Scene Setup` calistirildiginda default mobile archer definition asset'leri ve catalog asset'i olusturulur veya mevcutsa yeniden kullanilir.

Beklenen asset yolu:

- `Assets/ScriptableObject/MobileCastle/Archers/BasicArcher.asset`
- `Assets/ScriptableObject/MobileCastle/Archers/RapidArcher.asset`
- `Assets/ScriptableObject/MobileCastle/Archers/FrostArcher.asset`
- `Assets/ScriptableObject/MobileCastle/Archers/ArcherRecruitmentCatalog.asset`

Setup tool bu catalog'u `GameManager.ArcherCatalog` ve `MarketUI.ArcherCatalog` alanlarina baglar. Default Basic/Rapid/Frost definition'lari catalog'da eksikse ekler ama catalog'daki ekstra definition asset'lerini silmez; yeni okcu ekleme akisi catalog'a yeni asset ekleyerek ilerlemelidir. UI tarafinda `ArcherRecruitmentListRoot` ve inactive `ArcherRecruitmentRowTemplate` varsa `MarketUI` satirlari runtime'da template'ten uretir. Template `ArcherRetrainButton` tasir; Basic satirinda gizlenir, acilmis Rapid/Frost satirlarinda Basic'i yerinde donusturur. Template yoksa eski Basic/Rapid/Frost row binding'leri buy ve retrain fallback'i olarak calisir.

Ortak `1000` okcu cap'i catalog veya definition Inspector alani degildir. Cap dolunca
hem dynamic hem fallback row'lar `ARMY CAP 1000/1000` ve `MAX` gosterir.
Cap yeni entity buy'ını engeller; toplamı değiştirmeyen retrain'i engellemez. Buy/retrain
base maliyeti ile growth interval/exponent her definition asset'inde ayarlanır.

Balance icin `Window > DeadWalls > Difficulty Tuner > Archer Runtime Contract` kullanilir.
Panel aktif GameManager catalog'unu read-only owner olarak gosterir; her definition'in combat,
buy/retrain ve growth alanini asset uzerinde duzenler. `Preview target-type count` gameplay ile
ayni quote'u hesaplar. Play Mode `APPLY`, mevcut okculari state kaybi olmadan yeni combat
baseline'ina rebase eder; cost degisiklikleri sonraki buy/retrain transaction'inda dogrudan okunur.
