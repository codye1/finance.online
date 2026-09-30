using finance.online.api.Data;
using finance.online.api.Models;
using finance.online.api.Models.DTO;
using Microsoft.EntityFrameworkCore;

namespace finance.online.api.Repositories.OperationRepository
{
    public class OperationRepository : IOperationRepository
    {
        private const string IncomeType = "income";
        private const string ExpenseType = "expense";

        private const int PageSize = 5;

        private readonly FinanceOnlineDbContext _context;

        public OperationRepository(FinanceOnlineDbContext context)
        {
            _context = context;
        }

        public Task<bool> HasMemberAccessAsync(
            string orgId,
            string userId)
        {
            return _context.Members.AnyAsync(member =>
                member.OrganizationId == orgId &&
                member.UserId == userId);
        }

        public Task<bool> HasEditorAccessAsync(
            string orgId,
            string userId)
        {
            return _context.Members.AnyAsync(member =>
                member.OrganizationId == orgId &&
                member.UserId == userId &&
                (member.Role == MemberRole.Owner ||
                 member.Role == MemberRole.Accountant));
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

            var normalizedCategory =
                string.IsNullOrWhiteSpace(category)
                    ? null
                    : category.Trim();

            var normalizedQuery =
                string.IsNullOrWhiteSpace(q)
                    ? null
                    : q.Trim();

            var currentPage = page < 1 ? 1 : page;

            var query = _context.Operations
                .AsNoTracking()
                .Include(operation => operation.Category)
                .Include(operation => operation.CreatedBy)
                .Where(operation =>
                    operation.OrganizationId == orgId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedType))
            {
                query = query.Where(operation =>
                    operation.Type == normalizedType);
            }

            if (!string.IsNullOrWhiteSpace(normalizedCategory))
            {
                query = query.Where(operation =>
                    operation.CategoryId == normalizedCategory ||
                    operation.Category.Name == normalizedCategory);
            }

