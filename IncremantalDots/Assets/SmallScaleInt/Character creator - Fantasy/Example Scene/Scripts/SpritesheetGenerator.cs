
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.U2D.Sprites;
#endif

namespace SmallScaleInc.CharacterCreatorFantasy
{
    public class SpritesheetGenerator : MonoBehaviour
    {
        private string parentFolder = "SmallScaleInt/Character creator - Fantasy/Spritesheets";

        private Dictionary<TextMeshProUGUI, string> lastTMPValues = new Dictionary<TextMeshProUGUI, string>();
        private Dictionary<TextMeshProUGUI, Coroutine> runningAnimations = new Dictionary<TextMeshProUGUI, Coroutine>();
        private Dictionary<TextMeshProUGUI, Vector3> originalScales = new Dictionary<TextMeshProUGUI, Vector3>();


        // --- Gear Slot References (Shoes, Chest, belt, hands, Legs, Head) ---
        [Header("Gear Animators")]
        public Animator shoesAnimator;
        public Animator chestAnimator;
        public Animator beltAnimator;
        public Animator handsAnimator;
        public Animator legsAnimator;
        public Animator headAnimator;
        
        [Header("Gear Sprite Renderers")]
        public SpriteRenderer shoesRenderer;
        public SpriteRenderer chestRenderer;
        public SpriteRenderer beltRenderer;
        public SpriteRenderer handsRenderer;
        public SpriteRenderer legsRenderer;
        public SpriteRenderer headRenderer;

        [Header("Gear UI TMP (Names)")]
        public TextMeshProUGUI shoesGearNameText;
        public TextMeshProUGUI chestGearNameText;
        public TextMeshProUGUI beltGearNameText;
        public TextMeshProUGUI handsGearNameText;
        public TextMeshProUGUI legsGearNameText;
        public TextMeshProUGUI headGearNameText;

        [Header("Right panel Gear Names")] //This get auto updated according to the above names.
        public TextMeshProUGUI shoesGearNameText2;
        public TextMeshProUGUI chestGearNameText2;
        public TextMeshProUGUI beltGearNameText2;
        public TextMeshProUGUI handsGearNameText2;
        public TextMeshProUGUI legsGearNameText2;
        public TextMeshProUGUI headGearNameText2;
        // public TextMeshProUGUI skinNameText2;
        public TextMeshProUGUI slashNameText2;
        public TextMeshProUGUI effectNameText2;
        public TextMeshProUGUI effect2NameText2;
        
        [Header("Gear UI TMP (Colors)")]
        public TextMeshProUGUI shoesColorText;
        public TextMeshProUGUI chestColorText;
        public TextMeshProUGUI beltColorText;
        public TextMeshProUGUI handsColorText;
        public TextMeshProUGUI legsColorText;
        public TextMeshProUGUI headColorText;

        // --- Weapon Slot Section ---
        [System.Serializable]
        public class Weapon
        {
            public GameObject weaponGO;            // The weapon GameObject.
            public Animator animator;              // Weapon animator.
            public SpriteRenderer spriteRenderer;  // Weapon sprite renderer.
        }
        
        [Header("Weapon Setup")]
        public Weapon[] weapons;  // Array of all available weapons.
        
        [Header("Weapon UI TMP")]
        public TextMeshProUGUI weaponNameText;
        public TextMeshProUGUI weaponColorText;
        
        // --- Backpack Slot Section ---
        [System.Serializable]
        public class Backpack
        {
            public GameObject backpackGO;            // The backpack GameObject.
            public Animator animator;                // Backpack animator.
            public SpriteRenderer spriteRenderer;    // Backpack sprite renderer.
        }
        
        [Header("Backpack Setup")]
        public Backpack[] backpacks;  // Array of all available backpacks.
        
        [Header("Backpack UI TMP")]
        public TextMeshProUGUI backpackNameText;
        public TextMeshProUGUI backpackColorText;
        
        // --- Shield Slot Section ---
        [System.Serializable]
        public class Shield
        {
            public GameObject shieldGO;            // The shield GameObject.
            public Animator animator;              // Shield animator.
            public SpriteRenderer spriteRenderer;  // Shield sprite renderer.
        }
        
        [Header("Shield Setup")]
        public Shield[] shields;  // Array of all available shields.
        
        [Header("Shield UI TMP")]
        public TextMeshProUGUI shieldNameText;
        public TextMeshProUGUI shieldColorText;

        // --- Mount Slot Section ---
        [System.Serializable]
        public class Mount
        {
            public GameObject mountGO;            // The mount GameObject.
            public Animator animator;             // Mount animator.
            public SpriteRenderer spriteRenderer; // Mount sprite renderer.
        }

        [Header("Mount Setup")]
        public Mount[] mounts;  // Array of all available mounts.

        [Header("Mount UI TMP")]
        public TextMeshProUGUI mountNameText;
        public TextMeshProUGUI mountColorText;

        [Header("Skin Color Setup")]
        public SpriteRenderer skinColorRenderer;   // The skin's SpriteRenderer.
        public TextMeshProUGUI skinColorText;        // TMP to display the skin color hex.
        public TextMeshProUGUI skinNameText;
        public Animator skinAnimator;
        public Toggle skinToggle; //If false, no skin will be visable or included in the generated spritesheets. 

        [Header("Shadow Setup")]
        public Animator shadowAnimator;
        public SpriteRenderer shadowRenderer;
        public TextMeshProUGUI shadowGearNameText;
        public TextMeshProUGUI shadowColorText;

        [Header("Slash Setup")]
        public Animator slashAnimator;
        public SpriteRenderer slashRenderer;
        public TextMeshProUGUI slashNameText;
        public TextMeshProUGUI slashColorText;

        // New toggle to enable/disable slash display. if true the slash 
        // will be included in the generated spritesheets and extra spritesheets will be generated.
        public Toggle slashToggle;

        [Header("Effect Setup")]
        public Animator effectAnimator;
        public SpriteRenderer effectRenderer;
        public TextMeshProUGUI effectNameText;
        public TextMeshProUGUI effectColorText;

        public Animator effect2Animator;
        public SpriteRenderer effect2Renderer;
        public TextMeshProUGUI effect2NameText;
        public TextMeshProUGUI effect2ColorText;

        public Toggle effectToggle;
        public Toggle effect2Toggle;

        [Header("Load Screen UI")]
        public GameObject loadScreenPanel;
        public Slider loadProgressSlider;
        public TextMeshProUGUI currentlyGeneratingTMP;

        public Toggle sliceSpritesheets; // If true, slice the generated spritesheets

        public Toggle staticIdleAnimation; // If on, idle animations will be made static.
        public Toggle maxFramesToggle;   // 15 frames (default)
        public Toggle fourteenFramesToggle;   // 14 frames
        public Toggle twelveFramesToggle;   // 12 frames
        public Toggle tenFramesToggle;   // 10 frames
        public Toggle eightFramesToggle; // 8 frames
        public Toggle sixFramesToggle; // 6 frames
        public Toggle fourFramesToggle;  // 4 frames

        [Header("Outline")]
        public Toggle outlineToggle;  // If on, draw a 1px black outline around the character
        public Toggle gradientOutlineToggle;

        [Header("Glow Settings")]
        public Toggle glowOutlineToggle;
        public Slider glowThicknessSlider;
        public TextMeshProUGUI glowThicknessLabel;
        public int glowThickness = 4; // default


        public TextMeshProUGUI outlineColorText; //Outline color
        

        [Header("Outline Gradient brightness = 0 = pure inverted colour; 1 = pure white")]
        public float gradientBrightness = 0.2f;

        public Toggle use128Toggle;
        public Toggle use64Toggle;
        public Toggle exportTilesetToggle;
        public Toggle flatColorToggle;


        [Header("Character name input")]
        public TMP_InputField characterNameInput;  // this is what the character will be named.

        [Header("Animation Toggles")]
        public Toggle attack1Toggle;
        public Toggle attack2Toggle;
        public Toggle attack3Toggle;
        public Toggle attack4Toggle;
        public Toggle attack5Toggle;

        public Toggle attackRunToggle;
        public Toggle attackRun2Toggle;
        public Toggle special1Toggle;

        public Toggle crouchIdleToggle;
        public Toggle crouchRunToggle;
        public Toggle dieToggle;
        public Toggle idleToggle;
        public Toggle idle2Toggle;
        public Toggle idle3Toggle;
        public Toggle idle4Toggle;

        public Toggle runToggle;
        public Toggle runBackwardsToggle;
        public Toggle strafeLeftToggle;
        public Toggle strafeRightToggle;
        public Toggle walkToggle;
        public Toggle rideIdleToggle;
        public Toggle rideRunToggle;

