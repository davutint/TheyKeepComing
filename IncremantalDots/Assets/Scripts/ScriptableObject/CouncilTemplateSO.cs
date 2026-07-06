using UnityEngine;

namespace DeadWalls
{
    /// <summary>A/B seceneklerinin karsitlik tipi — composer atom kategorilerini buna gore secer.</summary>
    public enum CouncilContrastType
    {
        /// <summary>A = aninda kaynak, B = sureli uretim bonusu (ayni kaynak).</summary>
        NowVsLater = 0,

        /// <summary>A = X kaynagi ode Y kazan (takas), B = kucuk teselli odulu.</summary>
        ResourceTrade = 1,

        /// <summary>A = nufus kazanci, B = kaynak kazanci.</summary>
        PopulationVsResource = 2,

        /// <summary>A = odemeli savunma (okcu/heal), B = alternatif savunma/ekonomi.</summary>
        EconomyVsDefense = 3,

        /// <summary>A = guvenli kucuk fayda, B = buyuk odul + SONRAKI gece daha tehlikeli.</summary>
        SafeVsRisky = 4,

        /// <summary>Negatif event: A = cezaya katlan, B = kaynak ode ve gecistir.</summary>
        PayOrSuffer = 5,
    }

    /// <summary>
    /// Council event sablonu: tema/metin iskeleti + karsitlik tipi + flag kosullari.
    /// Somut sayilar ve secenek etiketleri runtime'da CouncilComposer tarafindan atomlardan
    /// uretilir; ayni sablon farkli gunlerde/baglamda farkli event'ler dogurur.
    /// Zincirler flag'lerle kurulur: bir secenegin SetsFlag'i, baska sablonun RequiredFlags'ini acar.
    /// </summary>
    [CreateAssetMenu(fileName = "CouncilTemplate", menuName = "DeadWalls/Mobile Castle/Council Template")]
    public class CouncilTemplateSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "template";
        public string Title = "COUNCIL MATTER";
        [TextArea(2, 4)] public string Body = "The council awaits your decision.";

        [Header("Narrative (placeholder destekli)")]
        [Tooltip("Doluysa composer rastgele birini secer (Body yerine). Placeholder'lar: {GAIN_N} {GAIN_RES} {PAY_N} {PAY_RES} {POP_N} {ARCHER_N} {BOOST_RES} {BOOST_PCT} {BOOST_D} {PEN_RES} {PEN_PCT} {PEN_D} {HEAL_PCT} {NIGHT_PCT} {CAP_RES} {CAP_N} {DAY} — once A sonra B seceneginin efektlerinden cozulur.")]
        [TextArea(2, 4)] public string[] BodyVariants = new string[0];
        [Tooltip("A secildikten sonra kartta gosterilen sonuc metni (yalniz A'nin efektlerinden cozulur).")]
        [TextArea(2, 3)] public string OutcomeA = string.Empty;
        [Tooltip("B secildikten sonra gosterilen sonuc metni (yalniz B'nin efektlerinden cozulur).")]
        [TextArea(2, 3)] public string OutcomeB = string.Empty;

        [Header("Composition")]
        public CouncilContrastType Contrast = CouncilContrastType.NowVsLater;

        [Tooltip("Bos degilse composer A secenegi icin SADECE bu atom Id'lerinden secer (bos = kind-uyumlu tum atomlar).")]
        public string[] OptionAAtomIds = new string[0];
        [Tooltip("Bos degilse B secenegi icin atom kisiti.")]
        public string[] OptionBAtomIds = new string[0];

        [Tooltip("Secenek buton fiil'leri; efekt ozeti composer tarafindan eklenir.")]
        public string OptionAVerb = "Accept";
        public string OptionBVerb = "Decline";

        [Header("Director")]
        [Min(0f)] public float BaseWeight = 1f;
        [Tooltip("Bu gunden once cikamaz (erken oyunda karmasik event'leri gizle).")]
        public int MinDay = 1;

        [Header("Memory / Chains")]
        [Tooltip("Bu flag'lerin HEPSI setliyse sablon havuza girer (zincir cocuklari icin).")]
        public string[] RequiredFlags = new string[0];
        [Tooltip("Bu flag'lerden herhangi biri setliyse sablon CIKMAZ.")]
        public string[] ForbiddenFlags = new string[0];
        [Tooltip("A secilirse setlenecek flag (bos = yok). Ayrica 'council_{Id}_a' otomatik setlenir.")]
        public string SetsFlagOnA = string.Empty;
        public string SetsFlagOnB = string.Empty;
        [Tooltip("Zincir cocugu: parent seciminden kac gun sonra cikabilir (RequiredFlags ile birlikte). 0 = kisit yok.")]
        public int ChainDelayDays;
        [Tooltip("True = bir kez gorulur, bir daha havuza girmez (imza/zincir event'leri).")]
        public bool OneShot;
    }
}
