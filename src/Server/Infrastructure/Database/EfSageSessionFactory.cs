using Server.Infrastructure.Authentication.Database;

namespace Server.Infrastructure.Database;

public class EfSageSessionFactory(ApplicationDbContext dbContext) : ISageSessionFactory
{
    public ISageSession CreateSession() => new EfSageSession(dbContext);
}
