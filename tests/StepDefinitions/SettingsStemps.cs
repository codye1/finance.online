using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;
using System.Text.RegularExpressions;

[Binding]
[Scope(Feature = "Settings page")]
public class SettingsSteps
{
    private readonly IPage _page;
    private readonly ScenarioContext _scenarioContext;
    private const string BaseUrl = "https://localhost:7024";

    // Тестовий юзер має бути ВЛАСНИКОМ активної організації
    // (інакше не буде кнопок #open-invite-member-modal-btn та #open-delete-organization-dialog).
    private const string TestEmail = "newuser@example.com";
    private const string TestPassword = "Password123!";

    private const string InviteEmail = "member@example.com";

    public SettingsSteps(IPage page, ScenarioContext scenarioContext)
    {
        _page = page;
        _scenarioContext = scenarioContext;
    }

    // ---------- Helpers ----------

    private ILocator DeleteDialog => _page.Locator("#delete-organization-dialog");

    private async Task OpenInviteMemberModalAsync()
    {
        await _page.Locator("#open-invite-member-modal-btn").ClickAsync();
        await Assertions.Expect(_page.Locator("#form-invite-member")).ToBeVisibleAsync();
    }

    private async Task OpenDeleteDialogAsync()
    {
        await _page.Locator("#open-delete-organization-dialog").ClickAsync();
        await Assertions.Expect(DeleteDialog).ToHaveClassAsync(new Regex("is-active"));
    }

    // ---------- Background ----------

    [Given(@"I am logged in and on the settings page")]
    public async Task GivenIAmLoggedInAndOnTheSettingsPage()
    {
        await _page.GotoAsync($"{BaseUrl}/auth");
        await _page.Locator("#loginEmail").FillAsync(TestEmail);
        await _page.Locator("#loginPassword").FillAsync(TestPassword);
        await _page.Locator("#loginForm button[type=submit]").ClickAsync();
        await _page.WaitForURLAsync(url => url.TrimEnd('/') == BaseUrl.TrimEnd('/'), new() { Timeout = 10000 });

        await _page.GotoAsync($"{BaseUrl}/settings");
        await Assertions.Expect(_page.Locator(".settings-page")).ToBeVisibleAsync();
    }

    // ---------- Page ----------

    [Then(@"I see the settings page header")]
    public async Task ThenISeeTheSettingsPageHeader()
    {
        await Assertions.Expect(_page.Locator(".settings-header h1")).ToHaveTextAsync("Налаштування організації");
    }

    [Then(@"I see the organization card")]
    public async Task ThenISeeTheOrganizationCard()
    {
        await Assertions.Expect(_page.Locator(".settings-organization")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator(".settings-organization-body h2")).ToBeVisibleAsync();
    }

    [Then(@"I see the members section")]
    public async Task ThenISeeTheMembersSection()
    {
        await Assertions.Expect(_page.Locator(".settings-members-list")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("#open-invite-member-modal-btn")).ToBeVisibleAsync();
    }

    // ---------- Members list ----------

    // Приймає крок як із лапками навколо маркера ("me", "Owner"), так і без них,
    // щоб не залежати від стилю конкретного .feature-файлу.
    [Then(@"the members list contains the current user marked as ""?([^""]+?)""?$")]
    public async Task ThenTheMembersListContainsTheCurrentUserMarkedAs(string marker)
    {
        var me = _page.Locator(".settings-member", new() { HasTextString = TestEmail });
        await Assertions.Expect(me).ToBeVisibleAsync();
        await Assertions.Expect(me.Locator(".settings-pill")).ToHaveTextAsync(marker);
    }

    [Then("the current user has the {string} role")]
    public async Task ThenTheCurrentUserHasTheRole(string role)
    {
        var me = _page.Locator(".settings-member", new() { HasTextString = TestEmail });
        await Assertions.Expect(me.Locator(".settings-role")).ToHaveTextAsync(role);
    }

    // ---------- Invite member modal ----------

    [When(@"I click the invite member button")]
    public async Task WhenIClickTheInviteMemberButton()
    {
        await _page.Locator("#open-invite-member-modal-btn").ClickAsync();
    }

    [Given(@"I open the invite member modal")]
    public async Task GivenIOpenTheInviteMemberModal()
    {
        await OpenInviteMemberModalAsync();
    }

    [Then(@"the invite member modal is visible")]
    public async Task ThenTheInviteMemberModalIsVisible()
    {
        await Assertions.Expect(_page.Locator("#form-invite-member")).ToBeVisibleAsync();
    }

    [Then(@"the invite member modal is hidden")]
    public async Task ThenTheInviteMemberModalIsHidden()
    {
        await Assertions.Expect(_page.Locator("#app-modal")).Not.ToHaveClassAsync(new Regex("is-active"));
    }

