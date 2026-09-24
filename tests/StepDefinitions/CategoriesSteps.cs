using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;
using System.Text.RegularExpressions;

[Binding]
[Scope(Feature = "Categories page")]
public class CategoriesSteps
{
    private readonly IPage _page;
    private readonly ScenarioContext _scenarioContext;
    private const string BaseUrl = "https://localhost:7024";

    // Той самий користувач, що й в OperationsSteps: має мати активну організацію.
    private const string TestEmail = "newuser@example.com";
    private const string TestPassword = "Password123!";

    private const string CreatedCategoryNameKey = "CreatedCategoryName";

    public CategoriesSteps(IPage page, ScenarioContext scenarioContext)
    {
        _page = page;
        _scenarioContext = scenarioContext;
    }

    // ---------- Helpers ----------

    private ILocator CategoryItems => _page.Locator("#categories-list .categories-item");

    private ILocator CategoryItemByName(string name) =>
        _page.Locator("#categories-list .categories-item", new() { HasTextString = name });

    private static string BuildUniqueCategoryName() =>
        $"Test cat {DateTime.UtcNow:HHmmssfff}";

    private string GetCreatedCategoryName() =>
        (string)_scenarioContext[CreatedCategoryNameKey];

    private async Task OpenAddCategoryModalAsync()
    {
        await _page.Locator("#open-add-category-modal-btn").ClickAsync();
        await Assertions.Expect(_page.Locator("#form-add-category")).ToBeVisibleAsync();
    }

    private async Task FillUniqueNameAsync()
    {
        var name = BuildUniqueCategoryName();
        _scenarioContext[CreatedCategoryNameKey] = name;
        await _page.Locator("#category-name").FillAsync(name);
    }

    private static bool IsCategoryDeleteRequest(IResponse r) =>
        r.Request.Method == "POST" && r.Url.EndsWith("/categories/delete");

    // ---------- Background ----------

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

    // ---------- Page ----------

    [Then(@"I see the categories page header")]
    public async Task ThenISeeTheCategoriesPageHeader()
    {
        await Assertions.Expect(_page.Locator(".categories-header h1")).ToHaveTextAsync("Категорії");
    }

    [Then(@"I see the categories list")]
    public async Task ThenISeeTheCategoriesList()
    {
        await Assertions.Expect(_page.Locator("#categories-list")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("#open-add-category-modal-btn")).ToBeVisibleAsync();
    }

    // ---------- Add category modal ----------

    [When(@"I click the add category button")]
    public async Task WhenIClickTheAddCategoryButton()
    {
        await _page.Locator("#open-add-category-modal-btn").ClickAsync();
    }

    [Given(@"I open the add category modal")]
    public async Task GivenIOpenTheAddCategoryModal()
    {
        await OpenAddCategoryModalAsync();
    }

    [Then(@"the add category modal is visible")]
    public async Task ThenTheAddCategoryModalIsVisible()
    {
        await Assertions.Expect(_page.Locator("#form-add-category")).ToBeVisibleAsync();
    }

    [Then(@"the add category modal is hidden")]
    public async Task ThenTheAddCategoryModalIsHidden()
    {
        await Assertions.Expect(_page.Locator("#app-modal")).Not.ToHaveClassAsync(new Regex("is-active"));
    }

    [When(@"I close the category modal")]
    public async Task WhenICloseTheCategoryModal()
    {
        await _page.Locator("#js-close-category-modal").ClickAsync();
    }

    // ---------- Form validation ----------

    [When(@"I submit the category form without a name")]
    public async Task WhenISubmitTheCategoryFormWithoutAName()
    {
        await _page.Locator("#category-name").FillAsync(string.Empty);
        await _page.Locator("#btn-submit-category").ClickAsync();
    }

    [When(@"I fill in a category name of (\d+) characters")]
    public async Task WhenIFillInACategoryNameOfNCharacters(int length)
    {
        await _page.Locator("#category-name").FillAsync(new string('a', length));
    }

    [When(@"I fill in a unique category name")]
    public async Task WhenIFillInAUniqueCategoryName()
    {
        await FillUniqueNameAsync();
    }

    [When(@"I submit the category form")]
    public async Task WhenISubmitTheCategoryForm()
    {
        await _page.Locator("#btn-submit-category").ClickAsync();
    }

    [Then(@"I see a validation error for the category name field")]
    public async Task ThenISeeAValidationErrorForTheCategoryNameField()
    {
        await Assertions.Expect(_page.Locator("#form-add-category .form-error-text").First).ToBeVisibleAsync();
    }

    [Then(@"the category submit button is disabled")]
    public async Task ThenTheCategorySubmitButtonIsDisabled()
    {
        await Assertions.Expect(_page.Locator("#btn-submit-category")).ToBeDisabledAsync();
    }