        public Toggle takeDamageToggle;
        public Toggle tauntToggle;

        [Header("Animator Generation")]
        public Toggle createAnimatorToggle;
        public AnimatorClipBuilder animatorClipBuilder; // assign in inspector




        void Awake()
        {
            CacheInitialTMPValues();
            UpdateGearUI();
            if (glowThicknessSlider != null)
            {
                glowThicknessSlider.minValue = 1;
                glowThicknessSlider.maxValue = 20;
                glowThicknessSlider.value = glowThickness;

                UpdateGlowThicknessUI(glowThicknessSlider.value);
                glowThicknessSlider.onValueChanged.AddListener(UpdateGlowThicknessUI);
            }
        }

        void Update()
        {
            UpdateGearUI();
        }

  private void CacheInitialTMPValues()
        {
            lastTMPValues[shoesGearNameText] = shoesGearNameText.text;
            lastTMPValues[chestGearNameText] = chestGearNameText.text;
            lastTMPValues[beltGearNameText] = beltGearNameText.text;
            lastTMPValues[handsGearNameText] = handsGearNameText.text;
            lastTMPValues[legsGearNameText] = legsGearNameText.text;
            lastTMPValues[headGearNameText] = headGearNameText.text;
            lastTMPValues[shoesColorText] = shoesColorText.text;
            lastTMPValues[chestColorText] = chestColorText.text;
            lastTMPValues[handsColorText] = handsColorText.text;
            lastTMPValues[beltColorText] = beltColorText.text;
            lastTMPValues[legsColorText] = legsColorText.text;
            lastTMPValues[headColorText] = headColorText.text;
            lastTMPValues[shadowGearNameText] = shadowGearNameText.text;
            lastTMPValues[shadowColorText] = shadowColorText.text;
            lastTMPValues[slashNameText] = slashNameText.text;
            lastTMPValues[slashColorText] = slashColorText.text;
            lastTMPValues[effectNameText] = effectNameText.text;
            lastTMPValues[effectColorText] = effectColorText.text;
            lastTMPValues[weaponNameText] = weaponNameText.text;
            lastTMPValues[weaponColorText] = weaponColorText.text;
            lastTMPValues[backpackNameText] = backpackNameText.text;
            lastTMPValues[backpackColorText] = backpackColorText.text;
            lastTMPValues[shieldNameText] = shieldNameText.text;
            lastTMPValues[shieldColorText] = shieldColorText.text;
            lastTMPValues[mountNameText] = mountNameText.text;
            lastTMPValues[mountColorText] = mountColorText.text;
            lastTMPValues[skinColorText] = skinColorText.text;
            lastTMPValues[skinNameText] = skinNameText.text;
        }

        private void AnimateTMPChange(TextMeshProUGUI tmp)
        {
            if (!originalScales.ContainsKey(tmp))
                originalScales[tmp] = tmp.transform.localScale;

            if (runningAnimations.ContainsKey(tmp) && runningAnimations[tmp] != null)
                StopCoroutine(runningAnimations[tmp]);

            runningAnimations[tmp] = StartCoroutine(AnimateTMP(tmp));
        }


        private IEnumerator AnimateTMP(TextMeshProUGUI tmp)
        {
            Color originalColor = Color.white;
            if (tmp.color != Color.yellow)
                originalColor = tmp.color;

            Vector3 cachedScale = originalScales[tmp];

            tmp.color = Color.yellow;
            tmp.transform.localScale = cachedScale * 1.2f;

            yield return new WaitForSeconds(0.1f);

            tmp.color = originalColor;
            tmp.transform.localScale = cachedScale;
        }


        private void SetTMPWithChangeDetection(TextMeshProUGUI tmp, string newValue)
        {
            if (!lastTMPValues.ContainsKey(tmp))
                lastTMPValues[tmp] = tmp.text;

            if (lastTMPValues[tmp] != newValue)
            {
                tmp.text = newValue;
                tmp.color = Color.white; // ✅ Reset color BEFORE animating
                AnimateTMPChange(tmp);
                lastTMPValues[tmp] = newValue;
            }
            else
            {
                tmp.text = newValue;
            }
        }



        /// <summary>
        /// Updates the UI for the gear slots (Shoes, Chest, hands, belt, Legs, Head)
        /// by retrieving each part’s animator name and sprite color.
        /// </summary>
    public void UpdateGearUI()
        {
            if (shoesAnimator != null)
                SetTMPWithChangeDetection(shoesGearNameText, GetAnimatorName(shoesAnimator));
            if (shoesRenderer != null)
                SetTMPWithChangeDetection(shoesColorText, GetColorHex(shoesRenderer.color));
            if (!shoesRenderer.enabled)
            {
                SetTMPWithChangeDetection(shoesGearNameText, "None");
                SetTMPWithChangeDetection(shoesColorText, "#000000");
            }
            if (!skinColorRenderer.enabled)
            {
                SetTMPWithChangeDetection(skinColorText, "None");
                SetTMPWithChangeDetection(skinNameText, "#000000");
            }


            if (chestAnimator != null)
                SetTMPWithChangeDetection(chestGearNameText, GetAnimatorName(chestAnimator));
            if (chestRenderer != null)
                SetTMPWithChangeDetection(chestColorText, GetColorHex(chestRenderer.color));
            if (!chestRenderer.enabled)
            {
                SetTMPWithChangeDetection(chestGearNameText, "None");
                SetTMPWithChangeDetection(chestColorText, "#000000");
            }

            if (beltAnimator != null)
                SetTMPWithChangeDetection(beltGearNameText, GetAnimatorName(beltAnimator));
            if (beltRenderer != null)
                SetTMPWithChangeDetection(beltColorText, GetColorHex(beltRenderer.color));
            if (!beltRenderer.enabled)
            {
                SetTMPWithChangeDetection(beltGearNameText, "None");
                SetTMPWithChangeDetection(beltColorText, "#000000");
            }

            if (handsAnimator != null)
                SetTMPWithChangeDetection(handsGearNameText, GetAnimatorName(handsAnimator));
            if (handsRenderer != null)
                SetTMPWithChangeDetection(handsColorText, GetColorHex(handsRenderer.color));
            if (!handsRenderer.enabled)
            {
                SetTMPWithChangeDetection(handsGearNameText, "None");
                SetTMPWithChangeDetection(handsColorText, "#000000");
            }

            if (legsAnimator != null)
                SetTMPWithChangeDetection(legsGearNameText, GetAnimatorName(legsAnimator));
            if (legsRenderer != null)
                SetTMPWithChangeDetection(legsColorText, GetColorHex(legsRenderer.color));
            if (!legsRenderer.enabled)
            {
                SetTMPWithChangeDetection(legsGearNameText, "None");
                SetTMPWithChangeDetection(legsColorText, "#000000");
            }

            if (headAnimator != null)
                SetTMPWithChangeDetection(headGearNameText, GetAnimatorName(headAnimator));
            if (headRenderer != null)
                SetTMPWithChangeDetection(headColorText, GetColorHex(headRenderer.color));
            if (!headRenderer.enabled)
            {
                SetTMPWithChangeDetection(headGearNameText, "None");
                SetTMPWithChangeDetection(headColorText, "#000000");
            }

            if (shadowAnimator != null)
                SetTMPWithChangeDetection(shadowGearNameText, GetAnimatorName(shadowAnimator));
            if (shadowRenderer != null)
                SetTMPWithChangeDetection(shadowColorText, GetAlphaValue(shadowRenderer.color));
            if (!shadowRenderer.enabled)
            {
                SetTMPWithChangeDetection(shadowGearNameText, "None");
                SetTMPWithChangeDetection(shadowColorText, "0%");
            }

            if (slashToggle != null && !slashToggle.isOn)
            {
                SetTMPWithChangeDetection(slashNameText, "None");
                SetTMPWithChangeDetection(slashColorText, "#FFFFFF");
            }
            else
            {
                if (slashAnimator != null)
                    SetTMPWithChangeDetection(slashNameText, GetAnimatorName(slashAnimator));
                if (slashRenderer != null)
                    SetTMPWithChangeDetection(slashColorText, GetColorHex(slashRenderer.color));
                if (slashRenderer != null && !slashRenderer.enabled)
                {
                    SetTMPWithChangeDetection(slashNameText, "None");
                    SetTMPWithChangeDetection(slashColorText, "#000000");
                }
            }

            if (effectToggle != null && !effectToggle.isOn)
            {
                SetTMPWithChangeDetection(effectNameText, "None");
                SetTMPWithChangeDetection(effectColorText, "#FFFFFF");
            }
            else
            {
                if (effectAnimator != null)
                    SetTMPWithChangeDetection(effectNameText, GetAnimatorName(effectAnimator));
                if (effectRenderer != null)
                    SetTMPWithChangeDetection(effectColorText, GetColorHex(effectRenderer.color));
                if (effectRenderer != null && !effectRenderer.enabled)
                {
                    SetTMPWithChangeDetection(effectNameText, "None");
                    SetTMPWithChangeDetection(effectColorText, "#000000");
                }
            }
            if (effect2Toggle != null && !effect2Toggle.isOn)
            {
                SetTMPWithChangeDetection(effect2NameText, "None");
                SetTMPWithChangeDetection(effect2ColorText, "#FFFFFF");
            }
            else
            {
                if (effect2Animator != null)
                    SetTMPWithChangeDetection(effect2NameText, GetAnimatorName(effect2Animator));
                if (effect2Renderer != null)
                    SetTMPWithChangeDetection(effect2ColorText, GetColorHex(effect2Renderer.color));
                if (effect2Renderer != null && !effect2Renderer.enabled)
                {
                    SetTMPWithChangeDetection(effect2NameText, "None");
                    SetTMPWithChangeDetection(effect2ColorText, "#000000");
                }
            }
            UpdateShieldUI();
            UpdateWeaponUI();
            UpdateBackpackUI();
            UpdateMountUI();
            UpdateSkinColorUI();

            //Update left side fields
            shoesGearNameText2.text = shoesGearNameText.text;
            chestGearNameText2.text = chestGearNameText.text;
            beltGearNameText2.text  = beltGearNameText.text;
            handsGearNameText2.text = handsGearNameText.text;
            legsGearNameText2.text  = legsGearNameText.text;
            headGearNameText2.text  = headGearNameText.text;
            // new ones
            // if (skinNameText2   != null && skinNameText   != null) skinNameText2.text   = skinNameText.text;
            if (slashNameText2  != null && slashNameText  != null) slashNameText2.text  = slashNameText.text;
            if (effectNameText2 != null && effectNameText != null) effectNameText2.text = effectNameText.text;
            if (effect2NameText2 != null && effect2NameText != null) effect2NameText2.text = effect2NameText.text;
        }

