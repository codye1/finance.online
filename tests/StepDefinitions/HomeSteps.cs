using Microsoft.Playwright;
using Reqnroll;

[Binding]
[Scope(Feature = "Home dashboard")]
public class HomeSteps
{
    private readonly IPage _page;
    private readonly ScenarioContext _scenarioContext;
    private const string BaseUrl = "https://localhost:7024";

    // Onov на реальні тестові креди існуючого юзера
    private const string TestEmail = "newuser@example.com";
    private const string TestPassword = "Password123!";

    public HomeSteps(IPage page,ScenarioContext scenarioContext)
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

    // ---------- KPI ----------

    [Then(@"I see 4 KPI cards")]
    public async Task ThenISee4KpiCards()
    {
        await Assertions.Expect(_page.Locator(".kpi-card")).ToHaveCountAsync(4);
    }

    // ---------- Organization dropdown ----------

    [When(@"I click the organization selector")]
    public async Task WhenIClickTheOrganizationSelector()
    {
        await _page.Locator("#org-select-toggle").ClickAsync();
    }

    [Then(@"the organization dropdown is visible")]
    public async Task ThenTheOrganizationDropdownIsVisible()
    {
        await Assertions.Expect(_page.Locator("#org-select-dropdown")).ToBeVisibleAsync();
    }

    [Then(@"the organization dropdown is hidden")]
    public async Task ThenTheOrganizationDropdownIsHidden()
    {
        await Assertions.Expect(_page.Locator("#org-select-dropdown")).ToBeHiddenAsync();
    }

    // ---------- Period dropdown ----------

    [When(@"I click the period selector")]
    public async Task WhenIClickThePeriodSelector()
    {
        await _page.Locator("#period-select-toggle").ClickAsync();
    }

    [Then(@"the period dropdown is visible")]
    public async Task ThenThePeriodDropdownIsVisible()
    {
        await Assertions.Expect(_page.Locator("#period-select-dropdown")).ToBeVisibleAsync();
    }

    [Then(@"the period dropdown is hidden")]
    public async Task ThenThePeriodDropdownIsHidden()
    {
        await Assertions.Expect(_page.Locator("#period-select-dropdown")).ToBeHiddenAsync();
    }

    // ---------- Add operation modal ----------

    [When(@"I click the add operation button")]
    public async Task WhenIClickTheAddOperationButton()
    {
        await _page.Locator("#open-operation-modal-btn").ClickAsync();
    }

    [Given(@"I open the add operation modal")]
    public async Task GivenIOpenTheAddOperationModal()
    {
        await _page.Locator("#open-operation-modal-btn").ClickAsync();
        await Assertions.Expect(_page.Locator("#form-add-operation")).ToBeVisibleAsync();
    }

    [Then(@"the add operation modal is visible")]
    public async Task ThenTheAddOperationModalIsVisible()
    {
        await Assertions.Expect(_page.Locator("#form-add-operation")).ToBeVisibleAsync();
    }

    [Then(@"the add operation modal is hidden")]
    public async Task ThenTheAddOperationModalIsHidden()
    {
        await Assertions.Expect(_page.Locator("#app-modal")).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-active"));
    }

    [When(@"I close the modal")]
    public async Task WhenICloseTheModal()
    {
        await _page.Locator("#js-close-modal, #js-close-operation-modal, #js-close-organization-modal").First.ClickAsync();
    }

    [When(@"I submit the operation form without an amount")]
    public async Task WhenISubmitTheOperationFormWithoutAnAmount()
    {
        await _page.Locator("#operation-category").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await _page.Locator("#operation-date").FillAsync(DateTime.Today.ToString("yyyy-MM-dd"));
        await _page.Locator("#btn-submit-operation").ClickAsync();
    }

    [When(@"I submit the operation form without a category")]
    public async Task WhenISubmitTheOperationFormWithoutACategory()
    {
        await _page.Locator("#operation-amount").FillAsync("100");
        await _page.Locator("#operation-date").FillAsync(DateTime.Today.ToString("yyyy-MM-dd"));
        await _page.Locator("#btn-submit-operation").ClickAsync();
    }

    [When(@"I fill in the operation amount ""(.*)"" and select the first category")]
    public async Task WhenIFillInTheOperationAmountAndSelectTheFirstCategory(string amount)
    {
        await _page.Locator("#operation-amount").FillAsync(amount);
        await _page.Locator("#operation-category").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await _page.Locator("#operation-date").FillAsync(DateTime.Today.ToString("yyyy-MM-dd"));
    }

    [When(@"I submit the operation form")]
    public async Task WhenISubmitTheOperationForm()
    {
        await _page.Locator("#btn-submit-operation").ClickAsync();
    }

    [Then(@"the new operation appears at the top of the operations list")]
    public async Task ThenTheNewOperationAppearsAtTheTopOfTheOperationsList()
    {
        await Assertions.Expect(_page.Locator("#operations-list .ledger-item").First).ToBeVisibleAsync();
    }

