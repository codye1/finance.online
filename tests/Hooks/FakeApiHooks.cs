using Reqnroll;
using tests.Mocks;

[Binding]
public class FakeApiHooks
{
    public static FakeFinanceApi Api { get; private set; } = null!;

    [BeforeTestRun]
    public static void StartFakeApi()
    {
        Api = new FakeFinanceApi();
    }

    [AfterScenario]
    public void ResetFakeApi()
    {
        Api.Reset();
    }

    [AfterTestRun]
    public static void StopFakeApi()
    {
        Api.Dispose();
    }
}