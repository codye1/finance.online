using System.Net.Http.Json;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

[Binding]
public class LoginSteps
{
    private readonly IPage _page;
    private readonly ScenarioContext _scenarioContext;
    private const string BaseUrl = "https://localhost:7024";

    private const string RegisterResponseStatusKey = "RegisterResponseStatus";
    private const string RegisterDialogsKey = "RegisterDialogs";

    public LoginSteps(IPage page, ScenarioContext scenarioContext)
    {
        _page = page;
        _scenarioContext = scenarioContext;
    }

    [Given(@"I am on the auth page")]
    public async Task GivenIAmOnTheAuthPage()
    {
        await _page.GotoAsync($"{BaseUrl}/auth");
    }

    // ---------- Panel visibility ----------

    [Then(@"the login panel is visible")]
    public async Task ThenTheLoginPanelIsVisible()
    {
        await Assertions.Expect(_page.Locator("#loginPanel")).ToBeVisibleAsync();
    }

    [Then(@"the register panel is visible")]
    public async Task ThenTheRegisterPanelIsVisible()
    {
        await Assertions.Expect(_page.Locator("#registerPanel")).ToBeVisibleAsync();
    }

    [Then(@"the login panel is hidden")]
    public async Task ThenTheLoginPanelIsHidden()
    {
        await Assertions.Expect(_page.Locator("#loginPanel")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("hidden"));
    }

    [Then(@"the register panel is hidden")]
    public async Task ThenTheRegisterPanelIsHidden()
    {
        await Assertions.Expect(_page.Locator("#registerPanel")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("hidden"));
    }

    [When(@"I click the toggle link")]
    public async Task WhenIClickTheToggleLink()
    {
        await _page.Locator(".toggle-auth-panels:visible").First.ClickAsync();
    }

    [When(@"I switch to the register panel")]
    public async Task WhenISwitchToTheRegisterPanel()
    {
        await _page.Locator("#loginPanel .toggle-auth-panels").ClickAsync();
    }

    // ---------- Login ----------

    [When(@"I log in with email ""(.*)"" and password ""(.*)""")]
    public async Task WhenILogInWithEmailAndPassword(string email, string password)
    {
        await _page.Locator("#loginEmail").FillAsync(email);
        await _page.Locator("#loginPassword").FillAsync(password);
        await _page.Locator("#loginForm button[type=submit]").ClickAsync();
    }

    [When(@"I log in with the registered email and password ""(.*)""")]
    public async Task WhenILogInWithTheRegisteredEmailAndPassword(string password)
    {
        var email = (string)_scenarioContext["RegisteredEmail"];
        await _page.Locator("#loginEmail").FillAsync(email);
        await _page.Locator("#loginPassword").FillAsync(password);
        await _page.Locator("#loginForm button[type=submit]").ClickAsync();
    }

    [When(@"I submit the login form without an email")]
    public async Task WhenISubmitTheLoginFormWithoutAnEmail()
    {
        await _page.Locator("#loginPassword").FillAsync("Password123!");
        await _page.Locator("#loginForm button[type=submit]").ClickAsync();
    }

    [When(@"I submit the login form without a password")]
    public async Task WhenISubmitTheLoginFormWithoutAPassword()
    {
        await _page.Locator("#loginEmail").FillAsync("user@example.com");
        await _page.Locator("#loginForm button[type=submit]").ClickAsync();
    }

    // ---------- Registration ----------

    [When(@"I register with a new unique email and password ""(.*)""")]
    public async Task WhenIRegisterWithANewUniqueEmailAndPassword(string password)
    {
        var uniqueEmail = $"test_{Guid.NewGuid():N}@example.com";
        _scenarioContext["RegisteredEmail"] = uniqueEmail;

        await _page.Locator("#registerEmail").FillAsync(uniqueEmail);
        await _page.Locator("#registerPassword").FillAsync(password);
        await _page.Locator("#registerForm button[type=submit]").ClickAsync();
    }

    [Given(@"I register with a new unique email and password ""(.*)"" and accept the alert")]
    public async Task GivenIRegisterWithANewUniqueEmailAndPasswordAndAcceptTheAlert(string password)
    {
        var uniqueEmail = $"test_{Guid.NewGuid():N}@example.com";
        _scenarioContext["RegisteredEmail"] = uniqueEmail;

        var dialogTask = new TaskCompletionSource<string>();
        _page.Dialog += async (_, dialog) =>
        {
            dialogTask.TrySetResult(dialog.Message);
            await dialog.AcceptAsync();
        };

        await _page.Locator(".toggle-auth-panels:visible").First.ClickAsync(); // switch to register panel
        await _page.Locator("#registerEmail").FillAsync(uniqueEmail);
        await _page.Locator("#registerPassword").FillAsync(password);
        await _page.Locator("#registerForm button[type=submit]").ClickAsync();

        await dialogTask.Task; // wait for the success alert to confirm registration completed
    }

