using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Server.Infrastructure.Authentication.Database;
using Server.Infrastructure.Database;

namespace Server.Tests.Infrastructure.Database;

public class DatabaseContextTests
{
    [Fact]
    public void CommitTran_PersistsChangesWhenTransactionCompletes()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        using (var setupContext = new ApplicationDbContext(options))
        {
            setupContext.Database.EnsureCreated();
        }

        using (var context = new ApplicationDbContext(options))
        {
            var databaseContext = new DatabaseContext(context, NullLogger<DatabaseContext>.Instance);

            databaseContext.BeginTran();

            var role = new IdentityRole("committed-role")
            {
                NormalizedName = "COMMITTED-ROLE"
            };

            context.Roles.Add(role);
            context.SaveChanges();

            databaseContext.CommitTran();
        }

        using var verificationContext = new ApplicationDbContext(options);
        var savedRole = verificationContext.Roles.SingleOrDefault(r => r.Name == "committed-role");
        savedRole.Should().NotBeNull();
        savedRole!.NormalizedName.Should().Be("COMMITTED-ROLE");
    }

    [Fact]
    public void RollbackTran_DiscardsChangesWhenTransactionFails()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        using (var setupContext = new ApplicationDbContext(options))
        {
            setupContext.Database.EnsureCreated();
        }

        using (var context = new ApplicationDbContext(options))
        {
            var databaseContext = new DatabaseContext(context, NullLogger<DatabaseContext>.Instance);

            databaseContext.BeginTran();

            var role = new IdentityRole("rolled-back-role")
            {
                NormalizedName = "ROLLED-BACK-ROLE"
            };

            context.Roles.Add(role);
            context.SaveChanges();

            databaseContext.RollbackTran();
        }

        using var verificationContext = new ApplicationDbContext(options);
        verificationContext.Roles.Any(r => r.Name == "rolled-back-role").Should().BeFalse();
    }
}
