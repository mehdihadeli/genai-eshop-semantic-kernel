namespace BuildingBlocks.Env;

public static class DotEnv
{
    public static void Load(string? filePath = null)
    {
        filePath ??= Path.Combine(Directory.GetCurrentDirectory(), ".env");

        if (!File.Exists(filePath))
            return;

        foreach (var rawLine in File.ReadAllLines(filePath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var separatorIndex = line.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0)
                continue;

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (value.Length >= 2 && value[0] == value[^1] && (value[0] == '"' || value[0] == '\''))
                value = value[1..^1];

            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
