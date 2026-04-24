namespace EndlessJourney.Combat
{
    /// <summary>
    /// Result payload returned by hittable targets after a hit attempt.
    /// </summary>
    public struct HitResult
    {
        public bool WasApplied;
        public float DamageApplied;
        public bool KilledTarget;
        public string Reason;

        public static HitResult Applied(float damageApplied, bool killedTarget = false)
        {
            return new HitResult
            {
                WasApplied = true,
                DamageApplied = damageApplied,
                KilledTarget = killedTarget,
                Reason = string.Empty
            };
        }

        public static HitResult Blocked(string reason = "")
        {
            return new HitResult
            {
                WasApplied = false,
                DamageApplied = 0f,
                KilledTarget = false,
                Reason = reason ?? string.Empty
            };
        }
    }
}
