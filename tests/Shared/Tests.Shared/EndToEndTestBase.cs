namespace Tests.Shared;

public abstract class EndToEndTestBase : IDisposable
{
    protected EndToEndTestBase(string serviceName)
    {
        Client = new HttpClient { BaseAddress = TestEnvironment.GetServiceUri(serviceName) };
    }

    protected HttpClient Client { get; }

    public void Dispose()
    {
        Client.Dispose();
        GC.SuppressFinalize(this);
    }
}
