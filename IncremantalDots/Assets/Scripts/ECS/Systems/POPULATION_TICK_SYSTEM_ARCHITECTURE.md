# PopulationTickSystem - Mimari

## Sorumluluk

Her frame `Idle = max(0, Total - Workers - Archers)` hesabını günceller. Game Over sırasında çalışmaz.

## V1 Food sözleşmesi

Nüfus sürekli Food tüketmez. Yeni nüfus geldiğinde uygulanacak Food maliyeti tek seferlik bir gameplay transaction'ıdır; `PopulationTickSystem` hiçbir `ResourceConsumptionRate` alanına yazmaz.

`PopulationState.FoodPerAssignedPerMin` eski scene/save uyumluluğu için component'te kalabilir fakat V1 runtime hesabının owner'ı değildir.

## Sistem sırası

Sistem `ResourceTickSystem` öncesinde kalır; bu sıra artık tüketim için değil, population cache'inin aynı frame içinde güncel olması içindir.
