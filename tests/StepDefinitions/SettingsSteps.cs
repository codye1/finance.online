using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;
using System.Text.RegularExpressions;
using static Microsoft.Playwright.Assertions;

// Кроки, специфічні для сторінки /settings. Решта перевірок у Settings.feature
// виконується універсальними табличними кроками з CommonSteps.
[Binding]
[Scope(Feature = "Settings page")]
public class SettingsSteps
{
    private readonly IPage _page;
    private readonly ScenarioContext _scenarioContext;
    private string BaseUrl => _scenarioContext.Get<TestBackendFixture>("BackendFixture").MvcBaseUrl;

    // Тестовий юзер має бути ВЛАСНИКОМ активної організації
    // (інакше не буде кнопок #open-invite-member-modal-btn та #open-delete-organization-dialog).
    private const string TestEmail = "newuser@example.com";
    private const string TestPassword = "Password123!";

    public SettingsSteps(IPage page, ScenarioContext scenarioContext)
    {
        _page = page;
        _scenarioContext = scenarioContext;
    }

    private ILocator DeleteDialog => _page.Locator("#delete-organization-dialog");

    [Given(@"I am logged in and on the settings page")]
    public async Task GivenIAmLoggedInAndOnTheSettingsPage()
    {
        await _page.GotoAsync($"{BaseUrl}/auth");
        await _page.Locator("#loginEmail").FillAsync(TestEmail);
        await _page.Locator("#loginPassword").FillAsync(TestPassword);
        await _page.Locator("#loginForm button[type=submit]").ClickAsync();
        await _page.WaitForURLAsync(url => url.TrimEnd('/') == BaseUrl.TrimEnd('/'), new() { Timeout = 10000 });

        await _page.GotoAsync($"{BaseUrl}/settings");
        await Expect(_page.Locator(".settings-page")).ToBeVisibleAsync();
    }

    [Given(@"I open the invite member modal")]
    public async Task GivenIOpenTheInviteMemberModal()
    {
        await _page.Locator("#open-invite-member-modal-btn").ClickAsync();
        await Expect(_page.Locator("#form-invite-member")).ToBeVisibleAsync();
    }

    [Given(@"I open the delete organization dialog")]
    public async Task GivenIOpenTheDeleteOrganizationDialog()
    {
        await _page.Locator("#open-delete-organization-dialog").ClickAsync();
        await Expect(DeleteDialog).ToHaveClassAsync(new Regex("is-active"));
    }

    // Назва організації динамічна, тому це не виразити табличним кроком.
    [Then(@"the delete organization dialog mentions the organization name")]
    public async Task ThenTheDeleteOrganizationDialogMentionsTheOrganizationName()
    {
        var organizationName = (await _page.Locator(".settings-organization-body h2").TextContentAsync())?.Trim();
        Assert.IsFalse(string.IsNullOrEmpty(organizationName), "Не вдалося прочитати назву організації.");

        await Expect(DeleteDialog).ToContainTextAsync(organizationName!);
    }

    // У CommonSteps немає кроку для клавіатури.
    [When(@"I press the Escape key")]
    public async Task WhenIPressTheEscapeKey()
    {
        await _page.Keyboard.PressAsync("Escape");
    }

    // Клік у кут оверлея: e.target === оверлей, а не вікно діалогу.
    [When(@"I click outside the delete organization dialog")]
    public async Task WhenIClickOutsideTheDeleteOrganizationDialog()
    {
        await DeleteDialog.ClickAsync(new() { Position = new Position { X = 5, Y = 5 } });
    }
}