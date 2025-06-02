using System;

namespace UnbelievaBoat.Net
{
    public class RetryPolicyConfig
    {
        public int MaxRetries { get; set; } = 3;
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(10);
        public double BackoffFactor { get; set; } = 2.0;
        public bool RetryOnRateLimitExceeded { get; set; } = false; // For 429 if they want to retry
    }
}
