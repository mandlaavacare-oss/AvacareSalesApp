namespace Server.Infrastructure.Authentication.Models;

public record RegisterRequest(string Username, string Password, string SageCustomerCode);
