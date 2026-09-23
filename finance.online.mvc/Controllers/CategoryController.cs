using finance.online.mvc.Models;
using finance.online.mvc.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;

namespace finance.online.mvc.Controllers
{
    public class CategoriesController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CategoriesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("/categories")]
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
                return View(new CategoriesViewModel { HasOrganizations = false });
            }

            var categoriesResult = await TryGetJsonAsync<List<CategoryResponseDto>>(
                client,
                $"/organizations/{activeOrganizationResult.Value.Id}/categories");

            if (categoriesResult.State is ApiFetchState.Unauthorized or ApiFetchState.Forbidden)
            {
                return RedirectToAction("Auth", "Auth");
            }

            return View(new CategoriesViewModel
            {
                HasOrganizations = true,
                ActiveOrganization = activeOrganizationResult.Value,
                Categories = categoriesResult.Value ?? new List<CategoryResponseDto>()
            });
        }

        [HttpPost("/categories/create")]
public async Task<IActionResult> Create([FromBody] CategoryCreateRequestDto request)
{
    if (request is null
        || string.IsNullOrWhiteSpace(request.OrganizationId)
        || string.IsNullOrWhiteSpace(request.Name))
    {
        return BadRequest();
    }

    var client = _httpClientFactory.CreateClient("FinanceOnlineApi");
    var response = await client.PostAsJsonAsync(
        $"/organizations/{request.OrganizationId}/categories",
        new { name = request.Name, color = request.Color });

    if (response.IsSuccessStatusCode)
    {
        var category = await response.Content.ReadFromJsonAsync<CategoryResponseDto>();
        return PartialView("~/Views/Categories/Partials/_CategoryItem.cshtml", category);
    }

    return await ForwardApiErrorAsync(response);
}

        [HttpPost("/categories/delete")]
        public async Task<IActionResult> Delete([FromBody] CategoryDeleteRequestDto request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.CategoryId))
            {
                return BadRequest();
            }

            var client = _httpClientFactory.CreateClient("FinanceOnlineApi");
            var response = await client.DeleteAsync($"/categories/{request.CategoryId}");

            if (response.IsSuccessStatusCode)
            {
                return NoContent();
            }

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

        private enum ApiFetchState
        {
            Success,
            Failed,
            Unauthorized,
            Forbidden
        }
    }
}