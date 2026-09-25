using Reqnroll;
using System.Collections.Concurrent;

[Binding]
public class FakeApiHooks
{
    // Має збігатися з <Workers> у .runsettings (MSTest Parallelize)
    private const int WorkerPoolSize = 4;

    private static BlockingCollection<TestBackendFixture> _pool = null!;

    [BeforeTestRun]
    public static void StartPool()
    {
        _pool = new BlockingCollection<TestBackendFixture>();
        for (var i = 0; i < WorkerPoolSize; i++)
            _pool.Add(new TestBackendFixture());
    }

    [BeforeScenario]
    public void CheckoutBackend(ScenarioContext scenarioContext)
    {
        var fixture = _pool.Take();          // блокує, якщо всі зайняті — але їх рівно стільки, скільки воркерів
        fixture.Reset();                     // безпечно: цей комплект зараз використовує тільки цей сценарій
        scenarioContext.Set(fixture, "BackendFixture");
    }

    [AfterScenario]
    public void ReturnBackend(ScenarioContext scenarioContext)
    {
        var fixture = scenarioContext.Get<TestBackendFixture>("BackendFixture");
        _pool.Add(fixture);                  // повертаємо в пул для наступного вільного воркера
    }

    [AfterTestRun]
    public static void StopPool()
    {
        while (_pool.TryTake(out var fixture)) fixture.Dispose();
    }
}