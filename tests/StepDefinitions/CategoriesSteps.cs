using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reqnroll;
using tests.StepDefinitions;

[Binding]
[Scope(Feature = "Categories page")]
public class CategoriesSteps
{
    private readonly IPage _page;
    private readonly ScenarioContext _scenarioContext;
    private readonly CommonSteps _common;
    private string BaseUrl => _scenarioContext.Get<TestBackendFixture>("BackendFixture").MvcBaseUrl;

    private const string TestEmail = "newuser@example.com";
    private const string TestPassword = "Password123!";

    public CategoriesSteps(IPage page, ScenarioContext scenarioContext, CommonSteps common)
    {
        _page = page;
        _scenarioContext = scenarioContext;
        _common = common;
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

    private ILocator CategoryItem(string resolvedName) =>
        _page.Locator("#categories-list .categories-item").Filter(new() { HasTextString = resolvedName });

    // Створює категорію через UI (підтримує {unique}) і чекає, поки вона з'явиться в списку.
    [Given(@"I have created a category ""(.*)"" with color ""(.*)""")]
    public async Task GivenIHaveCreatedACategory(string name, string color)
    {
        var resolved = _common.Resolve(name);

        await _page.Locator("#open-add-category-modal-btn").ClickAsync();
        await Assertions.Expect(_page.Locator("#form-add-category")).ToBeVisibleAsync();
        await _page.Locator("#category-name").FillAsync(resolved);
        await _page.Locator($".color-swatch-btn[data-color='{color}']").ClickAsync();
        await _page.Locator("#btn-submit-category").ClickAsync();

        await Assertions.Expect(_page.Locator("#app-modal"))
            .Not.ToHaveClassAsync(new Regex(@"(^|\s)is-active(\s|$)"));
        await Assertions.Expect(CategoryItem(resolved)).ToBeVisibleAsync();
    }

    [Given(@"I open the edit modal for category ""(.*)""")]
    public async Task GivenIOpenTheEditModalForCategory(string name)
    {
        var resolved = _common.Resolve(name);

        await CategoryItem(resolved).Locator(".category-edit-btn").ClickAsync();
        await Assertions.Expect(_page.Locator("#form-add-category")).ToBeVisibleAsync();
    }
}