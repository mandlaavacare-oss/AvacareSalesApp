using Microsoft.AspNetCore.Identity;

namespace Server.Infrastructure.Authentication.Models;

public class ApplicationUser : IdentityUser
{
    public string? CustomerId { get; set; }
}