    [When(@"I click the income type toggle")]
    public async Task WhenIClickTheIncomeTypeToggle()
    {
        await _page.Locator("#btn-type-income").ClickAsync();
    }

    [Then(@"the income type toggle is active")]
    public async Task ThenTheIncomeTypeToggleIsActive()
    {
        await Assertions.Expect(_page.Locator("#btn-type-income")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("active"));
    }

    [Then(@"the expense type toggle is not active")]
    public async Task ThenTheExpenseTypeToggleIsNotActive()
    {
        await Assertions.Expect(_page.Locator("#btn-type-expense")).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("active"));
    }

    [Then(@"I see a validation error for the amount field")]
    public async Task ThenISeeAValidationErrorForTheAmountField()
    {
        await Assertions.Expect(_page.Locator("#form-add-operation .form-error-text").First).ToBeVisibleAsync();
    }

    [Then(@"I see a validation error for the category field")]
    public async Task ThenISeeAValidationErrorForTheCategoryField()
    {
        await Assertions.Expect(_page.Locator("#form-add-operation .form-error-text").First).ToBeVisibleAsync();
    }

    // ---------- Add organization modal ----------

    [When(@"I click the create organization button")]
    public async Task WhenIClickTheCreateOrganizationButton()
    {
        await _page.Locator("#org-select-toggle").ClickAsync();
        await _page.Locator("#open-organization-modal-btn").ClickAsync();
    }

    [Given(@"I open the add organization modal")]
    public async Task GivenIOpenTheAddOrganizationModal()
    {
        await _page.Locator("#org-select-toggle").ClickAsync();
        await _page.Locator("#open-organization-modal-btn").ClickAsync();
        await Assertions.Expect(_page.Locator("#form-add-organization")).ToBeVisibleAsync();
    }

    [Then(@"the add organization modal is visible")]
    public async Task ThenTheAddOrganizationModalIsVisible()
    {
        await Assertions.Expect(_page.Locator("#form-add-organization")).ToBeVisibleAsync();
    }

    [Then(@"the add organization modal is hidden")]
    public async Task ThenTheAddOrganizationModalIsHidden()
    {
        await Assertions.Expect(_page.Locator("#app-modal")).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-active"));
    }

    [When(@"I submit the organization form without a name")]
    public async Task WhenISubmitTheOrganizationFormWithoutAName()
    {
        await _page.Locator("#btn-submit-organization").ClickAsync();
    }

    [Then(@"I see a validation error for the organization name field")]
    public async Task ThenISeeAValidationErrorForTheOrganizationNameField()
    {
        await Assertions.Expect(_page.Locator("#form-add-organization .form-error-text").First).ToBeVisibleAsync();
    }

    [When(@"I fill in a unique organization name and submit")]
    public async Task WhenIFillInAUniqueOrganizationNameAndSubmit()
    {
        var uniqueName = $"Test Org {Guid.NewGuid():N}".Substring(0, 20);
        _scenarioContext["OrganizationName"] = uniqueName;

        await _page.Locator("#organization-name").FillAsync(uniqueName);
        await _page.Locator("#btn-submit-organization").ClickAsync();
    }

    [Then(@"I am redirected to the home page with the new organization active")]
    public async Task ThenIAmRedirectedToTheHomePageWithTheNewOrganizationActive()
    {
        await _page.WaitForURLAsync(url => url.Contains("organizationId="), new() { Timeout = 10000 });
        var orgName = (string)_scenarioContext["OrganizationName"];
        await Assertions.Expect(_page.Locator("#org-select-current")).ToHaveTextAsync(orgName);
    }

    // ---------- Filters ----------

    [When(@"I click the operations filter ""(.*)""")]
    public async Task WhenIClickTheOperationsFilter(string filterLabel)
    {
        await _page.Locator("#operations-filter .filter-toggle", new() { HasTextString = filterLabel }).ClickAsync();
    }

    [Then(@"only income operations are visible in the list")]
    public async Task ThenOnlyIncomeOperationsAreVisibleInTheList()
    {
        var visibleItems = _page.Locator("#operations-list .ledger-item:visible");
        var count = await visibleItems.CountAsync();
        for (int i = 0; i < count; i++)
        {
            await Assertions.Expect(visibleItems.Nth(i)).ToHaveAttributeAsync("data-type", "income");
        }
    }

    [Then(@"only expense operations are visible in the list")]
    public async Task ThenOnlyExpenseOperationsAreVisibleInTheList()
    {
        var visibleItems = _page.Locator("#operations-list .ledger-item:visible");
        var count = await visibleItems.CountAsync();
        for (int i = 0; i < count; i++)
        {
            await Assertions.Expect(visibleItems.Nth(i)).ToHaveAttributeAsync("data-type", "expense");
        }
    }
}