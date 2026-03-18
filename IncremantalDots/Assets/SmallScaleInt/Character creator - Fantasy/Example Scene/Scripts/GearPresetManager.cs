using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;            // for EditorUtility & AssetDatabase
#endif

namespace SmallScaleInc.CharacterCreatorFantasy
{
    public class GearPresetManager : MonoBehaviour
    {
        [Header("Database (persists across Play Mode)")]
        public GearPresetDatabase presetDatabase;

        [System.Serializable]
        public class GearPreset
        {
            public RuntimeAnimatorController bodyAnimator;
            public Color                   bodyColor;

            public RuntimeAnimatorController headAnimator;
            public Color headColor;

            public RuntimeAnimatorController chestAnimator;
            public Color chestColor;

            public RuntimeAnimatorController legsAnimator;
            public Color legsColor;

            public RuntimeAnimatorController shoesAnimator;
            public Color shoesColor;

            public RuntimeAnimatorController handsAnimator;
            public Color handsColor;

            public RuntimeAnimatorController beltAnimator;
            public Color beltColor;

            public RuntimeAnimatorController slashAnimator;
            public Color slashColor;

            public RuntimeAnimatorController effectAnimator;
            public Color effectColor;

            public RuntimeAnimatorController effect2Animator;
            public Color effect2Color;

            // visibility flags
            public bool bodyVisible;
            public bool headVisible;
            public bool chestVisible;
            public bool legsVisible;
            public bool shoesVisible;
            public bool handsVisible;
            public bool beltVisible;
            public bool slashVisible;
            public bool effectVisible;
            public bool effect2Visible;

            public string weaponName;
            public string backpackName;
            public string shieldName;

            public Color backpackColor;
            public Color shieldColor;
            public Color mountColor;
            public Color   weaponColor; 

            public string shadowOpacityName;

            // Post-processing:
            public bool   sliceSpritesheet;
            public bool   staticIdle;
            public string outlineName;     // stores either "OutlineBlack", "OutlineGradient" or "None"
            public string resolutionName;  // stores either "resolution64", "resolution128" or "None"
            public string frameName;

            public string mountName;
            public string idleName;

            public string presetName; //Name of the preset character
            public string presetDate; //Date of creation of the preset character

        }

        [Header("UI Toggles for Presets")]
        public Button presetButtonPrefab;  
        public Transform presetsToggleContainer;
        private readonly List<Button> presetButtons = new List<Button>();
        public TMP_InputField presetNameInput;

        public Toggle showExamplesToggle;



         [Header("Show/Hide Toggles (Body Parts)")]
        public Toggle bodyToggle;
        public Toggle headToggle;
        public Toggle chestToggle;
        public Toggle legsToggle;
        public Toggle shoesToggle;
        public Toggle handsToggle;
        public Toggle beltToggle;
        public Toggle slashDisplayToggle;
        public Toggle effectDisplayToggle;
        public Toggle effect2DisplayToggle;

        [Header("Save Preset Button")]
        public Button savePresetButton;

        // --- Gear Slot References ---
        [Header("Animators")]
        public Animator bodyAnimator, headAnimator, chestAnimator, legsAnimator, shoesAnimator;
        public Animator handsAnimator, beltAnimator, slashAnimator, effectAnimator, effect2Animator;

        [Header("Renderers")]
        public SpriteRenderer bodyRenderer, headRenderer, chestRenderer, legsRenderer, shoesRenderer;
        public SpriteRenderer handsRenderer, beltRenderer, slashRenderer, effectRenderer, effect2Renderer;

        [Header("Weapon/Backpack/Shield Toggles")]
        public Toggle[] weaponToggles;
        public Toggle[] backpackToggles;
        public Toggle[] shieldToggles;

        [Header("Gear Objects (with SpriteRenderer)")]
        public GameObject[] weaponObjects;    
        public GameObject[] backpackObjects;
        public GameObject[] shieldObjects;
        public GameObject[] mountObjects;
        
        [Header("Shadow Opacity Toggles")]
        public Toggle[] shadowToggles;

        [Header("Frames Toggles (always one on)")]
        public Toggle[] frameToggles;  // assign your 7 frame-count toggles here

        [Header("Post-Processing Toggles")]
        public Toggle   sliceSpritesheetToggle;
        public Toggle   staticIdleToggle;
        public Toggle[] outlineToggles;     // must contain your two outline toggles, named accordingly
        public Toggle[] resolutionToggles;  // must contain your two resolution toggles, named accordingly

        [Header("Mount Toggles")]
        public Toggle[] mountToggles;   // assign your mount-selection toggles here

