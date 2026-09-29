using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;
using static Microsoft.Playwright.Assertions;

namespace tests.StepDefinitions
{
    [Binding]
    public class CommonSteps
    {
        public const string UniqueEmailKey = "CommonSteps.UniqueEmail";
        private const string UniqueKey = "CommonSteps.Unique";
        private const string DialogsKey = "CommonSteps.Dialogs";
        private const string KpiKey = "CommonSteps.KpiSnapshot";

        private readonly IPage _page;
        private readonly ScenarioContext _scenarioContext;

        public CommonSteps(IPage page, ScenarioContext scenarioContext)
        {
            _page = page;
            _scenarioContext = scenarioContext;
        }

        // ---------- helpers ----------

        private static string Cell(DataTable table, DataTableRow row, string column)
            => table.Header.Contains(column) ? row[column] : "";

        private static bool ParseBool(string value, bool defaultValue = true)
            => string.IsNullOrWhiteSpace(value) ? defaultValue : bool.Parse(value.Trim().ToLowerInvariant());

        /// <summary>
        /// Плейсхолдери: {chars:N} -> рядок з N символів 'a', {today} -> yyyy-MM-dd, {unique} -> одне унікальне значення на сценарій,
        /// {uniqueEmail} -> один унікальний email на сценарій (його ж використовує API-реєстрація в LoginSteps).
        /// </summary>
        public string Resolve(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";

            value = value.Replace("{today}", DateTime.Today.ToString("yyyy-MM-dd"));
            value = Regex.Replace(value, @"\{chars:(\d+)\}", m => new string('a', int.Parse(m.Groups[1].Value)));

            if (value.Contains("{uniqueEmail}"))
            {
                if (!_scenarioContext.ContainsKey(UniqueEmailKey))
                    _scenarioContext[UniqueEmailKey] = $"test_{Guid.NewGuid():N}@example.com";

                value = value.Replace("{uniqueEmail}", (string)_scenarioContext[UniqueEmailKey]);
            }

            if (value.Contains("{unique}"))
            {
                if (!_scenarioContext.ContainsKey(UniqueKey))
                    _scenarioContext[UniqueKey] = "Test " + Guid.NewGuid().ToString("N")[..12];

                value = value.Replace("{unique}", (string)_scenarioContext[UniqueKey]);
            }

            return value;
        }

        private static decimal ParseMoney(string text)
        {
            var digits = Regex.Replace(text ?? "", @"\D", "");
            if (digits.Length == 0) return 0;

            var value = decimal.Parse(digits);
            return Regex.IsMatch(text!, @"[-−–]") ? -value : value;
        }

        private static async Task AssertCountAsync(ILocator locator, Func<int, bool> isOk, string description)
        {
            var count = 0;
            for (var i = 0; i < 20; i++)
            {
                count = await locator.CountAsync();
                if (isOk(count)) return;
                await Task.Delay(250);
            }
            Assert.Fail($"Кількість елементів: очікувалось {description}, фактично {count}");
        }

        private ILocator KpiValue(string title) =>
            _page.Locator(".kpi-card")
                .Filter(new() { Has = _page.Locator(".kpi-title", new() { HasTextString = title }) })
                .Locator(".kpi-value");

        // ---------- existence ----------

        [Given("check elements existence")]
        [When("check elements existence")]
        [Then("check elements existence")]
        public async Task WhenCheckElementsExistence(DataTable dataTable)
        {
            foreach (var row in dataTable.Rows)
            {
                var locator = _page.Locator(Resolve(row["ElementLocator"]));

                await Expect(locator).ToBeVisibleAsync();

                if (dataTable.Header.Contains("ElementTagName") && !string.IsNullOrWhiteSpace(row["ElementTagName"]))
                {
                    var tag = await locator.EvaluateAsync<string>("e => e.tagName.toLowerCase()");
                    Assert.AreEqual(row["ElementTagName"].ToLower(), tag,
                        $"Тег елемента '{row["ElementLocator"]}'");
                }

                var attribute = dataTable.Header.Contains("AttributeName") ? row["AttributeName"] : "";
                var expected = Resolve(dataTable.Header.Contains("ElementValue") ? row["ElementValue"] : "");

                if (!string.IsNullOrWhiteSpace(attribute))
                    await Expect(locator).ToHaveAttributeAsync(attribute, expected);
                else if (!string.IsNullOrWhiteSpace(expected))
                    await Expect(locator).ToHaveTextAsync(expected);
            }
        }

