# Collision Response - Mimari

## Circle-Circle Carpisma
Iki daire (A, B) cakistiginda:

```
normal = normalize(A.pos - B.pos)
overlap = (A.radius + B.radius) - distance(A, B)
```

### 1. Pozisyon Duzeltme
```
A.pos += normal * overlap * 0.5
B.pos -= normal * overlap * 0.5
```
Her entity sadece kendini duzeltir (paralel guvenli). Komsunun duzeltmesi bir sonraki frame'de gelir.

### 2. Velocity Impulse (Momentum Transfer)
```
relativeVel = A.vel - B.vel
velAlongNormal = dot(relativeVel, normal)

if velAlongNormal > 0: return  // zaten ayriliyorlar

impulse = -velAlongNormal / (1/A.mass + 1/B.mass)

A.vel += (impulse / A.mass) * normal
B.vel -= (impulse / B.mass) * normal
```

### 3. Overlap Velocity Impulse (Ayirma Hizlandirmasi)
```
A.vel += normal * overlap * 2.0f
```
Pozisyon duzeltme tek basina yetmeyebilir — ozellikle yogun crowd'larda overlap bir sonraki frame'e tasabilir. Bu ek impulse, cisimleri overlap yonunde hizlandirarak ayrilmayi hizlandirir. Sadece kendi entity'sine uygulanir (paralel guvenli).

## Ne Yapar
- Arkadan gelen zombi ondekine carpar → hiz TRANSFER olur
- Ondeki zombi itilir, o da onundekine carpar → ZINCIR REAKSIYON
- Duvar onunde yigilma → Y ekseninde dogal yayilma
- Zombi olur → etraftakiler bosluga dogru akar

## Paralel Guvenlik
Her entity sadece kendi pozisyon + velocity'sini yazar.
Komsularin degerleri frame basindaki snapshot'tan okunur ([ReadOnly] ComponentLookup).
Bir-frame gecikme var ama birden fazla frame'de yakinsar.
HasComponent kontrolleri kaldirildi — spatial hash sadece gecerli entity'ler icerir, bu nedenle guard gereksizdi ve CPU israf ediyordu.

## Inelastic Collision
Restitution = 0 (zombiler ziplamaz). Impulse sadece birbirini itmek icin.
