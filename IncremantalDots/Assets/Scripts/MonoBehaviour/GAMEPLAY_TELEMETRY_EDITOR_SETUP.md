# Gameplay Telemetry Editor Setup

Inspector veya scene binding gerekmez. `GameManager.Telemetry.cs`, mevcut `GameManager` partial
sinifinin parcasidir; `GameplayTelemetry.cs` ayni `DeadWalls` runtime assembly'sinde derlenir.

## Dogrulama

1. `NewGameScene` Play Mode'a girilir veya ana menuden `NEW RUN` secilir.
2. Console'da tek `[DW-TELEMETRY]` kaydi ve envelope icinde `run_started` aranir.
3. Payload'da production Meta definition seviyeleri, gercek baslangic kaynaklari ve Heart
   graph identity kontrol edilir.
4. Save/Continue yapildiginda ayni RunId icin ikinci `run_started` olmamalidir.
5. Console'da Day 1 `day` ile baslayan tek `phase_changed` aranir; Dusk/Night/Dawn
   transition'larinda yeni event gelmeli, ayni phase icindeki enemy/backlog degisimi duplicate
   uretmemelidir.
6. `phase_changed` payload'indaki `AliveEnemies` ve `SpawnBacklog`, transition sonrasi
   `WaveStateData` ve `ContinuousSpawnBudgetData` snapshot'lariyla ayni olmalidir.
7. Bir bed veya Arrow refill satin alinir; Console'daki `resource_spent` kaydinda exact resource,
   amount, purchase type ve post-commit count kontrol edilir.
8. Wood + Iron isteyen bir worker building upgrade alinir; ayni purchase type/resulting level ile
   once Wood, sonra Iron olmak uzere iki `resource_spent` kaydi gelmelidir.
9. Yetersiz kaynakla ayni purchase tekrar denenir; yeni `resource_spent` kaydi gelmemelidir.
10. Heart catalog configured ortamda node alimi Grave Essence event'ini graph commit'inden sonra;
    Game Over Meta shop alimi ise `meta_currency` event'ini durable save'den sonra uretmelidir.
11. Basic Archer satin alinir; `archer_changed` payload'i `buy`, `none -> basic` ve post-commit
    toplam cap kullanimini vermelidir.
12. Basic Archer, Rapid veya Frost'a retrain edilir; payload `retrain`, `basic -> target` olmali ve
    toplam cap kullanimi degismemelidir.
13. Locked type, yetersiz kaynak/population veya 1000 cap nedeniyle reddedilen buy ile gecersiz
    retrain yeni `archer_changed` kaydi uretmemelidir.
14. Revealed bir Castle Heart node'u Grave Essence ile alinir; once `resource_spent`, sonra
    `heart_node_bought` gelmeli ve payload exact node Id, post-commit level, graph depth, toplam bulk
    cost ile yeni reveal edilen child sayisini vermelidir.
15. Ayni non-repeatable node tekrar alindiginda veya hidden/locked/yetersiz Essence node denendiginde
    yeni `heart_node_bought` kaydi gelmemelidir.
16. Day `3/6/9...` regular Council'da Option A veya B secilir; Console'daki `council_resolved`
    kaydi exact day/template, `option_a/option_b`, concrete effect listesi ve guardlanmis
    `NextNightDelta` degerini vermelidir.
17. Regular Council Dusk'a secilmeden tasinir; tek `expired` kaydi gelmeli, `Effects` bos ve
    `NextNightDelta=0` olmalidir. Bos active state'te ikinci expire yeni kayit uretmemelidir.
18. Cozulmus Council sonrasi save/Continue yapildiginda ayni karar tekrar yayilmamalidir;
    affordability veya content gate'inde reddedilen secim de event uretmemelidir.
19. Development combat acikken Fireball, Rally ve Night Emergency Repair sirayla kullanilir;
    `ability_cast` kayitlari `fireball/rally/emergency_repair`, exact phase ve resolved cooldown
    tasimalidir. Fireball `Targets=0/Repair=0`, Rally canonical archer totalini, Emergency Repair ise
    `Targets=1` ve gercek Wall HP artisini vermelidir.
