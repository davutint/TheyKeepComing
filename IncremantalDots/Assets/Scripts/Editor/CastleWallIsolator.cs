#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DeadWalls
{
    /// <summary>
    /// TEK AMACLI / GECICI yardimci (owner istegi): acik sahnedeki TUM Tilemap'lerde,
    /// asset adi "Wall " ile baslamayan tile'lari temizler -> sadece kale DUVAR tile'lari kalir.
    /// "Wall A1_S", "Wall D14_N" gibi duvarlar TUTULUR; "WallFlora..", "Door..", "Roof..",
    /// "Ground..", "Water.." vb. SILINIR. Tek Undo (Ctrl+Z) ile tamamen geri alinir.
    /// Amac: import edilen haritadan sadece kale duvarlarini gorup ayiklamak.
    /// Kalici olmasi gerekmez; ise yaramazsa Ctrl+Z, sonra bu dosya silinebilir.
    /// </summary>
    public static class CastleWallIsolator
    {
        // "Wall" (BOSLUKSUZ): hem "Wall A1_S" duvarlarini HEM "WallFlora A1_E" duvar bitkilerini tutar.
        // (owner: duvar bitkilerini silme dedi). Tileset'te "Wall" ile baslayan sadece Wall + WallFlora var.
        private const string KeepPrefix = "Wall";

        [MenuItem("Window/DeadWalls/Castle Isolate - Keep Only Wall Tiles")]
        public static void KeepOnlyWallTiles()
        {
            // GUVENLIK: yalnizca AKTIF sahne islenir. Baska sahne(ler) ayni anda acik olsa bile dokunulmaz.
            Scene activeScene = SceneManager.GetActiveScene();
            var allTilemaps = UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            var tilemaps = new List<Tilemap>();
            foreach (var tm in allTilemaps)
                if (tm.gameObject.scene == activeScene)
                    tilemaps.Add(tm);

            if (tilemaps.Count == 0)
            {
                EditorUtility.DisplayDialog("Castle Isolate", $"Aktif sahnede ('{activeScene.name}') Tilemap bulunamadi.", "Tamam");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "Castle Isolate",
                $"YALNIZCA aktif sahne: '{activeScene.name}' ({tilemaps.Count} tilemap).\n" +
                "Bu sahnedeki tilemap'lerde adi 'Wall' ile baslamayan tile'lar SILINECEK " +
                "(kale duvarlari + WallFlora duvar bitkileri KALIR). Door/Roof/Ground vb. gider.\n\n" +
                "Tile ASSET'lerine dokunulmaz; baska sahneler ETKILENMEZ. Tek Undo (Ctrl+Z) ile geri alinir.\n\nDevam edilsin mi?",
                "Evet, sadece duvarlari birak", "Iptal"))
            {
                return;
            }

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Keep Only Wall Tiles");

            int kept = 0, removed = 0, layersTouched = 0;
            var perLayerKept = new List<string>();

            foreach (var tm in tilemaps)
            {
                Undo.RegisterCompleteObjectUndo(tm, "Keep Only Wall Tiles");

                BoundsInt bounds = tm.cellBounds;
                var toClear = new List<Vector3Int>();
                int layerKept = 0;

                foreach (var pos in bounds.allPositionsWithin)
                {
                    TileBase t = tm.GetTile(pos);
                    if (t == null)
                        continue;

                    if (t.name != null && t.name.StartsWith(KeepPrefix, StringComparison.Ordinal))
                        layerKept++;
                    else
                        toClear.Add(pos);
                }

                kept += layerKept;
                if (toClear.Count > 0)
                {
                    foreach (var p in toClear)
                        tm.SetTile(p, null);
                    removed += toClear.Count;
                    layersTouched++;
                    tm.CompressBounds();
                    EditorUtility.SetDirty(tm);
                }

                if (layerKept > 0)
                    perLayerKept.Add($"{tm.name}: {layerKept}");
            }

            Undo.CollapseUndoOperations(group);

            string keptByLayer = perLayerKept.Count > 0 ? string.Join("\n", perLayerKept) : "(hicbir katmanda 'Wall ' tile yok!)";
            Debug.Log($"[CastleWallIsolator] Tutulan Wall: {kept}, Silinen: {removed}, Etkilenen katman: {layersTouched}.\nWall tile bulunan katmanlar:\n{keptByLayer}");
            EditorUtility.DisplayDialog(
                "Castle Isolate - Bitti",
                $"Tutulan 'Wall*' tile (Wall + WallFlora): {kept}\nSilinen tile: {removed}\nEtkilenen katman: {layersTouched}\n\n" +
                $"Wall tile bulunan katmanlar:\n{keptByLayer}\n\n" +
                "Begenmezsen Ctrl+Z. (Wall + WallFlora tutuldu; Door/Roof/Ground haric.)",
                "Tamam");
        }
    }
}
#endif
