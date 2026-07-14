# TechNodeDefinitionSO -> HeartNodeDefinitionSO Migration Plani

## Kural

Mevcut `TechNodeDefinitionSO` ve sabit `TechTreeCatalogSO` runtime'i E1 sonunda halen
aktiftir. Migration tek seferde owner cutover ile yapilacak; eski ve yeni satin alma
pipeline'lari ayni node'u paralel yonetmeyecek.

## Alan esleme

| Legacy alan | Heart alani | Donusum karari |
|---|---|---|
| `Id`, `Title`, `Description`, `Icon` | Ayni kimlik/sunum alanlari | Birebir tasinabilir |
| Yok | `Tags`, `Branch`, `Rarity`, depth range | Blueprint ve generator ihtiyacina gore insan review'u |
| `MaxLevel` | `HeartNodeType` / derived `IsRepeatable` | `MaxLevel > 1` yalniz Repeatable adayi; otomatik kesin karar degil |
| `ResourceCost` | `BaseGraveEssenceCost` | Otomatik kur cevirimi yok; Grave Essence tuning review'u zorunlu |
| `CostGrowthPerLevel` | `CostGrowthPerLevel` | Tuning girdisi tasinir; E4 fiyat evaluator'u formulu sahiplenir |
| `PrerequisiteNodeIds` | Generated edge/depth/guarantee kurallari | Sabit graph olarak kopyalanmaz |
| `RevealChildNodeIds` | Generated edges + runtime visibility | Sabit reveal listesi olarak yeni source'a kopyalanmaz |
| `Effects` | `HeartNodeEffect[]` | Tek tek semantic mapping ve runtime destek review'u |
| Yok | Keystone `ConflictNodeIds` | Yalniz onayli Keystone cifti icin yazilir |

## Effect esleme siniri

Aktif ve Blueprint ile uyumlu legacy etkiler yeni enum'da acik karsilik tasir:
archer unlock/damage/fire rate, worker capacity/production, population growth, tek Wall
HP/repair cost ve Fireball unlock/damage/radius/cooldown. Blueprint ornekleri split shot
ve burning ground Evolution etkisi olarak temsil edilebilir.

Legacy moat etkileri V1'de dormant oldugu icin otomatik migrate edilmez. Runtime'da
destegi olmayan bir effect yalniz asset doldurmak icin eklenmez.

## Asamali cutover

1. E1: yeni source/runtime contract'lari ve Grave Essence lifecycle'i eklenir.
2. E2: generator yalniz yeni Heart definition havuzunu tuketir; guarantee/reachability
   validation'i tamamlanmadan production graph acilmaz.
3. Icerik review: Basic, Rapid, Frost, Fireball ve Wall guarantee yollarinin node tipi,
   depth, Essence maliyeti ve effect eslemesi owner tarafindan onaylanir.
4. E4: tek Heart purchase/effect pipeline'i Grave Essence harcama kapisina baglanir.
   Legacy `TryBuyTechNode` ayni frame/node icin owner olmaktan cikarilir.
5. E6: `GeneratedRunGraph` exact save/load ve graph version validation'a baglanir.
6. Kabul sonrasi legacy catalog/UI yolu kaldirilir veya acik dormant migration fallback'i
   olarak isaretlenir; gizli ikinci progression owner birakilmaz.

## Otomatik migrate edilmeyecek kararlar

- Wood/Stone/Iron/Food -> Grave Essence kur orani.
- Unlock/Repeatable/Evolution/Keystone sinifi.
- Branch, rarity, depth ve tag dagilimi.
- Keystone esleri.
- Generator edge'leri ve cross-link'ler.
- Essence drop kaynagi/orani.

Bu kararlar Blueprint + gameplay tuning review'u olmadan script tarafindan tahmin edilmez.