        [Header("Idle Toggles")]
        public Toggle[] idleToggles;  


        void Awake()
        {
            // Spawn one toggle per saved preset
            for (int i = 0; i < presetDatabase.presets.Count; i++)
                CreateButtonForPreset(i);
            if (showExamplesToggle != null)
               showExamplesToggle.onValueChanged.AddListener(_ => RefreshPresetButtons());

        }

        void Start()
        {
            savePresetButton?.onClick.AddListener(SaveCurrentAsPreset);
        }

        public void SaveCurrentAsPreset()
        {
            // 1) Grab the names & visibilities first
            string shieldName    = GetActiveToggleName(shieldToggles);
            string weaponName    = GetActiveToggleName(weaponToggles);
            string backpackName  = GetActiveToggleName(backpackToggles);
            string mountName     = GetActiveToggleName(mountToggles);
            string idleName      = GetActiveToggleName(idleToggles);
            string shadowName    = GetActiveToggleName(shadowToggles);
            string frameName     = GetActiveToggleName(frameToggles);
            string outlineName   = GetActiveToggleName(outlineToggles);
            string resolutionName= GetActiveToggleName(resolutionToggles);
            string name = string.IsNullOrWhiteSpace(presetNameInput.text) ? "Character" : presetNameInput.text.Trim();
            string date = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

            bool sliceSS         = sliceSpritesheetToggle.isOn;
            bool staticIdle      = staticIdleToggle.isOn;
            bool bodyVis         = bodyToggle.isOn;
            bool headVis         = headToggle.isOn;
            bool chestVis        = chestToggle.isOn;
            bool legsVis         = legsToggle.isOn;
            bool shoesVis        = shoesToggle.isOn;
            bool handsVis        = handsToggle.isOn;
            bool beltVis         = beltToggle.isOn;
            bool slashVis        = slashDisplayToggle.isOn;
            bool effectVis       = effectDisplayToggle.isOn;
            bool effect2Vis       = effect2DisplayToggle.isOn;

            // 2) Build the preset
            var p = new GearPreset
            {
                // Animators & base‐colors
                bodyAnimator    = bodyAnimator.runtimeAnimatorController,
                bodyColor       = bodyRenderer.color,

                headAnimator    = headAnimator.runtimeAnimatorController,
                headColor       = headRenderer.color,

                chestAnimator   = chestAnimator.runtimeAnimatorController,
                chestColor      = chestRenderer.color,

                legsAnimator    = legsAnimator.runtimeAnimatorController,
                legsColor       = legsRenderer.color,

                shoesAnimator   = shoesAnimator.runtimeAnimatorController,
                shoesColor      = shoesRenderer.color,

                handsAnimator   = handsAnimator.runtimeAnimatorController,
                handsColor      = handsRenderer.color,

                beltAnimator    = beltAnimator.runtimeAnimatorController,
                beltColor       = beltRenderer.color,

                slashAnimator   = slashAnimator.runtimeAnimatorController,
                effectAnimator  = effectAnimator.runtimeAnimatorController,
                effect2Animator  = effect2Animator.runtimeAnimatorController,
                // Visibility
                bodyVisible     = bodyVis,
                headVisible     = headVis,
                chestVisible    = chestVis,
                legsVisible     = legsVis,
                shoesVisible    = shoesVis,
                handsVisible    = handsVis,
                beltVisible     = beltVis,
                slashVisible    = slashVis,
                effectVisible   = effectVis,
                effect2Visible   = effect2Vis,

                // Toggle‐based names
                shieldName        = shieldName,
                weaponName        = weaponName,
                backpackName      = backpackName,
                mountName         = mountName,
                idleName          = idleName,
                shadowOpacityName = shadowName,
                frameName         = frameName,

                // Post‐processing
                sliceSpritesheet  = sliceSS,
                staticIdle        = staticIdle,
                outlineName       = outlineName,
                resolutionName    = resolutionName,

                // Character name and date
                presetName = name,
                presetDate = date
            };

            // 3) Now read all of the actual SpriteRenderer colors…

            // Shield color
            var shGO = FindObjectByName(shieldObjects, shieldName);
            p.shieldColor = shGO != null
                ? shGO.GetComponent<SpriteRenderer>().color
                : Color.white;

            // Weapon color
            var wGO = FindObjectByName(weaponObjects, weaponName);
            p.weaponColor = wGO != null
                ? wGO.GetComponent<SpriteRenderer>().color
                : Color.white;

            // Slash color
            p.slashColor = slashRenderer.color;

            // Effect color
            p.effectColor = effectRenderer.color;

            // Effect color
            p.effect2Color = effect2Renderer.color;

            // Backpack color
            var bpGO = FindObjectByName(backpackObjects, backpackName);
            p.backpackColor = bpGO != null
                ? bpGO.GetComponent<SpriteRenderer>().color
                : Color.white;


            // Mount color
            var mtGO = FindObjectByName(mountObjects, mountName);
            p.mountColor = mtGO != null
                ? mtGO.GetComponent<SpriteRenderer>().color
                : Color.white;


            // 4) Persist & UI
            presetDatabase.presets.Add(p);
            RefreshPresetButtons();

            #if UNITY_EDITOR
            EditorUtility.SetDirty(presetDatabase);
            AssetDatabase.SaveAssets();
            #endif
        }



