using finance.online.api.Models;

namespace finance.online.api.Repositories.OrganizationRepository
{
    public interface IOrganizationRepository
    {
        Task<List<Organization>> GetMineAsync(string userId);

        Task<Organization?> GetByIdForMemberAsync(string orgId, string userId);

        Task<Organization?> GetByIdForOwnerAsync(string orgId, string userId);

        Task<List<Member>?> GetMembersAsync(string orgId, string currentUserId);

        Task<Member?> AddMemberAsync(string orgId, string targetUserId, MemberRole role, string currentUserId);

        Task<Member?> UpdateMemberRoleAsync(string orgId, string userId, MemberRole role, string currentUserId);

        Task<Member?> RemoveMemberAsync(string orgId, string userId, string currentUserId);

        Task<Member?> LeaveOrganizationAsync(string orgId, string currentUserId);

        Task<List<OrganizationListItemDto>> GetMineListAsync(string userId);

        Organization Add(string name, string description, string ownerUserId, IEnumerable<string> participantUserIds);

        void Update(Organization organization);

        Task DeleteAsync(string orgId);

        Task SaveChangesAsync();

        Task<MemberRole?> GetMemberRoleAsync(string orgId, string userId);
    }
}