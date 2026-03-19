using UnityEngine;
using UnityEditor;
using System.IO;

namespace DeadWalls
{
    /// <summary>
    /// Character Creator - Fantasy 2D'den export edilen animasyon PNG'lerini
    /// tek bir atlas texture'a birlestiren Editor araci.
    ///
    /// Kullanim:
    ///   Window > DeadWalls > Sprite Atlas Generator
    ///   4 PNG sec (Walk, Attack, Die, Idle) → Generate Atlas
    ///
    /// Cikti: 1920x4096 px atlas (15 col x 32 row)
    ///   Row  0- 7: Walk
    ///   Row  8-15: Attack
    ///   Row 16-23: Die
    ///   Row 24-31: Idle
    /// </summary>
    public class SpriteAtlasGenerator : EditorWindow
    {
        // Her animasyon PNG'si (1920x1024 = 15 col x 8 row x 128px)
        Texture2D walkSheet;
        Texture2D attackSheet;
        Texture2D dieSheet;
        Texture2D idleSheet;

        string outputFolder = "Assets/Art/Atlases";
        string outputName = "zombie_atlas";

        Vector2 scrollPos;

        [MenuItem("Window/DeadWalls/Sprite Atlas Generator")]
        static void ShowWindow()
        {
            var w = GetWindow<SpriteAtlasGenerator>("Atlas Generator");
            w.minSize = new Vector2(400, 380);
        }

        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.LabelField("Sprite Atlas Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Character Creator'dan export edilen 4 animasyon PNG'sini\n" +
                "tek atlas'a birlestirir (15 col x 32 row, 1920x4096 px).\n\n" +
                "Siralama: Walk (Row 0-7) → Attack (Row 8-15) → Die (Row 16-23) → Idle (Row 24-31)",
                MessageType.Info);
            EditorGUILayout.Space(8);

            // PNG slot'lari
            EditorGUILayout.LabelField("Animasyon PNG'leri (her biri 1920x1024)", EditorStyles.boldLabel);
            walkSheet = TextureField("Walk (Row 0-7)", walkSheet);
            attackSheet = TextureField("Attack (Row 8-15)", attackSheet);
            dieSheet = TextureField("Die (Row 16-23)", dieSheet);
            idleSheet = TextureField("Idle (Row 24-31)", idleSheet);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Cikti Ayarlari", EditorStyles.boldLabel);
            outputFolder = EditorGUILayout.TextField("Klasor", outputFolder);
            outputName = EditorGUILayout.TextField("Dosya Adi", outputName);

            EditorGUILayout.Space(12);

            // Dogrulama
            bool allAssigned = walkSheet != null && attackSheet != null
                && dieSheet != null && idleSheet != null;

            EditorGUI.BeginDisabledGroup(!allAssigned);
            if (GUILayout.Button("Generate Atlas", GUILayout.Height(36)))
                GenerateAtlas();
            EditorGUI.EndDisabledGroup();

            if (!allAssigned)
                EditorGUILayout.HelpBox("Tum 4 PNG slot'u doldurulmali.", MessageType.Warning);

            EditorGUILayout.EndScrollView();
        }

        Texture2D TextureField(string label, Texture2D tex)
        {
            return (Texture2D)EditorGUILayout.ObjectField(
                label, tex, typeof(Texture2D), false);
        }

        void GenerateAtlas()
        {
            // Boyut dogrulama
            const int expectedW = 1920;
            const int expectedH = 1024;

            Texture2D[] sheets = { walkSheet, attackSheet, dieSheet, idleSheet };
            string[] names = { "Walk", "Attack", "Die", "Idle" };

            // Readable kopyalar olustur (kaynak texture Read/Write kapaliysa)
            Texture2D[] readable = new Texture2D[4];
            for (int i = 0; i < 4; i++)
            {
                readable[i] = MakeReadable(sheets[i]);
                if (readable[i].width != expectedW || readable[i].height != expectedH)
                {
                    EditorUtility.DisplayDialog("Boyut Hatasi",
                        $"{names[i]} PNG boyutu {readable[i].width}x{readable[i].height},\n" +
                        $"beklenen: {expectedW}x{expectedH}.\n\n" +
                        "Character Creator'dan 15x8 grid (128px/frame) export ettiginizden emin olun.",
                        "Tamam");

                    // Temizlik
                    for (int j = 0; j <= i; j++)
                        if (readable[j] != sheets[j])
                            DestroyImmediate(readable[j]);
                    return;
                }
            }

            // Atlas olustur (1920 x 4096)
            int atlasW = expectedW;
            int atlasH = expectedH * 4;
            var atlas = new Texture2D(atlasW, atlasH, TextureFormat.RGBA32, false);

            // PNG'leri yukari-asagi sirala: Walk (ust) → Idle (alt)
            // Texture2D koordinat sistemi: y=0 alt, y=max ust
            // Walk   (Row 0-7)  → atlas'in EN UST kismi → y offset = 3 * 1024
            // Attack (Row 8-15) → y offset = 2 * 1024
            // Die    (Row 16-23)→ y offset = 1 * 1024
            // Idle   (Row 24-31)→ atlas'in EN ALT kismi → y offset = 0
            for (int i = 0; i < 4; i++)
            {
                int yOffset = (3 - i) * expectedH;
                Color[] pixels = readable[i].GetPixels(0, 0, expectedW, expectedH);
                atlas.SetPixels(0, yOffset, expectedW, expectedH, pixels);
            }

            atlas.Apply();

            // Klasor olustur
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
                AssetDatabase.Refresh();
            }

            // PNG kaydet
            string path = $"{outputFolder}/{outputName}.png";
            byte[] pngData = atlas.EncodeToPNG();
            File.WriteAllBytes(path, pngData);

            // Temizlik
            DestroyImmediate(atlas);
            for (int i = 0; i < 4; i++)
                if (readable[i] != sheets[i])
                    DestroyImmediate(readable[i]);

            AssetDatabase.Refresh();

            // Import ayarlarini otomatik set et
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 128;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 8192;
                importer.mipmapEnabled = false;

                // Default platform ayarlarini ACIKCA set et
                // (yoksa Unity 2048 default'unu kullanir ve atlas'i kuculttur)
                var defaultSettings = importer.GetDefaultPlatformTextureSettings();
                defaultSettings.maxTextureSize = 8192;
                defaultSettings.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(defaultSettings);

                importer.SaveAndReimport();
            }

            EditorUtility.DisplayDialog("Atlas Olusturuldu",
                $"Atlas kaydedildi: {path}\n" +
                $"Boyut: {atlasW}x{atlasH} px\n" +
                $"Layout: 15 col x 32 row (4 anim x 8 yon)\n\n" +
                "Import ayarlari otomatik set edildi:\n" +
                "  PPU=128, FilterMode=Point, Compression=None",
                "Tamam");

            // Asset'i sec
            var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }

        /// <summary>
        /// Texture'in Read/Write enabled kopyasini olusturur.
        /// Kaynak texture readable degilse RenderTexture uzerinden piksel okur.
        /// </summary>
        static Texture2D MakeReadable(Texture2D source)
        {
            if (source.isReadable) return source;

            // GPU'dan CPU'ya kopyala
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0,
                RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);

            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            return copy;
        }
    }
}
