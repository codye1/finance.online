using Microsoft.Playwright;
using Reqnroll;

[Binding]
[Scope(Feature = "Categories page")]
public class CategoriesSteps
{
    private readonly IPage _page;
    private readonly ScenarioContext _scenarioContext;
    private string BaseUrl => _scenarioContext.Get<TestBackendFixture>("BackendFixture").MvcBaseUrl;

    private const string TestEmail = "newuser@example.com";
    private const string TestPassword = "Password123!";

    public CategoriesSteps(IPage page, ScenarioContext scenarioContext)
    {
        _page = page;
        _scenarioContext = scenarioContext;
    }

    [Given(@"I am logged in and on the categories page")]
    public async Task GivenIAmLoggedInAndOnTheCategoriesPage()
    {
        await _page.GotoAsync($"{BaseUrl}/auth");
        await _page.Locator("#loginEmail").FillAsync(TestEmail);
        await _page.Locator("#loginPassword").FillAsync(TestPassword);
        await _page.Locator("#loginForm button[type=submit]").ClickAsync();
        await _page.WaitForURLAsync(url => url.TrimEnd('/') == BaseUrl.TrimEnd('/'), new() { Timeout = 10000 });

        await _page.GotoAsync($"{BaseUrl}/categories");
        await Assertions.Expect(_page.Locator(".categories-page")).ToBeVisibleAsync();
    }

    [Given(@"I open the add category modal")]
    public async Task GivenIOpenTheAddCategoryModal()
    {
        await _page.Locator("#open-add-category-modal-btn").ClickAsync();
        await Assertions.Expect(_page.Locator("#form-add-category")).ToBeVisibleAsync();
    }
}