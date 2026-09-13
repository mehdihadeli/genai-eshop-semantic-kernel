using BuildingBlocks.Env;

namespace BuildingBlocks.Tests;

public class DotEnvTests
{
    [Fact]
    public void Load_reads_comments_trimmed_values_and_values_containing_equals()
    {
        var filePath = Path.GetTempFileName();
        const string key = "BUILDING_BLOCKS_TEST_DOTENV";

        try
        {
            File.WriteAllText(
                filePath,
                $"# ignored{Environment.NewLine} {key} = \"value=with-equals\" {Environment.NewLine}"
            );

            DotEnv.Load(filePath);

            Environment.GetEnvironmentVariable(key).ShouldBe("value=with-equals");
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
            File.Delete(filePath);
        }
    }
}
