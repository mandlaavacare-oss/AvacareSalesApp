using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Server.Infrastructure.Database;

namespace Server.Infrastructure.BranchContext;

public class BranchContextLoader : IBranchContextLoader
{
    private readonly IDatabaseContext _databaseContext;
    private readonly ILogger<BranchContextLoader> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly Lazy<BranchConfigurationCache> _cache;

    public BranchContextLoader(
        IDatabaseContext databaseContext,
        ILogger<BranchContextLoader> logger,
        IWebHostEnvironment environment)
    {
        _databaseContext = databaseContext;
        _logger = logger;
        _environment = environment;
        _cache = new Lazy<BranchConfigurationCache>(LoadConfiguration, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public void ApplyBranchContext(string? branchCode)
    {
        var cache = _cache.Value;

        if (!string.IsNullOrWhiteSpace(branchCode)
            && cache.BranchesByCode.TryGetValue(branchCode, out var configuredBranch))
        {
            _databaseContext.SetBranchContext(configuredBranch.Id);
            _logger.LogDebug("Branch context applied for {BranchCode} (ID {BranchId}).", configuredBranch.Code, configuredBranch.Id);
            return;
        }

        if (!string.IsNullOrWhiteSpace(branchCode))
        {
            _logger.LogWarning(
                "Branch code {BranchCode} not found in configuration. Falling back to default {DefaultBranchCode}.",
                branchCode,
                cache.DefaultBranch.Code);
        }
        else
        {
            _logger.LogDebug(
                "No branch code supplied. Using default branch {DefaultBranchCode}.",
                cache.DefaultBranch.Code);
        }

        _databaseContext.SetBranchContext(cache.DefaultBranch.Id);
        _logger.LogDebug(
            "Branch context applied for default {BranchCode} (ID {BranchId}).",
            cache.DefaultBranch.Code,
            cache.DefaultBranch.Id);
    }

    private BranchConfigurationCache LoadConfiguration()
    {
        var path = ResolveConfigurationPath();
        var json = File.ReadAllText(path);
        var configuration = JsonSerializer.Deserialize<BranchConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (configuration?.Branches is null || configuration.Branches.Count == 0)
        {
            throw new InvalidOperationException("Branch configuration must contain at least one branch entry.");
        }

        var branchesByCode = configuration.Branches
            .ToDictionary(branch => branch.Code, StringComparer.OrdinalIgnoreCase);

        var defaultBranch = TryResolveDefaultBranch(configuration, branchesByCode);

        _logger.LogInformation(
            "Loaded {BranchCount} branch definitions from {ConfigurationPath} with default {DefaultBranchCode}.",
            branchesByCode.Count,
            path,
            defaultBranch.Code);

        return new BranchConfigurationCache(branchesByCode, defaultBranch);
    }

    private BranchConfigurationEntry TryResolveDefaultBranch(
        BranchConfiguration configuration,
        IReadOnlyDictionary<string, BranchConfigurationEntry> branchesByCode)
    {
        if (!string.IsNullOrWhiteSpace(configuration.DefaultBranchCode)
            && branchesByCode.TryGetValue(configuration.DefaultBranchCode, out var configuredDefault))
        {
            return configuredDefault;
        }

        return configuration.Branches[0];
    }

    private string ResolveConfigurationPath()
    {
        var directory = new DirectoryInfo(_environment.ContentRootPath);

        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "config", "branches.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "Unable to locate config/branches.json. Ensure the file exists at the repository root or application directory.");
    }

    private sealed record BranchConfigurationCache(
        IReadOnlyDictionary<string, BranchConfigurationEntry> BranchesByCode,
        BranchConfigurationEntry DefaultBranch);
}

public sealed record BranchConfiguration
{
    public string? DefaultBranchCode { get; init; }

    public List<BranchConfigurationEntry> Branches { get; init; } = new();
}

public sealed record BranchConfigurationEntry
{
    public required string Code { get; init; }

    public required string Name { get; init; }

    public int Id { get; init; }
}