        /// <summary>
        /// Updates the weapon UI by checking which weapon's SpriteRenderer is enabled.
        /// The enabled weapon is considered the active weapon.
        /// </summary>
        private void UpdateWeaponUI()
        {
            Weapon activeWeapon = GetActiveWeapon();
            if (activeWeapon != null)
            {
                SetTMPWithChangeDetection(weaponNameText, GetWeaponName(activeWeapon));
                SetTMPWithChangeDetection(weaponColorText, GetColorHex(activeWeapon.spriteRenderer.color));
            }
            else
            {
                SetTMPWithChangeDetection(weaponNameText, "None");
                SetTMPWithChangeDetection(weaponColorText, "#000000");
            }
        }


        /// <summary>
        /// Iterates through the weapons array to find the weapon whose SpriteRenderer is enabled.
        /// </summary>
        /// <returns>The active Weapon if found; otherwise, null.</returns>
        private Weapon GetActiveWeapon()
        {
            foreach (var weapon in weapons)
            {
                if (weapon.spriteRenderer != null && weapon.spriteRenderer.enabled)
                {
                    return weapon;
                }
            }
            return null;
        }

        /// <summary>
        /// Updates the backpack UI by checking which backpack's SpriteRenderer is enabled.
        /// The enabled backpack is considered the active backpack.
        /// </summary>
        private void UpdateBackpackUI()
        {
            Backpack activeBackpack = GetActiveBackpack();
            if (activeBackpack != null)
            {
                SetTMPWithChangeDetection(backpackNameText, GetItemName(activeBackpack.animator, activeBackpack.backpackGO));
                SetTMPWithChangeDetection(backpackColorText, GetColorHex(activeBackpack.spriteRenderer.color));
            }
            else
            {
                SetTMPWithChangeDetection(backpackNameText, "None");
                SetTMPWithChangeDetection(backpackColorText, "#000000");
            }
        }


        /// <summary>
        /// Iterates through the backpacks array to find the backpack whose SpriteRenderer is enabled.
        /// </summary>
        /// <returns>The active Backpack if found; otherwise, null.</returns>
        private Backpack GetActiveBackpack()
        {
            foreach (var backpack in backpacks)
            {
                if (backpack.spriteRenderer != null && backpack.spriteRenderer.enabled)
                {
                    return backpack;
                }
            }
            return null;
        }

        /// <summary>
        /// Updates the shield UI by checking which shield's SpriteRenderer is enabled.
        /// The enabled shield is considered the active shield.
        /// </summary>
        private void UpdateShieldUI()
        {
            Shield activeShield = GetActiveShield();
            if (activeShield != null)
            {
                SetTMPWithChangeDetection(shieldNameText, GetItemName(activeShield.animator, activeShield.shieldGO));
                SetTMPWithChangeDetection(shieldColorText, GetColorHex(activeShield.spriteRenderer.color));
            }
            else
            {
                SetTMPWithChangeDetection(shieldNameText, "None");
                SetTMPWithChangeDetection(shieldColorText, "#000000");
            }
        }


        /// <summary>
        /// Iterates through the shields array to find the shield whose SpriteRenderer is enabled.
        /// </summary>
        /// <returns>The active Shield if found; otherwise, null.</returns>
        private Shield GetActiveShield()
        {
            foreach (var shield in shields)
            {
                if (shield.shieldGO.activeInHierarchy &&
                    shield.animator != null &&
                    shield.animator.enabled &&
                    shield.spriteRenderer != null &&
                    shield.spriteRenderer.enabled &&
                    shield.animator.runtimeAnimatorController != null)
                {
                    return shield;
                }
            }
            return null;
        }


        /// <summary>
        /// Updates the mount UI by checking which mount's SpriteRenderer is enabled.
        /// The enabled mount is considered the active mount.
        /// </summary>
        private void UpdateMountUI()
        {
            Mount activeMount = GetActiveMount();
            if (activeMount != null)
            {
                SetTMPWithChangeDetection(mountNameText, GetItemName(activeMount.animator, activeMount.mountGO));
                SetTMPWithChangeDetection(mountColorText, GetColorHex(activeMount.spriteRenderer.color));
            }
            else
            {
                SetTMPWithChangeDetection(mountNameText, "None");
                SetTMPWithChangeDetection(mountColorText, "#000000");
            }
        }


        /// <summary>
        /// Iterates through the mounts array to find the mount whose SpriteRenderer is enabled.
        /// </summary>
        /// <returns>The active Mount if found; otherwise, null.</returns>
        private Mount GetActiveMount()
        {
            foreach (var mount in mounts)
            {
                if (mount.spriteRenderer != null && mount.spriteRenderer.enabled)
                {
                    return mount;
                }
            }
            return null;
        }


        private void UpdateSkinColorUI()
        {
            if (skinColorRenderer != null)
            {
                SetTMPWithChangeDetection(skinColorText, GetColorHex(skinColorRenderer.color));
                SetTMPWithChangeDetection(skinNameText, GetAnimatorName(skinAnimator));
            }
            else
            {
                SetTMPWithChangeDetection(skinColorText, "#000000");
            }
        }