        // | ElementLocator | Visible |
        [Given("check elements visibility")]
        [When("check elements visibility")]
        [Then("check elements visibility")]
        public async Task CheckElementsVisibility(DataTable table)
        {
            foreach (var row in table.Rows)
            {
                var locator = _page.Locator(Resolve(row["ElementLocator"])).First;

                if (ParseBool(Cell(table, row, "Visible")))
                    await Expect(locator).ToBeVisibleAsync();
                else
                    await Expect(locator).ToBeHiddenAsync();
            }
        }

        // | ElementLocator | ExpectedCount |   (ExpectedCount: 4, 0, >0, >=2)
        [Given("check elements count")]
        [When("check elements count")]
        [Then("check elements count")]
        public async Task CheckElementsCount(DataTable table)
        {
            foreach (var row in table.Rows)
            {
                var locator = _page.Locator(Resolve(row["ElementLocator"]));
                var expected = row["ExpectedCount"].Trim();

                if (expected.StartsWith(">="))
                {
                    var min = int.Parse(expected[2..]);
                    await AssertCountAsync(locator, c => c >= min, expected);
                }
                else if (expected.StartsWith(">"))
                {
                    var min = int.Parse(expected[1..]);
                    await AssertCountAsync(locator, c => c > min, expected);
                }
                else
                {
                    await Expect(locator).ToHaveCountAsync(int.Parse(expected));
                }
            }
        }

        // | ElementLocator | ClassName | HasClass |
        [Given("check elements class")]
        [When("check elements class")]
        [Then("check elements class")]
        public async Task CheckElementsClass(DataTable table)
        {
            foreach (var row in table.Rows)
            {
                var locator = _page.Locator(Resolve(row["ElementLocator"]));
                var regex = new Regex($@"(^|\s){Regex.Escape(row["ClassName"].Trim())}(\s|$)");

                if (ParseBool(Cell(table, row, "HasClass")))
                    await Expect(locator).ToHaveClassAsync(regex);
                else
                    await Expect(locator).Not.ToHaveClassAsync(regex);
            }
        }

        // | ElementLocator | ExpectedValue |   (працює і для hidden input)
        [Given("check elements value")]
        [When("check elements value")]
        [Then("check elements value")]
        public async Task CheckElementsValue(DataTable table)
        {
            foreach (var row in table.Rows)
                await Expect(_page.Locator(Resolve(row["ElementLocator"])))
                    .ToHaveValueAsync(Resolve(row["ExpectedValue"]));
        }

        // | ElementLocator | AttributeName | ExpectedValue |   (атрибут містить значення, без урахування регістру)
        [Given("check elements attribute contains")]
        [When("check elements attribute contains")]
        [Then("check elements attribute contains")]
        public async Task CheckElementsAttributeContains(DataTable table)
        {
            foreach (var row in table.Rows)
                await Expect(_page.Locator(Resolve(row["ElementLocator"])))
                    .ToHaveAttributeAsync(
                        row["AttributeName"],
                        new Regex(Regex.Escape(Resolve(row["ExpectedValue"])), RegexOptions.IgnoreCase));
        }

        // | ElementLocator | ExpectedText |   (містить текст, не точний збіг)
        [Given("check elements contain text")]
        [When("check elements contain text")]
        [Then("check elements contain text")]
        public async Task CheckElementsContainText(DataTable table)
        {
            foreach (var row in table.Rows)
                await Expect(_page.Locator(Resolve(row["ElementLocator"])))
                    .ToContainTextAsync(Resolve(row["ExpectedText"]));
        }

        // ---------- actions ----------

        // | ElementLocator |
        [Given("click elements")]
        [When("click elements")]
        public async Task ClickElements(DataTable table)
        {
            foreach (var row in table.Rows)
                await _page.Locator(Resolve(row["ElementLocator"])).ClickAsync();
        }

