using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Playwright;
using Reqnroll;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tests.StepDefinitions
{
    [Binding]
    [Scope(Feature = "MainPageFeature")]
    public class MainPageSteps
    {

        private readonly IPage _page;
        private readonly ScenarioContext _scenarioContext;
        private string BaseUrl => _scenarioContext.Get<TestBackendFixture>("BackendFixture").MvcBaseUrl;
        private const string TestEmail = "newuser@example.com";
        private const string TestPassword = "Password123!";
        public MainPageSteps(IPage page, ScenarioContext scenarioContext)
        {
            _page = page;
            _scenarioContext = scenarioContext;
        }


        [Given("i am logged in and on the main page")]
        public async Task GivenIAmLoggedInAndOnTheMainPage()
        {
            await _page.GotoAsync($"{BaseUrl}/auth");
            await _page.Locator("#loginEmail").FillAsync(TestEmail);
            await _page.Locator("#loginPassword").FillAsync(TestPassword);
            await _page.Locator("#loginForm button[type=submit]").ClickAsync();
            await _page.WaitForURLAsync(url => url.TrimEnd('/') == BaseUrl.TrimEnd('/'), new() { Timeout = 10000 });
        }
    }
}
