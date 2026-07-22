using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class GameplayToastServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            GameplayToastService.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            GameplayToastService.Clear();
        }

        [Test]
        public void Queue_PreservesFifoOrderToneAndDuration()
        {
            Assert.That(GameplayToastService.TryEnqueue(
                "FIRST",
                GameplayToastTone.Warning,
                3.2f), Is.True);
            Assert.That(GameplayToastService.TryEnqueue(
                "SECOND",
                GameplayToastTone.Secondary,
                1.1f), Is.True);

            Assert.That(GameplayToastService.TryDequeue(out GameplayToastMessage first), Is.True);
            Assert.That(first.Text, Is.EqualTo("FIRST"));
            Assert.That(first.Tone, Is.EqualTo(GameplayToastTone.Warning));
            Assert.That(first.DurationSeconds, Is.EqualTo(3.2f));
            Assert.That(GameplayToastService.TryDequeue(out GameplayToastMessage second), Is.True);
            Assert.That(second.Text, Is.EqualTo("SECOND"));
            Assert.That(second.Tone, Is.EqualTo(GameplayToastTone.Secondary));
        }

        [Test]
        public void Queue_RejectsEmptyAndRemainsBounded()
        {
            Assert.That(GameplayToastService.TryEnqueue("  "), Is.False);
            for (int i = 0; i < GameplayToastService.MaximumPendingMessages; i++)
                Assert.That(GameplayToastService.TryEnqueue("MESSAGE " + i), Is.True);

            Assert.That(GameplayToastService.PendingCount,
                Is.EqualTo(GameplayToastService.MaximumPendingMessages));
            Assert.That(GameplayToastService.TryEnqueue("OVERFLOW"), Is.False);
        }

        [Test]
        public void Queue_ClampsPresentationDuration()
        {
            Assert.That(GameplayToastService.TryEnqueue("SHORT", durationSeconds: 0.1f), Is.True);
            Assert.That(GameplayToastService.TryEnqueue("LONG", durationSeconds: 99f), Is.True);
            Assert.That(GameplayToastService.TryDequeue(out GameplayToastMessage shortMessage), Is.True);
            Assert.That(GameplayToastService.TryDequeue(out GameplayToastMessage longMessage), Is.True);
            Assert.That(shortMessage.DurationSeconds,
                Is.EqualTo(GameplayToastService.MinimumDurationSeconds));
            Assert.That(longMessage.DurationSeconds,
                Is.EqualTo(GameplayToastService.MaximumDurationSeconds));
        }

        [Test]
        public void Queue_PreservesRepeatedMessagesAsSeparatePlayerActions()
        {
            Assert.That(GameplayToastService.TryEnqueue("SAME ACTION"), Is.True);
            Assert.That(GameplayToastService.TryEnqueue("SAME ACTION"), Is.True);
            Assert.That(GameplayToastService.TryEnqueue("SAME ACTION"), Is.True);

            Assert.That(GameplayToastService.PendingCount, Is.EqualTo(3));
            for (int i = 0; i < 3; i++)
            {
                Assert.That(GameplayToastService.TryDequeue(out GameplayToastMessage message), Is.True);
                Assert.That(message.Text, Is.EqualTo("SAME ACTION"));
            }
        }
    }
}
