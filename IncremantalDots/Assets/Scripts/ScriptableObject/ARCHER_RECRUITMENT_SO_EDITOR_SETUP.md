# Archer Recruitment SO Editor Setup

`Window > DeadWalls > Mobile Castle Scene Setup` calistirildiginda default mobile archer definition asset'leri ve catalog asset'i olusturulur veya mevcutsa yeniden kullanilir.

Beklenen asset yolu:

- `Assets/ScriptableObject/MobileCastle/Archers/BasicArcher.asset`
- `Assets/ScriptableObject/MobileCastle/Archers/RapidArcher.asset`
- `Assets/ScriptableObject/MobileCastle/Archers/FrostArcher.asset`
- `Assets/ScriptableObject/MobileCastle/Archers/ArcherRecruitmentCatalog.asset`

Setup tool bu catalog'u `GameManager.ArcherCatalog` ve `MarketUI.ArcherCatalog` alanlarina baglar. Default Basic/Rapid/Frost definition'lari catalog'da eksikse ekler ama catalog'daki ekstra definition asset'lerini silmez; yeni okcu ekleme akisi catalog'a yeni asset ekleyerek ilerlemelidir. UI tarafinda `ArcherRecruitmentListRoot` ve inactive `ArcherRecruitmentRowTemplate` varsa `MarketUI` satirlari runtime'da template'ten uretir. Template yoksa eski Basic/Rapid/Frost row binding'leri fallback olarak calismaya devam eder.