    [When(@"I close the invite member modal")]
    public async Task WhenICloseTheInviteMemberModal()
    {
        await _page.Locator("#js-close-invite-member-modal").ClickAsync();
    }

    [Then(@"the invite member role is ""(.*)""")]
    public async Task ThenTheInviteMemberRoleIs(string role)
    {
        await Assertions.Expect(_page.Locator("#member-role")).ToHaveValueAsync(role);
    }

    // ---------- Invite member form ----------

    [When(@"I submit the invite member form without an email")]
    public async Task WhenISubmitTheInviteMemberFormWithoutAnEmail()
    {
        await _page.Locator("#member-email").FillAsync(string.Empty);
        await _page.Locator("#btn-submit-invite-member").ClickAsync();
    }

    [When(@"I fill in the invite email ""(.*)""")]
    public async Task WhenIFillInTheInviteEmail(string email)
    {
        await _page.Locator("#member-email").FillAsync(email);
    }

    [When(@"I fill in the email of an existing user")]
    public async Task WhenIFillInTheEmailOfAnExistingUser()
    {
        await _page.Locator("#member-email").FillAsync(InviteEmail);
    }

    [When(@"I submit the invite member form")]
    public async Task WhenISubmitTheInviteMemberForm()
    {
        await _page.Locator("#btn-submit-invite-member").ClickAsync();
    }

    [Then(@"I see a validation error for the invite email field")]
    public async Task ThenISeeAValidationErrorForTheInviteEmailField()
    {
        await Assertions.Expect(_page.Locator("#form-invite-member .form-error-text").First).ToBeVisibleAsync();
    }

    [Then(@"the invite member submit button is disabled")]
    public async Task ThenTheInviteMemberSubmitButtonIsDisabled()
    {
        await Assertions.Expect(_page.Locator("#btn-submit-invite-member")).ToBeDisabledAsync();
    }

    [Then(@"the invited user appears in the members list")]
    public async Task ThenTheInvitedUserAppearsInTheMembersList()
    {
        // Після успішного запрошення сторінка робить window.location.reload().
        var member = _page.Locator(".settings-member", new() { HasTextString = InviteEmail });
        await Assertions.Expect(member).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    // ---------- Delete organization dialog ----------

    [When(@"I click the delete organization button")]
    public async Task WhenIClickTheDeleteOrganizationButton()
    {
        await _page.Locator("#open-delete-organization-dialog").ClickAsync();
    }

    [Given(@"I open the delete organization dialog")]
    public async Task GivenIOpenTheDeleteOrganizationDialog()
    {
        await OpenDeleteDialogAsync();
    }

    [Then(@"the delete organization dialog is visible")]
    public async Task ThenTheDeleteOrganizationDialogIsVisible()
    {
        await Assertions.Expect(DeleteDialog).ToHaveClassAsync(new Regex("is-active"));
        await Assertions.Expect(_page.Locator("#delete-organization-title")).ToBeVisibleAsync();
    }

    [Then(@"the delete organization dialog mentions the organization name")]
    public async Task ThenTheDeleteOrganizationDialogMentionsTheOrganizationName()
    {
        var organizationName = (await _page.Locator(".settings-organization-body h2").TextContentAsync())?.Trim();
        Assert.IsFalse(string.IsNullOrEmpty(organizationName), "Не вдалося прочитати назву організації.");

        await Assertions.Expect(DeleteDialog).ToContainTextAsync(organizationName!);
    }

    [When(@"I click cancel in the delete organization dialog")]
    public async Task WhenIClickCancelInTheDeleteOrganizationDialog()
    {
        await _page.Locator("#cancel-delete-organization").ClickAsync();
    }

    [When(@"I press the Escape key")]
    public async Task WhenIPressTheEscapeKey()
    {
        await _page.Keyboard.PressAsync("Escape");
    }

    [When(@"I click outside the delete organization dialog")]
    public async Task WhenIClickOutsideTheDeleteOrganizationDialog()
    {
        // Клік у кут оверлея: e.target === оверлей, а не вікно діалогу.
        await DeleteDialog.ClickAsync(new() { Position = new Position { X = 5, Y = 5 } });
    }

    [Then(@"the delete organization dialog is hidden")]
    public async Task ThenTheDeleteOrganizationDialogIsHidden()
    {
        await Assertions.Expect(DeleteDialog).Not.ToHaveClassAsync(new Regex("is-active"));
    }

    [Then(@"I am still on the settings page")]
    public async Task ThenIAmStillOnTheSettingsPage()
    {
        StringAssert.Contains(_page.Url, "/settings");
        await Assertions.Expect(_page.Locator(".settings-organization")).ToBeVisibleAsync();
    }
}