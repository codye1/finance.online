using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll; // якщо у вас SpecFlow — замініть на: using TechTalk.SpecFlow;
using System.Text.RegularExpressions;
using static Microsoft.Playwright.Assertions;

// Кроки, специфічні для сторінки /operations. Тексти кроків нижче збігаються з
// деякими кроками в HomeSteps.cs (напр. "I open the add operation modal"), але
// розмітка на Home dashboard (.ledger-item, #btn-type-income) інша, ніж на
// сторінці Operations (.op-item, .op-type-toggle-btn) — тому це окремі методи
// з власним [Scope], а не виклик HomeSteps.
[Binding]
[Scope(Feature = "Operations page")]
public sealed class OperationsSteps
{
    private string BaseUrl => _scenarioContext.Get<TestBackendFixture>("BackendFixture").MvcBaseUrl;
    private const string ItemSelector = "#operations-list .op-item";

    // Той самий користувач, що й в інших Steps-файлах: має мати активну організацію.
    private const string TestEmail = "newuser@example.com";
    private const string TestPassword = "Password123!";

    private const string DeletedIdKey = "deletedOperationId";
    private const string NotedCategoryKey = "notedCategoryName";
    private const string ItemCountBeforeSubmitKey = "itemCountBeforeSubmit";

    private readonly ScenarioContext _scenarioContext;
    private readonly IPage _page;

    public OperationsSteps(ScenarioContext scenarioContext, IPage page)
    {
        _scenarioContext = scenarioContext;
        _page = page;
    }

    // ---------- Helpers ----------

    private ILocator Items => _page.Locator(ItemSelector);

    private async Task OpenAddOperationModalAsync()
    {
        await _page.Locator("#open-operation-modal-btn").ClickAsync();
        await Expect(_page.Locator("#form-add-operation")).ToBeVisibleAsync();
    }

    // Чекаємо, поки завершиться початкове довантаження (fillOperationsListIfNeeded
    // в operations.js), щоб подальші перевірки кількості елементів не влучили
    // в проміжний стан під час infinite-scroll довантаження.
    private async Task WaitForListToSettleAsync()
    {
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(_page.Locator("#operations-loading")).ToBeHiddenAsync();
    }

    // ---------- Background ----------

    [Given(@"I am logged in and on the operations page")]
    public async Task GivenIAmLoggedInAndOnTheOperationsPage()
    {
        await _page.GotoAsync($"{BaseUrl}/auth");
        await _page.Locator("#loginEmail").FillAsync(TestEmail);
        await _page.Locator("#loginPassword").FillAsync(TestPassword);
        await _page.Locator("#loginForm button[type=submit]").ClickAsync();
        await _page.WaitForURLAsync(url => url.TrimEnd('/') == BaseUrl.TrimEnd('/'), new() { Timeout = 10000 });

        await _page.GotoAsync($"{BaseUrl}/operations");
        // Обгортку сторінки (клас типу .operations-page) я не бачив, тож чекаю
        // на #operations-list — цей id точно є в operations.js.
        await Expect(_page.Locator("#operations-list")).ToBeVisibleAsync();
        await WaitForListToSettleAsync();
    }

    // ---------- Summary ----------

    [Then(@"I see the operations summary cards")]
    public async Task ThenISeeTheOperationsSummaryCards()
    {
        // Точного класу картки-обгортки я не бачив, тож перевіряю самі значення,
        // які напряму рахує recomputeSummary() в operations.js.
        await Expect(_page.Locator("#op-summary-income")).ToBeVisibleAsync();
        await Expect(_page.Locator("#op-summary-expense")).ToBeVisibleAsync();
        await Expect(_page.Locator("#op-summary-net")).ToBeVisibleAsync();
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
        await OpenAddOperationModalAsync();
    }

    [Then(@"the add operation modal is visible")]
    public async Task ThenTheAddOperationModalIsVisible()
    {
        await Expect(_page.Locator("#form-add-operation")).ToBeVisibleAsync();
    }

    [Then(@"the add operation modal is hidden")]
    public async Task ThenTheAddOperationModalIsHidden()
    {
        await Expect(_page.Locator("#app-modal")).Not.ToHaveClassAsync(new Regex("is-active"));
    }

