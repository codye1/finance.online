using finance.online.api.Models;
using finance.online.api.Models.DTO;

namespace finance.online.api.Repositories.OperationRepository
{
    public interface IOperationRepository
    {
        Task<bool> HasMemberAccessAsync(string orgId, string userId);

        Task<bool> HasEditorAccessAsync(string orgId, string userId);

        Task<List<OperationModel>> GetByOrganizationAsync(
            string orgId,
            string? type,
            string? category,
            DateTime? from,
            DateTime? to,
            string? q,
            int page,
            string currentUserId);

        Task<OperationModel?> GetByIdAsync(string operationId);

        Task<OperationModel?> CreateAsync(string orgId, string currentUserId, OperationCreateRequestDto dto);

        Task<OperationModel?> UpdateAsync(string operationId, OperationUpdateRequestDto dto);

        Task<bool> DeleteAsync(string operationId);

        Task<OperationsSummaryDto?> GetSummaryAsync(string orgId, string period);

        Task SaveChangesAsync();
    }
}