            if (from.HasValue)
            {
                query = query.Where(operation =>
                    operation.CreatedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(operation =>
                    operation.CreatedAt <= to.Value);
            }

            if (!string.IsNullOrWhiteSpace(normalizedQuery))
            {
                query = query.Where(operation =>
                    (operation.Description != null &&
                     operation.Description.Contains(normalizedQuery)) ||

                    operation.Type.Contains(normalizedQuery) ||

                    operation.Category.Name.Contains(normalizedQuery));
            }

            return await query
                .OrderByDescending(operation => operation.CreatedAt)
                .Skip((currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        public async Task<OperationModel?> GetByIdAsync(
            string operationId)
        {
            return await _context.Operations
                .Include(operation => operation.Category)
                .Include(operation => operation.CreatedBy)
                .FirstOrDefaultAsync(operation =>
                    operation.Id == operationId);
        }

        // Cash flow series for the dashboard chart.
        // Uses the same GetPeriodRange as GetSummaryAsync, so KPI cards and chart always match.
        public async Task<CashflowResponseDto?> GetCashflowAsync(
            string orgId,
            string period)
        {
            var normalizedPeriod =
                period.Trim().ToLowerInvariant();

            var range = GetPeriodRange(normalizedPeriod);

            if (range == null)
            {
                return null;
            }

            // week/month -> per day, year/all -> per month
            var byMonth = normalizedPeriod is "year" or "all";

            var query = _context.Operations
                .AsNoTracking()
                .Where(operation =>
                    operation.OrganizationId == orgId);

            if (range.Value.Start.HasValue)
            {
                query = query.Where(operation =>
                    operation.CreatedAt >= range.Value.Start.Value);
            }

            if (range.Value.End.HasValue)
            {
                query = query.Where(operation =>
                    operation.CreatedAt < range.Value.End.Value);
            }

            // Group in SQL by (bucket, type) - translates reliably in EF Core;
            // income/expense columns are pivoted in memory below.
            List<(DateTime Bucket, string Type, decimal Sum)> rows;

            if (byMonth)
            {
                rows = (await query
                        .GroupBy(operation => new
                        {
                            operation.CreatedAt.Year,
                            operation.CreatedAt.Month,
                            operation.Type
                        })
                        .Select(group => new
                        {
                            group.Key.Year,
                            group.Key.Month,
                            group.Key.Type,
                            Sum = group.Sum(item => (decimal)item.Amount)
                        })
                        .ToListAsync())
                    .Select(row => (
                        new DateTime(row.Year, row.Month, 1),
                        row.Type,
                        row.Sum))
                    .ToList();
            }
            else
            {
                rows = (await query
                        .GroupBy(operation => new
                        {
                            Day = operation.CreatedAt.Date,
                            operation.Type
                        })
                        .Select(group => new
                        {
                            group.Key.Day,
                            group.Key.Type,
                            Sum = group.Sum(item => (decimal)item.Amount)
                        })
                        .ToListAsync())
                    .Select(row => (row.Day, row.Type, row.Sum))
                    .ToList();
            }

            var result = new CashflowResponseDto
            {
                Granularity = byMonth ? "month" : "day"
            };

            if (rows.Count == 0)
            {
                return result;
            }

            // X axis without gaps: from period start up to today (or the last operation).
            var today = DateTime.UtcNow.Date;
            var lastBucket = rows.Max(row => row.Bucket);

            var first = byMonth
                ? (range.Value.Start ?? rows.Min(row => row.Bucket))
                : range.Value.Start!.Value;

            var reference = byMonth
                ? new DateTime(today.Year, today.Month, 1)
                : today;

            var last = lastBucket > reference ? lastBucket : reference;

            first = byMonth
                ? new DateTime(first.Year, first.Month, 1)
                : first.Date;

            var income = rows
                .Where(row => row.Type == IncomeType)
                .ToDictionary(row => row.Bucket, row => row.Sum);

            var expense = rows
                .Where(row => row.Type == ExpenseType)
                .ToDictionary(row => row.Bucket, row => row.Sum);

            for (var current = first;
                 current <= last;
                 current = byMonth ? current.AddMonths(1) : current.AddDays(1))
            {
                // Kind = Unspecified so JSON has no "Z": the browser then shows the same
                // calendar date instead of shifting it by the user's UTC offset.
                var point = DateTime.SpecifyKind(
                    current,
                    DateTimeKind.Unspecified);

                result.Points.Add(new CashflowPointDto
                {
                    Date = point,
                    Income = income.GetValueOrDefault(point),
                    Expense = expense.GetValueOrDefault(point)
                });
            }

            return result;
        }

        public async Task<OperationModel?> CreateAsync(
            string orgId,
            string currentUserId,
            OperationCreateRequestDto dto)
        {
            var normalizedType = NormalizeType(dto.Type);

            if (normalizedType == null)
            {
                return null;
            }

            if (dto.Amount < 1)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(dto.CategoryId))
            {
                return null;
            }

            var categoryExists = await _context.Categories.AnyAsync(
                category =>
                    category.Id == dto.CategoryId &&
                    category.OrganizationId == orgId);

            if (!categoryExists)
            {
                return null;
            }

            var operationDate = dto.Date?.ToUniversalTime()
                                ?? DateTime.UtcNow;

            var operation = new OperationModel
            {
                Id = Guid.NewGuid().ToString(),

                Type = normalizedType,

                Amount = dto.Amount,

                CategoryId = dto.CategoryId,

                Description = dto.Description,

                CreatedAt = operationDate,

                CreatedById = currentUserId,

                OrganizationId = orgId
            };

            _context.Operations.Add(operation);

            return operation;
        }

        public async Task<OperationModel?> UpdateAsync(
            string operationId,
            OperationUpdateRequestDto dto)
        {
            var operation = await _context.Operations
                .FirstOrDefaultAsync(item =>
                    item.Id == operationId);

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
                var categoryExists = await _context.Categories.AnyAsync(
                    category =>
                        category.Id == dto.CategoryId &&
                        category.OrganizationId ==
                        operation.OrganizationId);

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

            if (dto.Date.HasValue)
            {
                operation.CreatedAt =
                    dto.Date.Value.ToUniversalTime();
            }

            return operation;
        }

        public async Task<bool> DeleteAsync(
            string operationId)
        {
            var operation = await _context.Operations
                .FirstOrDefaultAsync(item =>
                    item.Id == operationId);

            if (operation == null)
            {
                return false;
            }

            _context.Operations.Remove(operation);

            return true;
        }

        public async Task<OperationsSummaryDto?> GetSummaryAsync(
            string orgId,
            string period)
        {
            var normalizedPeriod =
                period.Trim().ToLowerInvariant();

            var range = GetPeriodRange(normalizedPeriod);

            if (range == null)
            {
                return null;
            }

            var operations = _context.Operations
                .AsNoTracking()
                .Where(operation =>
                    operation.OrganizationId == orgId);

            if (range.Value.Start.HasValue)
            {
                operations = operations.Where(operation =>
                    operation.CreatedAt >=
                    range.Value.Start.Value);
            }

            if (range.Value.End.HasValue)
            {
                operations = operations.Where(operation =>
                    operation.CreatedAt <
                    range.Value.End.Value);
            }

            var income = await operations
                .Where(operation =>
                    operation.Type == IncomeType)
                .SumAsync(operation =>
                    (decimal?)operation.Amount) ?? 0m;

            var expense = await operations
                .Where(operation =>
                    operation.Type == ExpenseType)
                .SumAsync(operation =>
                    (decimal?)operation.Amount) ?? 0m;

            var netProfit = income - expense;

            var margin = income == 0m
                ? 0m
                : decimal.Round(
                    netProfit / income * 100m,
                    2);

            return new OperationsSummaryDto
            {
                Period = normalizedPeriod,
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

        private static (
            DateTime? Start,
            DateTime? End
        )? GetPeriodRange(string period)
        {
            var now = DateTime.UtcNow;

            return period switch
            {
                "week" =>
                    (
                        now.Date.AddDays(-6),
                        now.Date.AddDays(1)
                    ),

                "month" =>
                    (
                        new DateTime(
                            now.Year,
                            now.Month,
                            1,
                            0,
                            0,
                            0,
                            DateTimeKind.Utc),

                        new DateTime(
                            now.Year,
                            now.Month,
                            1,
                            0,
                            0,
                            0,
                            DateTimeKind.Utc)
                            .AddMonths(1)
                    ),

                "year" =>
                    (
                        new DateTime(
                            now.Year,
                            1,
                            1,
                            0,
                            0,
                            0,
                            DateTimeKind.Utc),

                        new DateTime(
                            now.Year + 1,
                            1,
                            1,
                            0,
                            0,
                            0,
                            DateTimeKind.Utc)
                    ),

                "all" =>
                    (
                        null,
                        null
                    ),

                _ => null
            };
        }

        private static string? NormalizeType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return null;
            }

            var normalizedType =
                type.Trim().ToLowerInvariant();

            return normalizedType is IncomeType or ExpenseType
                ? normalizedType
                : null;
        }
    }
}