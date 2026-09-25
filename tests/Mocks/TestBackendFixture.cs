using tests.Mocks;

public sealed class TestBackendFixture : IDisposable
{
    public FakeFinanceApi FakeApi { get; }
    public MvcAppFactory MvcFactory { get; }
    public string MvcBaseUrl => MvcFactory.ServerAddress.TrimEnd('/');

    public TestBackendFixture()
    {
        FakeApi = new FakeFinanceApi();               // випадковий порт
        MvcFactory = new MvcAppFactory(FakeApi.Url);   // MVC-додаток дізнається адресу фейку через конфіг
        _ = MvcFactory.Server; // тригерить CreateHost() і реальний запуск Kestrel
    }

    public void Reset() => FakeApi.Reset();

    public void Dispose()
    {
        MvcFactory.Dispose();
        FakeApi.Dispose();
    }
}