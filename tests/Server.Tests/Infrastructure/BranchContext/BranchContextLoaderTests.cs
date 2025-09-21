using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Infrastructure.BranchContext;
using Server.Infrastructure.Database;

namespace Server.Tests.Infrastructure.BranchContext;

public class BranchContextLoaderTests
{
    [Fact]
    public void ApplyBranchContext_WithMatchingBranchCode_SetsConfiguredBranchId()
    {
        using var fixture = new BranchContextLoaderFixture();
        fixture.WriteConfiguration(
            """
            {
              "defaultBranchCode": "HQ",
              "branches": [
                { "code": "HQ", "name": "Head", "id": 1 },
                { "code": "CT", "name": "Cape Town", "id": 2 }
              ]
            }
            """);

        var loader = fixture.CreateLoader();

        loader.ApplyBranchContext("CT");

        fixture.DatabaseContext.Verify(context => context.SetBranchContext(2), Times.Once);
    }

    [Fact]
    public void ApplyBranchContext_WhenBranchCodeMissing_UsesDefaultBranch()
    {
        using var fixture = new BranchContextLoaderFixture();
        fixture.WriteConfiguration(
            """
            {
              "defaultBranchCode": "HQ",
              "branches": [
                { "code": "HQ", "name": "Head", "id": 1 },
                { "code": "CT", "name": "Cape Town", "id": 2 }
              ]
            }
            """);

        var loader = fixture.CreateLoader();

        loader.ApplyBranchContext(null);

        fixture.DatabaseContext.Verify(context => context.SetBranchContext(1), Times.Once);
    }

    private sealed class BranchContextLoaderFixture : IDisposable
    {
        private readonly string _rootPath;
        private readonly string _contentRoot;
        private readonly Mock<IWebHostEnvironment> _environment = new();
        private readonly Mock<ILogger<BranchContextLoader>> _logger = new();

        public BranchContextLoaderFixture()
        {
            _rootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_rootPath);
            Directory.CreateDirectory(Path.Combine(_rootPath, "config"));

            _contentRoot = Path.Combine(_rootPath, "src", "Server");
            Directory.CreateDirectory(_contentRoot);

            _environment.SetupGet(environment => environment.ContentRootPath).Returns(_contentRoot);
        }

        public Mock<IDatabaseContext> DatabaseContext { get; } = new();

        public void WriteConfiguration(string json)
        {
            var configPath = Path.Combine(_rootPath, "config", "branches.json");
            File.WriteAllText(configPath, json);
        }

        public BranchContextLoader CreateLoader()
        {
            return new BranchContextLoader(DatabaseContext.Object, _logger.Object, _environment.Object);
        }

        public void Dispose()
        {
            if (Directory.Exists(_rootPath))
            {
                Directory.Delete(_rootPath, recursive: true);
            }
        }
    }
}
