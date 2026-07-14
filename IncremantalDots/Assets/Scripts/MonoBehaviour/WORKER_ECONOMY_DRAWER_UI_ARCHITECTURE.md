# Worker Economy Drawer UI - Architecture

`WorkerEconomyDrawerUI`, mobile HUD'un sol ust resource bar altinda acilip kapanan worker yonetim panelini kontrol eder.

## Amac

- Full-screen `CastleEconomyPanel` player-facing ana ekonomi UI'i olmaktan cikar.
- Worker target ratio yonetimi her an HUD uzerinden erisilebilir olur.
- Resource site objelerine tiklama gerekmez; buton input UI'dan gelir, gorsel feedback sahnedeki DOTS villager lojistigiyle verilir.

## Runtime Akisi

```text
WorkerDrawerToggleButton
-> WorkerEconomyDrawerPanel ac/kapat

Wood/Stone/Iron/Food +1% / +10% / +100% / direct input
-> GameManager.AdjustWorkerTargetRatioPercent() veya SetWorkerTargetRatioPercent()
-> WorkerAllocationUtility.SetTargetRatioBps()
-> Secilen hedef exact kalir; diger uc hedef oransal ve deterministik yeniden dagilir
-> Toplam hedef 10.000 basis-point kalir
-> Yalniz sonraki yeni population hedef acigina gore actual worker'a donusur

Wood/Stone/Iron/Food CAP veya EFF
-> GameManager.TryBuyWorkerBuildingUpgrade(resource, type)
-> Wood + Iron tek transaction olarak harcanir
-> MobileWorkerBuildingUpgradeState ilgili bagimsiz seviyeyi +1 yapar
-> GameManager base + Heart + Council + Meta + bina aggregate'ini yeniden kurar
-> CAP seviye basina +10 slot, EFF baz kisi uretimine additive +10% verir
```

`+1%` ve `+10%` secilen resource hedefini yuzde puan olarak artirir. `+100%`
secili hedefi clamp yoluyla `%100`'e tasir. Direct input `0-100` araliginda exact
hedef kabul eder. Hedef degisikligi mevcut worker sayilarini aninda hareket ettirmez.

## Guncellenen Alanlar

- Idle population
- Total worker count
- Archer population count
- Wood/Stone/Iron/Food worker count ve resource cap (`WOOD 20/40`)
- Wood/Stone/Iron/Food production rate
- Her resource icin `TGT xx%`; actual count cap'teyse `CAP` eki
- Her resource icin exact yuzde input'u ve `+1% / +10% / +100%` kontrolleri
- Her resource icin `CAP Lx` ve `EFF Lx` butonlari; butonda bir sonraki Wood + Iron maliyeti
- Sayisal cost limiti asilirsa `COST LIMIT` etiketi ve kilitli buton

## Scope

Bu controller ekonomi veya dagitim hesaplamasi yapmaz. Source-of-truth
`GameManager`, `WorkerAllocationUtility`, `PopulationState` ve
`MobilePopulationAllocation`, `MobileWorkerBuildingUpgradeState` ve
`MobileEconomyPriceTuning` ve `MobileWorkerBuildingUpgradeUtility` tarafindadir. Drawer fiyat/effect hesabi yapmaz;
yalniz GameManager API'sini gosterir ve cagirir.