    [When(@"I close the modal")]
    public async Task WhenICloseTheModal()
    {
        await _page.Locator("#js-close-operation-modal").ClickAsync();
    }

    // ---------- Form validation ----------

    [When(@"I submit the operation form without an amount")]
    public async Task WhenISubmitTheOperationFormWithoutAnAmount()
    {
        // #operation-date вже заповнена сьогоднішньою датою при відкритті модалки
        // (openAddOperationModal() в operations.js), тому її чіпати не треба.
        // Індекс 1 — перша реальна категорія (0 припускаю плейсхолдером).
        await _page.Locator("#operation-category").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await _page.Locator("#btn-submit-operation").ClickAsync();
    }

    [When(@"I submit the operation form without a category")]
    public async Task WhenISubmitTheOperationFormWithoutACategory()
    {
        await _page.Locator("#operation-amount").FillAsync("100");
        await _page.Locator("#btn-submit-operation").ClickAsync();
    }

    [Then(@"I see a validation error for the amount field")]
    public async Task ThenISeeAValidationErrorForTheAmountField()
    {
        await Expect(_page.Locator("#form-add-operation .form-error-text").First).ToBeVisibleAsync();
    }

    [Then(@"I see a validation error for the category field")]
    public async Task ThenISeeAValidationErrorForTheCategoryField()
    {
        await Expect(_page.Locator("#form-add-operation .form-error-text").First).ToBeVisibleAsync();
    }

    // ---------- Create operation ----------

    [When(@"I fill in the operation amount ""(.*)"" and select the first category")]
    public async Task WhenIFillInTheOperationAmountAndSelectTheFirstCategory(string amount)
    {
        _scenarioContext[ItemCountBeforeSubmitKey] = await Items.CountAsync();

        await _page.Locator("#operation-amount").FillAsync(amount);
        await _page.Locator("#operation-category").SelectOptionAsync(new SelectOptionValue { Index = 1 });
    }

    [When(@"I submit the operation form")]
    public async Task WhenISubmitTheOperationForm()
    {
        await _page.Locator("#btn-submit-operation").ClickAsync();
    }

    [Then(@"the new operation appears at the top of the operations list")]
    public async Task ThenTheNewOperationAppearsAtTheTopOfTheOperationsList()
    {
        var before = _scenarioContext.ContainsKey(ItemCountBeforeSubmitKey)
            ? (int)_scenarioContext[ItemCountBeforeSubmitKey]
            : 0;

        await Expect(Items).ToHaveCountAsync(before + 1);
        await Expect(Items.First).ToBeVisibleAsync();
    }

    // ---------- Type toggle ----------

    [When(@"I click the income type toggle")]
    public async Task WhenIClickTheIncomeTypeToggle()
    {
        await _page.Locator(".op-type-toggle-btn[data-type=\"income\"]").ClickAsync();
    }

    [Then(@"the income type toggle is active")]
    public async Task ThenTheIncomeTypeToggleIsActive()
    {
        await Expect(_page.Locator(".op-type-toggle-btn[data-type=\"income\"]")).ToHaveClassAsync(new Regex("active"));
    }

    [Then(@"the expense type toggle is not active")]
    public async Task ThenTheExpenseTypeToggleIsNotActive()
    {
        await Expect(_page.Locator(".op-type-toggle-btn[data-type=\"expense\"]")).Not.ToHaveClassAsync(new Regex("active"));
    }

    // ---------- Search ----------

    [When(@"I search operations for ""(.*)""")]
    public async Task WhenISearchOperationsFor(string query)
    {
        await _page.Locator("#op-search-input").FillAsync(query);
    }

    [Then(@"only operations matching ""(.*)"" are visible in the list")]
    public async Task ThenOnlyOperationsMatchingAreVisibleInTheList(string query)
    {
        var visible = _page.Locator($"{ItemSelector}:visible");
        var count = await visible.CountAsync();
        Assert.IsTrue(count > 0, $"No visible operations matched \"{query}\".");

        for (int i = 0; i < count; i++)
        {
            await Expect(visible.Nth(i)).ToContainTextAsync(query, new() { IgnoreCase = true });
        }
    }

