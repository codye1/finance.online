using finance.online.api.Models;
using finance.online.api.Models.DTO;
using finance.online.api.Repositories.CategoryRepository;
using finance.online.api.Repositories.OrganizationRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace finance.online.api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("organizations")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly UserManager<AppUser> _userManager;

        public CategoriesController(
            ICategoryRepository categoryRepository,
            IOrganizationRepository organizationRepository,
            UserManager<AppUser> userManager)
        {
            _categoryRepository = categoryRepository;
            _organizationRepository = organizationRepository;
            _userManager = userManager;
        }

        // Owner, Accountant, Member: перегляд
        [HttpGet("{orgId}/categories")]
        public async Task<IActionResult> GetByOrganization(string orgId)
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

            var categories = await _categoryRepository.GetByOrganizationAsync(orgId, currentUserId);
            if (categories == null)
            {
                return NotFound(ApiErrors.General("Organization not found."));
            }

            return Ok(categories.Select(MapCategory).ToList());
        }

        // Тільки Owner
        [HttpPost("{orgId}/categories")]
        public async Task<IActionResult> Create(string orgId, CategoryCreateRequestDto dto)
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

            if (role != MemberRole.Owner)
            {
                return ForbiddenResult();
            }

            var category = await _categoryRepository.CreateAsync(orgId, currentUserId, dto);
            if (category == null)
            {
                return ForbiddenResult("Unable to create category.");
            }

            await _categoryRepository.SaveChangesAsync();
            return Created($"/categories/{category.Id}", MapCategory(category));
        }

        // Тільки Owner
        [HttpPatch("/categories/{categoryId}")]
        public async Task<IActionResult> Update(string categoryId, CategoryUpdateRequestDto dto)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }

            var existingCategory = await _categoryRepository.GetByIdAsync(categoryId);
            if (existingCategory == null)
            {
                return NotFound(ApiErrors.General("Category not found."));
            }

            var role = await _organizationRepository.GetMemberRoleAsync(existingCategory.OrganizationId, currentUserId);
            if (role == null)
            {
                return NotFound(ApiErrors.General("Category not found."));
            }

            if (role != MemberRole.Owner)
            {
                return ForbiddenResult();
            }

            var category = await _categoryRepository.UpdateAsync(categoryId, dto, currentUserId);
            if (category == null)
            {
                return NotFound(ApiErrors.General("Category not found."));
            }

            await _categoryRepository.SaveChangesAsync();
            return Ok(MapCategory(category));
        }

        // Тільки Owner
        [HttpDelete("/categories/{categoryId}")]
        public async Task<IActionResult> Delete(string categoryId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }

            var existingCategory = await _categoryRepository.GetByIdAsync(categoryId);
            if (existingCategory == null)
            {
                return NotFound(ApiErrors.General("Category not found."));
            }

            var role = await _organizationRepository.GetMemberRoleAsync(existingCategory.OrganizationId, currentUserId);
            if (role == null)
            {
                return NotFound(ApiErrors.General("Category not found."));
            }

            if (role != MemberRole.Owner)
            {
                return ForbiddenResult();
            }

            var removed = await _categoryRepository.DeleteAsync(categoryId, currentUserId);
            if (!removed)
            {
                return NotFound(ApiErrors.General("Category not found."));
            }

            await _categoryRepository.SaveChangesAsync();
            return NoContent();
        }

        private ObjectResult ForbiddenResult(string message = "You don't have permission to perform this action.")
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiErrors.General(message));
        }

        private static CategoryResponseDto MapCategory(Category category)
        {
            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Color = category.Color,
                OrganizationId = category.OrganizationId,
                CreatedAt = category.CreatedAt
            };
        }
    }
}