        /// <summary>
        /// Retrieves the animator's name based on its runtimeAnimatorController.
        /// Falls back to the animator's GameObject name if not available.
        /// </summary>
        /// <param name="animator">The animator to query.</param>
        /// <returns>The name from the animator or its runtime controller.</returns>
        private string GetAnimatorName(Animator animator)
        {
            if (animator.runtimeAnimatorController != null)
            {
                string name = animator.runtimeAnimatorController.name;
                return name.EndsWith("Controller") ? name.Replace("Controller", "") : name;
            }
            return animator.name;
        }

        
        /// <summary>
        /// Retrieves the weapon's name using its animator or GameObject.
        /// </summary>
        /// <param name="weapon">The weapon to query.</param>
        /// <returns>The name of the weapon.</returns>
        private string GetWeaponName(Weapon weapon)
        {
            if (weapon.animator != null && weapon.animator.runtimeAnimatorController != null)
            {
                string name = weapon.animator.runtimeAnimatorController.name;
                return name.EndsWith("Controller") ? name.Replace("Controller", "") : name;
            }
            return weapon.weaponGO.name;
        }

        
        /// <summary>
        /// Retrieves the item name (for backpacks or shields) using its animator or GameObject.
        /// </summary>
        /// <param name="animator">The animator to query.</param>
        /// <param name="itemGO">The item GameObject.</param>
        /// <returns>The name of the item.</returns>
        private string GetItemName(Animator animator, GameObject itemGO)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                string name = animator.runtimeAnimatorController.name;
                return name.EndsWith("Controller") ? name.Replace("Controller", "") : name;
            }
            return itemGO.name;
        }

        
        /// <summary>
        /// Converts a Unity Color to a hexadecimal string (e.g., #RRGGBB).
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>A hex string representing the color.</returns>
        private string GetColorHex(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }

        private string GetAlphaValue(Color color)
        {
            float alphaPercent = color.a * 100f;
            return alphaPercent.ToString("F0") + "%";
        }

        private void UpdateGlowThicknessUI(float value)
        {
            glowThickness = Mathf.RoundToInt(value);

            if (glowThicknessLabel != null)
                glowThicknessLabel.text = "" + glowThickness;
        }




    /// <summary>
    /// Combines individual spritesheets for each animation into new, composite spritesheets.
    /// It assumes a folder structure under "Spritesheets" where each gear item has a folder named
    /// exactly as its stored name (from the UI) and that folder contains the 15 spritesheets.
    /// The base layer ("NakedBody") is always used (and tinted by skin color).
    /// The layering order is:
    ///  0: NakedBody (with skin color), 1: Shoes, 2: Legs, 3: Chest, 4: belt, 5: hands 6: Shield, 7: Weapon, 8: Backpack, 9: Head.
    /// The resulting combined spritesheets are saved into a new folder so as not to overwrite any originals.
    /// </summary>

    // Row policy: for rows 0..7 (i.e., 1..8), true => Weapon is AFTER Shield (in front).
    [SerializeField]
    private bool[] weaponAfterShieldRows = new bool[8] {
        true, true, true,  // rows 1,2,3 -> Weapon after Shield
        false, false, false, false, // rows 4,5,6, 7 -> Shield after Weapon
        true          // rows 8 -> Weapon after Shield
    };

    // true => Backpack is on TOP of BOTH shield & weapon on that row.
    // You wanted rows 1–5 behind (false), rows 6–8 in front (true).
    [SerializeField]
    private bool[] backpackInFrontRows = new bool[8] {
        true, false, false, false, false,   // rows 1..5 → backpack behind both
        true,  true,  true                   // rows 6..8 → backpack in front of both
    };



      public void StartCombineSpritesheets()
    {
        StartCoroutine(CombineCharacterSpritesheetsCoroutine());
    }

    /// <summary>
    /// Combines individual spritesheets for each animation into new composite spritesheets.
    /// Layering order:
    /// 0: Shadow (tinted by its alpha),
    /// 1: NakedBody (tinted by skin color; skipped if skinToggle is off),
    /// 2: Shoes,
    /// 3: Legs,
    /// 4: Chest,
    /// 5: belt,
    /// 6: hands,
    /// 7: Shield,
    /// 8: Weapon,
    /// 9: Backpack,
    /// 10: Head.
    /// 11: Slash.
    /// 12: Effect.
    /// </summary>
