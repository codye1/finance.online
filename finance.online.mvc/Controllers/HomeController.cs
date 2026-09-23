using finance.online.mvc.Models;
using finance.online.mvc.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using System.Diagnostics;

namespace finance.online.mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index(
    string? organizationId,
    string period = "month")
{
    var viewModel = await BuildHomeViewModelAsync(organizationId, period);

    if (viewModel == null)
    {
        return RedirectToAction("Auth", "Auth");
    }

    return View(viewModel);
}

        // Повний список організацій юзера — лишив як окремий екшн на випадок,
        // якщо знадобиться ліниво оновлювати дропдаун без перезавантаження сторінки
        [HttpGet]
        public async Task<IActionResult> OrganizationsList()
        {
            var client = _httpClientFactory.CreateClient("FinanceOnlineApi");
            var result = await TryGetJsonAsync<List<OrganizationResponseDto>>(client, "/organizations/list");

            if (result.State is ApiFetchState.Unauthorized or ApiFetchState.Forbidden)
            {
                return Unauthorized();
            }

            return Json(result.Value ?? new List<OrganizationResponseDto>());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

       private async Task<HomeViewModel?> BuildHomeViewModelAsync(
    string? organizationId,
    string period)
{
    var client = _httpClientFactory.CreateClient("FinanceOnlineApi");
    var periods = GetDefaultPeriods();

    period = periods.Any(x => x.Value == period)
        ? period
        : "month";

    var activeOrganizationTask =
        ResolveActiveOrganizationAsync(client, organizationId);

    var organizationsListTask =
        TryGetJsonAsync<List<OrganizationResponseDto>>(
            client,
            "/organizations/list");

    await Task.WhenAll(
        activeOrganizationTask,
        organizationsListTask);

    var activeOrganizationResult = await activeOrganizationTask;

    if (activeOrganizationResult.State is
        ApiFetchState.Unauthorized or
        ApiFetchState.Forbidden)
    {
        return null;
    }

    var organizationsListResult = await organizationsListTask;

    if (organizationsListResult.State is
        ApiFetchState.Unauthorized or
        ApiFetchState.Forbidden)
    {
        return null;
    }

    var organizations =
        organizationsListResult.Value ??
        new List<OrganizationResponseDto>();

    var activeOrganization = activeOrganizationResult.Value;

    if (activeOrganization is null)
    {
        return new HomeViewModel
        {
            ActiveOrganization = new OrganizationResponseDto(),
            Organizations = organizations,
            ActivePeriod = period,
            Periods = periods,
            Kpis = BuildEmptyKpis(period),
            Operations = new List<OperationViewModel>(),
            Categories = new List<CategoryViewModel>()
        };
    }

    var summaryTask = TryGetJsonAsync<OperationsSummaryDto>(
        client,
        $"/organizations/{activeOrganization.Id}/operations/summary?period={Uri.EscapeDataString(period)}");

    var operationsTask = TryGetJsonAsync<List<OperationResponseDto>>(
        client,
        $"/organizations/{activeOrganization.Id}/operations?period={Uri.EscapeDataString(period)}&page=1");

    var categoriesTask = TryGetJsonAsync<List<CategoryResponseDto>>(
        client,
        $"/organizations/{activeOrganization.Id}/categories");

    await Task.WhenAll(
        summaryTask,
        operationsTask,
        categoriesTask);

    var summaryResult = await summaryTask;

    if (summaryResult.State is
        ApiFetchState.Unauthorized or
        ApiFetchState.Forbidden)
    {
        return null;
    }

    var operationsResult = await operationsTask;

    if (operationsResult.State is
        ApiFetchState.Unauthorized or
        ApiFetchState.Forbidden)
    {
        return null;
    }

    var categoriesResult = await categoriesTask;

    if (categoriesResult.State is
        ApiFetchState.Unauthorized or
        ApiFetchState.Forbidden)
    {
        return null;
    }

    var summary =
        summaryResult.Value ??
        new OperationsSummaryDto
        {
            Period = period
        };

    var operations =
        operationsResult.Value ??
        new List<OperationResponseDto>();

    var categories =
        categoriesResult.Value ??
        new List<CategoryResponseDto>();

    return new HomeViewModel
    {
        // Значення приходить безпосередньо з API
        NetBalance = summary.NetProfit,

        ActiveOrganization = activeOrganization,
        Organizations = organizations,

        ActivePeriod = period,
        Periods = periods,

        Kpis = BuildKpis(summary, period),

        Operations = operations
            .OrderByDescending(operation => operation.CreatedAt)
            .Select(MapOperation)
            .ToList(),

        Categories = categories
            .Select(MapCategory)
            .ToList()
    };
}
[HttpGet("/organizations/{orgId}/operations/more")]
public async Task<IActionResult> LoadMoreOperations(string orgId, int page = 2, string period = "month")
{
    var client = _httpClientFactory.CreateClient("FinanceOnlineApi");
    var result = await TryGetJsonAsync<List<OperationResponseDto>>(
        client,
        $"/organizations/{orgId}/operations?period={period}&page={page}");

    if (result.State is ApiFetchState.Unauthorized or ApiFetchState.Forbidden)
    {
        return Unauthorized();
    }

    var operations = (result.Value ?? new List<OperationResponseDto>())
        .OrderByDescending(operation => operation.CreatedAt)
        .Select(MapOperation)
        .ToList();

    return PartialView("~/Views/Home/Partials/_OperationItemsList.cshtml", operations);
}
        // Якщо передано organizationId (з localStorage на клієнті) — тягнемо конкретну організацію по id.
        // Якщо його нема, або він виявився невалідним (404/Failed) — фолбек на /organizations/active.
        private static async Task<(ApiFetchState State, OrganizationResponseDto? Value)> ResolveActiveOrganizationAsync(
            HttpClient client,
            string? organizationId)
        {
            if (!string.IsNullOrWhiteSpace(organizationId))
            {
                var byIdResult = await TryGetJsonAsync<OrganizationResponseDto>(client, $"/organizations/{organizationId}");

                if (byIdResult.State is ApiFetchState.Unauthorized or ApiFetchState.Forbidden)
                {
                    return byIdResult;
                }

                if (byIdResult.State == ApiFetchState.Success && byIdResult.Value is not null)
                {
                    return byIdResult;
                }

                // невалідний/видалений id зі старого localStorage — падаємо на активну
            }

            return await TryGetJsonAsync<OrganizationResponseDto>(client, "/organizations/active");
        }

        private static async Task<(ApiFetchState State, T? Value)> TryGetJsonAsync<T>(HttpClient client, string requestUri)
        {
            using var response = await client.GetAsync(requestUri);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return (ApiFetchState.Unauthorized, default);
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return (ApiFetchState.Forbidden, default);
            }

            if (!response.IsSuccessStatusCode)
            {
                return (ApiFetchState.Failed, default);
            }

            var value = await response.Content.ReadFromJsonAsync<T>();
            return (ApiFetchState.Success, value);
        }

        private static List<PeriodOption> GetDefaultPeriods()
{
    return new List<PeriodOption>
    {
        new()
        {
            Value = "week",
            Label = "Тиждень"
        },
        new()
        {
            Value = "month",
            Label = "Місяць"
        },
        new()
        {
            Value = "year",
            Label = "Рік"
        },
        new()
        {
            Value = "all",
            Label = "Весь час"
        }
    };
}

        private static List<KpiViewModel> BuildEmptyKpis(string period)
{
    var periodLabel = period switch
    {
        "week" => "За тиждень",
        "month" => "За місяць",
        "year" => "За рік",
        "all" => "За весь час",
        _ => "За період"
    };

    return new List<KpiViewModel>
    {
        new()
        {
            Tone = "income",
            Title = "Дохід",
            Value = "0.00",
            Sub = periodLabel,
            Icon = "trend-up"
        },

        new()
        {
            Tone = "expense",
            Title = "Витрати",
            Value = "0.00",
            Sub = periodLabel,
            Icon = "trend-down"
        },

        new()
        {
            Tone = "primary",
            Title = "Чистий прибуток",
            Value = "0.00",
            Sub = periodLabel,
            Icon = "percent"
        },

        new()
        {
            Tone = "neutral",
            Title = "Маржа",
            Value = "0.00%",
            Sub = periodLabel,
            Icon = "percent"
        }
    };
}

        private static List<KpiViewModel> BuildKpis(
    OperationsSummaryDto summary,
    string period)
{
    var periodLabel = period switch
    {
        "week" => "За тиждень",
        "month" => "За місяць",
        "year" => "За рік",
        "all" => "За весь час",
        _ => "За період"
    };

    return new List<KpiViewModel>
    {
        new()
        {
            Tone = "income",
            Title = "Дохід",
            Value = FormatMoney(summary.Income),
            Sub = periodLabel,
            Icon = "trend-up"
        },

        new()
        {
            Tone = "expense",
            Title = "Витрати",
            Value = FormatMoney(summary.Expense),
            Sub = periodLabel,
            Icon = "trend-down"
        },

        new()
        {
            Tone = "primary",
            Title = "Чистий прибуток",
            Value = FormatMoney(summary.NetProfit),
            Sub = periodLabel,
            Icon = "percent"
        },

        new()
        {
            Tone = "neutral",
            Title = "Маржа",
            Value = $"{summary.Margin:N2}%",
            Sub = periodLabel,
            Icon = "percent"
        }
    };
}

        private static OperationViewModel MapOperation(OperationResponseDto operation)
        {
            return new OperationViewModel
            {
                Date = operation.CreatedAt,
                Amount = operation.Amount,
                Category = operation.CategoryName,
                CategoryIcon = GetOperationIcon(operation),
                Type = operation.Type,
                Description = operation.Description
            };
        }

        private static CategoryViewModel MapCategory(CategoryResponseDto category)
        {
            return new CategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Color = category.Color
            };
        }

        private static string GetOperationIcon(OperationResponseDto operation)
        {
            if (!string.IsNullOrWhiteSpace(operation.CategoryName))
            {
                return operation.CategoryName.Trim()[0].ToString().ToUpperInvariant();
            }

            return string.Equals(operation.Type, "income", StringComparison.OrdinalIgnoreCase) ? "↑" : "↓";
        }

        private static string FormatMoney(decimal amount)
        {
            return amount.ToString("N2");
        }

        private enum ApiFetchState
        {
            Success,
            Failed,
            Unauthorized,
            Forbidden
        }
    }
}