        public void ApplyPreset(int i)
        {
            if (i < 0 || i >= presetDatabase.presets.Count) return;
            var p = presetDatabase.presets[i];

            // --- BODY (includes skin) ---
            bodyToggle.isOn = p.bodyVisible;
            if (p.bodyVisible)
            {
                bodyAnimator.runtimeAnimatorController = p.bodyAnimator;
                bodyRenderer.color                   = p.bodyColor;
            }

            // --- HEAD ---
            headToggle.isOn = p.headVisible;
            if (p.headVisible)
            {
                headAnimator.runtimeAnimatorController = p.headAnimator;
                headRenderer.color                    = p.headColor;
            }

            // --- CHEST ---
            chestToggle.isOn = p.chestVisible;
            if (p.chestVisible)
            {
                chestAnimator.runtimeAnimatorController = p.chestAnimator;
                chestRenderer.color                    = p.chestColor;
            }

            // --- LEGS ---
            legsToggle.isOn = p.legsVisible;
            if (p.legsVisible)
            {
                legsAnimator.runtimeAnimatorController = p.legsAnimator;
                legsRenderer.color                    = p.legsColor;
            }

            // --- SHOES ---
            shoesToggle.isOn = p.shoesVisible;
            if (p.shoesVisible)
            {
                shoesAnimator.runtimeAnimatorController = p.shoesAnimator;
                shoesRenderer.color                    = p.shoesColor;
            }

            // --- HANDS ---
            handsToggle.isOn = p.handsVisible;
            if (p.handsVisible)
            {
                handsAnimator.runtimeAnimatorController = p.handsAnimator;
                handsRenderer.color                    = p.handsColor;
            }

            // --- BELT ---
            beltToggle.isOn = p.beltVisible;
            if (p.beltVisible)
            {
                beltAnimator.runtimeAnimatorController = p.beltAnimator;
                beltRenderer.color                    = p.beltColor;
            }

            // --- SLASH EFFECT ---
            slashDisplayToggle.isOn = p.slashVisible;
            if (p.slashVisible)
            {
                slashAnimator.runtimeAnimatorController = p.slashAnimator;
                slashRenderer.color                    = p.slashColor;
            }

            // --- MAGIC EFFECT ---
            effectDisplayToggle.isOn = p.effectVisible;
            if (p.effectVisible)
            {
                effectAnimator.runtimeAnimatorController = p.effectAnimator;
                effectRenderer.color                    = p.effectColor;
            }

            // --- MAGIC EFFECT 2---
            effect2DisplayToggle.isOn = p.effect2Visible;
            if (p.effect2Visible)
            {
                effect2Animator.runtimeAnimatorController = p.effect2Animator;
                effect2Renderer.color                    = p.effect2Color;
            }

            // shadow opacity
            SetToggleByName(shadowToggles, p.shadowOpacityName);

            // --- POST-PROCESSING ---
            sliceSpritesheetToggle.isOn = p.sliceSpritesheet;
            staticIdleToggle.isOn       = p.staticIdle;

            // Outline: could be "OutlineBlack", "OutlineGradient" or "None"
            if (p.outlineName == "None")
            {
                // turn *all* outline toggles off
                foreach (var tog in outlineToggles)
                    tog.isOn = false;
            }
            else
            {
                // turn only the matching one on
                SetToggleByName(outlineToggles, p.outlineName);
            }

            SetToggleByName(resolutionToggles, p.resolutionName);
            SetToggleByName(frameToggles, p.frameName);

            // --- MOUNT & IDLE (exactly like shadow/frames) ---
                        // Mount
            SetToggleByName(mountToggles, p.mountName);
            var mt = FindObjectByName(mountObjects, p.mountName);
            if (mt) mt.GetComponent<SpriteRenderer>().color = p.mountColor;

            SetToggleByName(idleToggles,  p.idleName);

            // --- WEAPON / BACKPACK / SHIELD ---
            // Shield
            SetToggleByName(shieldToggles, p.shieldName);
            var sh = FindObjectByName(shieldObjects, p.shieldName);
            if (sh) sh.GetComponent<SpriteRenderer>().color = p.shieldColor;

            SetToggleByName(weaponToggles, p.weaponName);
            var w = FindObjectByName(weaponObjects, p.weaponName);
            if (w) w.GetComponent<SpriteRenderer>().color = p.weaponColor;
                        // Backpack
            SetToggleByName(backpackToggles, p.backpackName);
            var bp = FindObjectByName(backpackObjects, p.backpackName);
            if (bp) bp.GetComponent<SpriteRenderer>().color = p.backpackColor;

        }