20. Ayni ability cooldown/active-effect guard'inda tekrar denendiginde yeni event gelmemeli;
    save/Continue daha once kabul edilen cast'leri tekrar yaymamalidir.
21. Day veya Dusk'ta hasarli Wall normal repair ile iyilestirilir; once
    `resource_spent: stone/wall_repair`, sonra tek `wall_repaired` kaydi gelmelidir.
22. `wall_repaired` payload'inda exact transaction phase'i, gercek Stone debit'i ve canonical
    `WallSegment` HP before/after degerleri kontrol edilir.
23. Full/dead Wall, Night/Dawn, yetersiz Stone ve free economy test modunda yeni
    `wall_repaired` kaydi gelmemelidir. Emergency Repair ve Council heal de bu eventi uretmemelidir.
24. Basarili normal repair sonrasi save/Continue yapildiginda ayni transaction tekrar
    yayilmamalidir.
25. Normal bir run'da enemy sayisi yuksek su isaretine cikarilir; daha sonra alive sayisi dusse de
    `run_ended.PeakEnemies` onceki en yuksek production count'u korumalidir.
26. Wall farkli day/phase'lerde hasar aldiginda `WallDamageTimeline` kronolojik olmalidir; ayni
    day/phase icindeki cok sayida hit tek aggregate bucket'ta toplanmalidir.
27. Save/Continue sonrasi peak enemy ve Wall damage bucket'lari aynen korunmali; death aninda
    `Day/Kills/PeakPopulation` ile durable `MetaReward` tek `run_ended` payload'ina girmelidir.
28. Ayni lethal state/death transaction'i yeniden islenmeye calisildiginda ikinci `run_ended`
    cikmamali; Meta save durable degilse event hic cikmamalidir.
29. `run_ended v2` final snapshot'inda upgrade-applied Wall MaxHP, Wood/Stone/Iron/Food, Arrow stock/capacity,
    population/capacity/idle, Basic/Rapid/Frost count ve unspent Grave Essence canonical death
    state'iyle ayni olmalidir.
30. `Window > DeadWalls > Difficulty Tuner` icindeki launch target paneli 19 band, sifir validation
    problemi ve authority dokumanindaki fingerprint'i gostermelidir.

Otomatik kapsam:

- EditMode `GameplayTelemetryTests`: run/phase/resource/archer/Heart/Council/ability/Wall repair/
  run-ended
  payload factory'leri,
  multi-resource canonical order, envelope serialization ve invalid identity/amount/result/
  transition/cap/node/level/depth/cost/reveal/resolution/effect/night-delta/ability-result/
  repair-phase/cost/HP-transition, final-summary/timeline/order guard'lari ve ECS accumulator
  high-water/bucket aggregation davranisi.
- PlayMode `GameplayTelemetryPlayModeTests`: gercek NewGameScene yeni-run emission'i, canonical
  phase/horde snapshot'i, ayni-phase idempotency, exact Continue duplicate guard'i, tek/iki kaynakli
  purchase commit event'leri, player archer buy/retrain transition snapshot'lari, canonical Castle
  Heart purchase/reveal snapshot'i, regular Council secim/expire/Continue snapshot'i ve rejected
  transaction sifir-event guard'i; gercek Fireball/Rally/Emergency Repair commit/result snapshot'i
  ile cooldown/rejected/Continue sifir-event davranisi; gercek Day normal repair Stone debit +
  HP transition snapshot'i, event sirasi ve full/Night/resource/Continue sifir-event davranisi.
  Ayni class ayrica run telemetry accumulator'larini save/Continue boyunca koruyup durable death
  sonrasinda tek `run_ended` final summary'ye baglar.

Harici analytics provider bu kurulumun parcasi degildir. Production launch target asset'i yalniz
provider-independent designer review bantlarini tutar; SDK, servis veya endpoint eklemez.