private IEnumerator CombineCharacterSpritesheetsCoroutine()
{
    string rootPath = Application.isEditor
    ? Application.dataPath
    : Application.persistentDataPath;
    
    // Activate load screen and reset progress.
    if (loadScreenPanel != null)
    {
        loadScreenPanel.SetActive(true);
        if (loadProgressSlider != null)
            loadProgressSlider.value = 0f;
    }
    if (currentlyGeneratingTMP != null)
        currentlyGeneratingTMP.text = "Starting spritesheet generation...";

    // Define standard animation names.
    List<string> animList = new List<string>();

    if (attack1Toggle != null && attack1Toggle.isOn) animList.Add("Attack1");
    if (attack2Toggle != null && attack2Toggle.isOn) animList.Add("Attack2");
    if (attack3Toggle != null && attack3Toggle.isOn) animList.Add("Attack3");
    if (attack4Toggle != null && attack4Toggle.isOn) animList.Add("Attack4");
    if (attack5Toggle != null && attack5Toggle.isOn) animList.Add("Attack5");

    if (attackRunToggle != null && attackRunToggle.isOn) animList.Add("AttackRun");
    if (attackRun2Toggle != null && attackRun2Toggle.isOn) animList.Add("AttackRun2");
    if (special1Toggle != null && special1Toggle.isOn) animList.Add("Special1");

    if (crouchIdleToggle != null && crouchIdleToggle.isOn) animList.Add("CrouchIdle");
    if (crouchRunToggle != null && crouchRunToggle.isOn) animList.Add("CrouchRun");
    if (dieToggle != null && dieToggle.isOn) animList.Add("Die");
    if (idleToggle != null && idleToggle.isOn) animList.Add("Idle");
    if (idle2Toggle != null && idle2Toggle.isOn) animList.Add("Idle2");
    if (idle3Toggle != null && idle3Toggle.isOn) animList.Add("Idle3");
    if (idle4Toggle != null && idle4Toggle.isOn) animList.Add("Idle4");

    if (runToggle != null && runToggle.isOn) animList.Add("Run");
    if (runBackwardsToggle != null && runBackwardsToggle.isOn) animList.Add("RunBackwards");
    if (strafeLeftToggle != null && strafeLeftToggle.isOn) animList.Add("StrafeLeft");
    if (strafeRightToggle != null && strafeRightToggle.isOn) animList.Add("StrafeRight");
    if (walkToggle != null && walkToggle.isOn) animList.Add("Walk");
    if (rideIdleToggle != null && rideIdleToggle.isOn) animList.Add("RideIdle");
    if (rideRunToggle != null && rideRunToggle.isOn) animList.Add("RideRun");

    if (takeDamageToggle != null && takeDamageToggle.isOn) animList.Add("TakeDamage");
    if (tauntToggle != null && tauntToggle.isOn) animList.Add("Taunt");

    string[] animations = animList.ToArray(); 

    // Get folder names from UI.
    string shoesFolder    = shoesGearNameText.text;
    string legsFolder     = legsGearNameText.text;
    string chestFolder    = chestGearNameText.text;
    string beltFolder    = beltGearNameText.text;
    string handsFolder    = handsGearNameText.text;
    string headFolder     = headGearNameText.text;
    string weaponFolder   = weaponNameText.text;
    string backpackFolder = backpackNameText.text;
    string shieldFolder   = shieldNameText.text;
    string mountFolder    = mountNameText.text; 
    string nakedBodyFolder = skinNameText.text;
    string shadowFolder   = shadowGearNameText.text;
    string slashFolder = slashNameText.text;
    string effectFolder = effectNameText.text;
    string effect2Folder = effect2NameText.text;

    // Setup paths…
    // In editor: use the editable asset folder
    #if UNITY_EDITOR
    parentFolder = Path.Combine(Application.dataPath, "SmallScaleInt/Character creator - Fantasy/Spritesheets");

    // In builds: use raw PNGs from StreamingAssets
    #else
    parentFolder = Path.Combine(Application.streamingAssetsPath, "Spritesheets");
    #endif

    string outputParent;

    #if UNITY_EDITOR
    outputParent = Path.Combine(Application.dataPath, "SmallScaleInt/Character creator - Fantasy/Created Spritesheets");
    #elif UNITY_STANDALONE
    outputParent = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Created Spritesheets");
    #else
    outputParent = Path.Combine(Application.persistentDataPath, "Created Spritesheets");
    #endif

    if (!Directory.Exists(outputParent))
        Directory.CreateDirectory(outputParent);

    //Use input field for character name
    string userInputName = "Character";  // default fallback
    if (characterNameInput != null && !string.IsNullOrWhiteSpace(characterNameInput.text))
    {
        userInputName = characterNameInput.text.Trim();
    }

    // Clean illegal file characters (just in case)
    foreach (char c in Path.GetInvalidFileNameChars())
        userInputName = userInputName.Replace(c, '_');

    // Append date to name
    string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
    string configFolder = Path.Combine(outputParent, userInputName + "_" + timestamp);

    Directory.CreateDirectory(configFolder);


    // slash setup…
    bool includeSlash = (slashToggle != null && slashToggle.isOn &&
                           !weaponNameText.text.StartsWith("Test") &&
                           !weaponNameText.text.StartsWith("Special2") &&
                           !weaponNameText.text.StartsWith("None"));
    Color slashTint = includeSlash ? ParseColorFromTMP(slashColorText.text) : Color.white;

    // effect setup…
    bool includeEffect = (effectToggle != null && effectToggle.isOn &&
                           !weaponNameText.text.StartsWith("Test") &&
                           !weaponNameText.text.StartsWith("Special2") &&
                           !weaponNameText.text.StartsWith("None"));
    Color effectTint = includeEffect ? ParseColorFromTMP(effectColorText.text) : Color.white;

    bool includeEffect2 = (effect2Toggle != null && effect2Toggle.isOn &&
                       !weaponNameText.text.StartsWith("Test") &&
                       !weaponNameText.text.StartsWith("Special2") &&
                       !weaponNameText.text.StartsWith("None"));
    Color effect2Tint = includeEffect2 ? ParseColorFromTMP(effect2ColorText.text) : Color.white;


    int totalAnimations = animations.Length;
    int processedAnimations = 0;

    // --- Standard animations loop ---
    foreach (string anim in animations)
    {
        if (currentlyGeneratingTMP != null)
            currentlyGeneratingTMP.text = "Generating " + anim + " spritesheet...";

        // Load naked body (for size) and shadow
        Texture2D nakedBodyTex = LoadTexture(Path.Combine(parentFolder, nakedBodyFolder, anim + ".png"));
        if (nakedBodyTex == null)
        {
            Debug.LogError("Missing NakedBody spritesheet for animation: " + anim);
            processedAnimations++;
            if (loadProgressSlider != null)
                loadProgressSlider.value = (float)processedAnimations / totalAnimations;
            yield return null;
            continue;
        }
        Texture2D shadowTex = (shadowFolder == "None")
            ? null
            : LoadTexture(Path.Combine(parentFolder, shadowFolder, anim + ".png"));
        float shadowAlpha = ParseAlphaPercentage(shadowColorText.text);

        

        // Slash is loaded separately
        Texture2D slashTex = null;
        if (includeSlash && slashFolder != "None")
            slashTex = LoadTexture(Path.Combine(parentFolder, slashFolder, anim + ".png"));

            // Effect is loaded separately
        Texture2D effectTex = null;
        if (includeEffect && effectFolder != "None")
            effectTex = LoadTexture(Path.Combine(parentFolder, effectFolder, anim + ".png"));
        Texture2D effect2Tex = null;
        if (includeEffect2 && effect2Folder != "None")
            effect2Tex = LoadTexture(Path.Combine(parentFolder, effect2Folder, anim + ".png"));

                

        // Prepare tints
        Color shoesTint    = shoesRenderer   != null ? shoesRenderer.color   : Color.white;
        Color legsTint     = legsRenderer    != null ? legsRenderer.color    : Color.white;
        Color mountTint    = ParseColorFromTMP(mountColorText.text);
        Color chestTint    = chestRenderer   != null ? chestRenderer.color   : Color.white;
        Color beltTint    = beltRenderer   != null ? beltRenderer.color   : Color.white;
        Color handsTint    = handsRenderer   != null ? handsRenderer.color   : Color.white;
        Color shieldTint   = ParseColorFromTMP(shieldColorText.text);
        Color weaponTint   = ParseColorFromTMP(weaponColorText.text);
        Color backpackTint = ParseColorFromTMP(backpackColorText.text);
        Color headTint     = headRenderer    != null ? headRenderer.color    : Color.white;

        bool flatColorMode = flatColorToggle != null && flatColorToggle.isOn;

        Texture2D shoesTex    = LoadAndTintTexture(shoesFolder, anim, shoesTint, flatColorMode);
        Texture2D legsTex     = LoadAndTintTexture(legsFolder, anim, legsTint, flatColorMode);
        Texture2D mountTex    = LoadAndTintTexture(mountFolder, anim, mountTint, flatColorMode);
        Texture2D chestTex    = LoadAndTintTexture(chestFolder, anim, chestTint, flatColorMode);
        Texture2D beltTex     = LoadAndTintTexture(beltFolder, anim, beltTint, flatColorMode);
        Texture2D handsTex    = LoadAndTintTexture(handsFolder, anim, handsTint, flatColorMode);
        Texture2D shieldTex   = LoadAndTintTexture(shieldFolder, anim, shieldTint, flatColorMode);
        Texture2D weaponTex   = LoadAndTintTexture(weaponFolder, anim, weaponTint, flatColorMode);
        Texture2D backpackTex = LoadAndTintTexture(backpackFolder, anim, backpackTint, flatColorMode);
        Texture2D headTex     = LoadAndTintTexture(headFolder, anim, headTint, flatColorMode);

        // Build layer arrays
        Texture2D[] layers = new Texture2D[] {
            shoesTex, legsTex, mountTex, chestTex, beltTex, handsTex,
            shieldTex, weaponTex, backpackTex, headTex
        };
        Color[] layerTints = new Color[] {
            shoesTint, legsTint, mountTint, chestTint, beltTint, handsTint,
            shieldTint, weaponTint, backpackTint, headTint
        };

        // Allocate buffers
        int W = nakedBodyTex.width, H = nakedBodyTex.height;
        Color[] finalPixels    = new Color[W * H]; // starts with shadow
        Color[] bodyGearPixels = new Color[W * H]; // will hold nakedBody+gear

        // 1) Shadow → finalPixels
        if (shadowTex != null)
        {
            var sp = shadowTex.GetPixels();
            for (int i = 0; i < sp.Length; i++)
            {
                sp[i].a *= shadowAlpha;
                finalPixels[i] = sp[i];
            }
        }
        else
        {
            for (int i = 0; i < finalPixels.Length; i++)
                finalPixels[i] = new Color(0,0,0,0);
        }

        // 2) NakedBody → bodyGearPixels
        if (skinToggle == null || skinToggle.isOn)
        {
            var nb = nakedBodyTex.GetPixels();
            Color skinCol = skinColorRenderer != null ? skinColorRenderer.color : Color.white;

            if (flatColorMode)
                nb = ApplyFlatColor(nb, skinCol);

            for (int i = 0; i < nb.Length; i++)
                bodyGearPixels[i] = nb[i] * skinCol;
        }
        else
        {
            for (int i = 0; i < bodyGearPixels.Length; i++)
                bodyGearPixels[i] = new Color(0, 0, 0, 0); // Fully transparent
        }


        // 3) Gear layers → bodyGearPixels (ROW-AWARE with dynamic ordering for Shield, Weapon, Backpack)

        // Pre-extract pixel arrays once (they are already tinted in LoadAndTintTexture)
        Color[][] layerPix = new Color[layers.Length][];
        for (int i = 0; i < layers.Length; i++)
            layerPix[i] = (layers[i] != null) ? layers[i].GetPixels() : null;

        // Indices into our layers[] array so we can build per-row order
        const int IDX_SHOES    = 0;
        const int IDX_LEGS     = 1;
        const int IDX_MOUNT    = 2;
        const int IDX_CHEST    = 3;
        const int IDX_BELT     = 4;
        const int IDX_HANDS    = 5;
        const int IDX_SHIELD   = 6;
        const int IDX_WEAPON   = 7;
        const int IDX_BACKPACK = 8;
        const int IDX_HEAD     = 9;

        int rows = 8;
        int rowHeight = H / rows;

        for (int r = 0; r < rows; r++)
        {
            int topDownRow = rows - 1 - r;

            List<int> order = new List<int>(10)
            {
                IDX_SHOES, IDX_LEGS, IDX_MOUNT, IDX_CHEST, IDX_BELT, IDX_HANDS
            };

            bool weaponAfterShield = (topDownRow < weaponAfterShieldRows.Length)
                ? weaponAfterShieldRows[topDownRow] : true;

            // Backpack on top for top-based rows: 0,4,5,6,7  (i.e., visual rows 1,5,6,7,8)
            bool backpackOnTop = (topDownRow == 0) || (topDownRow == 4) || (topDownRow >= 5);

            if (backpackOnTop)
            {
                // Backpack should be in FRONT of BOTH shield & weapon
                if (weaponAfterShield)
                {
                    order.Add(IDX_SHIELD);
                    order.Add(IDX_WEAPON);
                }
                else
                {
                    order.Add(IDX_WEAPON);
                    order.Add(IDX_SHIELD);
                }
                order.Add(IDX_BACKPACK);
            }
            else
            {
                // Backpack should be BEHIND BOTH shield & weapon
                order.Add(IDX_BACKPACK);
                if (weaponAfterShield)
                {
                    order.Add(IDX_SHIELD);
                    order.Add(IDX_WEAPON);
                }
                else
                {
                    order.Add(IDX_WEAPON);
                    order.Add(IDX_SHIELD);
                }
            }

            order.Add(IDX_HEAD);

            int yStart = r * rowHeight;
            for (int oi = 0; oi < order.Count; oi++)
            {
                int li = order[oi];
                var pix = (li >= 0 && li < layerPix.Length) ? layerPix[li] : null;
                if (pix == null) continue;

                for (int y = 0; y < rowHeight; y++)
                {
                    int rowOffset = (yStart + y) * W;
                    int end = rowOffset + W;
                    for (int p = rowOffset; p < end; p++)
                        bodyGearPixels[p] = AlphaBlend(bodyGearPixels[p], pix[p]);
                }
            }

            // Optional: quick sanity check
            // Debug.Log($"topRow {topDownRow+1}: backpackOnTop={backpackOnTop}, weaponAfterShield={weaponAfterShield}");
        }



        // 4) Composite body+gear → finalPixels
        for (int i = 0; i < finalPixels.Length; i++)
            finalPixels[i] = AlphaBlend(finalPixels[i], bodyGearPixels[i]);

        // 5) Save mask-copy for outlining
        Color[] bodyGearCopy = (Color[])bodyGearPixels.Clone();

        // 6) Create Texture2D
        Texture2D outTex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        outTex.SetPixels(finalPixels);
        outTex.Apply();

        // 7) Static idle tweak (optional)
        if (staticIdleAnimation != null && staticIdleAnimation.isOn &&
            (anim == "Idle" || anim == "Idle2" || anim == "Idle3" || anim == "Idle4" || anim == "CrouchIdle"))
        {
            outTex = MakeTextureStatic(outTex, 15, 8);
        }

        // 8) Resize (if needed) — track it!
        bool didResize = (use64Toggle != null && use64Toggle.isOn);
        if (didResize)
            outTex = ResizeTexturePixelPerfect(outTex, outTex.width / 2, outTex.height / 2);

        // 9) Apply outline (using a correctly‐sized mask)
        Color[] mask = didResize
            ? ResizePixelsPixelPerfect(bodyGearCopy, W, H, outTex.width, outTex.height)
            : bodyGearCopy;

        // ⬇️ NEW: if this is a “Magic” weapon, strip its pixels out of the mask
        if (weaponFolder.StartsWith("Magic"))
        {
            if (weaponTex != null)
            {
                // 1) grab the original weapon pixels and tint them
                Color[] weaponMask = weaponTex.GetPixels();
                for (int i = 0; i < weaponMask.Length; i++)
                    weaponMask[i] *= weaponTint;

                // 2) if we resized the main sheet, resize the weapon‐mask exactly the same
                if (didResize)
                    weaponMask = ResizePixelsPixelPerfect(
                        weaponMask, 
                        W, H,              // original dims
                        outTex.width, 
                        outTex.height
                    );

                // 3) carve out: wherever weaponMask is non-transparent, zero out mask α
                for (int i = 0; i < mask.Length; i++)
                {
                    if (weaponMask[i].a > 0f)
                        mask[i].a = 0f;
                }
            }
        }

        // 9.5) Finally, run your outline routines on that edited mask:
        if (gradientOutlineToggle != null && gradientOutlineToggle.isOn)
        {
            var px       = outTex.GetPixels();
            var outlined = AddGradientOutline(px, mask, outTex.width, outTex.height, gradientBrightness);
            outTex.SetPixels(outlined);
            outTex.Apply();
        }
        else if (glowOutlineToggle != null && glowOutlineToggle.isOn)
        {
            Color glowColor = Color.black;

            if (outlineColorText != null && ColorUtility.TryParseHtmlString(outlineColorText.text, out Color parsedGlow))
                glowColor = parsedGlow;

            int outlineW = outTex.width;
            int outlineH = outTex.height;

            // If resized, use already-resized versions of final+mask pixels
            Color[] basePixels = outTex.GetPixels(); // Always use resized texture as base
            Color[] resizedMask = didResize
                ? ResizePixelsPixelPerfect(bodyGearPixels, W, H, outlineW, outlineH)
                : bodyGearPixels;

            var outlined = AddGlowOutline(basePixels, resizedMask, outlineW, outlineH, glowColor, glowThickness);
            outTex.SetPixels(outlined);
            outTex.Apply();
        }

        else if (outlineToggle != null && outlineToggle.isOn)
        {
            Color outlineColor = Color.black;

            if (outlineColorText != null && ColorUtility.TryParseHtmlString(outlineColorText.text, out Color parsed))
                outlineColor = parsed;

            var px       = outTex.GetPixels();
            var outlined = AddOutlineMask(px, mask, outTex.width, outTex.height, outlineColor);
            outTex.SetPixels(outlined);
            outTex.Apply();
        }



        // 10) Finally, overlay slash (if any)
        if (slashTex != null)
        {
            var gfp = slashTex.GetPixels();
            for (int i = 0; i < gfp.Length; i++)
                gfp[i] *= slashTint;

            if (didResize)
                gfp = ResizePixelsPixelPerfect(gfp, W, H, outTex.width, outTex.height);

            var basePixels = outTex.GetPixels();
            for (int i = 0; i < basePixels.Length; i++)
                basePixels[i] = AlphaBlend(basePixels[i], gfp[i]);
            outTex.SetPixels(basePixels);
            outTex.Apply();
        }

        if (effectTex != null)
        {
            var gfp = effectTex.GetPixels();
            for (int i = 0; i < gfp.Length; i++)
                gfp[i] *= effectTint;

            if (didResize)
                gfp = ResizePixelsPixelPerfect(gfp, W, H, outTex.width, outTex.height);

            var basePixels = outTex.GetPixels();
            for (int i = 0; i < basePixels.Length; i++)
                basePixels[i] = AlphaBlend(basePixels[i], gfp[i]);
            outTex.SetPixels(basePixels);
            outTex.Apply();
        }

        if (effect2Tex != null)
        {
            var gfp = effect2Tex.GetPixels();
            for (int i = 0; i < gfp.Length; i++)
                gfp[i] *= effect2Tint;

            if (didResize)
                gfp = ResizePixelsPixelPerfect(gfp, W, H, outTex.width, outTex.height);

            var basePixels = outTex.GetPixels();
            for (int i = 0; i < basePixels.Length; i++)
                basePixels[i] = AlphaBlend(basePixels[i], gfp[i]);
            outTex.SetPixels(basePixels);
            outTex.Apply();
        }


        // Save out
        string outputPath = Path.Combine(configFolder, anim + ".png");
        File.WriteAllBytes(outputPath, outTex.EncodeToPNG());
        ExportTilesetFrames(outTex, anim, configFolder);
        Debug.Log($"Saved combined spritesheet for {anim} at {outputPath}");

        processedAnimations++;
        if (loadProgressSlider != null)
            loadProgressSlider.value = (float)processedAnimations / totalAnimations;
        yield return null;
    }


    if (loadProgressSlider != null)
        loadProgressSlider.value = 1f;
    if (currentlyGeneratingTMP != null)
        currentlyGeneratingTMP.text = "Generation complete!";
    yield return new WaitForSeconds(0.5f);
    if (loadScreenPanel != null)
        loadScreenPanel.SetActive(false);

    #if UNITY_EDITOR
    if (sliceSpritesheets != null && sliceSpritesheets.isOn)
    {
        AssetDatabase.Refresh();
        string[] files = Directory.GetFiles(configFolder, "*.png");
        SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
        factory.Init();
        foreach (string file in files)
        {
            string assetPath = "Assets" + file.Substring(Application.dataPath.Length);
            TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (ti != null)
            {
                ti.spriteImportMode = SpriteImportMode.Multiple;
                ti.spritePixelsPerUnit = 100;
                ti.filterMode = FilterMode.Point;
                ti.textureCompression = TextureImporterCompression.Uncompressed;

                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (tex != null)
                {
                    int columns = 15;
                    int rows = 8;
                    float sliceWidth = tex.width / (float)columns;
                    float sliceHeight = tex.height / (float)rows;
                    List<SpriteRect> spriteRects = new List<SpriteRect>(columns * rows);
                    for (int y = 0; y < rows; y++)
                    {
                        for (int x = 0; x < columns; x++)
                        {
                            SpriteRect rect = new SpriteRect();
                            rect.name = Path.GetFileNameWithoutExtension(assetPath) + "_" + y + "_" + x;
                            rect.rect = new Rect(x * sliceWidth, y * sliceHeight, sliceWidth, sliceHeight);
                            rect.pivot = new Vector2(0.5f, 0.5f);
                            rect.alignment = SpriteAlignment.Custom;
                            rect.spriteID = GUID.Generate();
                            spriteRects.Add(rect);
                        }
                    }

                    var provider = factory.GetSpriteEditorDataProviderFromObject(ti);
                    if (provider != null)
                    {
                        provider.InitSpriteEditorDataProvider();
                        provider.SetSpriteRects(spriteRects.ToArray());
                        provider.Apply();
                    }
                }
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }
    }
    AssetDatabase.Refresh();
    #endif
    ApplyFrameCountOption(configFolder);
    Debug.Log($"use64Toggle.isOn = {use64Toggle.isOn}");
    #if UNITY_EDITOR
    if (createAnimatorToggle != null && createAnimatorToggle.isOn && animatorClipBuilder != null)
    {
        int targetColumns = 15;
        if (fourteenFramesToggle != null && fourteenFramesToggle.isOn) targetColumns = 14;
        else if (twelveFramesToggle != null && twelveFramesToggle.isOn) targetColumns = 12;
        else if (tenFramesToggle != null && tenFramesToggle.isOn) targetColumns = 10;
        else if (eightFramesToggle != null && eightFramesToggle.isOn) targetColumns = 8;
        else if (sixFramesToggle != null && sixFramesToggle.isOn) targetColumns = 6;
        else if (fourFramesToggle != null && fourFramesToggle.isOn) targetColumns = 4;

        string[] sheetPaths = Directory.GetFiles(configFolder, "*.png");
        #if UNITY_EDITOR
        animatorClipBuilder.GenerateClipsForSpritesheets(sheetPaths, configFolder, targetColumns, userInputName + "_" + timestamp);
        #endif
    }
    #endif

    yield break;
}

private Color[] ResizePixelsPixelPerfect(Color[] original, int origWidth, int origHeight, int newWidth, int newHeight)
{
    Color[] result = new Color[newWidth * newHeight];
    for (int y = 0; y < newHeight; y++)
    {
        for (int x = 0; x < newWidth; x++)
        {
            int origX = x * 2;
            int origY = y * 2;
            if (origX >= origWidth) origX = origWidth - 1;
            if (origY >= origHeight) origY = origHeight - 1;
            result[y * newWidth + x] = original[origY * origWidth + origX];
        }
    }
    return result;
}


private Texture2D ResizeTexturePixelPerfect(Texture2D source, int newWidth, int newHeight)
{
    Texture2D result = new Texture2D(newWidth, newHeight, source.format, false);
    for (int y = 0; y < newHeight; y++)
    {
        for (int x = 0; x < newWidth; x++)
        {
            // Calculate the source pixel to sample
            int srcX = Mathf.FloorToInt(x * (source.width / (float)newWidth));
            int srcY = Mathf.FloorToInt(y * (source.height / (float)newHeight));
            Color pixel = source.GetPixel(srcX, srcY);
            result.SetPixel(x, y, pixel);
        }
    }
    result.filterMode = FilterMode.Point; // Important: keep pixels sharp
    result.Apply();
    return result;
}


// Helper to avoid boilerplate:
private Texture2D LoadTextureOrNull(string folder, string animName)
{
    return (folder == "None")
        ? null
        : LoadTexture(Path.Combine(parentFolder, folder, animName + ".png"));
}
    
    
  private Texture2D LoadTexture(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            return null;
        }
        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (tex.LoadImage(fileData))
            return tex;
        return null;
    }

    private Color ParseColorFromTMP(string hex)
    {
        Color parsedColor = Color.white;
        if (!string.IsNullOrEmpty(hex))
        {
            ColorUtility.TryParseHtmlString(hex, out parsedColor);
        }
        return parsedColor;
    }

    private float ParseAlphaPercentage(string percentageString)
    {
        if (string.IsNullOrEmpty(percentageString))
            return 1f;
        percentageString = percentageString.Trim();
        if (percentageString.EndsWith("%"))
            percentageString = percentageString.Substring(0, percentageString.Length - 1);
        if (float.TryParse(percentageString, out float percent))
            return Mathf.Clamp01(percent / 100f);
        return 1f;
    }

    private Color AlphaBlend(Color bottom, Color top)
    {
        float alpha = top.a;
        return top * alpha + bottom * (1f - alpha);
    }

    private Texture2D MakeTextureStatic(Texture2D original, int columns, int rows)
    {
        int tileWidth = original.width / columns;
        int tileHeight = original.height / rows;
        Color[] origPixels = original.GetPixels();
        Color[] newPixels = new Color[original.width * original.height];

        for (int row = 0; row < rows; row++)
        {
            for (int y = 0; y < tileHeight; y++)
            {
                int globalY = row * tileHeight + y;
                for (int x = 0; x < tileWidth; x++)
                {
                    int srcIndex = globalY * original.width + x;
                    Color staticColor = origPixels[srcIndex];
                    for (int col = 0; col < columns; col++)
                    {
                        int globalX = col * tileWidth + x;
                        int destIndex = globalY * original.width + globalX;
                        newPixels[destIndex] = staticColor;
                    }
                }
            }
        }
        Texture2D staticTexture = new Texture2D(original.width, original.height, original.format, false);
        staticTexture.SetPixels(newPixels);
        staticTexture.Apply();
        return staticTexture;
    }

