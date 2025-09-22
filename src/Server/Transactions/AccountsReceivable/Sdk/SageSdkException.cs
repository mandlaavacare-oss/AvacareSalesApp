namespace Server.Transactions.AccountsReceivable.Sdk;

public class SageSdkException : Exception
{
    public SageSdkException(string message) : base(message)
    {
    }

    public SageSdkException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class SageEntityNotFoundException : SageSdkException
{
    public SageEntityNotFoundException(string message) : base(message)
    {
    }

    public SageEntityNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
