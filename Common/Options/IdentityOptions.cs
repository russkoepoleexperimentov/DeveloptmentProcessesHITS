namespace Web.Options
{
    public class IdentityPasswordOptions
    {
        public int RequiredLength { get; set; }
        public bool RequireDigit { get; set; }
        public bool RequireUppercase { get; set; }
        public bool RequireLowercase { get; set; }
        public bool RequireNonAlphanumeric { get; set; }
    }
}