    // ---------- Filter by type ----------

    [When(@"I filter operations by type ""(.*)""")]
    public async Task WhenIFilterOperationsByType(string typeLabel)
    {
        await _page.Locator("#op-type-filter").SelectOptionAsync(new SelectOptionValue { Label = typeLabel });
    }

    [Then(@"only income operations are visible in the list")]
    public async Task ThenOnlyIncomeOperationsAreVisibleInTheList()
    {
        await AssertAllVisibleHaveType("income");
    }

    [Then(@"only expense operations are visible in the list")]
    public async Task ThenOnlyExpenseOperationsAreVisibleInTheList()
    {
        await AssertAllVisibleHaveType("expense");
    }

    private async Task AssertAllVisibleHaveType(string type)
    {
        var visible = _page.Locator($"{ItemSelector}:visible");
        var count = await visible.CountAsync();
        Assert.IsTrue(count > 0, $"No visible {type} operations after filtering.");

        for (int i = 0; i < count; i++)
        {
            await Expect(visible.Nth(i)).ToHaveAttributeAsync("data-type", type);
        }
    }

    // ---------- Filter by category ----------

    [Given(@"I note the first category name")]
    public async Task GivenINoteTheFirstCategoryName()
    {
        var category = await Items.First.GetAttributeAsync("data-category");
        Assert.IsFalse(string.IsNullOrEmpty(category), "First operation has no data-category.");
        _scenarioContext[NotedCategoryKey] = category!;
    }

    [When(@"I filter operations by that category")]
    public async Task WhenIFilterOperationsByThatCategory()
    {
        var category = (string)_scenarioContext[NotedCategoryKey];
        await _page.Locator("#op-category-filter").SelectOptionAsync(new SelectOptionValue { Value = category });
    }

    [Then(@"only operations of that category are visible in the list")]
    public async Task ThenOnlyOperationsOfThatCategoryAreVisibleInTheList()
    {
        var category = (string)_scenarioContext[NotedCategoryKey];
        var visible = _page.Locator($"{ItemSelector}:visible");
        var count = await visible.CountAsync();
        Assert.IsTrue(count > 0, $"No visible operations for category \"{category}\".");

        for (int i = 0; i < count; i++)
        {
            await Expect(visible.Nth(i)).ToHaveAttributeAsync("data-category", category);
        }
    }

    // ---------- Delete ----------

    [Given("there is at least one operation in the list")]
    public async Task GivenThereIsAtLeastOneOperationInTheList()
    {
        await Expect(Items.First).ToBeVisibleAsync();
    }

    [When("I delete the first operation and confirm")]
    public async Task WhenIDeleteTheFirstOperationAndConfirm()
    {
        var firstItem = Items.First;

        var id = await firstItem.GetAttributeAsync("data-id");
        Assert.IsFalse(string.IsNullOrEmpty(id), "First operation has no data-id.");
        _scenarioContext[DeletedIdKey] = id!;

        // confirm() приймаємо ДО кліку, інакше діалог може автоматично закритись.
        void OnDialog(object? _, IDialog dialog) => _ = dialog.AcceptAsync();
        _page.Dialog += OnDialog;

        try
        {
            // Чекаємо саме на DELETE-запит, щоб не покладатись на таймінги.
            var response = await _page.RunAndWaitForResponseAsync(
                async () => await firstItem.Locator(".op-delete-btn").ClickAsync(),
                r => r.Request.Method == "DELETE" && r.Url.Contains("/operations/"));

            Assert.IsTrue(response.Ok, $"DELETE failed with status {response.Status}.");
        }
        finally
        {
            _page.Dialog -= OnDialog;
        }
    }

    [Then("the operation is removed from the list")]
    public async Task ThenTheOperationIsRemovedFromTheList()
    {
        var id = (string)_scenarioContext[DeletedIdKey];

        // Перевіряємо конкретний елемент, а не загальну кількість:
        // infinite scroll може легально довантажити інший запис після видалення.
        await Expect(_page.Locator($"{ItemSelector}[data-id='{id}']")).ToHaveCountAsync(0);
    }
}