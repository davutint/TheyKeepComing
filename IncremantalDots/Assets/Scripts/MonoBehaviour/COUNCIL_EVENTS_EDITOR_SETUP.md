# Council Events - Editor Setup

## Otomatik Kurulum

`Window > DeadWalls > Mobile Castle Scene Setup > Setup NewGameScene`:

1. `Assets/ScriptableObject/MobileCastle/Council/` altina 11 atom (`Atom_*.asset`) +
   9 sablon (`Template_*.asset`) + `CouncilEventCatalog.asset` seed edilir — MERGE-ONLY:
   mevcut asset degerlerine ve kullanicinin ekledigi atom/sablonlara dokunulmaz.
2. Katalog `GameManager.councilCatalog` alanina baglanir.
3. `CouncilEventUI` HUD root'a eklenir; kart objeleri isimle bulunur, SFX baglanir
   (Appear = `Book Handle 1-2.wav`, Choose = `Card Place 1-1.wav`).
4. `ValidateCatalog()` sorunlari Console'a warning basilir.

## Yeni Icerik Ekleme (kod GEREKMEZ)

- **Yeni atom:** Create > DeadWalls > Mobile Castle > Council Effect Atom. Id benzersiz;
  buyuklugu MinutesOfProduction (kaynak) veya Rate/PerDay (adet/oran) ile ver; BudgetMinutes
  dakika-degeri; director carpanlarini ayarla. Kataloga ekle. TEK atom onlarca varyant dogurur.
- **Yeni sablon:** Create > ... > Council Template. Karsitlik tipini sec; OptionA/BAtomIds ile
  atom kisitla (bos = tur-uyumlu havuz); zincir icin RequiredFlags + ChainDelayDays (+OneShot).
  Kataloga ekle.
- Pacing katalog asset'inde: DailyEventChance / PityDays / CooldownDays / RecentTemplateMemory.

## Test Adimlari

1. Play'e gir; DAWN'a kadar bekle (veya birkac gun) — sans %30 + 4 gun pity.
2. Kart sol-alt bolgede belirir (odul toast'undan ~1.2s sonra); sure seridi DAY sonuna
   kadar akar; DUSK girisinde secilmediyse kaybolur.
3. Secim kaynak/pop/okcu/savunma etkisini aninda uygular; odeme karsilanamiyorsa buton pasif.
4. `refugees_at_gate`'te "Take them in" sec -> 2+ gun sonra `AMONG THE REFUGEES` zinciri
   cikabilir (OneShot).
5. EditMode testleri: Test Runner > EditMode > `DeadWalls.EditMode.Tests` (6 test; composer
   determinizm/butce/zincir/olcekleme).

## Dikkat

- Kart objeleri PREFABDADIR (`CouncilEventPanel`) — sahne-override degil; UI export sync
  borcu listesine dahildir (UIImporter calistirilmamali).
- Test/debug icin `CouncilEventUI.enabled` veya `freeEconomyTestMode` degistirilirse
  EDIT MODDA yapilan degisiklik sahneye KALICI yazilir — geri almayi unutma (bir kez yasandi).
