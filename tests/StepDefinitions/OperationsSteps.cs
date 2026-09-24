using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll; // якщо у вас SpecFlow — замініть на: using TechTalk.SpecFlow;
using static Microsoft.Playwright.Assertions;

[Binding]
public sealed class OperationsSteps
{
    private const string ItemSelector = "#operations-list .op-item";
    private const string DeletedIdKey = "deletedOperationId";

    private readonly ScenarioContext _scenarioContext;
    private readonly IPage _page;

    public OperationsSteps(ScenarioContext scenarioContext, IPage page)
    {
        _scenarioContext = scenarioContext;
        _page = page;
    }

    [Given("there is at least one operation in the list")]
    public async Task GivenThereIsAtLeastOneOperationInTheList()
    {
        await Expect(_page.Locator(ItemSelector).First).ToBeVisibleAsync();
    }

    [When("I delete the first operation and confirm")]
    public async Task WhenIDeleteTheFirstOperationAndConfirm()
    {
        var firstItem = _page.Locator(ItemSelector).First;

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

    // Чекаємо, поки завершиться початкове довантаження (fillOperationsListIfNeeded).
    private async Task WaitForListToSettleAsync()
    {
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(_page.Locator("#operations-loading")).ToBeHiddenAsync();
    }
}