using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadWalls.Tests
{
    public class HordeReadabilityTests
    {
        [Test]
        public void MotionCadence_SeedDistributesFramesAndTimerSlicesDeterministically()
        {
            var frames = new HashSet<int>();
            var timers = new HashSet<int>();
            for (int index = 0; index < 240; index++)
            {
                SpriteAnimation animation = CreateAnimation();
                HordeMotionCadenceUtility.Seed(ref animation, index, 1u);
                frames.Add(animation.CurrentFrame);
                timers.Add(Mathf.RoundToInt(animation.FrameTimer * 100000f));
                Assert.That(animation.FrameInterval, Is.EqualTo(0.1f).Within(0.0001f));
                Assert.That(animation.FrameTimer, Is.GreaterThan(0f).And.LessThan(0.1f));
            }

            Assert.That(frames.Count, Is.GreaterThanOrEqualTo(12));
            Assert.That(timers.Count, Is.GreaterThanOrEqualTo(12));

            SpriteAnimation first = CreateAnimation();
            SpriteAnimation second = CreateAnimation();
            HordeMotionCadenceUtility.Seed(ref first, 42, 7u);
            HordeMotionCadenceUtility.Seed(ref second, 42, 7u);
            Assert.That(second.CurrentFrame, Is.EqualTo(first.CurrentFrame));
            Assert.That(second.FrameTimer, Is.EqualTo(first.FrameTimer).Within(0.000001f));
        }

        [Test]
        public void MotionCadence_AdvanceCatchesUpAcrossFrameHitchInConstantWork()
        {
            SpriteAnimation animation = CreateAnimation();
            animation.CurrentFrame = 2;
            animation.FrameTimer = 0.025f;

            HordeMotionCadenceUtility.Advance(ref animation, 0.325f);

            Assert.That(animation.CurrentFrame, Is.EqualTo(5));
            Assert.That(animation.FrameTimer, Is.EqualTo(0.05f).Within(0.0001f));
        }

        [Test]
        public void VampireMaterial_UsesBoundedSinglePassOpaqueReadabilityContract()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/Vampire.mat");

            Assert.That(material, Is.Not.Null);
            Assert.That(material.shader.name, Is.EqualTo("DeadWalls/SpriteSheet"));
            Assert.That(material.GetTag("RenderType", false), Is.EqualTo("Opaque"));
            Assert.That(material.renderQueue, Is.EqualTo((int)RenderQueue.Geometry));
            Assert.That(material.passCount, Is.EqualTo(1));
            Assert.That(material.enableInstancing, Is.True);
            Assert.That(material.HasProperty("_HordeReadability"), Is.True);
            Assert.That(material.HasProperty("_HordeEdgeColor"), Is.True);
            Assert.That(material.HasProperty("_HordeGroundColor"), Is.True);

            Vector4 readability = material.GetVector("_HordeReadability");
            Assert.That(readability.x, Is.InRange(0.5f, 1f));
            Assert.That(readability.y, Is.InRange(0.5f, 1.25f));
            Assert.That(readability.z, Is.InRange(0.5f, 0.75f));
            Assert.That(readability.w, Is.Zero.Within(0.0001f));
        }

        private static SpriteAnimation CreateAnimation()
        {
            return new SpriteAnimation
            {
                TotalColumns = 15,
                TotalRows = 32,
                DirectionRow = 4,
                FrameCount = 15,
                CurrentFrame = 0,
                FrameTimer = 0f,
                FrameInterval = 0.1f
            };
        }
    }
}