        /// <summary>
        /// Spawns one preset-button in your UI list and wires it to ApplyPreset(index).
        /// </summary>
        private void CreateButtonForPreset(int index)
        {
            var btn = Instantiate(presetButtonPrefab, presetsToggleContainer);
            var preset = presetDatabase.presets[index];

            // Update name and date
            var nameText = btn.transform.Find("PresetName")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
                nameText.SetText(preset.presetName);

            var dateText = btn.transform.Find("PresetDate")?.GetComponent<TextMeshProUGUI>();
            if (dateText != null)
                dateText.SetText(preset.presetDate);

            // ✅ Find DeleteButton recursively and hook up delete logic
            Button[] allButtons = btn.GetComponentsInChildren<Button>(true);
            foreach (var b in allButtons)
            {
                if (b.name == "DeleteButton")
                {
                    int idx = index; // Capture the index in the closure
                    b.onClick.AddListener(() => DeletePreset(idx));
                    break;
                }
            }

            // Hook up apply logic
            int applyIndex = index;
            btn.onClick.AddListener(() => ApplyPreset(applyIndex));

            presetButtons.Add(btn);
        }


        private void DeletePreset(int index)
        {
            if (index < 0 || index >= presetDatabase.presets.Count) return;

            presetDatabase.presets.RemoveAt(index);

            // Destroy the UI button and refresh all
            foreach (var btn in presetButtons)
                Destroy(btn.gameObject);
            presetButtons.Clear();

            for (int i = 0; i < presetDatabase.presets.Count; i++)
                RefreshPresetButtons();


        #if UNITY_EDITOR
            EditorUtility.SetDirty(presetDatabase);
            AssetDatabase.SaveAssets();
        #endif
        }



        /// <summary>
        /// Returns the GameObject.name of the first Toggle that’s on, or "None".
        /// </summary>
        private string GetActiveToggleName(Toggle[] arr)
        {
            foreach (var tog in arr)
                if (tog.isOn)
                    return tog.gameObject.name;
            return "None";
        }

        /// <summary>
        /// Finds the Toggle whose GameObject.name == name and turns it on (off all others).
        /// Falls back to "None" toggle, then to the first in the array.
        /// </summary>
        private void SetToggleByName(Toggle[] arr, string name)
        {
            bool found = false;
            // 1) exact match
            foreach (var tog in arr)
            {
                if (tog.gameObject.name == name)
                {
                    tog.isOn = true;
                    found = true;
                }
                else
                {
                    tog.isOn = false;
                }
            }
            if (found) return;

            // 2) "None"
            foreach (var tog in arr)
            {
                if (tog.gameObject.name == "None")
                {
                    tog.isOn = true;
                    found = true;
                }
                else
                {
                    tog.isOn = false;
                }
            }
            if (found) return;

            // 3) fallback
            if (arr.Length > 0)
            {
                arr[0].isOn = true;
                Debug.LogWarning($"Could not find toggle '{name}' or 'None'; defaulting to '{arr[0].gameObject.name}'.");
            }
        }

        private GameObject FindObjectByName(GameObject[] arr, string name)
        {
            foreach (var go in arr)
                if (go != null && go.name == name)
                    return go;
            return null;
        }

        private void RefreshPresetButtons()
        {
            // Clear all existing buttons
            foreach (var btn in presetButtons)
                Destroy(btn.gameObject);
            presetButtons.Clear();

            // Re-create only those that should be shown
            for (int i = 0; i < presetDatabase.presets.Count; i++)
            {
                var preset = presetDatabase.presets[i];

                // Skip if it's an Example and we're hiding them
                if (!showExamplesToggle.isOn && preset.presetDate.StartsWith("Example"))
                    continue;

                CreateButtonForPreset(i);
            }
        }


    }
}