        [Given(@"click element ""(.*)""")]
        [When(@"click element ""(.*)""")]
        public async Task ClickElement(string locator)
        {
            await _page.Locator(Resolve(locator)).ClickAsync();
        }

        // | ElementLocator | Action | Value |
        // Action: fill | select (число = індекс, інакше label) | click | blur | check | uncheck
        // Value:  підтримує {today} та {unique}
        [Given("fill form")]
        [When("fill form")]
        public async Task FillForm(DataTable table)
        {
            foreach (var row in table.Rows)
            {
                var locator = _page.Locator(Resolve(row["ElementLocator"]));
                var action = row["Action"].Trim().ToLowerInvariant();
                var value = Resolve(Cell(table, row, "Value"));

                switch (action)
                {
                    case "fill":
                        await locator.FillAsync(value);
                        break;
                    case "select":
                        await locator.SelectOptionAsync(int.TryParse(value, out var index)
                            ? new SelectOptionValue { Index = index }
                            : new SelectOptionValue { Label = value });
                        break;
                    case "click":
                        await locator.ClickAsync();
                        break;
                    case "blur":
                        await locator.BlurAsync();
                        break;
                    case "check":
                        await locator.CheckAsync();
                        break;
                    case "uncheck":
                        await locator.UncheckAsync();
                        break;
                    default:
                        Assert.Fail($"Невідома дія '{action}' для '{row["ElementLocator"]}'");
                        break;
                }
            }
        }

        // | ElementLocator | Enabled |
        [Given("check elements state")]
        [When("check elements state")]
        [Then("check elements state")]
        public async Task CheckElementsState(DataTable table)
        {
            foreach (var row in table.Rows)
            {
                var locator = _page.Locator(Resolve(row["ElementLocator"]));

                if (ParseBool(Cell(table, row, "Enabled")))
                    await Expect(locator).ToBeEnabledAsync();
                else
                    await Expect(locator).ToBeDisabledAsync();
            }
        }

        [When("the page is reloaded")]
        public async Task ReloadPage()
        {
            await _page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        }

        // ---------- navigation / url / viewport ----------

        // | ElementLocator | ExpectedUrl |  — клік, перевірка URL, повернення назад
        [When("check navigation")]
        public async Task CheckNavigation(DataTable table)
        {
            foreach (var row in table.Rows)
            {
                await _page.Locator(row["ElementLocator"]).ClickAsync();

                await Expect(_page).ToHaveURLAsync(
                    new Regex($@"{Regex.Escape(row["ExpectedUrl"].Trim())}/?(\?.*)?$"));

                await _page.GoBackAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            }
        }

        [Then(@"current url contains ""(.*)""")]
        public async Task ThenCurrentUrlContains(string part)
        {
            await Expect(_page).ToHaveURLAsync(new Regex(Regex.Escape(part)), new() { Timeout = 10000 });
        }

        [Given(@"the viewport size is (\d+)x(\d+)")]
        [When(@"the viewport size is (\d+)x(\d+)")]
        public async Task SetViewport(int width, int height)
        {
            await _page.SetViewportSizeAsync(width, height);
        }

        [Then(@"the current path is ""(.*)""")]
        public async Task ThenTheCurrentPathIs(string path)
        {
            var trimmed = path.TrimEnd('/');
            await Expect(_page).ToHaveURLAsync(
                new Regex($@"^https?://[^/]+{Regex.Escape(trimmed)}/?(\?.*)?$"),
                new() { Timeout = 10000 });
        }

        // ---------- dialogs (alert/confirm) ----------

        private string[] DialogsSnapshot()
        {
            if (!_scenarioContext.TryGetValue(DialogsKey, out var stored)) return Array.Empty<string>();
            var list = (List<string>)stored;
            lock (list) return list.ToArray();
        }

        // Підписка ДО дії, що викликає alert/confirm: діалог записується й приймається
        [Given("the browser accepts dialogs")]
        [When("the browser accepts dialogs")]
        public void BrowserAcceptsDialogs() => SubscribeToDialogs(accept: true);

