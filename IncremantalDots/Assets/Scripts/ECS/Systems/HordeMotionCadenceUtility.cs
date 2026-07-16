using Unity.Mathematics;

namespace DeadWalls
{
    /// <summary>
    /// Pool'dan sahaya donen zombie'leri ayni frame/timer noktasinda baslatmak yerine
    /// entity kimligi ve pool generation ile deterministik faz bantlarina dagitir.
    /// Gameplay movement, attack cooldown veya authored FPS degerini degistirmez.
    /// </summary>
    public static class HordeMotionCadenceUtility
    {
        public const int TimerSlices = 16;

        public static void Seed(ref SpriteAnimation animation, int entityIndex, uint generation)
        {
            int frameCount = math.max(1, animation.FrameCount);
            uint safeIndex = (uint)math.max(0, entityIndex) + 1u;
            uint hash = math.hash(new uint2(
                safeIndex ^ 0x9E3779B9u,
                generation ^ 0x85EBCA6Bu));

            animation.CurrentFrame = (int)(hash % (uint)frameCount);
            if (animation.FrameInterval <= 0f)
            {
                animation.FrameTimer = 0f;
                return;
            }

            uint timerSlice = (hash >> 16) % TimerSlices;
            float timer01 = (timerSlice + 0.5f) / TimerSlices;
            animation.FrameTimer = animation.FrameInterval * timer01;
        }

        public static void Advance(ref SpriteAnimation animation, float deltaTime)
        {
            int frameCount = math.max(1, animation.FrameCount);
            float frameInterval = math.max(0.0001f, animation.FrameInterval);
            animation.FrameTimer = math.max(0f, animation.FrameTimer + deltaTime);

            int elapsedFrames = (int)math.floor(animation.FrameTimer / frameInterval);
            if (elapsedFrames > 0)
            {
                animation.FrameTimer -= elapsedFrames * frameInterval;
                animation.CurrentFrame = (animation.CurrentFrame + elapsedFrames) % frameCount;
            }
            else
            {
                animation.CurrentFrame = math.clamp(
                    animation.CurrentFrame, 0, frameCount - 1);
            }
        }
    }
}