private Texture2D PackSpritesheet(Texture2D original, int targetColumns)
{
    int originalColumns = 15;
    int rows = 8;
    int tileWidth = original.width / originalColumns;
    int tileHeight = original.height / rows;

    int[] mapping;
    if (targetColumns == 15)
    {
        mapping = new int[15];
        for (int i = 0; i < 15; i++) mapping[i] = i;
    }
    else if (targetColumns == 14)
    {
        // Remove columns 3,6,9,12,15 (1-indexed) → indices 2,5,8,11,14.
        mapping = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };
    }
    else if (targetColumns == 12)
    {
        // Remove columns 3,6,9,12,15 (1-indexed) → indices 2,5,8,11,14.
        mapping = new int[] { 0, 1, 2, 3, 4, 6, 7, 8, 9, 10, 12, 13 };
    }
    else if (targetColumns == 10)
    {
        // Remove columns 3,6,9,12,15 (1-indexed) → indices 2,5,8,11,14.
        mapping = new int[] { 0, 1, 3, 4, 6, 7, 9, 10, 12, 13 };
    }
    else if (targetColumns == 8)
    {
        // Keep columns 1,3,5,7,9,11,13,15 (1-indexed) → indices 0,2,4,6,8,10,12,14.
        mapping = new int[] { 0, 2, 4, 6, 8, 10, 12, 14 };
    }
    else if (targetColumns == 6)
    {
        // Keep columns 1,3,5,7,9,11,13,15 (1-indexed) → indices 0,2,4,6,8,10,12,14.
        mapping = new int[] { 0, 3, 6, 9, 12, 14 };
    }
    else if (targetColumns == 4)
    {
        // Keep only columns 1,5,9,13 (1-indexed) → indices 0,5,10,14.
        mapping = new int[] { 0, 4, 8, 12 };
    }
    else
    {
        Debug.LogError("Unsupported targetColumns: " + targetColumns);
        return original;
    }

    int newWidth = tileWidth * targetColumns;
    int newHeight = tileHeight * rows;
    // Create the new texture explicitly in RGBA32 format.
    Texture2D packed = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);

    for (int row = 0; row < rows; row++)
    {
        for (int newCol = 0; newCol < targetColumns; newCol++)
        {
            int origCol = mapping[newCol];
            int origX = origCol * tileWidth;
            int origY = row * tileHeight;
            Color[] tilePixels = original.GetPixels(origX, origY, tileWidth, tileHeight);
            int newX = newCol * tileWidth;
            int newY = row * tileHeight;
            packed.SetPixels(newX, newY, tileWidth, tileHeight, tilePixels);
        }
    }
    packed.Apply();
    return packed;
}

