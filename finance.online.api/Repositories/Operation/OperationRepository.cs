using finance.online.api.Data;
using finance.online.api.Models;
using finance.online.api.Models.DTO;
using Microsoft.EntityFrameworkCore;

namespace finance.online.api.Repositories.OperationRepository
{
    public class OperationRepository : IOperationRepository
    {
        private const string OwnerRole = "owner";
        private const string AccountantRole = "accountant";
        private const string IncomeType = "income";
        private const string ExpenseType = "expense";
        private const int PageSize = 20;

        private readonly FinanceOnlineDbContext _context;

        public OperationRepository(FinanceOnlineDbContext context)
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

        public async Task<List<OperationModel>> GetByOrganizationAsync(
            string orgId,
            string? type,
            string? category,
            DateTime? from,
            DateTime? to,
            string? q,
            int page,
            string currentUserId)
        {
            if (!await HasMemberAccessAsync(orgId, currentUserId))
            {
                return new List<OperationModel>();
            }

            var normalizedType = NormalizeType(type);
            var normalizedCategory = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
            var normalizedQuery = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
            var currentPage = page < 1 ? 1 : page;

            var query = _context.Operations
                .AsNoTracking()
                .Include(operation => operation.Category)
                .Include(operation => operation.CreatedBy)
                .Where(operation => operation.OrganizationId == orgId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedType))
            {
                query = query.Where(operation => operation.Type == normalizedType);
            }

            if (!string.IsNullOrWhiteSpace(normalizedCategory))
            {
                query = query.Where(operation =>
                    operation.CategoryId == normalizedCategory ||
                    operation.Category.Name == normalizedCategory);
            }

            if (from.HasValue)
            {
                query = query.Where(operation => operation.CreatedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(operation => operation.CreatedAt <= to.Value);
            }

            if (!string.IsNullOrWhiteSpace(normalizedQuery))
            {
                query = query.Where(operation =>
                    (operation.Description != null && operation.Description.Contains(normalizedQuery)) ||
                    operation.Type.Contains(normalizedQuery) ||
                    operation.Category.Name.Contains(normalizedQuery));
            }

            return await query
                .OrderByDescending(operation => operation.CreatedAt)
                .Skip((currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        public async Task<OperationModel?> GetByIdAsync(string operationId)
        {
            return await _context.Operations
                .Include(operation => operation.Category)
                .Include(operation => operation.CreatedBy)
                .FirstOrDefaultAsync(operation => operation.Id == operationId);
        }

        public async Task<OperationModel?> CreateAsync(string orgId, string currentUserId, OperationCreateRequestDto dto)
        {
            var normalizedType = NormalizeType(dto.Type);
            if (normalizedType == null || dto.Amount < 1)
            {
                return null;
            }

            var categoryExists = await _context.Categories.AnyAsync(category =>
                category.Id == dto.CategoryId && category.OrganizationId == orgId);
            if (!categoryExists)
            {
                return null;
            }

            var operation = new OperationModel
            {
                Id = Guid.NewGuid().ToString(),
                Type = normalizedType,
                Amount = dto.Amount,
                CategoryId = dto.CategoryId,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                CreatedById = currentUserId,
                OrganizationId = orgId
            };

            _context.Operations.Add(operation);
            return operation;
        }

        public async Task<OperationModel?> UpdateAsync(string operationId, OperationUpdateRequestDto dto)
        {
            var operation = await _context.Operations.FirstOrDefaultAsync(item => item.Id == operationId);
            if (operation == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(dto.Type))
            {
                var normalizedType = NormalizeType(dto.Type);
                if (normalizedType == null)
                {
                    return null;
                }

                operation.Type = normalizedType;
            }

            if (dto.Amount.HasValue)
            {
                if (dto.Amount.Value < 1)
                {
                    return null;
                }

                operation.Amount = dto.Amount.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.CategoryId))
            {
                var categoryExists = await _context.Categories.AnyAsync(category =>
                    category.Id == dto.CategoryId && category.OrganizationId == operation.OrganizationId);
                if (!categoryExists)
                {
                    return null;
                }

                operation.CategoryId = dto.CategoryId;
            }

            if (dto.Description != null)
            {
                operation.Description = dto.Description;
            }

            return operation;
        }

        public async Task<bool> DeleteAsync(string operationId)
        {
            var operation = await _context.Operations.FirstOrDefaultAsync(item => item.Id == operationId);
            if (operation == null)
            {
                return false;
            }

            _context.Operations.Remove(operation);
            return true;
        }

        public async Task<OperationsSummaryDto?> GetSummaryAsync(string orgId, string period)
        {
            if (!string.Equals(period, "month", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);

            var operations = _context.Operations
                .AsNoTracking()
                .Where(operation => operation.OrganizationId == orgId && operation.CreatedAt >= start && operation.CreatedAt < end);

            var income = await operations
                .Where(operation => operation.Type == IncomeType)
                .SumAsync(operation => (decimal?)operation.Amount) ?? 0m;

            var expense = await operations
                .Where(operation => operation.Type == ExpenseType)
                .SumAsync(operation => (decimal?)operation.Amount) ?? 0m;

            var netProfit = income - expense;
            var margin = income == 0m ? 0m : decimal.Round((netProfit / income) * 100m, 2);

            return new OperationsSummaryDto
            {
                Period = period.ToLowerInvariant(),
                Income = income,
                Expense = expense,
                NetProfit = netProfit,
                Margin = margin
            };
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        private static string? NormalizeType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return null;
            }

            var normalizedType = type.Trim().ToLowerInvariant();
            return normalizedType is IncomeType or ExpenseType ? normalizedType : null;
        }
    }
}