    [Given(@"a user is already registered")]
    public async Task GivenAUserIsAlreadyRegistered()
    {
        var email = $"existing_{Guid.NewGuid():N}@example.com";
        _scenarioContext["ExistingEmail"] = email;

        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var response = await client.PostAsJsonAsync("/auth/register", new { email, password = "Password123!" });
        response.EnsureSuccessStatusCode();
    }

    [When(@"I register with the existing email and password ""(.*)""")]
    public async Task WhenIRegisterWithTheExistingEmailAndPassword(string password)
    {
        var email = (string)_scenarioContext["ExistingEmail"];

        // Якщо .done() спрацює замість .fail(), auth.js покаже alert() і викличе
        // togglePanels() — панель реєстрації миттєво зникне. Без підписки на
        // Dialog тут Playwright сам закриє нештатний діалог, і ми втратимо доказ,
        // ЩО саме сталось. Тому фіксуємо і статус відповіді, і текст діалогу.
        var dialogs = new List<string>();
        void OnDialog(object? _, IDialog dialog)
        {
            dialogs.Add($"{dialog.Type}: {dialog.Message}");
            _ = dialog.AcceptAsync();
        }
        _page.Dialog += OnDialog;

        try
        {
            await _page.Locator("#registerEmail").FillAsync(email);
            await _page.Locator("#registerPassword").FillAsync(password);

            var response = await _page.RunAndWaitForResponseAsync(
                async () => await _page.Locator("#registerForm button[type=submit]").ClickAsync(),
                r => r.Request.Method == "POST" && r.Url.EndsWith("/auth/register"),
                new() { Timeout = 10000 });

            _scenarioContext[RegisterResponseStatusKey] = response.Status;
        }
        finally
        {
            _page.Dialog -= OnDialog;
            _scenarioContext[RegisterDialogsKey] = dialogs;
        }
    }

    // ---------- Assertions ----------

    [Then(@"I am redirected to the home page")]
    public async Task ThenIAmRedirectedToTheHomePage()
    {
        await _page.WaitForURLAsync(url => url.TrimEnd('/') == BaseUrl.TrimEnd('/'), new() { Timeout = 10000 });
    }

    [Then(@"I see a login error message")]
    public async Task ThenISeeALoginErrorMessage()
    {
        await Assertions.Expect(_page.Locator("#loginForm .server-error")).ToBeVisibleAsync();
    }

    [Then(@"I see a register error message")]
    public async Task ThenISeeARegisterErrorMessage()
    {
        var status = _scenarioContext.ContainsKey(RegisterResponseStatusKey)
            ? (int)_scenarioContext[RegisterResponseStatusKey]
            : -1;
        var dialogs = _scenarioContext.ContainsKey(RegisterDialogsKey)
            ? (List<string>)_scenarioContext[RegisterDialogsKey]
            : new List<string>();

        // Явна, промовиста перевірка ДО пошуку елемента в DOM: якщо сервер
        // повернув 2xx замість 4xx, помилки в DOM просто не буде — і краще
        // впасти тут з точним поясненням, ніж через 5с таймауту ToBeVisibleAsync.
        Assert.IsTrue(
            status is >= 400,
            $"Очікував помилку реєстрації (4xx) для вже зареєстрованого email, " +
            $"але POST /auth/register повернув {status}. " +
            (dialogs.Count > 0
                ? $"З'явився діалог: [{string.Join("; ", dialogs)}] — тобто реєстрація пройшла успішно."
                : "Жодного діалогу не було."));

        await Assertions.Expect(_page.Locator("label.error[for='registerEmail']")).ToBeVisibleAsync();
    }

    [Then(@"I see the registration success alert")]
    public async Task ThenISeeTheRegistrationSuccessAlert()
    {
        var dialogTask = new TaskCompletionSource<string>();
        _page.Dialog += async (_, dialog) =>
        {
            dialogTask.TrySetResult(dialog.Message);
            await dialog.AcceptAsync();
        };

        var message = await dialogTask.Task;
        Assert.IsTrue(message.Contains("успішна"));
    }

    [Then(@"the browser shows a validation message for the email field")]
    public async Task ThenTheBrowserShowsAValidationMessageForTheEmailField()
    {
        var isValid = await _page.Locator("#loginEmail").EvaluateAsync<bool>("el => el.checkValidity()");
        Assert.IsFalse(isValid);
    }

    [Then(@"the browser shows a validation message for the password field")]
    public async Task ThenTheBrowserShowsAValidationMessageForThePasswordField()
    {
        var isValid = await _page.Locator("#loginPassword").EvaluateAsync<bool>("el => el.checkValidity()");
        Assert.IsFalse(isValid);
    }

    [Then(@"the login submit button is disabled while the request is in progress")]
    public async Task ThenTheLoginSubmitButtonIsDisabledWhileTheRequestIsInProgress()
    {
        await Assertions.Expect(_page.Locator("#loginForm button[type=submit]")).ToBeDisabledAsync();
    }
}