    // ---------- Color ----------

    [When(@"I select the category color ""(.*)""")]
    public async Task WhenISelectTheCategoryColor(string color)
    {
        await _page.Locator($".color-swatch-btn[data-color=\"{color}\"]").ClickAsync();
    }

    [Then(@"the category color ""(.*)"" is active")]
    public async Task ThenTheCategoryColorIsActive(string color)
    {
        await Assertions.Expect(_page.Locator($".color-swatch-btn[data-color=\"{color}\"]"))
            .ToHaveClassAsync(new Regex("active"));
        await Assertions.Expect(_page.Locator("#category-color")).ToHaveValueAsync(color);
    }

    [Then(@"the category color ""(.*)"" is not active")]
    public async Task ThenTheCategoryColorIsNotActive(string color)
    {
        await Assertions.Expect(_page.Locator($".color-swatch-btn[data-color=\"{color}\"]"))
            .Not.ToHaveClassAsync(new Regex("active"));
    }

    // ---------- Create result ----------

    [Then(@"the new category appears in the categories list")]
    public async Task ThenTheNewCategoryAppearsInTheCategoriesList()
    {
        await Assertions.Expect(CategoryItemByName(GetCreatedCategoryName())).ToBeVisibleAsync();
    }

    [Then(@"the new category has the color ""(.*)""")]
    public async Task ThenTheNewCategoryHasTheColor(string color)
    {
        var dot = CategoryItemByName(GetCreatedCategoryName()).Locator(".category-color-dot");
        await Assertions.Expect(dot).ToHaveAttributeAsync(
            "style",
            new Regex(Regex.Escape(color), RegexOptions.IgnoreCase));
    }

    // ---------- Delete ----------
    // Видаляємо ТІЛЬКИ категорію, створену самим сценарієм, щоб не зачепити
    // засіяні категорії, від яких залежать тести Operations page.

    [Given(@"I have created a new category")]
    public async Task GivenIHaveCreatedANewCategory()
    {
        await OpenAddCategoryModalAsync();
        await FillUniqueNameAsync();
        await _page.Locator("#btn-submit-category").ClickAsync();
        await Assertions.Expect(CategoryItemByName(GetCreatedCategoryName())).ToBeVisibleAsync();
    }

    [When(@"I delete that category and confirm")]
    public async Task WhenIDeleteThatCategoryAndConfirm()
    {
        // .category-delete-btn викликає нативний confirm(); далі при помилці JS показує alert().
        // Приймаємо всі діалоги, але запам'ятовуємо їхній текст для діагностики.
        var dialogs = new List<string>();
        void OnDialog(object? _, IDialog dialog)
        {
            dialogs.Add($"{dialog.Type}: {dialog.Message}");
            _ = dialog.AcceptAsync();
        }

        _page.Dialog += OnDialog;
        try
        {
            // Чекаємо на реальну відповідь сервера, а не просто на клік.
            var response = await _page.RunAndWaitForResponseAsync(
                async () => await CategoryItemByName(GetCreatedCategoryName())
                    .Locator(".category-delete-btn").ClickAsync(),
                IsCategoryDeleteRequest,
                new() { Timeout = 10000 });

            Assert.IsTrue(
                response.Ok,
                $"POST /categories/delete повернув {response.Status}. Діалоги: [{string.Join("; ", dialogs)}]");
        }
        finally
        {
            _page.Dialog -= OnDialog;
        }
    }

    [When(@"I delete that category and cancel")]
    public async Task WhenIDeleteThatCategoryAndCancel()
    {
        var dialogShown = new TaskCompletionSource<bool>();

        _page.Dialog += async (_, dialog) =>
        {
            await dialog.DismissAsync();
            dialogShown.TrySetResult(true);
        };

        await CategoryItemByName(GetCreatedCategoryName()).Locator(".category-delete-btn").ClickAsync();

        var completed = await Task.WhenAny(dialogShown.Task, Task.Delay(5000));
        Assert.AreSame(dialogShown.Task, completed, "Очікувався діалог підтвердження видалення.");
    }

    [Then(@"that category is removed from the categories list")]
    public async Task ThenThatCategoryIsRemovedFromTheCategoriesList()
    {
        await Assertions.Expect(CategoryItemByName(GetCreatedCategoryName())).ToHaveCountAsync(0);
    }

    [Then(@"that category is still in the categories list")]
    public async Task ThenThatCategoryIsStillInTheCategoriesList()
    {
        await Assertions.Expect(CategoryItemByName(GetCreatedCategoryName())).ToBeVisibleAsync();
    }
}