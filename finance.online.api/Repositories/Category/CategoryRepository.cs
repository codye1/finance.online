using finance.online.api.Data;
using finance.online.api.Models;
using finance.online.api.Models.DTO;
using Microsoft.EntityFrameworkCore;

namespace finance.online.api.Repositories.CategoryRepository
{
    public class CategoryRepository : ICategoryRepository
    {
        private const string OwnerRole = "owner";
        private const string AccountantRole = "accountant";

        private readonly FinanceOnlineDbContext _context;

        public CategoryRepository(FinanceOnlineDbContext context)
        {
            _context = context;
        }

        public Task<bool> HasMemberAccessAsync(string orgId, string userId)
        {
            return _context.Members.AnyAsync(member => member.OrganizationId == orgId && member.UserId == userId);
        }

        public Task<bool> HasEditorAccessAsync(string orgId, string userId)
        {
            return _context.Members.AnyAsync(member =>
                member.OrganizationId == orgId &&
                member.UserId == userId &&
                (member.Role == OwnerRole || member.Role == AccountantRole));
        }

        public async Task<List<Category>?> GetByOrganizationAsync(string orgId, string currentUserId)
        {
            if (!await HasMemberAccessAsync(orgId, currentUserId))
            {
                return null;
            }

            return await _context.Categories
                .AsNoTracking()
                .Where(category => category.OrganizationId == orgId)
                .OrderBy(category => category.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(string categoryId)
        {
            return await _context.Categories
                .FirstOrDefaultAsync(category => category.Id == categoryId);
        }

        public async Task<Category?> CreateAsync(string orgId, string currentUserId, CategoryCreateRequestDto dto)
        {
            if (!await HasEditorAccessAsync(orgId, currentUserId))
            {
                return null;
            }

            var category = new Category
            {
                Id = Guid.NewGuid().ToString(),
                Name = dto.Name.Trim(),
                Color = dto.Color.Trim(),
                OrganizationId = orgId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            return category;
        }

        public async Task<Category?> UpdateAsync(string categoryId, CategoryUpdateRequestDto dto, string currentUserId)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(item => item.Id == categoryId);
            if (category == null)
            {
                return null;
            }

            if (!await HasEditorAccessAsync(category.OrganizationId, currentUserId))
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                category.Name = dto.Name.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.Color))
            {
                category.Color = dto.Color.Trim();
            }

            return category;
        }

        public async Task<bool> DeleteAsync(string categoryId, string currentUserId)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(item => item.Id == categoryId);
            if (category == null)
            {
                return false;
            }

            if (!await HasEditorAccessAsync(category.OrganizationId, currentUserId))
            {
                return false;
            }

            await _context.Operations
                .Where(operation => operation.CategoryId == categoryId)
                .ExecuteDeleteAsync();

            _context.Categories.Remove(category);
            return true;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

    }
}