using finance.online.api.Models;
using finance.online.api.Models.DTO;
using finance.online.api.Repositories.OperationRepository;
using finance.online.api.Repositories.OrganizationRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace finance.online.api.Controllers
{
    [ApiController]
    [Authorize]
    public class OperationsController : ControllerBase
    {
        private readonly IOperationRepository _operationRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly UserManager<AppUser> _userManager;

        public OperationsController(
            IOperationRepository operationRepository,
            IOrganizationRepository organizationRepository,
            UserManager<AppUser> userManager)
        {
            _operationRepository = operationRepository;
            _organizationRepository = organizationRepository;
            _userManager = userManager;
        }

        // Owner, Accountant, Member: перегляд
        [HttpGet("/organizations/{orgId}/operations")]
        public async Task<IActionResult> GetByOrganization(
            string orgId,
            [FromQuery] string? type,
            [FromQuery] string? category,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? q,
            [FromQuery] int page = 1)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }

            var role = await _organizationRepository.GetMemberRoleAsync(orgId, currentUserId);
            if (role == null)
            {
                return NotFound(ApiErrors.General("Organization not found."));
            }

            var operations = await _operationRepository.GetByOrganizationAsync(orgId, type, category, from, to, q, page, currentUserId);
            return Ok(operations.Select(MapOperation).ToList());
        }

        // Owner, Accountant, Member: перегляд
        [HttpGet("/organizations/{orgId}/operations/summary")]
        public async Task<IActionResult> GetSummary(string orgId, [FromQuery] string period = "month")
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }

            var role = await _organizationRepository.GetMemberRoleAsync(orgId, currentUserId);
            if (role == null)
            {
                return NotFound(ApiErrors.General("Organization not found."));
            }

            var summary = await _operationRepository.GetSummaryAsync(orgId, period);
            if (summary == null)
            {
                return BadRequest(ApiErrors.General("Unsupported period."));
            }

            return Ok(summary);
        }

        // Owner, Accountant, Member: перегляд
        [HttpGet("/organizations/{orgId}/operations/cashflow")]
        public async Task<IActionResult> GetCashflow(string orgId, [FromQuery] string period = "month")
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }

            var role = await _organizationRepository.GetMemberRoleAsync(orgId, currentUserId);
            if (role == null)
            {
                return NotFound(ApiErrors.General("Organization not found."));
            }

            var cashflow = await _operationRepository.GetCashflowAsync(orgId, period);
            if (cashflow == null)
            {
                return BadRequest(ApiErrors.General("Unsupported period."));
            }

            return Ok(cashflow);
        }

        // Owner, Accountant: створення
        [HttpPost("/organizations/{orgId}/operations")]
        public async Task<IActionResult> Create(string orgId, OperationCreateRequestDto dto)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }

            var role = await _organizationRepository.GetMemberRoleAsync(orgId, currentUserId);
            if (role == null)
            {
                return NotFound(ApiErrors.General("Organization not found."));
            }

            if (role != MemberRole.Owner && role != MemberRole.Accountant)
            {
                return ForbiddenResult();
            }

            var operation = await _operationRepository.CreateAsync(orgId, currentUserId, dto);
            if (operation == null)
            {
                return BadRequest(ApiErrors.General("Unable to create operation."));
            }

            await _operationRepository.SaveChangesAsync();

            var created = await _operationRepository.GetByIdAsync(operation.Id);
            return Created($"/operations/{operation.Id}", MapOperation(created ?? operation));
        }

        // Тільки Owner
        [HttpPatch("/operations/{operationId}")]
        public async Task<IActionResult> Update(string operationId, OperationUpdateRequestDto dto)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }

            var existingOperation = await _operationRepository.GetByIdAsync(operationId);
            if (existingOperation == null)
            {
                return NotFound(ApiErrors.General("Operation not found."));
            }

            var role = await _organizationRepository.GetMemberRoleAsync(existingOperation.OrganizationId, currentUserId);
            if (role == null)
            {
                return NotFound(ApiErrors.General("Operation not found."));
            }

            if (role != MemberRole.Owner)
            {
                return ForbiddenResult();
            }

            var operation = await _operationRepository.UpdateAsync(operationId, dto);
            if (operation == null)
            {
                return BadRequest(ApiErrors.General("Unable to update operation."));
            }

            await _operationRepository.SaveChangesAsync();

            var updated = await _operationRepository.GetByIdAsync(operationId);
            return Ok(MapOperation(updated ?? operation));
        }

        // Тільки Owner
        [HttpDelete("/operations/{operationId}")]
        public async Task<IActionResult> Delete(string operationId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }

            var existingOperation = await _operationRepository.GetByIdAsync(operationId);
            if (existingOperation == null)
            {
                return NotFound(ApiErrors.General("Operation not found."));
            }

            var role = await _organizationRepository.GetMemberRoleAsync(existingOperation.OrganizationId, currentUserId);
            if (role == null)
            {
                return NotFound(ApiErrors.General("Operation not found."));
            }

            if (role != MemberRole.Owner)
            {
                return ForbiddenResult();
            }

            var removed = await _operationRepository.DeleteAsync(operationId);
            if (!removed)
            {
                return NotFound(ApiErrors.General("Operation not found."));
            }

            await _operationRepository.SaveChangesAsync();
            return NoContent();
        }

        private ObjectResult ForbiddenResult(string message = "You don't have permission to perform this action.")
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiErrors.General(message));
        }

        private static OperationResponseDto MapOperation(OperationModel operation)
        {
            return new OperationResponseDto
            {
                Id = operation.Id,
                Type = operation.Type,
                Amount = operation.Amount,
                CategoryId = operation.CategoryId,
                CategoryName = operation.Category?.Name ?? string.Empty,
                CategoryColor = operation.Category?.Color ?? string.Empty,
                Description = operation.Description,
                CreatedAt = operation.CreatedAt,
                CreatedById = operation.CreatedById,
                CreatedByEmail = operation.CreatedBy?.Email,
                CreatedByFullName = operation.CreatedBy?.FullName,
                OrganizationId = operation.OrganizationId
            };
        }
    }
}