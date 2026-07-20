using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DeadWalls.Editor
{
    public sealed class DeadWallsAudioDirectorWindow : EditorWindow
    {
        private const string GamemasterRoot =
            "Assets/Sounds/Gamemaster Audio - Pro Sound Collection v1.3 - 16bit 48k";
        private const string BiugRoot = "Assets/Sounds/Biug Multi Genre Musics";
        private const string ExistingMagicRoot = "Assets/RPG Magic Sound Effects Pack 3 [ELEMENTAL]";
        private const string ExistingUiRoot = "Assets/Fantasy UI SFX - Lite Edition";

        private DeadWallsAudioProfileSO _profile;
        private SerializedObject _serializedProfile;
        private Vector2 _scroll;
        private readonly List<string> _missingPaths = new List<string>();

        [MenuItem("Tools/Dead Walls/Audio Director")]
        public static void Open()
        {
            GetWindow<DeadWallsAudioDirectorWindow>("Audio Director");
        }

        [MenuItem("Tools/Dead Walls/Audio/Install Curated Profile")]
        public static void InstallCuratedProfileMenu()
        {
            DeadWallsAudioProfileSO profile = LoadOrCreateDefaultProfile();
            ApplyCuratedDefaults(profile, true);
            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
        }

        [MenuItem("Tools/Dead Walls/Audio/Preview Soul Arrival")]
        public static void PreviewSoulArrivalMenu()
        {
            DeadWallsAudioProfileSO profile = LoadOrCreateDefaultProfile();
            AudioPreview.Play(profile != null ? profile.SoulArrivalClip : null);
        }

        [MenuItem("Tools/Dead Walls/Audio/Stop Preview")]
        public static void StopPreviewMenu()
        {
            AudioPreview.StopAll();
        }

        private void OnEnable()
        {
            _profile = LoadOrCreateDefaultProfile();
            BindSerializedProfile();
        }

        private void OnDisable()
        {
            AudioPreview.StopAll();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("DEAD WALLS AUDIO DIRECTOR", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Tek merkezi profil runtime seslerini yonetir. Ability ve music candidate "
                + "alanlari audition icindir; acikca route edilene kadar gameplay'i degistirmez. "
                + "Zombie death sesi tasarim geregi profilde bulunmaz.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            _profile = (DeadWallsAudioProfileSO)EditorGUILayout.ObjectField(
                "Active Profile",
                _profile,
                typeof(DeadWallsAudioProfileSO),
                false);
            if (EditorGUI.EndChangeCheck())
                BindSerializedProfile();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("LOAD CURATED DEFAULTS", GUILayout.Height(28f)))
                {
                    if (EditorUtility.DisplayDialog(
                            "Load curated defaults?",
                            "Profile fields will be replaced with the reviewed Dead Walls shortlist.",
                            "Load",
                            "Cancel"))
                    {
                        ApplyCuratedDefaults(_profile, true);
                        BindSerializedProfile();
                    }
                }

                if (GUILayout.Button("STOP PREVIEW", GUILayout.Height(28f)))
                    AudioPreview.StopAll();

                if (GUILayout.Button("PING PROFILE", GUILayout.Height(28f)))
                {
                    Selection.activeObject = _profile;
                    EditorGUIUtility.PingObject(_profile);
                }
            }

            if (_profile == null || _serializedProfile == null)
            {
                EditorGUILayout.HelpBox("Audio profile could not be created.", MessageType.Error);
                return;
            }

            _serializedProfile.Update();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawSection("RUNTIME ROUTING");
            DrawProperty("OverrideCombat");
            DrawProperty("OverrideInterface");
            DrawProperty("OverrideCastleHeart");
            DrawProperty("OverrideAmbience");
            DrawProperty("OverrideMenuMusic");
            DrawProperty("EnableCurrencyArrival");

            DrawSection("COMBAT");
            DrawClipArray("ArrowShootClips", "Arrow shoot family");
            DrawClip("ArrowHitClip", "Arrow impact");
            DrawClip("FrostHitClip", "Frost impact");
            DrawClipArray("WallHitClips", "Wall impact family");
            DrawClip("FireballBlastClip", "Fireball blast");

            DrawSection("ABILITY CANDIDATES - AUDITION ONLY");
            DrawClip("FireballCastCandidate", "Fireball cast");
            DrawClip("FireballBurnTailCandidate", "Fireball burn tail");
            DrawClip("EmergencyRepairCandidate", "Emergency repair");
            DrawClip("RallyCandidate", "Rally");

            DrawSection("INTERFACE");
            DrawClip("UiClickClip", "Click");
            DrawClip("UiSuccessClip", "Confirm");
            DrawClip("UiFailClip", "Denied");
            DrawClip("DeathStingClip", "Game over sting");
            DrawProperty("UiClickVolume");
            DrawProperty("UiSuccessVolume");
            DrawProperty("UiFailVolume");
            DrawProperty("DeathStingVolume");

            DrawSection("CASTLE HEART");
            DrawClip("HeartResearchClip", "Research");
            DrawClip("HeartRevealClip", "Reveal");
            DrawClip("HeartDeniedClip", "Denied");
            DrawClip("HeartPanelOpenClip", "Panel open");
            DrawProperty("HeartResearchVolume");
            DrawProperty("HeartRevealVolume");
            DrawProperty("HeartDeniedVolume");
            DrawProperty("HeartPanelOpenVolume");

            DrawSection("CURRENCY ARRIVAL");
            DrawClip("SoulArrivalClip", "Soul reaches HUD");
            DrawClip("EssenceArrivalClip", "Essence reaches HUD");
            DrawProperty("SoulArrivalVolume");
            DrawProperty("EssenceArrivalVolume");
            DrawProperty("CurrencyArrivalMinInterval");
            DrawProperty("CurrencyAmountVolumeGain");
            DrawProperty("CurrencyAmountPitchGain");

            DrawSection("AMBIENCE");
            DrawClip("NightLoop", "Night loop");
            DrawClip("DuskRiser", "Dusk riser");
            DrawClip("DawnCue", "Dawn cue");
            DrawClip("NightHordeLoop", "Night horde bed");
            DrawClipArray("WorkerFoleyClips", "Worker foley family");

            DrawSection("MUSIC CANDIDATES");
            DrawClip("MenuMusic", "Main menu / Castle Heart");
            DrawClip("DayMusicCandidate", "Day candidate");
            DrawClip("NightMusicCandidate", "Night candidate");
            DrawClip("IntenseNightMusicCandidate", "Intense night candidate");

            EditorGUILayout.EndScrollView();

            if (_serializedProfile.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(_profile);
                AssetDatabase.SaveAssets();
            }
        }

        private void BindSerializedProfile()
        {
            _serializedProfile = _profile != null ? new SerializedObject(_profile) : null;
        }

        private void DrawSection(string title)
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);
        }

        private void DrawProperty(string propertyName)
        {
            SerializedProperty property = _serializedProfile.FindProperty(propertyName);
            if (property != null)
                EditorGUILayout.PropertyField(property);
        }

        private void DrawClip(string propertyName, string label)
        {
            SerializedProperty property = _serializedProfile.FindProperty(propertyName);
            if (property == null)
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label));
                using (new EditorGUI.DisabledScope(property.objectReferenceValue == null))
                {
                    if (GUILayout.Button("PLAY", GUILayout.Width(52f)))
                        AudioPreview.Play(property.objectReferenceValue as AudioClip);
                }
            }
        }

        private void DrawClipArray(string propertyName, string label)
        {
            SerializedProperty property = _serializedProfile.FindProperty(propertyName);
            if (property == null)
                return;

            property.isExpanded = EditorGUILayout.Foldout(
                property.isExpanded,
                $"{label} ({property.arraySize})",
                true);
            if (!property.isExpanded)
                return;

            EditorGUI.indentLevel++;
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(element, new GUIContent($"Clip {i + 1}"));
                    using (new EditorGUI.DisabledScope(element.objectReferenceValue == null))
                    {
                        if (GUILayout.Button("PLAY", GUILayout.Width(52f)))
                            AudioPreview.Play(element.objectReferenceValue as AudioClip);
                    }
                    if (GUILayout.Button("-", GUILayout.Width(24f)))
                    {
                        int previousSize = property.arraySize;
                        property.DeleteArrayElementAtIndex(i);
                        if (property.arraySize == previousSize)
                            property.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
            }
            if (GUILayout.Button("+ ADD CLIP"))
            {
                property.InsertArrayElementAtIndex(property.arraySize);
                property.GetArrayElementAtIndex(property.arraySize - 1).objectReferenceValue = null;
            }
            EditorGUI.indentLevel--;
        }

        private static DeadWallsAudioProfileSO LoadOrCreateDefaultProfile()
        {
            DeadWallsAudioProfileSO profile = AssetDatabase.LoadAssetAtPath<DeadWallsAudioProfileSO>(
                DeadWallsAudioProfileSO.DefaultAssetPath);
            if (profile != null)
                return profile;

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            profile = CreateInstance<DeadWallsAudioProfileSO>();
            AssetDatabase.CreateAsset(profile, DeadWallsAudioProfileSO.DefaultAssetPath);
            AssetDatabase.SaveAssets();
            DeadWallsAudioProfileSO.ResetDefaultCache();
            return profile;
        }

        private static void ApplyCuratedDefaults(
            DeadWallsAudioProfileSO profile,
            bool logResult)
        {
            if (profile == null)
                return;

            Undo.RecordObject(profile, "Load Dead Walls Curated Audio");
            var missing = new List<string>();

            profile.OverrideCombat = true;
            profile.OverrideInterface = true;
            profile.OverrideCastleHeart = true;
            profile.OverrideAmbience = false;
            profile.OverrideMenuMusic = false;
            profile.EnableCurrencyArrival = true;

            profile.ArrowShootClips = LoadMany(missing,
                $"{GamemasterRoot}/Guns_Weapons/Bow_Arrow/bow_crossbow_arrow_shoot_type1_02.wav",
                $"{GamemasterRoot}/Guns_Weapons/Bow_Arrow/bow_crossbow_arrow_shoot_type1_03.wav",
                $"{GamemasterRoot}/Guns_Weapons/Bow_Arrow/bow_crossbow_arrow_shoot_type1_04.wav",
                $"{GamemasterRoot}/Guns_Weapons/Bow_Arrow/bow_crossbow_arrow_shoot_type1_07.wav");
            profile.ArrowHitClip = Load(missing,
                $"{ExistingMagicRoot}/Generic Magic and Impacts/RPG3_GenericArrow_Impact01.wav");
            profile.FrostHitClip = Load(missing,
                $"{ExistingMagicRoot}/Ice Magic/RPG3_IceMagic2_IceBreak01.wav");
            profile.WallHitClips = LoadMany(missing,
                $"{GamemasterRoot}/Impacts_Smashable/rock_impact_small_hit_01.wav",
                $"{GamemasterRoot}/Impacts_Smashable/rock_impact_small_hit_02.wav",
                $"{GamemasterRoot}/Impacts_Smashable/rock_impact_small_hit_03.wav");
            profile.FireballBlastClip = Load(missing,
                $"{GamemasterRoot}/Magic_Spells/fireball_blast_projectile_spell_03.wav");

            profile.FireballCastCandidate = Load(missing,
                $"{GamemasterRoot}/Magic_Spells/fireball_conjure_03.wav");
            profile.FireballBurnTailCandidate = Load(missing,
                $"{GamemasterRoot}/Magic_Spells/fireball_impact_burn_02.wav");
            profile.EmergencyRepairCandidate = Load(missing,
                $"{GamemasterRoot}/Magic_Spells/healing_magic_spell_03.wav");
            profile.RallyCandidate = Load(missing,
                $"{GamemasterRoot}/Voice/Human Male C/voice_male_c_battle_shout_charge_01.wav");

            profile.UiClickClip = Load(missing,
                $"{GamemasterRoot}/User_Interface_Menu/ui_button_simple_click_01.wav");
            profile.UiSuccessClip = Load(missing,
                $"{GamemasterRoot}/User_Interface_Menu/ui_menu_button_confirm_02.wav");
            profile.UiFailClip = Load(missing,
                $"{GamemasterRoot}/User_Interface_Menu/ui_menu_button_error_03.wav");
            profile.DeathStingClip = Load(missing,
                $"{ExistingMagicRoot}/Generic Magic and Impacts/RPG3_GenericMisc_LowBoom01.wav");

            profile.HeartResearchClip = Load(missing,
                $"{GamemasterRoot}/Magic_Spells/magic_general_item_collect_03.wav");
            profile.HeartRevealClip = Load(missing,
                $"{ExistingUiRoot}/Magical Texture Chimes 1-1.wav");
            profile.HeartDeniedClip = Load(missing,
                $"{ExistingUiRoot}/Key & Lock 1-1.wav");
            profile.HeartPanelOpenClip = Load(missing,
                $"{ExistingUiRoot}/Book Page 1-2.wav");

            profile.SoulArrivalClip = Load(missing,
                $"{GamemasterRoot}/Collectibles_Items_Powerup/collect_item_sparkle_pop_10.wav");
            profile.EssenceArrivalClip = Load(missing,
                $"{GamemasterRoot}/Magic_Spells/chimes_magic_bell_ding_5.wav");

            profile.NightLoop = Load(missing,
                $"{ExistingMagicRoot}/Wind Magic/RPG3_WindMagic_Drone01_LowSubtleLoop.wav");
            profile.DuskRiser = Load(missing,
                $"{ExistingMagicRoot}/Wind Magic/RPG3_WindMagicEpic_Cast01_P1.wav");
            profile.DawnCue = Load(missing,
                $"{ExistingMagicRoot}/Wind Magic/RPG3_WindMagic_Buff03v2_Shorter.wav");
            profile.NightHordeLoop = Load(missing,
                $"{ExistingMagicRoot}/Wind Magic/RPG3_WindMagic_Drone01_DarkWindLoop.wav");
            profile.WorkerFoleyClips = LoadMany(missing,
                $"{ExistingUiRoot}/Sawing Wood 1-1.wav",
                $"{ExistingUiRoot}/Nail Wood 1-1.wav",
                $"{ExistingUiRoot}/Blacksmithing 2-2.wav",
                $"{ExistingUiRoot}/Rock Impact 37.wav");

            profile.MenuMusic = Load(missing,
                $"{BiugRoot}/Dark Medieval Music Pack/Dark Medieval Theme #5 (Looped).wav");
            profile.DayMusicCandidate = Load(missing,
                $"{BiugRoot}/Dark Medieval Music Pack/Dark Medieval Theme #3 (Looped).wav");
            profile.NightMusicCandidate = Load(missing,
                $"{BiugRoot}/Action Combat Orchestral Music Pack/Action Combat Orchestral Theme #5 (Looped).wav");
            profile.IntenseNightMusicCandidate = Load(missing,
                $"{BiugRoot}/Action Combat Orchestral Music Pack/Action Combat Orchestral Theme #4 (Looped).wav");

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            DeadWallsAudioProfileSO.ResetDefaultCache();

            if (!logResult)
                return;

            if (missing.Count == 0)
                Debug.Log("Dead Walls curated audio profile installed. Zombie death audio remains disabled by design.");
            else
                Debug.LogError("Dead Walls audio profile has missing clips:\n" + string.Join("\n", missing));
        }

        private static AudioClip Load(List<string> missing, string path)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
                missing.Add(path);
            return clip;
        }

        private static AudioClip[] LoadMany(List<string> missing, params string[] paths)
        {
            var clips = new AudioClip[paths.Length];
            for (int i = 0; i < paths.Length; i++)
                clips[i] = Load(missing, paths[i]);
            return clips;
        }

        private static class AudioPreview
        {
            private static readonly Type AudioUtilType =
                typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            private static MethodInfo _playMethod;
            private static MethodInfo _stopMethod;

            public static void Play(AudioClip clip)
            {
                if (clip == null || AudioUtilType == null)
                    return;

                StopAll();
                _playMethod ??= FindMethod("PlayPreviewClip", "PlayClip");
                if (_playMethod == null)
                    return;

                ParameterInfo[] parameters = _playMethod.GetParameters();
                var args = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    Type type = parameters[i].ParameterType;
                    args[i] = type == typeof(AudioClip)
                        ? clip
                        : type == typeof(int)
                            ? 0
                            : type == typeof(bool)
                                ? false
                                : type.IsValueType
                                    ? Activator.CreateInstance(type)
                                    : null;
                }
                _playMethod.Invoke(null, args);
            }

            public static void StopAll()
            {
                if (AudioUtilType == null)
                    return;
                _stopMethod ??= FindMethod("StopAllPreviewClips", "StopAllClips");
                _stopMethod?.Invoke(null, Array.Empty<object>());
            }

            private static MethodInfo FindMethod(params string[] names)
            {
                MethodInfo[] methods = AudioUtilType.GetMethods(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
                {
                    for (int methodIndex = 0; methodIndex < methods.Length; methodIndex++)
                    {
                        if (methods[methodIndex].Name == names[nameIndex])
                            return methods[methodIndex];
                    }
                }
                return null;
            }
        }
    }
}
