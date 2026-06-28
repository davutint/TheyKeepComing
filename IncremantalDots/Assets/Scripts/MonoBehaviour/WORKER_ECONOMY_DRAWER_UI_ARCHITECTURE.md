# Worker Economy Drawer UI - Architecture

`WorkerEconomyDrawerUI`, mobile HUD'un sol ust resource bar altinda acilip kapanan worker yonetim panelini kontrol eder.

## Amac

- Full-screen `CastleEconomyPanel` player-facing ana ekonomi UI'i olmaktan cikar.
- Worker assignment her an HUD uzerinden erisilebilir olur.
- Resource site objelerine tiklama gerekmez; buton input UI'dan gelir, gorsel feedback sahnedeki DOTS villager lojistigiyle verilir.

## Runtime Akisi

```text
WorkerDrawerToggleButton
-> WorkerEconomyDrawerPanel ac/kapat

Wood/Stone/Iron/Food + Worker button
-> GameManager.AssignResourceWorker(resource)
-> MobilePopulationAllocation artar
-> DOTS VillagerWorker route visual sync
-> WorkerLogisticsMovementSystem villager'i pickup/hub arasinda yurutur
```

## Guncellenen Alanlar

- Idle population
- Total worker count
- Archer population count
- Wood/Stone/Iron/Food worker count
- Wood/Stone/Iron/Food production rate
- `NEED POP` / `READY` status

## Scope

Bu controller ekonomi hesaplamasi yapmaz. Source-of-truth `GameManager`, `PopulationState` ve `MobilePopulationAllocation` tarafindadir.
