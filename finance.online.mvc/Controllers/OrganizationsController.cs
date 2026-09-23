using finance.online.mvc.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace finance.online.mvc.Controllers
{
    public class OrganizationsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public OrganizationsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("/organizations")]
        public async Task<IActionResult> CreateOrganization([FromBody] OrganizationCreateDto organizationData)
        {
            var client = _httpClientFactory.CreateClient("FinanceOnlineApi");
            var response = await client.PostAsJsonAsync("/organizations", organizationData);

            if (response.IsSuccessStatusCode)
            {
                var createdOrganization = await response.Content.ReadFromJsonAsync<OrganizationResponseDto>();
                if (createdOrganization == null)
                    return BadRequest(new ApiErrorResponseDto
                    {
                        Errors = new Dictionary<string, string[]>
                        {
                            ["_general"] = new[] { "Unable to read API response." }
                        }
                    });

                return Json(createdOrganization);
            }

            return await ForwardApiErrorAsync(response);
        }
    }
}
