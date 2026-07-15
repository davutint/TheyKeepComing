using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Council event etkilerinin turleri. Atomlar event DEGIL, etki parcaciklaridir;
    /// CouncilComposer bunlari sablonlarla esleyip yuzlerce farkli event uretir.
    /// </summary>
    public enum CouncilEffectKind
    {
        None = 0,

        /// <summary>Aninda kaynak kazanci (Resource hedefi; miktar uretim-oranli).</summary>
        GainResource = 1,

        /// <summary>Aninda kaynak odeme (secenegin bedeli; miktar uretim-oranli).</summary>
        PayResource = 2,

        /// <summary>Gecici uretim carpani (Resource hedefi, DurationDays gun). Ayni anda TEK aktif bonus olur; yenisi eskisini ezer.</summary>
        TempProductionBoost = 3,

        /// <summary>Gecici uretim cezasi (negatif event'lerin "katlan" secenegi).</summary>
        TempProductionPenalty = 4,

        /// <summary>Kalici worker cap artisi (Resource hedefi).</summary>
        WorkerCapBonus = 5,

        /// <summary>Aninda nufus kazanci.</summary>
        GainPopulation = 6,

        /// <summary>Aninda Basic okcu; her okcu bir idle population kullanir ve ortak 1000 cap'e tabidir.</summary>
        GainFreeArchers = 7,

        /// <summary>Yalniz Wall Max HP yuzdesi kadar, eksik HP ile sinirli iyilestirme.</summary>
        HealDefensePercent = 8,

        /// <summary>SONRAKI gece spawn yogunlugu delta'si (risk atomu: +0.20 tehlike, -0.25 sakin gece).</summary>
        NextNightSpawnDelta = 9,
    }

    /// <summary>
    /// Tek bir etki parcaciginin tanimi. Buyuklukler SABIT SAYI DEGIL, uretim-oranli
    /// formullerdir ("X dakikalik uretim") — event'ler DAY 3'te de DAY 30'da da anlamli kalir.
    /// BudgetMinutes tum etkileri ortak para birimine ("dakika-degeri") cevirir; composer
    /// A/B seceneklerini bu butceyle dengeler (kirik kombinasyon matematiksel olarak engellenir).
    /// </summary>
    [CreateAssetMenu(fileName = "CouncilEffectAtom", menuName = "DeadWalls/Mobile Castle/Council Effect Atom")]
    public class CouncilEffectAtomSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "atom";
        public CouncilEffectKind Kind = CouncilEffectKind.GainResource;

        [Tooltip("Kaynak hedefli etkiler icin. Balanced = composer kaynak secer (director kitligi kayirir).")]
        public EconomyFocusType Resource = EconomyFocusType.Balanced;

        [Header("Magnitude (uretim-oranli)")]
        [Tooltip("Kaynak etkileri: miktar = hedef kaynagin dakikalik uretimi * bu deger. Uretim 0 ise FlatFallback kullanilir.")]
        public float MinutesOfProduction = 1.5f;
        [Tooltip("Uretim-oranli hesap mumkun degilse (uretim 0) kullanilacak taban miktar.")]
        public float FlatFallback = 60f;
        [Tooltip("Yuzde/carpan etkiler icin oran (TempProduction 0.25 = +%25; HealDefense 0.20 = %20; NextNightSpawnDelta +-oran). Pop/Archer icin adet tabani.")]
        public float Rate = 0.25f;
        [Tooltip("Pop/Archer gibi adet etkilerinde gun basina buyume (adet = Rate + gun * bu).")]
        public float PerDay = 0.3f;
        [Tooltip("Sureli etkiler icin gun (cycle) sayisi.")]
        public int DurationDays = 2;

        [Header("Budget (dakika-degeri)")]
        [Tooltip("Bu etkinin 'degeri' kac dakikalik uretime esdeger (butce dengeleme icin). PayResource/penalty gibi bedeller NEGATIF girilir.")]
        public float BudgetMinutes = 1.5f;

        [Header("Director (baglam agirliklari)")]
        [Tooltip("Hedef kaynagin stogu bu kadar dakikalik uretimin ALTINDAYSA kit sayilir.")]
        public float ScarcityThresholdMinutes = 2f;
        [Tooltip("Kitlikta agirlik carpani (>1 = director bu atomu kayirir).")]
        public float ScarcityWeightMult = 3f;
        [Tooltip("Bollukta (stok > 2x esik) agirlik carpani (gider/risk atomlari icin >1 mantikli).")]
        public float AbundanceWeightMult = 1f;
        [Tooltip("Savunma yuzdesi 0.5 altindayken agirlik carpani (savunma atomlari icin >1).")]
        public float LowDefenseWeightMult = 1f;

        [Header("Text")]
        [Tooltip("Secenek etiketinde gorunen kisa parca; {N} miktar, {RES} kaynak adi, {D} gun. Orn: '+{N} {RES}' / '{RES} production +{P}% for {D} days'.")]
        public string LabelFormat = "+{N} {RES}";
    }
}
