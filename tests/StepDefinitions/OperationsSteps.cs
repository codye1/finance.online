using Microsoft.Playwright;
using Reqnroll; // якщо у вас SpecFlow — замініть на: using TechTalk.SpecFlow;
using static Microsoft.Playwright.Assertions;

[Binding]
[Scope(Feature = "Operations page")]
public sealed class OperationsSteps
{
    private string BaseUrl => _scenarioContext.Get<TestBackendFixture>("BackendFixture").MvcBaseUrl;

    // Той самий користувач, що й в інших Steps-файлах: має мати активну організацію.
    private const string TestEmail = "newuser@example.com";
    private const string TestPassword = "Password123!";

    private readonly ScenarioContext _scenarioContext;
    private readonly IPage _page;

    public OperationsSteps(ScenarioContext scenarioContext, IPage page)
    {
        _scenarioContext = scenarioContext;
        _page = page;
    }

    [Given(@"I am logged in and on the operations page")]
    public async Task GivenIAmLoggedInAndOnTheOperationsPage()
    {
        await _page.GotoAsync($"{BaseUrl}/auth");
        await _page.Locator("#loginEmail").FillAsync(TestEmail);
        await _page.Locator("#loginPassword").FillAsync(TestPassword);
        await _page.Locator("#loginForm button[type=submit]").ClickAsync();
        await _page.WaitForURLAsync(url => url.TrimEnd('/') == BaseUrl.TrimEnd('/'), new() { Timeout = 10000 });

        await _page.GotoAsync($"{BaseUrl}/operations");
        await Expect(_page.Locator("#operations-list")).ToBeVisibleAsync();

        // Чекаємо, поки завершиться початкове довантаження (fillOperationsListIfNeeded
        // в operations.js), щоб перевірки кількості елементів не влучили
        // в проміжний стан під час infinite-scroll.
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(_page.Locator("#operations-loading")).ToBeHiddenAsync();
    }

    [Given(@"I open the add operation modal")]
    public async Task GivenIOpenTheAddOperationModal()
    {
        await _page.Locator("#open-operation-modal-btn").ClickAsync();
        await Expect(_page.Locator("#form-add-operation")).ToBeVisibleAsync();
    }
}