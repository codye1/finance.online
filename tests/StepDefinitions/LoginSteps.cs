using System.Net.Http.Json;
using Microsoft.Playwright;
using Reqnroll;
using tests.StepDefinitions;

[Binding]
public class LoginSteps
{
    private readonly IPage _page;
    private readonly ScenarioContext _scenarioContext;
    private string BaseUrl => _scenarioContext.Get<TestBackendFixture>("BackendFixture").MvcBaseUrl;

    public LoginSteps(IPage page, ScenarioContext scenarioContext)
    {
        _page = page;
        _scenarioContext = scenarioContext;
    }

    [Given(@"I am on the auth page")]
    public async Task GivenIAmOnTheAuthPage()
    {
        await _page.GotoAsync($"{BaseUrl}/auth");
    }

    // Реєструє юзера напряму через API; його email доступний у feature як {uniqueEmail}
    [Given(@"a user is already registered")]
    public async Task GivenAUserIsAlreadyRegistered()
    {
        var email = $"test_{Guid.NewGuid():N}@example.com";
        _scenarioContext[CommonSteps.UniqueEmailKey] = email;

        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var response = await client.PostAsJsonAsync("/auth/register", new { email, password = "Password123!" });
        response.EnsureSuccessStatusCode();
    }
}