        // Те саме, але діалог відхиляється (наприклад, "Скасувати" у confirm)
        [Given("the browser dismisses dialogs")]
        [When("the browser dismisses dialogs")]
        public void BrowserDismissesDialogs() => SubscribeToDialogs(accept: false);

        private void SubscribeToDialogs(bool accept)
        {
            var dialogs = new List<string>();
            _scenarioContext[DialogsKey] = dialogs;

            _page.Dialog += (_, dialog) =>
            {
                lock (dialogs) dialogs.Add($"{dialog.Type}: {dialog.Message}");
                _ = accept ? dialog.AcceptAsync() : dialog.DismissAsync();
            };
        }

        [Given(@"a dialog is shown containing ""(.*)""")]
        [When(@"a dialog is shown containing ""(.*)""")]
        [Then(@"a dialog is shown containing ""(.*)""")]
        public async Task DialogIsShownContaining(string text)
        {
            for (var i = 0; i < 40; i++)
            {
                if (DialogsSnapshot().Any(d => d.Contains(text))) return;
                await Task.Delay(250);
            }
            Assert.Fail($"Діалог з текстом '{text}' не з'явився. Були: [{string.Join("; ", DialogsSnapshot())}]");
        }

        [Then("no dialog is shown")]
        public async Task NoDialogIsShown()
        {
            // чекаємо, поки завершиться запит після сабміту, щоб alert з .done() встиг з'явитись
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var dialogs = DialogsSnapshot();
            Assert.AreEqual(0, dialogs.Length, $"Неочікувані діалоги: [{string.Join("; ", dialogs)}]");
        }

        // ---------- KPI ----------

        // | KpiTitle |
        [Given("remember kpi values")]
        [When("remember kpi values")]
        public async Task RememberKpiValues(DataTable table)
        {
            var snapshot = new Dictionary<string, decimal>();
            foreach (var row in table.Rows)
            {
                var title = row["KpiTitle"];
                snapshot[title] = ParseMoney(await KpiValue(title).InnerTextAsync());
            }
            _scenarioContext[KpiKey] = snapshot;
        }

        // | KpiTitle | Change |   (Change: increased | decreased | unchanged)
        // Працює для грошових KPI; для відсотків (Маржа) формат може дати хибний результат.
        [Then("check kpi values changed")]
        public async Task ThenCheckKpiValuesChanged(DataTable table)
        {
            var before = (Dictionary<string, decimal>)_scenarioContext[KpiKey];

            foreach (var row in table.Rows)
            {
                var title = row["KpiTitle"];
                var change = row["Change"].Trim().ToLowerInvariant();
                var old = before[title];
                var current = old;

                bool Matches(decimal value) => change switch
                {
                    "increased" => value > old,
                    "decreased" => value < old,
                    "unchanged" => value == old,
                    _ => throw new ArgumentException($"Невідомий Change '{change}'")
                };

                for (var i = 0; i < 40; i++)
                {
                    try { current = ParseMoney(await KpiValue(title).InnerTextAsync()); }
                    catch (PlaywrightException) { /* сторінка перезавантажується */ }

                    if (Matches(current)) break;
                    await Task.Delay(500);
                }

                Assert.IsTrue(Matches(current),
                    $"KPI '{title}': було {old}, стало {current}, очікувалось: {change}");
            }
        }

        // ---------- organizations ----------

        // | OrganizationName | IsActive |   (dropdown має бути відкритий)
        [Then("the organization dropdown contains")]
        public async Task ThenTheOrganizationDropdownContains(DataTable table)
        {
            var activeClass = new Regex(@"(^|\s)active(\s|$)");

            foreach (var row in table.Rows)
            {
                var name = Resolve(row["OrganizationName"]);
                var item = _page.Locator("#org-select-dropdown .org-select-item")
                    .Filter(new() { HasTextRegex = new Regex($@"^\s*{Regex.Escape(name)}\s*$") });

                await Expect(item).ToBeVisibleAsync();

                if (ParseBool(Cell(table, row, "IsActive")))
                    await Expect(item).ToHaveClassAsync(activeClass);
                else
                    await Expect(item).Not.ToHaveClassAsync(activeClass);
            }
        }
    }
}