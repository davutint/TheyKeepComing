using UnityEditor;
using UnityEngine;

namespace DeadWalls.Editor
{
    public static class HordeReadabilitySetup
    {
        public const string MaterialPath = "Assets/Materials/Vampire.mat";
        public const string ShaderName = "DeadWalls/SpriteSheet";

        private static readonly Vector4 Readability = new Vector4(0.66f, 1f, 0.56f, 0f);
        private static readonly Color EdgeColor = new Color(0.18f, 0.26f, 0.36f, 1f);
        private static readonly Color GroundColor = new Color(0.03f, 0.045f, 0.065f, 1f);

        [MenuItem("Window/DeadWalls/Repair Horde Readability")]
        public static void Repair()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Shader shader = Shader.Find(ShaderName);
            if (material == null || shader == null)
            {
                Debug.LogError($"[HordeReadability] Missing material or shader: {MaterialPath} / {ShaderName}");
                return;
            }

            Undo.RecordObject(material, "Repair Horde Readability");
            material.shader = shader;
            material.enableInstancing = true;
            material.SetVector("_HordeReadability", Readability);
            material.SetColor("_HordeEdgeColor", EdgeColor);
            material.SetColor("_HordeGroundColor", GroundColor);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            Debug.Log("[HordeReadability] Vampire material repaired without extra passes or renderers.");
        }
    }
}
