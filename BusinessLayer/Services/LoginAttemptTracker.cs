using System.Collections.Concurrent;

namespace BusinessLayer.Services
{
    /// <summary>
    /// Tracks failed login attempts per combination of username and client IP address.
    /// After <see cref="MaxFailures"/> failed attempts the combination is locked for <see cref="LockDuration"/>.
    /// </summary>
    /// <remarks>
    /// Keying by username + IP means that a classroom behind one NAT address is not blocked as a whole
    /// (only the account that keeps failing is), and an attacker from another IP cannot lock out a victim's account.
    /// A successful login resets the counter. State is kept in memory only (resets on application restart).
    /// </remarks>
    public class LoginAttemptTracker
    {
        /// <summary>
        /// Number of failed attempts after which the username + IP combination is locked.
        /// </summary>
        public const int MaxFailures = 5;

        /// <summary>
        /// How long the lock lasts (counted from the last failed attempt).
        /// </summary>
        public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(5);

        /// <summary>
        /// When the number of tracked entries exceeds this value, expired entries are purged.
        /// </summary>
        private const int CleanupThreshold = 10_000;

        private readonly ConcurrentDictionary<string, Entry> _entries = new();
        private readonly TimeProvider _timeProvider;

        private sealed record Entry(int Failures, DateTimeOffset LastFailure);

        /// <summary>
        /// Creates a new tracker.
        /// </summary>
        /// <param name="timeProvider">Time source (for tests); defaults to the system clock.</param>
        public LoginAttemptTracker(TimeProvider? timeProvider = null)
        {
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <summary>
        /// Returns <c>true</c> when the username + IP combination is currently locked.
        /// </summary>
        public bool IsLocked(string? username, string? ip)
        {
            string key = Key(username, ip);
            if (!_entries.TryGetValue(key, out var entry))
            {
                return false;
            }

            if (IsExpired(entry, _timeProvider.GetUtcNow()))
            {
                _entries.TryRemove(key, out _);
                return false;
            }

            return entry.Failures >= MaxFailures;
        }

        /// <summary>
        /// Records one failed login attempt for the username + IP combination.
        /// </summary>
        public void RegisterFailure(string? username, string? ip)
        {
            var now = _timeProvider.GetUtcNow();
            _entries.AddOrUpdate(
                Key(username, ip),
                _ => new Entry(1, now),
                (_, existing) => IsExpired(existing, now) ? new Entry(1, now) : new Entry(existing.Failures + 1, now));

            if (_entries.Count > CleanupThreshold)
            {
                RemoveExpired(now);
            }
        }

        /// <summary>
        /// Clears the failed attempts for the username + IP combination (call after a successful login).
        /// </summary>
        public void Reset(string? username, string? ip)
        {
            _entries.TryRemove(Key(username, ip), out _);
        }

        private static string Key(string? username, string? ip)
        {
            return $"{(username ?? string.Empty).Trim().ToLowerInvariant()}|{ip ?? "unknown"}";
        }

        private static bool IsExpired(Entry entry, DateTimeOffset now)
        {
            return now - entry.LastFailure >= LockDuration;
        }

        private void RemoveExpired(DateTimeOffset now)
        {
            foreach (var pair in _entries)
            {
                if (IsExpired(pair.Value, now))
                {
                    _entries.TryRemove(pair.Key, out _);
                }
            }
        }
    }
}
