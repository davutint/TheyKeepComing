using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    public enum GameplayToastTone : byte
    {
        Primary = 0,
        Secondary = 1,
        Warning = 2,
        Critical = 3
    }

    public readonly struct GameplayToastMessage
    {
        public GameplayToastMessage(string text, GameplayToastTone tone, float durationSeconds)
        {
            Text = text;
            Tone = tone;
            DurationSeconds = durationSeconds;
        }

        public string Text { get; }
        public GameplayToastTone Tone { get; }
        public float DurationSeconds { get; }
    }

    /// <summary>
    /// Gameplay toast taleplerini bounded FIFO kuyrugunda tutar. Hangi gameplay durumunun
    /// toast uretecegine karar vermez; yalniz onaylanmis cagrilari sunum owner'ina tasir.
    /// </summary>
    public static class GameplayToastService
    {
        public const int MaximumPendingMessages = 8;
        public const int MaximumVisibleMessages = 3;
        public const float DefaultDurationSeconds = 2.4f;
        public const float MinimumDurationSeconds = 0.8f;
        public const float MaximumDurationSeconds = 6f;
        public const float ExitAnimationSeconds = 0.18f;

        private static readonly Queue<GameplayToastMessage> Pending =
            new Queue<GameplayToastMessage>(MaximumPendingMessages);

        public static int PendingCount => Pending.Count;

        public static bool TryEnqueue(
            string text,
            GameplayToastTone tone = GameplayToastTone.Primary,
            float durationSeconds = DefaultDurationSeconds)
        {
            if (string.IsNullOrWhiteSpace(text) || Pending.Count >= MaximumPendingMessages)
                return false;

            Pending.Enqueue(new GameplayToastMessage(
                text.Trim(),
                tone,
                Mathf.Clamp(durationSeconds, MinimumDurationSeconds, MaximumDurationSeconds)));
            return true;
        }

        public static bool TryDequeue(out GameplayToastMessage message)
        {
            if (Pending.Count == 0)
            {
                message = default;
                return false;
            }

            message = Pending.Dequeue();
            return true;
        }

        public static void Clear()
        {
            Pending.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewPlayerLoop()
        {
            Clear();
        }
    }
}
