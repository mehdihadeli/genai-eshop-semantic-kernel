using BuildingBlocks.Env;

namespace Tests.Shared;

public static class TestEnvironment
{
    public static readonly IReadOnlyDictionary<string, string> ServiceDefaults = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["catalogs"] = "GENAI_TEST_CATALOGS_BASE_URL",
        ["carts"] = "GENAI_TEST_CARTS_BASE_URL",
        ["orders"] = "GENAI_TEST_ORDERS_BASE_URL",
        ["recommendation"] = "GENAI_TEST_RECOMMENDATION_BASE_URL",
        ["reviews"] = "GENAI_TEST_REVIEWS_BASE_URL",
        ["mcp"] = "GENAI_TEST_MCP_BASE_URL",
    };

    private static readonly IReadOnlyDictionary<string, string> ServiceFallbacks = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["catalogs"] = "http://localhost:5000",
        ["carts"] = "http://localhost:4000",
        ["orders"] = "http://localhost:7000",
        ["recommendation"] = "http://localhost:2000",
        ["reviews"] = "http://localhost:8000",
        ["mcp"] = "http://localhost:9000",
    };

    static TestEnvironment()
    {
        LoadDotEnv();
    }

    public static void LoadDotEnv()
    {
        DotEnv.Load(FindRepositoryFile(".env"));
    }

    public static Uri GetServiceUri(string variableName, string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(variableName) ?? defaultValue;
        return new Uri(value, UriKind.Absolute);
    }

    public static Uri GetServiceUri(string serviceName)
    {
        if (!ServiceDefaults.TryGetValue(serviceName, out var variableName))
            throw new ArgumentException($"Unknown service '{serviceName}'.", nameof(serviceName));

        return GetServiceUri(variableName, ServiceFallbacks[serviceName]);
    }

    private static string FindRepositoryFile(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), fileName);
    }
}
