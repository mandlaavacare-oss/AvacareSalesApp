namespace Server.Infrastructure.Database;

public interface ISageSessionFactory
{
    ISageSession CreateSession();
}
