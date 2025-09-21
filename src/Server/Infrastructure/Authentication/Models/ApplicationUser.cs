using Microsoft.AspNetCore.Identity;

namespace Server.Infrastructure.Authentication.Models;

public class ApplicationUser : IdentityUser
{
    public string SageCustomerCode { get; set; } = string.Empty;
}
