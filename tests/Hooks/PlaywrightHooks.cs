using Microsoft.Playwright;
using Reqnroll;
using Reqnroll.BoDi;

[Binding]
public class PlaywrightHooks
{
    private static IPlaywright _playwright = null!;
    private static IBrowser _browser = null!;
    private readonly IObjectContainer _objectContainer;
    private IBrowserContext _context = null!;

    public PlaywrightHooks(IObjectContainer objectContainer)
    {
        _objectContainer = objectContainer;
    }

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false, // Set to true for headless mode
        });
    }

    [BeforeScenario(Order = 0)]
    public async Task BeforeScenario()
    {
        _context = await _browser.NewContextAsync();
        var page = await _context.NewPageAsync();
        _objectContainer.RegisterInstanceAs<IPage>(page);
    }

    [AfterScenario]
    public async Task AfterScenario()
    {
        await _context.CloseAsync();
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();
    }
}