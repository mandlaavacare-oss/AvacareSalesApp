namespace Server.Infrastructure.Authentication.Sage;

public class SageAuthenticationException : Exception
{
    public SageAuthenticationException(string message)
        : base(message)
    {
    }

    public SageAuthenticationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