private int GetTargetColumns()
{
    int columns = 15;
    if (fourteenFramesToggle != null && fourteenFramesToggle.isOn) columns = 14;
    else if (twelveFramesToggle != null && twelveFramesToggle.isOn) columns = 12;
    else if (tenFramesToggle != null && tenFramesToggle.isOn) columns = 10;
    else if (eightFramesToggle != null && eightFramesToggle.isOn) columns = 8;
    else if (sixFramesToggle != null && sixFramesToggle.isOn) columns = 6;
    else if (fourFramesToggle != null && fourFramesToggle.isOn) columns = 4;
    return columns;
}


    private void ApplyFrameCountOption(string configFolder)
    {
        #if UNITY_EDITOR
        int targetColumns = 15; // default
        if (fourteenFramesToggle != null && fourteenFramesToggle.isOn) targetColumns = 14;
        else if (twelveFramesToggle != null && twelveFramesToggle.isOn) targetColumns = 12;
        else if (tenFramesToggle != null && tenFramesToggle.isOn) targetColumns = 10;
        else if (eightFramesToggle != null && eightFramesToggle.isOn) targetColumns = 8;
        else if (sixFramesToggle != null && sixFramesToggle.isOn) targetColumns = 6;
        else if (fourFramesToggle != null && fourFramesToggle.isOn) targetColumns = 4;
        Debug.Log("Target columns for packing: " + targetColumns);
        
        if (targetColumns < 15)
        {
            string[] files = Directory.GetFiles(configFolder, "*.png");
            foreach (string file in files)
            {
                string assetPath = "Assets" + file.Substring(Application.dataPath.Length);
                TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (ti != null)
                {
                    ti.isReadable = true;
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                    if (tex != null)
                    {
                        Texture2D packed = PackSpritesheet(tex, targetColumns);
                        byte[] pngData = packed.EncodeToPNG();
                        File.WriteAllBytes(file, pngData);
                    }
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }
            }
        }
        #endif
    }


    private void ExportTilesetFrames(Texture2D spritesheet, string baseName, string outputFolder)
    {
        if (exportTilesetToggle == null || !exportTilesetToggle.isOn)
            return;

        int columns = spritesheet.width / (use64Toggle.isOn ? 64 : 128);  // ✅ Always derive from width

        int rows = 8;
        int frameWidth = spritesheet.width / columns;
        int frameHeight = spritesheet.height / rows;

        // 🔸 Create subfolder for this animation
        string animationFolder = Path.Combine(outputFolder, baseName);
        if (!Directory.Exists(animationFolder))
            Directory.CreateDirectory(animationFolder);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Texture2D frame = new Texture2D(frameWidth, frameHeight, TextureFormat.RGBA32, false);
                frame.SetPixels(spritesheet.GetPixels(col * frameWidth, row * frameHeight, frameWidth, frameHeight));
                frame.Apply();

                string tileName = $"{baseName}_{row}_{col}.png";
                string tilePath = Path.Combine(animationFolder, tileName);
                File.WriteAllBytes(tilePath, frame.EncodeToPNG());
            }
        }

        Debug.Log($"✅ Exported {rows * columns} frames to: {animationFolder}");
    }


    /// <summary>
    /// Takes a flat RGBA pixel array plus its dimensions
    /// and returns a new array where any transparent
    /// neighbor of an opaque pixel gets painted with
    /// outlineColor (a single‑pixel orthogonal dilation).
    /// </summary>
    private Color[] AddOutline(Color[] pixels, int width, int height, Color outlineColor)
    {
        Color[] result = (Color[])pixels.Clone();
        // Build a mask of where the character lives
        bool[] mask = new bool[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
            mask[i] = pixels[i].a > 0f;

        // Offsets for 4‑neighborhood:
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        // For each pixel that is part of the character:
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                if (!mask[idx]) continue;

                // Check each neighbor:
                for (int k = 0; k < 4; k++)
                {
                    int nx = x + dx[k];
                    int ny = y + dy[k];
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        continue;

                    int nIdx = ny * width + nx;
                    if (!mask[nIdx])
                    {
                        // Paint an outline pixel
                        result[nIdx] = outlineColor;
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Paints a 1px black outline in finalPixels around any body pixel
    /// in bodyPixels, but ignores the shadow that’s already in finalPixels.
    /// </summary>
    private Color[] AddOutlineMask(Color[] finalPixels,
                                Color[] bodyPixels,
                                int width, int height,
                                Color outlineColor)
    {
        bool[] mask = new bool[bodyPixels.Length];
        for (int i = 0; i < mask.Length; i++)
            mask[i] = bodyPixels[i].a > 0f;

        int[] dx = { -1, 1, 0, 0 }, dy = { 0, 0, -1, 1 };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                if (!mask[idx]) continue;

                // for each 4‑neighbor:
                for (int k = 0; k < 4; k++)
                {
                    int nx = x + dx[k], ny = y + dy[k];
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                    int nIdx = ny * width + nx;
                    if (!mask[nIdx])
                        finalPixels[nIdx] = outlineColor;
                }
            }
        }

        return finalPixels;
    }


    /// <summary>
/// Like AddOutlineMask, but each outline pixel is colored by
/// taking the nearest opaque body pixel and darkening it.
/// </summary>
public float redColor   = 0.5f;
public float greenColor = 0.5f;
public float blueColor  = 0.5f;

/// <summary>
/// Paints a 1px outline around any body pixel, sampling the nearest body pixel colour,
/// inverting it, and then lightening it toward white by lightenFactor.
/// finalPixels[] already contains shadow; bodyPixels[] is your nakedBody+gear composite.
/// </summary>
private Color[] AddGradientOutline(
    Color[] finalPixels,
    Color[] bodyPixels,
    int width,
    int height,
    float lightenFactor = 0.5f  // 0 = pure inverted colour; 1 = pure white
)
{
    Color[] result = (Color[])finalPixels.Clone();

    // build mask of where body exists
    bool[] mask = new bool[bodyPixels.Length];
    for (int i = 0; i < mask.Length; i++)
        mask[i] = bodyPixels[i].a > 0f;

    // 4‑neighbour offsets
    int[] dx = { -1, 1, 0, 0 }, dy = { 0, 0, -1, 1 };

    // for every body pixel, paint its transparent neighbours
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            int idx = y * width + x;
            if (!mask[idx]) continue;

            // sample the body pixel colour
            Color src = bodyPixels[idx];

            // invert it
            Color inverted = new Color(1f - src.r, 1f - src.g, 1f - src.b, 1f);

            // lighten toward white
            Color outlineCol = Color.Lerp(inverted, Color.white, lightenFactor);

            // paint 1‑px thick outline
            for (int k = 0; k < 4; k++)
            {
                int nx = x + dx[k], ny = y + dy[k];
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                int nIdx = ny * width + nx;
                if (!mask[nIdx])
                {
                    result[nIdx] = outlineCol;
                }
            }
        }
    }

    return result;
}

