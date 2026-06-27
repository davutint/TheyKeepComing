# ZombieSlowTimerSystem - Mimari

## Amac

Frost oklarin verdigi tek hedef yavaslatma etkisinin suresini ECS tarafinda takip eder.

## Veri

- `ZombieSlow.Duration`: kalan slow suresi.
- `ZombieSlow.SpeedMultiplier`: hareket kuvvetine uygulanacak carpan.
- Component enableable'dir; etkin degilken zombi normal hizinda yurur.

## Davranis

- `ArrowHitSystem`, Frost ok hedefe vurdugunda `ZombieSlow` component'ini enable eder ve duration'i refresh eder.
- `ZombieSlowTimerSystem`, her frame duration'i azaltir.
- Slow aktifken zombi `SpriteTint` degeri soguk/maviye cekilir.
- Duration sifira inince multiplier `1` olur ve component pasiflenir.
- Duration bitince veya zombi Dead state'e gecince tint normal beyaza doner.
- `ApplyMovementForceSystem`, component enabled ise zombi hareket kuvvetini multiplier ile carpar.

Slow stack yapmaz; ayni hedefe tekrar Frost oku vurmak yalnizca duration'i yeniler.
