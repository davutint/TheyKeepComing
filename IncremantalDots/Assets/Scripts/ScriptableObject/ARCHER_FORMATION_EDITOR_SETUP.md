# Archer Formation V1 - Editor Kurulum

## Otoriter asset ve sahne bağı

- Asset: `Assets/ScriptableObject/MobileCastle/Archers/ArcherFormationV1.asset`
- Sahne component'i: `NewGameScene/Grid/MobileCastleArcherTilePlacement`
- Spawn tilemap: `NewGameScene/Grid/outside`

Component üzerinde `Formation Definition` alanı `ArcherFormationV1` asset'ine,
`Outside Tilemap` alanı `Grid/outside` objesine bağlı olmalıdır.

## Beklenen contract

- Formation version: `1`
- Canonical tile count: `40`
- Slot per tile: `25`
- Toplam kapasite: `1000`
- Safe inset: `0.18`
- Minimum local distance: `0.055`
- Candidate attempts: `128`

`Grid/outside`, V1 asset'indeki 40 hücrenin tamamını içermelidir. Ek dolu tile'lar formation
sırasına otomatik alınmaz; V1 koordinat listesi data otoritesidir.

## Onarım

Yalnız formation asset/binding onarımı için:

`Window -> DeadWalls -> Repair Archer Formation V1`

Bu menü eksik default asset'i oluşturur, değerlerini doğrular, aktif `NewGameScene` içindeki
placement component'ine asset/tilemap bağlarını yazar ve sahneyi kaydeder. Tilemap boyamaz,
hücre eklemez veya owner görselini taşımaz. Tam scene setup da aynı binding'i uygular.

## Gizmo doğrulaması

Scene view'da `Grid` seçiliyken Gizmos açık olmalıdır. `Draw Gizmos` açık component, tam
`1000` formasyon noktasını çizer. Preview limiti yoktur. Gizmo yalnız editör sunumudur;
runtime placement aynı precomputed cache'i kullanır.

## Testler

- EditMode: `ArcherFormationUtilityTests`
- PlayMode: `ArcherFormationPlayModeTests.FormationV1_BuildsStableThousandPointsAndContinueUsesSameLayout`
- Migration: `RunPersistenceTests.TryLoad_Version6Snapshot_MigratesToFormationVersion1`

PlayMode testi gerçek `NewGameScene` bağını ve Continue sonrasında aynı 1000 noktanın
kurulduğunu doğrular. Script değişikliklerinden sonra harici compile komutu çalıştırılmaz;
Unity Editor refresh ile derler.

