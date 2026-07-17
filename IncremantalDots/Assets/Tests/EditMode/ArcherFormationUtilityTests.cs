using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class ArcherFormationUtilityTests
    {
        private static readonly Vector2 RightVertex = new Vector2(0.5f, 0f);
        private static readonly Vector2 TopVertex = new Vector2(0f, 0.25f);

        [Test]
        public void FormationContract_AssetOwnsFortyCellsAndUtilityOwnsVersionedTwentyFiveSlots()
        {
            const string assetPath =
                "Assets/ScriptableObject/MobileCastle/Archers/ArcherFormationV1.asset";
            ArcherFormationDefinitionSO definition =
                AssetDatabase.LoadAssetAtPath<ArcherFormationDefinitionSO>(assetPath);

            Assert.That(definition, Is.Not.Null,
                "Canonical ArcherFormationV1 asset bulunamadi.");
            Assert.That(definition.ValidateV1(out string problem), Is.True, problem);
            Assert.That(definition.Version,
                Is.EqualTo(ArcherFormationUtility.CurrentVersion));
            Assert.That(definition.TileCoordinates.Length,
                Is.EqualTo(ArcherFormationUtility.RequiredTileCount));
            Assert.That(ArcherFormationUtility.MatchesCanonicalV1(
                definition.TileCoordinates), Is.True);
            Assert.That(ArcherFormationUtility.SlotsPerTile, Is.EqualTo(25));
            Assert.That(ArcherFormationUtility.TotalCapacity,
                Is.EqualTo(definition.TileCoordinates.Length
                    * ArcherFormationUtility.SlotsPerTile));
            Assert.That(ArcherFormationUtility.TotalCapacity, Is.EqualTo(1000));
            Assert.That(ArcherFormationUtility.NormalizeVersion(0),
                Is.EqualTo(ArcherFormationUtility.CurrentVersion));
        }

        [Test]
        public void CanonicalV1Layout_ContainsExactOrderedFortyOutsideTiles()
        {
            Vector3Int[] coordinates = ArcherFormationUtility.CreateCanonicalV1TileCoordinates();

            Assert.That(coordinates.Length, Is.EqualTo(40));
            Assert.That(new HashSet<Vector3Int>(coordinates).Count, Is.EqualTo(40));
            Assert.That(coordinates[0], Is.EqualTo(new Vector3Int(0, 0, 0)));
            Assert.That(coordinates[1], Is.EqualTo(new Vector3Int(1, 1, 0)));
            Assert.That(coordinates[2], Is.EqualTo(new Vector3Int(-1, -1, 0)));
            Assert.That(coordinates[39], Is.EqualTo(new Vector3Int(20, 20, 0)));
            Assert.That(ArcherFormationUtility.MatchesCanonicalV1(coordinates), Is.True);
        }

        [Test]
        public void V1TileOffsets_AreDeterministicInsideDiamondAndRespectMinimumDistance()
        {
            Vector3Int[] coordinates = ArcherFormationUtility.CreateCanonicalV1TileCoordinates();
            Vector2[] firstTileOffsets = null;

            for (int tileIndex = 0; tileIndex < coordinates.Length; tileIndex++)
            {
                Assert.That(ArcherFormationUtility.TryGenerateTileOffsets(
                    coordinates[tileIndex],
                    RightVertex,
                    TopVertex,
                    ArcherFormationUtility.CurrentVersion,
                    ArcherFormationUtility.SlotsPerTile,
                    ArcherFormationUtility.DefaultSafeInset,
                    ArcherFormationUtility.DefaultMinimumLocalDistance,
                    ArcherFormationUtility.DefaultCandidateAttempts,
                    out Vector2[] offsets), Is.True);
                Assert.That(offsets.Length, Is.EqualTo(25));

                Assert.That(ArcherFormationUtility.TryGenerateTileOffsets(
                    coordinates[tileIndex],
                    RightVertex,
                    TopVertex,
                    ArcherFormationUtility.CurrentVersion,
                    ArcherFormationUtility.SlotsPerTile,
                    ArcherFormationUtility.DefaultSafeInset,
                    ArcherFormationUtility.DefaultMinimumLocalDistance,
                    ArcherFormationUtility.DefaultCandidateAttempts,
                    out Vector2[] repeated), Is.True);

                for (int i = 0; i < offsets.Length; i++)
                {
                    Assert.That(repeated[i], Is.EqualTo(offsets[i]));
                    Assert.That(ArcherFormationUtility.IsInsideDiamond(
                        offsets[i],
                        RightVertex,
                        TopVertex,
                        ArcherFormationUtility.DefaultSafeInset), Is.True,
                        $"Tile {tileIndex}, slot {i} diamond disinda.");

                    for (int j = 0; j < i; j++)
                    {
                        Assert.That(Vector2.Distance(offsets[i], offsets[j]),
                            Is.GreaterThanOrEqualTo(
                                ArcherFormationUtility.DefaultMinimumLocalDistance - 0.00001f),
                            $"Tile {tileIndex}, slot {i}/{j} minimum mesafeyi bozdu.");
                    }
                }

                if (tileIndex == 0)
                    firstTileOffsets = offsets;
                else if (tileIndex == 1)
                    Assert.That(offsets[0], Is.Not.EqualTo(firstTileOffsets[0]),
                        "Tile coordinate seed'i lokal dagilimi degistirmeli.");
            }
        }

        [Test]
        public void ArcherIndex_UsesLayerFirstFortyByTwentyFiveFillOrder()
        {
            Assert.That(ArcherFormationUtility.TotalCapacity, Is.EqualTo(1000));
            Assert.That(ArcherFormationUtility.GetTileIndex(0), Is.Zero);
            Assert.That(ArcherFormationUtility.GetTileIndex(39), Is.EqualTo(39));
            Assert.That(ArcherFormationUtility.GetLocalSlotIndex(39), Is.Zero);
            Assert.That(ArcherFormationUtility.GetTileIndex(40), Is.Zero);
            Assert.That(ArcherFormationUtility.GetLocalSlotIndex(40), Is.EqualTo(1));
            Assert.That(ArcherFormationUtility.GetTileIndex(999), Is.EqualTo(39));
            Assert.That(ArcherFormationUtility.GetLocalSlotIndex(999), Is.EqualTo(24));
            Assert.That(ArcherFormationUtility.GetTileIndex(1000), Is.EqualTo(-1));
            Assert.That(ArcherFormationUtility.GetLocalSlotIndex(-1), Is.EqualTo(-1));
        }
    }
}
