using finance.online.api.Data;
using finance.online.api.Models;
using Microsoft.EntityFrameworkCore;

namespace finance.online.api.Repositories.OrganizationRepository
{
    public class OrganizationRepository : IOrganizationRepository
    {
        private const string OwnerRole = "owner";
        private const string MemberRole = "member";

        private readonly FinanceOnlineDbContext _context;

        public OrganizationRepository(FinanceOnlineDbContext context)
        {
            _context = context;
        }

        public async Task<List<Organization>> GetMineAsync(string userId)
        {
            return await _context.Organizations
                .AsNoTracking()
                .Include(organization => organization.Members)
                .Where(organization => organization.Members.Any(member => member.UserId == userId))
                .ToListAsync();
        }

        public async Task<Organization?> GetByIdForMemberAsync(string orgId, string userId)
        {
            return await _context.Organizations
                .AsNoTracking()
                .Include(organization => organization.Members)
                .FirstOrDefaultAsync(organization => organization.Id == orgId && organization.Members.Any(member => member.UserId == userId));
        }

        // Тепер "власник" визначається по ролі в Members ("owner"), а не по CreatedById.
        // Використовується скрізь, де раніше стояла перевірка CreatedById == userId:
        // Update/Delete організації, AddMember, UpdateMemberRole, RemoveMember.
        public async Task<Organization?> GetByIdForOwnerAsync(string orgId, string userId)
        {
            return await _context.Organizations
                .Include(organization => organization.Members)
                .FirstOrDefaultAsync(organization => organization.Id == orgId
                    && organization.Members.Any(member => member.UserId == userId
                        && member.Role == OwnerRole));
        }

        public async Task<List<OrganizationListItemDto>> GetMineListAsync(string userId)
        {
            return await _context.Organizations
                .AsNoTracking()
                .Where(organization =>
                    organization.Members.Any(member => member.UserId == userId))
                .Select(organization => new OrganizationListItemDto
                {
                    Id = organization.Id,
                    Name = organization.Name
                })
                .ToListAsync();
        }

        public async Task<List<Member>?> GetMembersAsync(string orgId, string currentUserId)
        {
            var hasAccess = await _context.Members.AnyAsync(member => member.OrganizationId == orgId && member.UserId == currentUserId);
            if (!hasAccess)
            {
                return null;
            }

            return await _context.Members
                .AsNoTracking()
                .Include(member => member.User)
                .Where(member => member.OrganizationId == orgId)
                .OrderBy(member => member.CreatedAt)
                .ToListAsync();
        }

        public async Task<Member?> AddMemberAsync(string orgId, string targetUserId, string role, string currentUserId)
        {
            var organization = await GetByIdForOwnerAsync(orgId, currentUserId);
            if (organization == null)
            {
                return null;
            }

            var normalizedRole = NormalizeRole(role);
            if (string.IsNullOrWhiteSpace(targetUserId))
            {
                return null;
            }

            var userExists = await _context.Users.AnyAsync(user => user.Id == targetUserId);
            if (!userExists)
            {
                return null;
            }

            var existingMember = await _context.Members.FirstOrDefaultAsync(member => member.OrganizationId == orgId && member.UserId == targetUserId);
            if (existingMember != null)
            {
                return null;
            }

            var member = new Member
            {
                UserId = targetUserId,
                OrganizationId = orgId,
                Role = normalizedRole,
                CreatedAt = DateTime.UtcNow
            };

            _context.Members.Add(member);
            return member;
        }

        public async Task<Member?> UpdateMemberRoleAsync(string orgId, string userId, string role, string currentUserId)
        {
            var organization = await GetByIdForOwnerAsync(orgId, currentUserId);
            if (organization == null)
            {
                return null;
            }

            var member = await _context.Members.FirstOrDefaultAsync(item => item.UserId == userId && item.OrganizationId == orgId);
            if (member == null)
            {
                return null;
            }

            var normalizedRole = NormalizeRole(role);
            if (member.Role == OwnerRole && normalizedRole != OwnerRole)
            {
                var ownerCount = await _context.Members.CountAsync(item => item.OrganizationId == orgId && item.Role == OwnerRole);
                if (ownerCount <= 1)
                {
                    return null;
                }
            }

            member.Role = normalizedRole;
            return member;
        }

        public async Task<Member?> RemoveMemberAsync(string orgId, string userId, string currentUserId)
        {
            var organization = await GetByIdForOwnerAsync(orgId, currentUserId);
            if (organization == null)
            {
                return null;
            }

            var member = await _context.Members.FirstOrDefaultAsync(item => item.UserId == userId && item.OrganizationId == orgId);
            if (member == null)
            {
                return null;
            }

            if (member.Role == OwnerRole)
            {
                var ownerCount = await _context.Members.CountAsync(item => item.OrganizationId == orgId && item.Role == OwnerRole);
                if (ownerCount <= 1)
                {
                    return null;
                }
            }

            _context.Members.Remove(member);
            return member;
        }

        public async Task<Member?> LeaveOrganizationAsync(string orgId, string currentUserId)
        {
            var member = await _context.Members.FirstOrDefaultAsync(item => item.OrganizationId == orgId && item.UserId == currentUserId);
            if (member == null)
            {
                return null;
            }

            if (member.Role == OwnerRole)
            {
                var ownerCount = await _context.Members.CountAsync(item => item.OrganizationId == orgId && item.Role == OwnerRole);
                if (ownerCount <= 1)
                {
                    return null;
                }
            }

            _context.Members.Remove(member);
            return member;
        }

        public Organization Add(string name, string description, string ownerUserId, IEnumerable<string> participantUserIds)
        {
            var organization = new Organization
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Description = description,
                CreatedById = ownerUserId,
                CreatedAt = DateTime.UtcNow
            };

            var ownerMembership = new Member
            {
                UserId = ownerUserId,
                OrganizationId = organization.Id,
                Role = OwnerRole,
                CreatedAt = organization.CreatedAt
            };

            organization.Members.Add(ownerMembership);
            _context.Organizations.Add(organization);
            _context.Members.Add(ownerMembership);

            foreach (var participantUserId in participantUserIds
                         .Where(userId => !string.IsNullOrWhiteSpace(userId))
                         .Select(userId => userId.Trim())
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (participantUserId == ownerUserId)
                {
                    continue;
                }

                var participantMembership = new Member
                {
                    UserId = participantUserId,
                    OrganizationId = organization.Id,
                    Role = MemberRole,
                    CreatedAt = organization.CreatedAt
                };

                organization.Members.Add(participantMembership);
                _context.Members.Add(participantMembership);
            }

            return organization;
        }

        public void Update(Organization organization)
        {
            _context.Organizations.Update(organization);
        }

        private static string NormalizeRole(string role)
        {
            return string.IsNullOrWhiteSpace(role)
                ? MemberRole
                : role.Trim().ToLowerInvariant();
        }

        public async Task DeleteAsync(string orgId)
        {
            await _context.Operations
                .Where(operation => operation.OrganizationId == orgId)
                .ExecuteDeleteAsync();

            await _context.Categories
                .Where(category => category.OrganizationId == orgId)
                .ExecuteDeleteAsync();

            await _context.Members
                .Where(member => member.OrganizationId == orgId)
                .ExecuteDeleteAsync();

            var organization = await _context.Organizations.FirstOrDefaultAsync(organization => organization.Id == orgId);
            if (organization != null)
            {
                _context.Organizations.Remove(organization);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}