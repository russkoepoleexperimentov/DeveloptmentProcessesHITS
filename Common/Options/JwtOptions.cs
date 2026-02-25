namespace Common.Options
{
    public class JwtOptions
    {
        public AccessOptions Access { get; set; } = null!;
        public RefreshOptions Refresh { get; set; } = null!;
    }

    public class AccessOptions
    {
        public string Secret { get; set; } = null!;
        public int LifetimeMinutes { get; set; }
        public string ClaimValidSession { get; set; } = null!;
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
    }

    public class RefreshOptions
    {
        public string Secret { get; set; } = null!;
        public int LifetimeDays { get; set; }
    }
}