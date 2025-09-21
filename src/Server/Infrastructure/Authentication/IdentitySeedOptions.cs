namespace Server.Infrastructure.Authentication;

public class IdentitySeedOptions
{
    public SeedUserOptions Admin { get; set; } = new();
    public SeedUserOptions Customer { get; set; } = new();

    public class SeedUserOptions
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
    }
}
