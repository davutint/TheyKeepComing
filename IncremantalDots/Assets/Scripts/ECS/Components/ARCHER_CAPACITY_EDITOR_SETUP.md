# Archer Capacity Editor Setup

## Kurulum

Ek bir scene objesi, prefab component'i veya Inspector alanı gerekmez. `1000` ortak
cap bir balance slider'ı değil, V1 ürün guardrail'idir ve
`ArcherCapacityUtility.MaxTotalArchers` tarafından sahiplenilir.

`NewGameScene` içindeki mevcut `GameManager`, `ArcherRecruitmentCatalogSO` ve
`Archer.prefab` bağlantıları korunur. Setup tool'un cap için asset üretmesi gerekmez.

## Manuel doğrulama

1. `NewGameScene` Play Mode'u aç.
2. Basic/Rapid/Frost toplamını `1000` değerine getir.
3. Recruitment drawer'da bütün buy butonlarının `MAX` olduğunu ve
   `ARMY CAP 1000/1000` metnini doğrula.
4. 1001. satın almayı dene; kaynak, idle population ve entity sayısı değişmemeli.
5. Free Economy Test Mode açıkken de 1001. okçunun alınamadığını doğrula.

Otomatik kanıt için `ArcherCapacityUtilityTests` ve `ArcherCapacityPlayModeTests`
çalıştırılır.

