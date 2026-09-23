using finance.online.mvc.Models;
using finance.online.mvc.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;

namespace finance.online.mvc.Controllers
{
    public class OperationsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public OperationsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Сторінка "Усі операції". Незалежна від Home: своя ViewModel, свій партіал, свій JS/CSS.
        [HttpGet("/operations")]
        public async Task<IActionResult> Index(string? organizationId)
        {
            var client = _httpClientFactory.CreateClient("FinanceOnlineApi");
            var activeOrganizationResult = await ResolveActiveOrganizationAsync(client, organizationId);

            if (activeOrganizationResult.State is ApiFetchState.Unauthorized or ApiFetchState.Forbidden)
            {
                return RedirectToAction("Auth", "Auth");
            }

            if (activeOrganizationResult.Value is null)
            {
                return View(new OperationsViewModel { HasOrganizations = false });
            }

            var operationsTask = TryGetJsonAsync<List<OperationModel>>(
                client,
                $"/organizations/{activeOrganizationResult.Value.Id}/operations?page=1");

            var categoriesTask = TryGetJsonAsync<List<CategoryResponseDto>>(
                client,
                $"/organizations/{activeOrganizationResult.Value.Id}/categories");

            await Task.WhenAll(operationsTask, categoriesTask);

            var operationsResult = await operationsTask;
            if (operationsResult.State is ApiFetchState.Unauthorized or ApiFetchState.Forbidden)
            {
                return RedirectToAction("Auth", "Auth");
            }

            var categoriesResult = await categoriesTask;
            if (categoriesResult.State is ApiFetchState.Unauthorized or ApiFetchState.Forbidden)
            {
                return RedirectToAction("Auth", "Auth");
            }

            var operations = operationsResult.Value ?? new List<OperationModel>();
            var categories = categoriesResult.Value ?? new List<CategoryResponseDto>();

            return View(new OperationsViewModel
            {
                HasOrganizations = true,
                ActiveOrganization = activeOrganizationResult.Value,
                Operations = operations
                    .OrderByDescending(operation => operation.CreatedAt)
                    .Select(MapOperationListItem)
                    .ToList(),
                Categories = categories
                    .Select(category => new OperationCategoryOptionViewModel
                    {
                        Id = category.Id,
                        Name = category.Name,
                        Color = category.Color
                    })
                    .ToList()
            });
        }

        [HttpPost("/organizations/{orgId}/operations")]
public async Task<IActionResult> CreateOperation(string orgId, [FromBody] OperationCreateDto operationData, [FromQuery] string? view = null)
{
    var client = _httpClientFactory.CreateClient("FinanceOnlineApi");
    var response = await client.PostAsJsonAsync($"/organizations/{orgId}/operations", operationData);

    if (response.IsSuccessStatusCode)
    {
        var createdOperation = await response.Content.ReadFromJsonAsync<OperationModel>();
        if (createdOperation == null)
            return BadRequest(new ApiErrorResponseDto
            {
                Errors = new Dictionary<string, string[]>
                {
                    ["_general"] = new[] { "Unable to read API response." }
                }
            });

        if (string.Equals(view, "operations", StringComparison.OrdinalIgnoreCase))
        {
            var operationsViewModel = MapOperationListItem(createdOperation);
            return PartialView("~/Views/Operations/Partials/_OperationListItem.cshtml", operationsViewModel);
        }

        var homeViewModel = MapOperation(createdOperation);
        return PartialView("~/Views/Home/Partials/_OperationItem.cshtml", homeViewModel);
    }

    return await ForwardApiErrorAsync(response);
}
        [HttpDelete("/operations/{id}")]
        public async Task<IActionResult> DeleteOperation(string id)
        {
            var client = _httpClientFactory.CreateClient("FinanceOnlineApi");
            var response = await client.DeleteAsync($"/operations/{id}");

            if (response.IsSuccessStatusCode)
                return NoContent();

            return await ForwardApiErrorAsync(response);
        }

        [HttpPatch("/operations/{id}")]
        public async Task<IActionResult> UpdateOperation(string id, [FromBody] OperationUpdateDto operationData)
        {
            var client = _httpClientFactory.CreateClient("FinanceOnlineApi");
            var response = await client.PatchAsJsonAsync($"/operations/{id}", operationData);

            if (response.IsSuccessStatusCode)
                return NoContent();

            return await ForwardApiErrorAsync(response);
        }

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

        // ---- Home-сумісний маппінг (не чіпаємо, Home від нього залежить) ----
        private static OperationViewModel MapOperation(OperationModel operation)
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

        // ---- Власний маппінг для сторінки /operations (незалежний від Home) ----
        private static OperationListItemViewModel MapOperationListItem(OperationModel operation)
        {
            return new OperationListItemViewModel
            {
                Id = operation.Id,
                Date = operation.CreatedAt,
                Amount = operation.Amount,
                Category = operation.CategoryName,
                CategoryIcon = GetOperationIcon(operation),
                Type = operation.Type,
                Description = operation.Description
            };
        }

        private static string GetOperationIcon(OperationModel operation)
        {
            if (!string.IsNullOrWhiteSpace(operation.CategoryName))
            {
                return operation.CategoryName.Trim()[0].ToString().ToUpperInvariant();
            }

            return string.Equals(operation.Type, "income", StringComparison.OrdinalIgnoreCase) ? "↑" : "↓";
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