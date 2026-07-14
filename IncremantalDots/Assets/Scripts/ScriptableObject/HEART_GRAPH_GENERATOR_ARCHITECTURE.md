# Castle Heart Graph Generator - Mimari

## Kapsam

`DW-E-GRAPH`, Castle Heart graph'ini run basinda authored `HeartNodeDefinitionSO`
havuzundan tamamen ve deterministic olarak uretir. Bu katman reveal, satin alma,
effect uygulama, Heart UI veya save/load entegrasyonunun owner'i degildir.

Production Heart node icerigi, Grave Essence maliyetleri ve Keystone trade-off
tasarimlari owner onayi bekledigi icin bu pakette gercek catalog asset'i uretilmedi.
Generator sentetik EditMode kataloglariyla dogrulandi.

## Source owner'lari

- `HeartNodeDefinitionSO`: tek authored node tanimi; branch, type, rarity, depth,
  tags, effects, maliyet girdileri ve Keystone partner Id'si.
- `HeartNodeCatalogSO`: generator'in immutable authored node havuzu.
- `HeartGraphGenerationRequest`: run seed'i ve acik generation/tuning sinirlari.
- `GeneratedRunGraph`: run basinda uretilen, asset referanssiz exact graph state'i.
- `HeartGraphGenerator`: deterministic secim, yerlestirme, edge ve cross-link owner'i.
- `HeartGraphValidator`: broken graph'i runtime'a gecirmeyen structural kabul kapisi.

Legacy `TechTreeCatalogSO`, bu paket sonunda halen mevcut UI/satin alma akisinin
owner'idir. Yeni katalog legacy catalog ile paralel satin alma owner'i degildir.

## Sabit graph sozlesmesi

- Root Id her zaman `castle_heart`; authored node havuzunda bu Id kullanilamaz.
- Dort sabit yon `Army`, `Defense`, `Production`, `HeartMagic` enum'lariyla temsil edilir.
- Her yon root'tan baslayan, depth'i birer artan kesintisiz bir core spine tasir.
- Her depth'te bir core node vardir; controlled cross-link core path'in yerine gecmez.
- Cross-link yalniz baska branch'teki bir sonraki depth'e gider. Bu nedenle cycle
  uretemez; source ve target basina en fazla bir cross-link kullanilir.
- Root run basinda `Revealed`, level `1` ve unlocked'dir. Diger butun node'lar
  `Hidden`, level `0` ve unlocked uretilir. E3 `HeartGraphRevealService`, generator
  bittikten sonra root'un yalniz dogrudan komsularini reveal eder; ayrintili redaction
  contract'i `HEART_GRAPH_REVEAL_ARCHITECTURE.md` belgesindedir.

## Authored tag sozlesmesi

| Tag | Zorunlu branch/effect |
|---|---|
| `guarantee:rapid` | Army + Rapid archer unlock |
| `guarantee:frost` | Army + Frost archer unlock |
| `guarantee:fireball` | HeartMagic + spellcasting unlock |
| `guarantee:wall` | Defense + Wall max HP effect |
| `sink:repeatable` | Her branch'te en az bir Repeatable node |

Guarantee tag'leri catalog'da tam bir kez bulunur. Tag ile gameplay effect'i ayni
gercegi soylemelidir; yalniz node adina bakilarak guarantee kabul edilmez.

## Deterministic generation

Generator global `UnityEngine.Random` state'ini kullanmaz. Run seed'inden her attempt
icin stable xorshift state turetilir. Catalog girdisi Id'ye gore siralandigi icin ayni
catalog icerigi, request ve seed ayni graph JSON'unu uretir.

Uretim sirasi:

1. Request ve catalog preflight validation.
2. Rapid, Frost, Fireball ve Wall guarantee node'larini mandatory listeye alma.
3. Her branch icin bir repeatable sink secme.
4. Request kadar tam ve simetrik Keystone cifti secme.
5. Mandatory node'lari authored depth araliklarina backtracking ile yerlestirme.
6. Bos slotlari branch + depth uygunlugu ve Standard/Rare agirliklariyla doldurma.
7. Dort root spine edge'ini ve sinirli forward cross-link'leri kurma.
8. Tam graph'i validator'dan gecirme; invalid ise yeni deterministic attempt.

Graph reveal aninda yeni RNG cekmez. Bir attempt valid degilse `MaximumAttempts`
sinirinda reroll edilir. Hicbiri valid degilse `TryGenerate` null graph ve acik hata
raporu verir; `GenerateOrThrow` broken run baslatmak yerine exception uretir.

## Keystone ve lock siniri

Catalog validation her Keystone'un tam bir partner tasimasini, partnerin de Keystone
olmasini ve conflict'in simetrik olmasini zorunlu tutar. Generator secilen cifti birlikte
yerlestirir. Initial graph hicbir node'u lock etmez; secimden sonra yalniz partneri lock
etme davranisi E4 purchase/effect pipeline'ina aittir.

Normal node'larda `ConflictNodeIds` yasaktir. Validator initial normal veya Keystone
node'da accidental runtime lock bulursa graph'i reddeder.

## Validation kabul kapisi

Validator su durumlari acik hata olarak raporlar:

- Yanlis version, seed veya root.
- Duplicate/unknown node ve duplicate/invalid edge.
- Definition branch/depth araligi disindaki node.
- Dort branch'ten birinde kopuk veya depth bosluklu core path.
- Root'tan ulasilamayan node veya guarantee.
- Eksik Rapid, Frost, Fireball, Wall guarantee'i.
- Eksik branch repeatable sink'i.
- Eksik, fazla veya yarim Keystone cifti.
- Cross-link limit asimi.
- Run basinda yanlis visibility, level veya lock state.

## Test kapsami

`HeartGraphGeneratorTests` sentetik ScriptableObject kataloglariyla su sozlesmeleri
kanitlar:

- Ayni seed + ayni catalog byte-equivalent graph JSON.
- Sabit root, dort connected spine, guarantee ve repeatable sink varligi.
- Tam Keystone cifti ve sifir initial lock.
- Forward ve limitli cross-link.
- Duplicate catalog Id preflight rejection.
- Depth disi guarantee icin null graph + explicit exception.
- Disconnected core path detection.
- Request ile Keystone sayisi uyusmazligi detection.
- Root disindaki butun node'larin hidden/level 0/unlocked baslamasi.
