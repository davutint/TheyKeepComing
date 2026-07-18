#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadWalls
{
    /// <summary>
    /// Legacy resource-tech fikirlerini procedural Castle Heart sozlesmesine temizce tasiyan
    /// canonical production catalog builder. Legacy asset'leri silmez veya yeniden yazmaz.
    /// </summary>
    public static class CastleHeartProductionCatalogBuilder
    {
        public const string CatalogFolder =
            "Assets/ScriptableObject/MobileCastle/CastleHeart";
        public const string NodeFolder = CatalogFolder + "/Nodes";
        public const string CatalogPath = CatalogFolder + "/HeartNodeCatalog.asset";
        public const string TargetScenePath = "Assets/Scenes/NewGameScene.unity";
        private const string HudPrefabPath =
            "Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab";

        private const int CatalogVersion = 1;
        private const string ArmyIconPath =
            "Assets/Sprites/UI/Generated/mobile_castle_hud/arrow_basic.png";
        private const string RapidIconPath =
            "Assets/Sprites/UI/Generated/mobile_castle_hud/archer_rapid_portrait_v4.png";
        private const string FrostIconPath =
            "Assets/Sprites/UI/Generated/mobile_castle_hud/archer_frost_portrait_v4.png";
        private const string DefenseIconPath =
            "Assets/Sprites/UI/Generated/mobile_castle_hud/defense_shield_icon_v1.png";
        private const string RepairIconPath =
            "Assets/Sprites/UI/Generated/mobile_castle_hud/castle_yard_repair_icon_v1.png";
        private const string ArrowStockIconPath =
            "Assets/Sprites/UI/Generated/mobile_castle_hud/arrow_stock_v4.png";
        private const string ProductionIconPath =
            "Assets/Sprites/UI/Generated/mobile_castle_hud/wood_icon.png";
        private const string StoneIconPath =
            "Assets/Sprites/UI/Generated/mobile_castle_hud/stone_icon.png";
        private const string IronIconPath =
            "Assets/Sprites/UI/Generated/mobile_castle_hud/iron_icon.png";
        private const string FoodIconPath =
            "Assets/Sprites/UI/Generated/mobile_castle_hud/food_icon.png";
        private const string PopulationIconPath =
            "Assets/Sprites/UI/Generated/mobile_castle_hud/population_people_v4.png";
        private const string MagicIconPath = "Assets/3x/EXTRAS/Projectiles/Fireball.png";

        private sealed class NodeSeed
        {
            public string Id;
            public string Title;
            public string Description;
            public HeartNodeType Type;
            public HeartNodeBranch Branch;
            public HeartNodeRarity Rarity;
            public int MinimumDepth;
            public int MaximumDepth;
            public long BaseCost;
            public double CostGrowth;
            public string[] Tags;
            public string ConflictId;
            public HeartNodeEffect[] Effects;
        }

        [MenuItem("Window/DeadWalls/Rebuild Castle Heart Production Catalog")]
        public static void RebuildProductionCatalogAndBind()
        {
            HeartNodeCatalogSO catalog = EnsureProductionCatalog();
            bool sceneBound = BindActiveNewGameScene(catalog);
            bool presentationPolished = PolishHeartPresentation();
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(CatalogPath, ImportAssetOptions.ForceUpdate);

            Debug.Log(
                $"[CastleHeartCatalog] Production v{catalog.CatalogVersion} rebuilt with "
                + $"{catalog.Nodes.Length} canonical nodes. NewGameScene binding: {sceneBound}; "
                + $"presentation polish: {presentationPolished}.",
                catalog);
        }

        public static HeartNodeCatalogSO EnsureProductionCatalog()
        {
            EnsureFolder(NodeFolder);
            NodeSeed[] seeds = CreateSeeds();
            var nodes = new HeartNodeDefinitionSO[seeds.Length];
            for (int i = 0; i < seeds.Length; i++)
                nodes[i] = UpsertNode(seeds[i]);

            HeartNodeCatalogSO catalog = AssetDatabase.LoadAssetAtPath<HeartNodeCatalogSO>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<HeartNodeCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            Undo.RecordObject(catalog, "Rebuild Castle Heart Production Catalog");
            catalog.name = "HeartNodeCatalog";
            catalog.CatalogVersion = CatalogVersion;
            catalog.RootNodeId = HeartGraphConstants.RootNodeId;
            catalog.Nodes = nodes;
            EditorUtility.SetDirty(catalog);

            ValidateCatalogOrThrow(catalog);
            return catalog;
        }

        private static bool BindActiveNewGameScene(HeartNodeCatalogSO catalog)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != TargetScenePath)
                return false;

            GameManager gameManager = null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length && gameManager == null; i++)
                gameManager = roots[i].GetComponentInChildren<GameManager>(true);
            if (gameManager == null)
                throw new InvalidOperationException("NewGameScene icinde GameManager bulunamadi.");

            var serialized = new SerializedObject(gameManager);
            SerializedProperty property = serialized.FindProperty("heartCatalog");
            if (property == null)
                throw new InvalidOperationException("GameManager.heartCatalog serialized alani bulunamadi.");

            Undo.RecordObject(gameManager, "Bind Castle Heart Production Catalog");
            property.objectReferenceValue = catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gameManager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return true;
        }

        private static bool PolishHeartPresentation()
        {
            bool polished = false;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath) != null)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(HudPrefabPath);
                try
                {
                    HeartScreenUI heart = root.GetComponentInChildren<HeartScreenUI>(true);
                    if (heart != null)
                    {
                        ApplyPresentationTuning(heart);
                        PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
                        polished = true;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != TargetScenePath)
                return polished;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                HeartScreenUI heart = roots[i].GetComponentInChildren<HeartScreenUI>(true);
                if (heart == null)
                    continue;

                Undo.RecordObject(heart, "Polish Castle Heart Presentation");
                ApplyPresentationTuning(heart);
                EditorUtility.SetDirty(heart);
                polished = true;
                break;
            }

            if (polished)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            return polished;
        }

        private static void ApplyPresentationTuning(HeartScreenUI heart)
        {
            heart.NodeSize = new Vector2(292f, 188f);
            heart.HorizontalSpacing = 386f;
            heart.VerticalSpacing = 278f;
            heart.ContentPadding = new Vector2(190f, 160f);
        }

        private static HeartNodeDefinitionSO UpsertNode(NodeSeed seed)
        {
            string path = NodeFolder + "/" + seed.Branch + "_" + seed.Id + ".asset";
            HeartNodeDefinitionSO node = AssetDatabase.LoadAssetAtPath<HeartNodeDefinitionSO>(path);
            if (node == null)
            {
                node = ScriptableObject.CreateInstance<HeartNodeDefinitionSO>();
                AssetDatabase.CreateAsset(node, path);
            }

            Undo.RecordObject(node, "Migrate Castle Heart Node");
            node.name = seed.Branch + "_" + seed.Id;
            node.Id = seed.Id;
            node.Title = seed.Title;
            node.Description = seed.Description;
            node.Icon = LoadSprite(GetIconPath(seed));
            node.Tags = seed.Tags ?? Array.Empty<string>();
            node.LegacySourceNodeIds = GetLegacySourceNodeIds(seed.Id);
            node.Type = seed.Type;
            node.Branch = seed.Branch;
            node.Rarity = seed.Rarity;
            node.MinimumDepth = seed.MinimumDepth;
            node.MaximumDepth = seed.MaximumDepth;
            node.BaseGraveEssenceCost = seed.BaseCost;
            node.CostGrowthPerLevel = seed.CostGrowth;
            node.ConflictNodeIds = string.IsNullOrWhiteSpace(seed.ConflictId)
                ? Array.Empty<string>()
                : new[] { seed.ConflictId };
            node.Effects = seed.Effects ?? Array.Empty<HeartNodeEffect>();
            EditorUtility.SetDirty(node);
            return node;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite candidate)
                    return candidate;
            }
            return null;
        }

        private static string GetIconPath(NodeSeed seed)
        {
            switch (seed.Id)
            {
                case "rapid_archer_unlock":
                case "rapid_drill":
                case "storm_cadence":
                    return RapidIconPath;
                case "frost_archer_unlock":
                case "frostbite_tips":
                    return FrostIconPath;
                case "repair_efficiency":
                case "salvage_doctrine":
                    return RepairIconPath;
                case "arrow_vault":
                case "fletchers_measure":
                case "arrow_workshop":
                case "reserve_stacks":
                    return ArrowStockIconPath;
                case "stone_guild":
                    return StoneIconPath;
                case "iron_foundry":
                    return IronIconPath;
                case "harvest_ledger":
                    return FoodIconPath;
                case "worker_camp":
                case "dawn_housing":
                case "deep_stores":
                case "relentless_shifts":
                    return PopulationIconPath;
            }

            return seed.Branch switch
            {
                HeartNodeBranch.Army => ArmyIconPath,
                HeartNodeBranch.Defense => DefenseIconPath,
                HeartNodeBranch.Production => ProductionIconPath,
                HeartNodeBranch.HeartMagic => MagicIconPath,
                _ => string.Empty
            };
        }

        private static void ValidateCatalogOrThrow(HeartNodeCatalogSO catalog)
        {
            var errors = new List<string>();
            catalog.CollectValidationErrors(errors);
            if (errors.Count > 0)
                throw new InvalidOperationException("Castle Heart catalog invalid: " + string.Join(" | ", errors));

            uint[] auditSeeds = { 1u, 7u, 37u, 101u, 777u, 4099u };
            var settings = new HeartGraphRuntimeSettings();
            for (int i = 0; i < auditSeeds.Length; i++)
            {
                if (HeartGraphGenerator.TryGenerate(
                        settings.CreateRequest(catalog, auditSeeds[i]),
                        out _,
                        out HeartGraphGenerationReport report))
                {
                    continue;
                }

                throw new InvalidOperationException(
                    $"Castle Heart catalog seed {auditSeeds[i]} icin graph uretmedi: "
                    + string.Join(" | ", report.Errors));
            }
        }

        private static NodeSeed[] CreateSeeds()
        {
            return new[]
            {
                // ARMY - legacy archer fikirleri run-ici Grave Essence progression'a tasinir.
                Node("rapid_archer_unlock", "Rapid Doctrine",
                    "Unlock Rapid Archer recruitment for this run.",
                    HeartNodeType.Unlock, HeartNodeBranch.Army, 1, 2, 16, 0,
                    Tags(HeartGraphConstants.RapidGuaranteeTag), null,
                    Fx(HeartNodeEffectType.UnlockArcherType, archer: ArcherType.Rapid)),
                Node("frost_archer_unlock", "Winter Oath",
                    "Unlock Frost Archer recruitment for this run.",
                    HeartNodeType.Unlock, HeartNodeBranch.Army, 1, 3, 18, 0,
                    Tags(HeartGraphConstants.FrostGuaranteeTag), null,
                    Fx(HeartNodeEffectType.UnlockArcherType, archer: ArcherType.Frost)),
                Node("bow_mastery", "Bow Mastery",
                    "Endless drills keep Basic Archers relevant deep into the siege.",
                    HeartNodeType.Repeatable, HeartNodeBranch.Army, 2, 5, 15, 0.40,
                    Tags(HeartGraphConstants.RepeatableSinkTag), null,
                    Fx(HeartNodeEffectType.ModifyArcherDamagePercent, 0.06, ArcherType.Basic)),
                Node("volley_mastery", "Volley Mastery",
                    "Tighter release discipline raises Basic Archer fire rate with diminishing returns.",
                    HeartNodeType.Repeatable, HeartNodeBranch.Army, 1, 5, 16, 0.42,
                    null, null,
                    Fx(HeartNodeEffectType.ModifyArcherFireRatePercent, 0.06, ArcherType.Basic, softCap: 0.75)),
                Node("rapid_drill", "Clockwork Volley",
                    "Rapid Archers hold a faster, steadier cadence.",
                    HeartNodeType.Evolution, HeartNodeBranch.Army, 2, 5, 32, 0,
                    null, null,
                    Fx(HeartNodeEffectType.ModifyArcherFireRatePercent, 0.18, ArcherType.Rapid, softCap: 0.75)),
                Node("frostbite_tips", "Frostbite Tips",
                    "Frost arrows drive enemy movement closer to a frozen crawl.",
                    HeartNodeType.Evolution, HeartNodeBranch.Army, 2, 5, 34, 0,
                    null, null,
                    Fx(HeartNodeEffectType.ReduceFrostSlowMultiplier, 0.10, ArcherType.Frost, softCap: 0.35)),
                Node("longbow_geometry", "Longbow Geometry",
                    "Reworked limbs extend the Basic Archer engagement line.",
                    HeartNodeType.Evolution, HeartNodeBranch.Army, 2, 5, 30, 0,
                    null, null,
                    Fx(HeartNodeEffectType.AddArcherRange, 0.55, ArcherType.Basic, softCap: 3.0)),
                Node("heavy_draw", "Heavy Draw",
                    "Turn every Basic Archer volley into a crushing, deliberate strike.",
                    HeartNodeType.Keystone, HeartNodeBranch.Army, 3, 5, 48, 0,
                    null, "storm_cadence",
                    Fx(HeartNodeEffectType.ModifyArcherDamagePercent, 0.30, ArcherType.Basic)),
                Node("storm_cadence", "Storm Cadence",
                    "Drive Basic Archers toward relentless release speed.",
                    HeartNodeType.Keystone, HeartNodeBranch.Army, 3, 5, 48, 0,
                    null, "heavy_draw",
                    Fx(HeartNodeEffectType.ModifyArcherFireRatePercent, 0.28, ArcherType.Basic, softCap: 0.75)),

                // DEFENSE - tek Wall owner'i; Gate/Core/Moat yolu yoktur.
                Node("living_ramparts", "Living Ramparts",
                    "Awaken the Wall's buried stone memory and raise its maximum integrity.",
                    HeartNodeType.Unlock, HeartNodeBranch.Defense, 1, 2, 14, 0,
                    Tags(HeartGraphConstants.WallGuaranteeTag), null,
                    Fx(HeartNodeEffectType.ModifyWallMaxHpPercent, 0.12)),
                Node("stone_memory", "Stone Memory",
                    "Feed the Wall an endless sequence of stronger layers.",
                    HeartNodeType.Repeatable, HeartNodeBranch.Defense, 2, 5, 18, 0.45,
                    Tags(HeartGraphConstants.RepeatableSinkTag), null,
                    Fx(HeartNodeEffectType.ModifyWallMaxHpPercent, 0.07)),
                Node("repair_efficiency", "Measured Repairs",
                    "Salvage exact cuts and reduce every future Wall repair cost.",
                    HeartNodeType.Evolution, HeartNodeBranch.Defense, 2, 5, 30, 0,
                    null, null,
                    Fx(HeartNodeEffectType.ReduceWallRepairCostPercent, 0.15)),
                Node("layered_masonry", "Layered Masonry",
                    "Interlocked courses absorb impacts before cracks can spread.",
                    HeartNodeType.Evolution, HeartNodeBranch.Defense, 2, 5, 32, 0,
                    null, null,
                    Fx(HeartNodeEffectType.ModifyWallMaxHpPercent, 0.18)),
                Node("arrow_vault", "Arrow Vault",
                    "Open sealed Wall stores and expand the arrow reserve.",
                    HeartNodeType.Evolution, HeartNodeBranch.Defense, 1, 5, 28, 0,
                    null, null,
                    Fx(HeartNodeEffectType.IncreaseArrowCapacity, 80)),
                Node("fletchers_measure", "Fletcher's Measure",
                    "Standardized shafts produce more usable arrows from every Wood purchase.",
                    HeartNodeType.Evolution, HeartNodeBranch.Defense, 2, 5, 28, 0,
                    null, null,
                    Fx(HeartNodeEffectType.IncreaseArrowEfficiency, 4)),
                Node("bastion_doctrine", "Bastion Doctrine",
                    "Raise a fortress of sheer mass and maximum integrity.",
                    HeartNodeType.Keystone, HeartNodeBranch.Defense, 3, 5, 50, 0,
                    null, "salvage_doctrine",
                    Fx(HeartNodeEffectType.ModifyWallMaxHpPercent, 0.35)),
                Node("salvage_doctrine", "Salvage Doctrine",
                    "Standardize recovery around ruthless Stone efficiency.",
                    HeartNodeType.Keystone, HeartNodeBranch.Defense, 3, 5, 50, 0,
                    null, "bastion_doctrine",
                    Fx(HeartNodeEffectType.ReduceWallRepairCostPercent, 0.30)),

                // PRODUCTION - her resource ayri runtime target; surekli drain yaratmaz.
                Node("lumber_covenant", "Lumber Covenant",
                    "Refine Wood output without adding passive upkeep.",
                    HeartNodeType.Repeatable, HeartNodeBranch.Production, 2, 5, 12, 0.48,
                    Tags(HeartGraphConstants.RepeatableSinkTag), null,
                    Fx(HeartNodeEffectType.IncreaseResourceProductionPercent, 0.08,
                        resource: EconomyFocusType.Wood)),
                Node("stone_guild", "Stone Guild",
                    "Sharper routes raise Stone output per assigned worker.",
                    HeartNodeType.Repeatable, HeartNodeBranch.Production, 1, 5, 14, 0.50,
                    null, null,
                    Fx(HeartNodeEffectType.IncreaseResourceProductionPercent, 0.08,
                        resource: EconomyFocusType.Stone)),
                Node("iron_foundry", "Iron Foundry",
                    "Hotter furnaces raise Iron output per assigned worker.",
                    HeartNodeType.Repeatable, HeartNodeBranch.Production, 1, 5, 15, 0.52,
                    null, null,
                    Fx(HeartNodeEffectType.IncreaseResourceProductionPercent, 0.08,
                        resource: EconomyFocusType.Iron)),
                Node("harvest_ledger", "Harvest Ledger",
                    "Measured harvest cycles raise Food output per assigned worker.",
                    HeartNodeType.Repeatable, HeartNodeBranch.Production, 1, 5, 13, 0.49,
                    null, null,
                    Fx(HeartNodeEffectType.IncreaseResourceProductionPercent, 0.08,
                        resource: EconomyFocusType.Food)),
                Node("worker_camp", "Worker Quarters",
                    "Permanent stations expand every building's worker capacity for this run.",
                    HeartNodeType.Evolution, HeartNodeBranch.Production, 2, 5, 34, 0,
                    null, null,
                    Fx(HeartNodeEffectType.IncreaseWorkerCapacity, 3, resource: EconomyFocusType.Wood),
                    Fx(HeartNodeEffectType.IncreaseWorkerCapacity, 3, resource: EconomyFocusType.Stone),
                    Fx(HeartNodeEffectType.IncreaseWorkerCapacity, 3, resource: EconomyFocusType.Iron),
                    Fx(HeartNodeEffectType.IncreaseWorkerCapacity, 3, resource: EconomyFocusType.Food)),
                Node("dawn_housing", "Dawn Housing",
                    "Prepared shelter brings more survivors through each Dawn.",
                    HeartNodeType.Evolution, HeartNodeBranch.Production, 2, 5, 32, 0,
                    null, null,
                    Fx(HeartNodeEffectType.IncreasePopulationGrowth, 4)),
                Node("arrow_workshop", "Arrow Workshop",
                    "Dedicated jigs improve every Wood-to-arrow transaction.",
                    HeartNodeType.Evolution, HeartNodeBranch.Production, 2, 5, 27, 0,
                    null, null,
                    Fx(HeartNodeEffectType.IncreaseArrowEfficiency, 3)),
                Node("reserve_stacks", "Reserve Stacks",
                    "Dry storage makes room for a much larger arrow reserve.",
                    HeartNodeType.Evolution, HeartNodeBranch.Production, 2, 5, 29, 0,
                    null, null,
                    Fx(HeartNodeEffectType.IncreaseArrowCapacity, 100)),
                Node("deep_stores", "Deep Stores",
                    "Build broad reserves that expand every workshop's staffing limit.",
                    HeartNodeType.Keystone, HeartNodeBranch.Production, 3, 5, 52, 0,
                    null, "relentless_shifts",
                    Fx(HeartNodeEffectType.IncreaseWorkerCapacity, 6, resource: EconomyFocusType.Wood),
                    Fx(HeartNodeEffectType.IncreaseWorkerCapacity, 6, resource: EconomyFocusType.Stone),
                    Fx(HeartNodeEffectType.IncreaseWorkerCapacity, 6, resource: EconomyFocusType.Iron),
                    Fx(HeartNodeEffectType.IncreaseWorkerCapacity, 6, resource: EconomyFocusType.Food)),
                Node("relentless_shifts", "Relentless Shifts",
                    "Drive every staffed building toward concentrated output.",
                    HeartNodeType.Keystone, HeartNodeBranch.Production, 3, 5, 52, 0,
                    null, "deep_stores",
                    Fx(HeartNodeEffectType.IncreaseResourceProductionPercent, 0.20,
                        resource: EconomyFocusType.Wood),
                    Fx(HeartNodeEffectType.IncreaseResourceProductionPercent, 0.20,
                        resource: EconomyFocusType.Stone),
                    Fx(HeartNodeEffectType.IncreaseResourceProductionPercent, 0.20,
                        resource: EconomyFocusType.Iron),
                    Fx(HeartNodeEffectType.IncreaseResourceProductionPercent, 0.20,
                        resource: EconomyFocusType.Food)),

                // HEART / MAGIC - yalniz mevcut, gercek Fireball adapter'lari kullanilir.
                Node("fireball_unlock", "Ember Rite",
                    "Unlock Fireball and awaken the Heart's active battle magic.",
                    HeartNodeType.Unlock, HeartNodeBranch.HeartMagic, 1, 2, 20, 0,
                    Tags(HeartGraphConstants.FireballGuaranteeTag), null,
                    Fx(HeartNodeEffectType.UnlockSpellcasting)),
                Node("searing_flames", "Searing Flames",
                    "Feed the spell an endless reserve of raw damage.",
                    HeartNodeType.Repeatable, HeartNodeBranch.HeartMagic, 2, 5, 18, 0.50,
                    Tags(HeartGraphConstants.RepeatableSinkTag), null,
                    Fx(HeartNodeEffectType.ModifySpellDamagePercent, 0.10)),
                Node("greater_blast", "Greater Blast",
                    "A wider impact ring catches more of the advancing horde.",
                    HeartNodeType.Evolution, HeartNodeBranch.HeartMagic, 2, 5, 34, 0,
                    null, null,
                    Fx(HeartNodeEffectType.AddSpellRadius, 0.90, softCap: 3.5)),
                Node("arcane_focus", "Arcane Focus",
                    "Compress the ritual and return Fireball sooner.",
                    HeartNodeType.Evolution, HeartNodeBranch.HeartMagic, 2, 5, 36, 0,
                    null, null,
                    Fx(HeartNodeEffectType.ReduceSpellCooldownPercent, 0.18, softCap: 0.60)),
                Node("blazing_core", "Blazing Core",
                    "A denser core turns the first impact into a decisive strike.",
                    HeartNodeType.Evolution, HeartNodeBranch.HeartMagic, 2, 5, 38, 0,
                    null, null,
                    Fx(HeartNodeEffectType.ModifySpellDamagePercent, 0.35)),
                Node("ember_reservoir", "Ember Reservoir",
                    "Store a deeper charge and widen Fireball's final detonation.",
                    HeartNodeType.Evolution, HeartNodeBranch.HeartMagic, 1, 5, 31, 0,
                    null, null,
                    Fx(HeartNodeEffectType.AddSpellRadius, 0.65, softCap: 3.5)),
                Node("inferno_heart", "Inferno Heart",
                    "Shape Fireball around the largest possible first impact.",
                    HeartNodeType.Keystone, HeartNodeBranch.HeartMagic, 3, 5, 55, 0,
                    null, "chronomancer_heart",
                    Fx(HeartNodeEffectType.ModifySpellDamagePercent, 0.45)),
                Node("chronomancer_heart", "Chronomancer Heart",
                    "Shorten the ritual until Fireball cycles become relentless.",
                    HeartNodeType.Keystone, HeartNodeBranch.HeartMagic, 3, 5, 55, 0,
                    null, "inferno_heart",
                    Fx(HeartNodeEffectType.ReduceSpellCooldownPercent, 0.26, softCap: 0.60))
            };
        }

        private static string[] GetLegacySourceNodeIds(string heartNodeId)
        {
            return heartNodeId switch
            {
                "rapid_archer_unlock" => Legacy("rapid_archer"),
                "frost_archer_unlock" => Legacy("frost_archer"),
                "bow_mastery" => Legacy("bow_mastery"),
                "volley_mastery" => Legacy("volley_mastery"),
                "rapid_drill" => Legacy("rapid_volley"),
                "frostbite_tips" => Legacy("frost_arrows"),
                "heavy_draw" => Legacy("bow_training"),
                "living_ramparts" => Legacy("wall_reinforcement"),
                "repair_efficiency" => Legacy("repair_efficiency"),
                "layered_masonry" => Legacy("repair_crew"),
                "lumber_covenant" => Legacy("wood_camp"),
                "harvest_ledger" => Legacy("food_stores"),
                "worker_camp" => Legacy("worker_camp"),
                "dawn_housing" => Legacy("population_growth"),
                "fireball_unlock" => Legacy("arcane_tower"),
                "searing_flames" => Legacy("fire_power"),
                "greater_blast" => Legacy("fire_radius"),
                "arcane_focus" => Legacy("fire_cooldown"),
                _ => Array.Empty<string>()
            };
        }

        private static NodeSeed Node(
            string id,
            string title,
            string description,
            HeartNodeType type,
            HeartNodeBranch branch,
            int minimumDepth,
            int maximumDepth,
            long baseCost,
            double costGrowth,
            string[] tags,
            string conflictId,
            params HeartNodeEffect[] effects)
        {
            return new NodeSeed
            {
                Id = id,
                Title = title,
                Description = description,
                Type = type,
                Branch = branch,
                Rarity = type == HeartNodeType.Keystone || type == HeartNodeType.Evolution
                    ? HeartNodeRarity.Rare
                    : HeartNodeRarity.Standard,
                MinimumDepth = minimumDepth,
                MaximumDepth = maximumDepth,
                BaseCost = baseCost,
                CostGrowth = costGrowth,
                Tags = tags ?? Array.Empty<string>(),
                ConflictId = conflictId,
                Effects = effects ?? Array.Empty<HeartNodeEffect>()
            };
        }

        private static HeartNodeEffect Fx(
            HeartNodeEffectType type,
            double value = 0,
            ArcherType archer = default,
            EconomyFocusType resource = default,
            double softCap = 0)
        {
            return new HeartNodeEffect
            {
                Type = type,
                Value = value,
                ArcherType = archer,
                Resource = resource,
                SoftCap = softCap
            };
        }

        private static string[] Tags(params string[] values)
        {
            return values ?? Array.Empty<string>();
        }

        private static string[] Legacy(params string[] values)
        {
            return values ?? Array.Empty<string>();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int separator = path.LastIndexOf('/');
            if (separator <= 0 || separator >= path.Length - 1)
                throw new InvalidOperationException("Gecersiz asset folder path: " + path);
            string parent = path.Substring(0, separator);
            string name = path.Substring(separator + 1);
            EnsureFolder(parent);
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
