using BusinessLayer.Services;
using Xunit;

namespace NetResVM.UnitTests
{
    public class LoginAttemptTrackerTests
    {
        private sealed class FakeTimeProvider : TimeProvider
        {
            public DateTimeOffset Now { get; set; } = new DateTimeOffset(2026, 10, 19, 8, 0, 0, TimeSpan.Zero);
            public override DateTimeOffset GetUtcNow() => Now;
        }

        private readonly FakeTimeProvider _time = new();
        private readonly LoginAttemptTracker _tracker;

        public LoginAttemptTrackerTests()
        {
            _tracker = new LoginAttemptTracker(_time);
        }

        private void Fail(string user, string ip, int times)
        {
            for (int i = 0; i < times; i++)
            {
                _tracker.RegisterFailure(user, ip);
            }
        }

        [Fact]
        public void IsLocked_NoFailures_ReturnsFalse()
        {
            Assert.False(_tracker.IsLocked("nov0123", "10.0.0.1"));
        }

        [Fact]
        public void IsLocked_BelowLimit_ReturnsFalse()
        {
            Fail("nov0123", "10.0.0.1", LoginAttemptTracker.MaxFailures - 1);
            Assert.False(_tracker.IsLocked("nov0123", "10.0.0.1"));
        }

        [Fact]
        public void IsLocked_AtLimit_ReturnsTrue()
        {
            Fail("nov0123", "10.0.0.1", LoginAttemptTracker.MaxFailures);
            Assert.True(_tracker.IsLocked("nov0123", "10.0.0.1"));
        }

        [Fact]
        public void IsLocked_OtherUserFromSameIp_IsNotLocked()
        {
            // Classroom behind one NAT address: one student failing must not block the others
            Fail("nov0123", "10.0.0.1", LoginAttemptTracker.MaxFailures);
            Assert.False(_tracker.IsLocked("svo0099", "10.0.0.1"));
        }

        [Fact]
        public void IsLocked_SameUserFromOtherIp_IsNotLocked()
        {
            // An attacker from another IP cannot lock the victim's account
            Fail("nov0123", "10.0.0.1", LoginAttemptTracker.MaxFailures);
            Assert.False(_tracker.IsLocked("nov0123", "10.0.0.2"));
        }

        [Fact]
        public void IsLocked_UsernameIsCaseInsensitive()
        {
            Fail("NOV0123", "10.0.0.1", LoginAttemptTracker.MaxFailures);
            Assert.True(_tracker.IsLocked(" nov0123 ", "10.0.0.1"));
        }

        [Fact]
        public void Reset_UnlocksUser()
        {
            Fail("nov0123", "10.0.0.1", LoginAttemptTracker.MaxFailures);
            _tracker.Reset("nov0123", "10.0.0.1");
            Assert.False(_tracker.IsLocked("nov0123", "10.0.0.1"));
        }

        [Fact]
        public void IsLocked_AfterLockDuration_Unlocks()
        {
            Fail("nov0123", "10.0.0.1", LoginAttemptTracker.MaxFailures);
            _time.Now += LoginAttemptTracker.LockDuration;
            Assert.False(_tracker.IsLocked("nov0123", "10.0.0.1"));
        }

        [Fact]
        public void RegisterFailure_AfterExpiry_StartsCountingAgain()
        {
            Fail("nov0123", "10.0.0.1", LoginAttemptTracker.MaxFailures - 1);
            _time.Now += LoginAttemptTracker.LockDuration;
            Fail("nov0123", "10.0.0.1", 1);
            Assert.False(_tracker.IsLocked("nov0123", "10.0.0.1"));
        }

        [Fact]
        public void IsLocked_NullInputs_DoNotThrow()
        {
            _tracker.RegisterFailure(null, null);
            Assert.False(_tracker.IsLocked(null, null));
        }
    }
}
