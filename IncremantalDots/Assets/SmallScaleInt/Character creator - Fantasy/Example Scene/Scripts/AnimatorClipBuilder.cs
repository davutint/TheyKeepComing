using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

namespace SmallScaleInc.CharacterCreatorFantasy
{
    public class AnimatorClipBuilder : MonoBehaviour
    {
        public int rows = 8;
        private int columnsCount;
        public float frameRate = 12f;
        public string characterName;

        public bool includePlayerPrefab;
        public GameObject genericPlayerPrefab;

        public bool shouldPreviewCharacter;
        public GameObject previewContainer;
        public GameObject previewCanvas; // The UI canvas you want to show during preview
        public List<GameObject> objectsToHideDuringPreview = new List<GameObject>();

        private GameObject previewInstance;

        public UtilityManager utilityManager;
        public Transform mainPlayerTransform; // Assign your default player object here
        // Keep this asset self-contained: PixelPerfectCamera lives in com.unity.2d.pixel-perfect (optional dependency).
        // Using Behaviour lets users assign PixelPerfectCamera (or any other behaviour) without requiring the package to compile.
        public Behaviour pixelPerfectCamera;
        private bool wasPixelPerfectEnabled;





        #if UNITY_EDITOR
        public void GenerateClipsForSpritesheets(string[] sheetPaths, string rootFolder, int columns, string name)
        {
            characterName = name;
            columnsCount = columns;
            string clipsRootFolder = Path.Combine(rootFolder, "Animation Clips");
            if (!Directory.Exists(clipsRootFolder))
                Directory.CreateDirectory(clipsRootFolder);

            Dictionary<string, List<AnimationClip>> animationGroups = new();

            foreach (string path in sheetPaths)
            {
                string assetPath = "Assets" + path.Substring(Application.dataPath.Length).Replace("\\", "/");
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (tex == null)
                {
                    Debug.LogWarning("Skipped null texture: " + assetPath);
                    continue;
                }

                UnityEngine.Object[] allSprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
                List<Sprite> sprites = new();
                foreach (var s in allSprites)
                    if (s is Sprite sprite)
                        sprites.Add(sprite);

                if (sprites.Count == 0)
                {
                    Debug.LogWarning("No sliced sprites found for: " + assetPath);
                    continue;
                }

                string baseName = Path.GetFileNameWithoutExtension(path);
                string animationName = baseName.Split('_')[0];

                string clipsFolder = Path.Combine(clipsRootFolder, animationName);
                if (!Directory.Exists(clipsFolder))
                    Directory.CreateDirectory(clipsFolder);

                for (int row = 0; row < rows; row++)
                {
                    var clip = new AnimationClip();
                    clip.frameRate = frameRate;

                    EditorCurveBinding binding = new EditorCurveBinding
                    {
                        type = typeof(SpriteRenderer),
                        path = "",
                        propertyName = "m_Sprite"
                    };

                    ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[columns];
                    for (int col = 0; col < columns; col++)
                    {
                        string spriteName = $"{baseName}_{row}_{col}";
                        Sprite sprite = sprites.Find(s => s.name == spriteName);

                        keyframes[col] = new ObjectReferenceKeyframe
                        {
                            time = col / frameRate,
                            value = sprite
                        };
                    }

                    AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
                    // Set LoopTime to true
                    var settings = AnimationUtility.GetAnimationClipSettings(clip);
                    settings.loopTime = true;
                    AnimationUtility.SetAnimationClipSettings(clip, settings);

                    // Remap index so 0->E, 1->SE, ..., 7->NE
                    int[] remap = { 7, 6, 5, 4, 3, 2, 1, 0 }; // flip order if GetDirectionName was backwards
                    string direction = GetDirectionName(remap[row]);

                    string clipSystemPath = Path.Combine(clipsFolder, $"{baseName}_{direction}.anim");
                    string clipAssetPath = "Assets" + clipSystemPath.Substring(Application.dataPath.Length).Replace("\\", "/");

                    AssetDatabase.CreateAsset(clip, clipAssetPath);

                    if (!animationGroups.ContainsKey(animationName))
                        animationGroups[animationName] = new List<AnimationClip>();
                    animationGroups[animationName].Add(clip);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GenerateAnimator(animationGroups, clipsRootFolder);
        }


        private void GenerateAnimator(Dictionary<string, List<AnimationClip>> animationGroups, string rootFolder)
        {
            // Build a filename like “Knight1_animator.controller”
            string animatorFileName = $"{characterName}_animator.controller";

            // Absolute system path for where to write it:
            string absoluteAnimatorPath = Path.Combine(rootFolder, animatorFileName);
            string animatorPath = "Assets" + absoluteAnimatorPath.Substring(Application.dataPath.Length).Replace("\\", "/");
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(animatorPath);

            controller.AddParameter("Direction", AnimatorControllerParameterType.Float);
            controller.AddParameter("DirIndex", AnimatorControllerParameterType.Int);
            foreach (string trigger in new[] {
                "Attack1","Attack2","Attack3","Attack4","Attack5",
                "AttackRun","AttackRun2",
                "Special1","Special2","Taunt",
                "Die","TakeDamage"
            })
                controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);

            foreach (string b in new[] {
                "IsRun","IsWalk","IsStrafeLeft","IsStrafeRight","IsRunBackwards",
                "UseIdle2","UseIdle3","UseIdle4",
                "IsCrouching","IsMounted",
                "Speed1x","Speed2x"
            })
                controller.AddParameter(b, AnimatorControllerParameterType.Bool);

            var rootSM = controller.layers[0].stateMachine;
            var dirMap = new Dictionary<string, int> {
                {"E",0}, {"W",1}, {"S",2}, {"N",3},
                {"NE",4}, {"NW",5}, {"SE",6}, {"SW",7}
            };

            // --- Setup known BT groups ---
            string[] blendGroups = new[] {
                "Idle", "Idle2", "Idle3", "Idle4",
                "Run", "Walk", "StrafeLeft", "StrafeRight", "RunBackwards",
                "CrouchIdle", "CrouchRun", "RideIdle", "RideRun"
            };

            foreach (string group in blendGroups)
            {
                var stateName = group + "BT";
                var tree = new BlendTree {
                    name = stateName + "_Tree",
                    blendType = BlendTreeType.Simple1D,
                    useAutomaticThresholds = false,
                    blendParameter = "Direction"
                };
                controller.AddMotion(tree, 0);
                AssetDatabase.AddObjectToAsset(tree, controller);

                foreach (var kvp in animationGroups)
                {
                    if (!kvp.Key.Equals(group, StringComparison.OrdinalIgnoreCase)) continue;

                    foreach (var clip in kvp.Value)
                    {
                        if (clip == null) continue;
                        var parts = clip.name.Split('_');
                        var dir = parts.Last();
                        if (!dirMap.ContainsKey(dir)) continue;
                        tree.AddChild(clip, dirMap[dir]);
                    }
                }

                var blendState = rootSM.AddState(stateName);
                blendState.motion = tree;
                blendState.speed = columnsCount / 30f; // Adjust speed to compensate for fewer frames
                EditorUtility.SetDirty(tree);
            }

            var states = rootSM.states.ToDictionary(s => s.state.name, s => s.state);

            AnimatorState idle = states["IdleBT"];
            AnimatorState idle2 = states.ContainsKey("Idle2BT") ? states["Idle2BT"] : null;
            AnimatorState idle3 = states.ContainsKey("Idle3BT") ? states["Idle3BT"] : null;
            AnimatorState idle4 = states.ContainsKey("Idle4BT") ? states["Idle4BT"] : null;
            AnimatorState crouchIdle = states.ContainsKey("CrouchIdleBT") ? states["CrouchIdleBT"] : null;
            AnimatorState rideIdle = states.ContainsKey("RideIdleBT") ? states["RideIdleBT"] : null;

            void Trans(AnimatorState from, AnimatorState to, string cond, bool ifTrue)
            {
                if (from == null || to == null) return;
                var t = from.AddTransition(to);
                t.AddCondition(ifTrue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, cond);
                t.hasExitTime = false;
                t.duration = 0f;
            }

            Trans(idle, idle2, "UseIdle2", true);
            Trans(idle2, idle, "UseIdle2", false);
            Trans(idle2, idle, "UseIdle3", false);
            Trans(idle2, idle, "UseIdle4", false);
            Trans(idle2, idle, "IsCrouching", false);
            Trans(idle2, idle, "IsMounted", false);

            Trans(idle, idle3, "UseIdle3", true);
            Trans(idle3, idle, "UseIdle3", false);
            Trans(idle3, idle, "UseIdle4", false);
            Trans(idle3, idle, "IsCrouching", false);
            Trans(idle3, idle, "IsMounted", false);

            Trans(idle, idle4, "UseIdle4", true);
            Trans(idle4, idle, "UseIdle4", false);
            Trans(idle4, idle, "IsCrouching", false);
            Trans(idle4, idle, "IsMounted", false);

            Trans(idle, crouchIdle, "IsCrouching", true);
            Trans(idle2, crouchIdle, "IsCrouching", true);
            Trans(idle3, crouchIdle, "IsCrouching", true);
            Trans(idle4, crouchIdle, "IsCrouching", true);

            if (crouchIdle != null) {
                var bci = crouchIdle.AddTransition(idle);
                bci.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCrouching");
                bci.AddCondition(AnimatorConditionMode.IfNot, 0, "UseIdle2");
                bci.AddCondition(AnimatorConditionMode.IfNot, 0, "UseIdle3");
                bci.AddCondition(AnimatorConditionMode.IfNot, 0, "UseIdle4");
                bci.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMounted");
                bci.hasExitTime = false;
                bci.duration = 0f;
            }

            Trans(idle, rideIdle, "IsMounted", true);
            Trans(idle2, rideIdle, "IsMounted", true);
            Trans(idle3, rideIdle, "IsMounted", true);
            Trans(idle4, rideIdle, "IsMounted", true);

            if (rideIdle != null) {
                var bri = rideIdle.AddTransition(idle);
                bri.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMounted");
                bri.AddCondition(AnimatorConditionMode.IfNot, 0, "UseIdle2");
                bri.AddCondition(AnimatorConditionMode.IfNot, 0, "UseIdle3");
                bri.AddCondition(AnimatorConditionMode.IfNot, 0, "UseIdle4");
                bri.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCrouching");
                bri.hasExitTime = false;
                bri.duration = 0f;
            }

            string[] trueIdles = { "IdleBT", "Idle2BT", "Idle3BT", "Idle4BT" };
            string[] moveStates = {
                "RunBT", "WalkBT", "StrafeLeftBT", "StrafeRightBT", "RunBackwardsBT"
            };
            string[] moveBools = {
                "IsRun", "IsWalk", "IsStrafeLeft", "IsStrafeRight", "IsRunBackwards"
            };

            foreach (string fromName in trueIdles)
            {
                foreach ((string toName, string cond) in moveStates.Zip(moveBools, (s, b) => (s, b)))
                {
                    if (!states.ContainsKey(fromName) || !states.ContainsKey(toName)) continue;
                    Trans(states[fromName], states[toName], cond, true);
                    Trans(states[toName], states[fromName], cond, false);
                }
            }

            // CrouchIdle <-> CrouchRun
            if (states.ContainsKey("CrouchRunBT"))
            {
                var cr = states["CrouchRunBT"];
                foreach (string b in moveBools)
                    Trans(crouchIdle, cr, b, true);

                var back = cr.AddTransition(crouchIdle);
                foreach (string b in moveBools)
                    back.AddCondition(AnimatorConditionMode.IfNot, 0, b);
                back.hasExitTime = false;
                back.duration = 0f;
            }

            // RideIdle <-> RideRun
            if (states.ContainsKey("RideRunBT"))
            {
                var rr = states["RideRunBT"];
                foreach (string b in moveBools)
                    Trans(rideIdle, rr, b, true);

                var back = rr.AddTransition(rideIdle);
                foreach (string b in moveBools)
                    back.AddCondition(AnimatorConditionMode.IfNot, 0, b);
                back.hasExitTime = false;
                back.duration = 0f;
            }

            // --- Setup one-shot sub machines ---
            string[] oneshots = {
                "Attack1", "Attack2", "Attack3", "Attack4", "Attack5",
                "AttackRun", "AttackRun2", "Special1", "Special2", "Taunt",
                "Die", "TakeDamage"
            };

            foreach (string group in oneshots)
            {
                if (!animationGroups.ContainsKey(group)) continue;
                var clips = animationGroups[group];
                var subSM = rootSM.AddStateMachine(group);

                foreach (var clip in clips)
                {
                    if (clip == null) continue;
                    var parts = clip.name.Split('_');
                    var dir = parts.Last();
                    if (!dirMap.ContainsKey(dir)) continue;

                    var st = subSM.AddState($"{group}_{dir}");
                    st.motion = clip;

                    var any = rootSM.AddAnyStateTransition(st);
                    any.AddCondition(AnimatorConditionMode.If, 0, group);
                    any.AddCondition(AnimatorConditionMode.Equals, dirMap[dir], "DirIndex");
                    any.hasExitTime = false;
                    any.duration = 0f;

                    if (idle != null)
                    {
                        var exitT = st.AddExitTransition(idle);
                        exitT.hasExitTime = true;
                        exitT.exitTime = 1f;
                        exitT.duration = 0f;
                    }
                }
            }

            rootSM.defaultState = idle;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (includePlayerPrefab && genericPlayerPrefab != null)
            {
                string prefabName = $"{characterName}_Player.prefab";

                // Go one level up from Animation Clips folder
                string animationClipsFolder = Path.GetDirectoryName(animatorPath);
                string characterFolder = Path.GetDirectoryName(animationClipsFolder);

                string relativeFolder = characterFolder.Replace(Application.dataPath, "Assets").Replace("\\", "/");
                string prefabAssetPath = Path.Combine(relativeFolder, prefabName).Replace("\\", "/");

                GameObject instance = PrefabUtility.InstantiatePrefab(genericPlayerPrefab) as GameObject;

                if (instance != null)
                {
                    Animator anim = instance.GetComponent<Animator>();
                    if (anim != null)
                    {
                        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animatorPath);
                    }

                    PrefabUtility.SaveAsPrefabAsset(instance, prefabAssetPath);
                    GameObject.DestroyImmediate(instance);

                    Debug.Log($"✅ Player prefab created at: {prefabAssetPath}");
                }
                else
                {
                    Debug.LogError("❌ Failed to instantiate generic player prefab.");
                }

                // Optional: preview the generated character
                if (shouldPreviewCharacter && previewContainer != null)
                {
                    GameObject loadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
                    if (loadedPrefab != null)
                    {
                        previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(loadedPrefab);
                        previewInstance.transform.SetParent(previewContainer.transform, false);

                        // Hide all other specified objects
                        foreach (var go in objectsToHideDuringPreview)
                        {
                            if (go != null) go.SetActive(false);
                        }

                        // Show preview canvas
                        if (previewCanvas != null)
                            previewCanvas.SetActive(true);

                        // Set camera to follow preview instance
                        if (utilityManager != null && previewInstance != null)
                        {
                            utilityManager.target = previewInstance.transform;
                        }
                        if (pixelPerfectCamera != null)
                        {
                            wasPixelPerfectEnabled = pixelPerfectCamera.enabled;
                            pixelPerfectCamera.enabled = false;
                        }

                    }
                }


            }

        }

        public void ClosePreview()
        {
            if (previewInstance != null)
            {
                DestroyImmediate(previewInstance);
                previewInstance = null;
            }

            foreach (var go in objectsToHideDuringPreview)
            {
                if (go != null) go.SetActive(true);
            }

            if (previewCanvas != null)
                previewCanvas.SetActive(false);

            // Reset camera target
            if (utilityManager != null && mainPlayerTransform != null)
            {
                utilityManager.target = mainPlayerTransform;
            }

            if (pixelPerfectCamera != null && wasPixelPerfectEnabled)
            {
                pixelPerfectCamera.enabled = true;
            }

        }

        public void SetShouldPreviewCharacter(bool value)
        {
            shouldPreviewCharacter = value;
        }



        public void SetIncludePlayerPrefab(bool value)
        {
            includePlayerPrefab = value;
        }


        private string GetDirectionName(int row)
        {
            return row switch
            {
                0 => "E",
                1 => "SE",
                2 => "S",
                3 => "SW",
                4 => "W",
                5 => "NW",
                6 => "N",
                7 => "NE",
                _ => "Unknown"
            };
        }
        #endif
    }
}
