using Microsoft.Playwright;
using Reqnroll;

[Binding]
[Scope(Feature = "Home dashboard")]
public class HomeSteps
{
    private readonly IPage _page;
    private readonly ScenarioContext _scenarioContext;
    private string BaseUrl => _scenarioContext.Get<TestBackendFixture>("BackendFixture").MvcBaseUrl;

    // Онови на реальні тестові креди існуючого юзера
    private const string TestEmail = "newuser@example.com";
    private const string TestPassword = "Password123!";

    public HomeSteps(IPage page, ScenarioContext scenarioContext)
    {
        _page = page;
        _scenarioContext = scenarioContext;
    }

    [Given(@"I am logged in and on the home page")]
    public async Task GivenIAmLoggedInAndOnTheHomePage()
    {
        await _page.GotoAsync($"{BaseUrl}/auth");
        await _page.Locator("#loginEmail").FillAsync(TestEmail);
        await _page.Locator("#loginPassword").FillAsync(TestPassword);
        await _page.Locator("#loginForm button[type=submit]").ClickAsync();
        await _page.WaitForURLAsync(url => url.TrimEnd('/') == BaseUrl.TrimEnd('/'), new() { Timeout = 10000 });
    }

    [Given(@"I open the add operation modal")]
    public async Task GivenIOpenTheAddOperationModal()
    {
        await _page.Locator("#open-operation-modal-btn").ClickAsync();
        await Assertions.Expect(_page.Locator("#form-add-operation")).ToBeVisibleAsync();
    }

    [Given(@"I open the add organization modal")]
    public async Task GivenIOpenTheAddOrganizationModal()
    {
        await _page.Locator("#org-select-toggle").ClickAsync();
        await _page.Locator("#open-organization-modal-btn").ClickAsync();
        await Assertions.Expect(_page.Locator("#form-add-organization")).ToBeVisibleAsync();
    }
}