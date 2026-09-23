using finance.online.mvc.Models;
using finance.online.mvc.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;

namespace finance.online.mvc.Controllers
{
    public class SettingsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SettingsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("/settings")]
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
                return View(new SettingsViewModel());
            }

            var membersResult = await TryGetJsonAsync<List<OrganizationMemberResponseDto>>(
                client,
                $"/organizations/{activeOrganizationResult.Value.Id}/members");

            if (membersResult.State is ApiFetchState.Unauthorized or ApiFetchState.Forbidden)
            {
                return RedirectToAction("Auth", "Auth");
            }

            return View(new SettingsViewModel
            {
                ActiveOrganization = activeOrganizationResult.Value,
                Members = membersResult.Value ?? new List<OrganizationMemberResponseDto>()
            });
        }

[HttpPost("/settings/delete-organization")]
public async Task<IActionResult> DeleteOrganization([FromBody] DeleteOrganizationRequestDto request)
{
    if (request is null || string.IsNullOrWhiteSpace(request.OrganizationId))
    {
        return BadRequest();
    }

    var client = _httpClientFactory.CreateClient("FinanceOnlineApi");
    var response = await client.DeleteAsync($"/organizations/{request.OrganizationId}");

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

[HttpPost("/settings/invite-member")]
public async Task<IActionResult> InviteMember([FromBody] InviteMemberRequestDto request)
{
    if (request is null
        || string.IsNullOrWhiteSpace(request.OrganizationId)
        || string.IsNullOrWhiteSpace(request.Email))
    {
        return BadRequest();
    }

    var client = _httpClientFactory.CreateClient("FinanceOnlineApi");
    var response = await client.PostAsJsonAsync(
        $"/organizations/{request.OrganizationId}/members",
        new { email = request.Email, role = request.Role });

    if (response.IsSuccessStatusCode)
    {
        var member = await response.Content.ReadFromJsonAsync<OrganizationMemberResponseDto>();
        return Ok(member);
    }

    return await ForwardApiErrorAsync(response);
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
