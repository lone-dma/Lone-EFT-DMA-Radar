namespace LoneEftDmaRadar.Misc
{
    /// <summary>
    /// Defines a simple rate-limiter.
    /// </summary>
    public struct RateLimiter
    {
        private readonly TimeSpan _interval = TimeSpan.Zero;
        private DateTimeOffset _last = DateTimeOffset.MinValue;

        public RateLimiter() { }
        public RateLimiter(TimeSpan interval)
        {
            _interval = interval;
        }

        /// <summary>
        /// Tries to enter the rate-limiter, and if successful, updates the last-entered time.
        /// </summary>
        /// <returns><see langword="true"/> if the rate-limiter was entered, otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnter()
        {
            var now = DateTimeOffset.UtcNow;
            if (now - _last >= _interval)
            {
                _last = now;
                return true;
            }
            return false;
        }
    }
}
