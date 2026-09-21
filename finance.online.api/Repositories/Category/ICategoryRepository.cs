using finance.online.api.Models;
using finance.online.api.Models.DTO;

namespace finance.online.api.Repositories.CategoryRepository
{
    public interface ICategoryRepository
    {
        Task<bool> HasMemberAccessAsync(string orgId, string userId);

        Task<bool> HasEditorAccessAsync(string orgId, string userId);

        Task<List<Category>?> GetByOrganizationAsync(string orgId, string currentUserId);

        Task<Category?> GetByIdAsync(string categoryId);

        Task<Category?> CreateAsync(string orgId, string currentUserId, CategoryCreateRequestDto dto);

        Task<Category?> UpdateAsync(string categoryId, CategoryUpdateRequestDto dto, string currentUserId);

        Task<bool> DeleteAsync(string categoryId, string currentUserId);

        Task SaveChangesAsync();
    }
}