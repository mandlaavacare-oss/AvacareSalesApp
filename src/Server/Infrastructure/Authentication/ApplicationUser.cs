using Microsoft.AspNetCore.Identity;

namespace Server.Infrastructure.Authentication;

public class ApplicationUser : IdentityUser
{
    public string? CustomerId { get; set; }
}
