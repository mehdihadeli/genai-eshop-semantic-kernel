namespace Tests.Shared;

public static class TestHostConfiguration
{
    public static bool RunExternalTests =>
        string.Equals(
            Environment.GetEnvironmentVariable("GENAI_RUN_EXTERNAL_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase
        );
}
