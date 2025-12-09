namespace LoneEftDmaRadar.Misc
{
    /// <summary>
    /// Defines a simple rate-limiter.
    /// </summary>
    public struct RateLimiter
    {
        private readonly long _intervalTicks;
        private long _lastTicks;

        public RateLimiter() { }
        
        public RateLimiter(TimeSpan interval)
        {
            _intervalTicks = interval.Ticks;
        }

        /// <summary>
        /// Tries to enter the rate-limiter, and if successful, updates the last-entered time.
        /// </summary>
        /// <returns><see langword="true"/> if the rate-limiter was entered, otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnter()
        {
            long now = Stopwatch.GetTimestamp();
            long elapsed = Stopwatch.GetElapsedTime(_lastTicks, now).Ticks;
            if (elapsed >= _intervalTicks)
            {
                _lastTicks = now;
                return true;
            }
            return false;
        }
    }
}
