# Management Drawer Coordinator - Editor Setup

## Aktif sahne

`NewGameScene/Canvas/MobileCastleHudRoot` scene instance'inda tam bir
`ManagementDrawerCoordinatorUI` bulunmalidir. Ayni GameObject uzerinde su owner'lar da
tekil olarak bulunur:

- `WorkerEconomyDrawerUI`;
- `MarketUI`;
- `ArrowSupplyUI`.

`Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab` runtime coordinator tasimaz.
`Window -> DeadWalls -> Mobile Castle Scene Setup` calistirildiginda coordinator scene
root'a idempotent eklenir.

## Play Mode QA

1. HUD acilisinda Workers, Archers ve Ammo panellerinin kapali oldugunu dogrula.
2. `WORKERS + HOUSING` acikken `ARCHERS`a bas; Worker paneli ayni frame kapanmali.
3. Archer paneli acikken `ARROW SUPPLY` butonuna bas; Archer paneli aninda off-screen kapanmali.
4. Ammo acikken Worker butonuna bas; Ammo kapanip Worker acilmali.
5. Her adimda `ManagementDrawerCoordinatorUI.ActiveDrawer` yalniz gorunen yuzeyi gostermeli.
6. Castle Heart acilis/pause davranisinin coordinator'dan bagimsiz kaldigini dogrula.
