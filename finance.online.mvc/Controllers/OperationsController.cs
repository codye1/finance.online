using finance.online.mvc.Models;
using finance.online.mvc.Models.DTO;
using Microsoft.AspNetCore.Mvc;
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

        [HttpPost("/organizations/{orgId}/operations")]
        public async Task<IActionResult> CreateOperation(string orgId, [FromBody] OperationCreateDto operationData)
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

                var viewModel = MapOperation(createdOperation);

                return PartialView("~/Views/Home/Partials/_OperationItem.cshtml", viewModel);
            }

            return await ForwardApiErrorAsync(response);
        }

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

        private static string GetOperationIcon(OperationModel operation)
        {
            if (!string.IsNullOrWhiteSpace(operation.CategoryName))
            {
                return operation.CategoryName.Trim()[0].ToString().ToUpperInvariant();
            }

            return string.Equals(operation.Type, "income", StringComparison.OrdinalIgnoreCase) ? "↑" : "↓";
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

            // Відправляємо PATCH запит на Web API
            var response = await client.PatchAsJsonAsync($"/operations/{id}", operationData);

            if (response.IsSuccessStatusCode)
                return NoContent();

            return await ForwardApiErrorAsync(response);
        }
    }
}