/// <summary>
/// Adds a soft glow around the bodyPixels using the specified glow color.
/// The glow is a 1px soft blend outward from bodyPixels into transparent areas.
/// </summary>
private Color[] AddGlowOutline(
    Color[] finalPixels,
    Color[] bodyPixels,
    int width,
    int height,
    Color glowColor,
    int thickness = 4 // how far the glow spreads
)
{
    Color[] result = (Color[])finalPixels.Clone();

    // Build mask of where body exists
    bool[] mask = new bool[bodyPixels.Length];
    for (int i = 0; i < mask.Length; i++)
        mask[i] = bodyPixels[i].a > 0f;

    // Create distance field: 0 for body, large for far-away pixels
    int[] distance = new int[width * height];
    for (int i = 0; i < distance.Length; i++)
        distance[i] = mask[i] ? 0 : int.MaxValue;

    // 8-direction offsets for better smoothing
    int[] dx = { -1, 1,  0, 0, -1, -1, 1, 1 };
    int[] dy = {  0, 0, -1, 1, -1,  1, -1, 1 };

    // Breadth-first propagation (like flood fill) to calculate pixel distances
    Queue<int> frontier = new Queue<int>();
    for (int i = 0; i < mask.Length; i++)
    {
        if (mask[i])
            frontier.Enqueue(i);
    }

    while (frontier.Count > 0)
    {
        int idx = frontier.Dequeue();
        int x = idx % width;
        int y = idx / width;
        int d = distance[idx];

        for (int k = 0; k < dx.Length; k++)
        {
            int nx = x + dx[k], ny = y + dy[k];
            if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                continue;

            int nIdx = ny * width + nx;
            if (distance[nIdx] > d + 1 && d + 1 <= thickness)
            {
                distance[nIdx] = d + 1;
                frontier.Enqueue(nIdx);
            }
        }
    }

    // Paint glow using distance + alpha falloff
    for (int i = 0; i < result.Length; i++)
    {
        int d = distance[i];
        if (d > 0 && d <= thickness)
        {
            float alpha = 1f - ((float)(d - 1) / thickness); // solid at 1px, fades out
            Color c = glowColor;
            c.a *= alpha;
            result[i] = AlphaBlend(result[i], c);
        }
    }

    return result;
}


        private Color[] ApplyFlatColor(Color[] pixels, Color tint)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > 0.01f)
                    pixels[i] = new Color(tint.r, tint.g, tint.b, pixels[i].a);
            }
            return pixels;
        }


        private Texture2D LoadAndTintTexture(string folder, string anim, Color tint, bool useFlatColor)
        {
            Texture2D tex = (folder == "None") ? null : LoadTexture(Path.Combine(parentFolder, folder, anim + ".png"));
            if (tex == null) return null;
            Color[] pix = tex.GetPixels();
            for (int i = 0; i < pix.Length; i++)
                pix[i] *= tint;
            if (useFlatColor)
                pix = ApplyFlatColor(pix, tint);
            Texture2D result = new Texture2D(tex.width, tex.height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        

    }
}
