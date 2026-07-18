using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Meta ekonomisinin player-facing kimligi. Save alanlari ve reward matematigi bu
    /// presentation contract'ina bagli degildir; isim/ikon/copy migration'i mevcut
    /// bakiyeyi ve stable upgrade Id'lerini degistiremez.
    /// </summary>
    [Serializable]
    public class MetaPresentationSettings
    {
        public const int CurrentVersion = 2;

        [Header("Identity")]
        public int Version = CurrentVersion;
        public string CurrencyId = "last_embers";
        public string CurrencyName = "LAST EMBERS";
        public string CurrencyShortName = "EMBERS";
        public Sprite CurrencyIcon;
        public Color CurrencyColor = new Color(1f, 0.66f, 0.20f, 1f);

        [Header("Death Screen Copy")]
        public string DeathTitle = "THE WALL HAS FALLEN";
        [TextArea(1, 2)]
        public string DeathSubtitle =
            "THE RUN ENDS HERE. WHAT REMAINS WILL STRENGTHEN THE NEXT STAND.";
        public string NewRecordLabel = "NEW LONGEST STAND";
        public string ShopTitle = "FORTIFY THE NEXT STAND";
        public string ShopHint = "PERMANENT UPGRADES APPLY TO YOUR NEXT RUN.";
        public string RestartLabel = "BEGIN NEXT RUN";

        public static MetaPresentationSettings CreateLastEmbers(Sprite icon = null)
        {
            return new MetaPresentationSettings { CurrencyIcon = icon };
        }

        public string DisplayName => string.IsNullOrWhiteSpace(CurrencyName)
            ? MetaProgression.CurrencyName
            : CurrencyName.Trim();

        public string ShortName => string.IsNullOrWhiteSpace(CurrencyShortName)
            ? DisplayName
            : CurrencyShortName.Trim();

        public void CollectValidationErrors(List<string> problems)
        {
            if (problems == null)
                throw new ArgumentNullException(nameof(problems));

            if (Version != CurrentVersion)
                problems.Add($"Meta presentation version v{Version}; beklenen v{CurrentVersion}.");
            if (string.IsNullOrWhiteSpace(CurrencyId))
                problems.Add("Meta currency stable CurrencyId bos.");
            if (string.IsNullOrWhiteSpace(CurrencyName))
                problems.Add("Meta currency CurrencyName bos.");
            if (string.IsNullOrWhiteSpace(CurrencyShortName))
                problems.Add("Meta currency CurrencyShortName bos.");
            if (CurrencyIcon == null)
                problems.Add("Meta currency icon atanmamis.");
            if (CurrencyColor.a <= 0f)
                problems.Add("Meta currency rengi gorunur olmali.");
            if (string.IsNullOrWhiteSpace(DeathTitle)
                || string.IsNullOrWhiteSpace(DeathSubtitle)
                || string.IsNullOrWhiteSpace(NewRecordLabel)
                || string.IsNullOrWhiteSpace(ShopTitle)
                || string.IsNullOrWhiteSpace(ShopHint)
                || string.IsNullOrWhiteSpace(RestartLabel))
            {
                problems.Add("Meta death-screen copy seti eksik.");
            }
        }
    }

    /// <summary>Blueprint v1.0 sabit meta magaza katalogu.</summary>
    [CreateAssetMenu(fileName = "MetaUpgradeCatalog", menuName = "DeadWalls/Mobile Castle/Meta Upgrade Catalog")]
    public class MetaUpgradeCatalogSO : ScriptableObject
    {
        [Header("Player-Facing Identity")]
        public MetaPresentationSettings Presentation = MetaPresentationSettings.CreateLastEmbers();

        [Header("Death Reward")]
        public MetaRewardSettings RewardSettings = new MetaRewardSettings();

        [Header("Permanent Upgrade Definitions")]
        public MetaUpgradeSO[] Upgrades = new MetaUpgradeSO[0];

        public MetaUpgradeSO GetUpgrade(string id)
        {
            if (string.IsNullOrEmpty(id) || Upgrades == null)
                return null;

            foreach (var upgrade in Upgrades)
            {
                if (upgrade != null && upgrade.Id == id)
                    return upgrade;
            }

            return null;
        }

        public List<string> ValidateCatalog()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();
            if (Presentation == null)
                problems.Add("Meta presentation settings bos.");
            else
                Presentation.CollectValidationErrors(problems);

            if (RewardSettings == null)
                problems.Add("Meta reward settings bos.");
            else
                RewardSettings.CollectValidationErrors(problems);

            if (Upgrades != null)
            {
                foreach (var upgrade in Upgrades)
                {
                    if (upgrade == null) { problems.Add("Upgrades listesinde null giris."); continue; }
                    if (string.IsNullOrEmpty(upgrade.Id)) problems.Add($"'{upgrade.name}' Id bos.");
                    else if (!ids.Add(upgrade.Id)) problems.Add($"Duplicate Id: '{upgrade.Id}'.");
                    if (MoatDormancyRules.IsDormantMetaUpgradeId(upgrade.Id))
                        problems.Add($"'{upgrade.Id}' dormant V1 meta content; aktif catalog'da bulunamaz.");
                    upgrade.CollectValidationErrors(problems);
                }
            }

            return problems;
        }
    }
}
