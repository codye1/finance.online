using finance.online.api.Models;
using finance.online.api.Models.DTO;
using finance.online.api.Repositories.OrganizationRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace finance.online.api.Controllers
{
    [ApiController]
    [Route("organizations")]
    [Authorize]
    public class OrganizationsController : ControllerBase
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly UserManager<AppUser> _userManager;

        public OrganizationsController(
            IOrganizationRepository organizationRepository,
            UserManager<AppUser> userManager)
        {
            _organizationRepository = organizationRepository;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }

            var organizations = (await _organizationRepository.GetMineAsync(currentUserId))
                .Select(organization => MapOrganization(organization, currentUserId))
                .ToList();

            return Ok(organizations);
        }

        [HttpPost]
        public async Task<IActionResult> Create(OrganizationCreateRequestDto dto)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }

            var organization = _organizationRepository.Add(dto.Name, dto.Description, currentUserId, dto.ParticipantUserIds);
            await _organizationRepository.SaveChangesAsync();

            var response = MapOrganization(organization, currentUserId);
            return CreatedAtAction(nameof(GetById), new { orgId = organization.Id }, response);
        }

        [HttpGet("{orgId}")]
        public async Task<IActionResult> GetById(string orgId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }
            var organization = await GetOrganizationResponseAsync(orgId, currentUserId);
            if (organization == null)
            {
                return NotFound(ApiErrors.General("Organization not found."));
            }

            return Ok(organization);
        }

        [HttpPatch("{orgId}")]
        public async Task<IActionResult> Update(string orgId, OrganizationUpdateRequestDto dto)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }

            var organization = await _organizationRepository.GetByIdForOwnerAsync(orgId, currentUserId);

            if (organization == null)
            {
                return NotFound(ApiErrors.General("Organization not found."));
            }

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                organization.Name = dto.Name;
            }

            if (!string.IsNullOrWhiteSpace(dto.Description))
            {
                organization.Description = dto.Description;
            }

            _organizationRepository.Update(organization);
            await _organizationRepository.SaveChangesAsync();

            var response = await GetOrganizationResponseAsync(orgId, currentUserId);
            return Ok(response);
        }

        [HttpDelete("{orgId}")]
        public async Task<IActionResult> Delete(string orgId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }

            var organization = await _organizationRepository.GetByIdForOwnerAsync(orgId, currentUserId);

            if (organization == null)
            {
                return NotFound(ApiErrors.General("Organization not found."));
            }

            await _organizationRepository.DeleteAsync(orgId);
            await _organizationRepository.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{orgId}/members")]
        public async Task<IActionResult> GetMembers(string orgId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }

            var members = await _organizationRepository.GetMembersAsync(orgId, currentUserId);
            if (members == null)
            {
                return NotFound(ApiErrors.General("Organization not found."));
            }

            return Ok(members.Select(member => MapMember(member, currentUserId)).ToList());
        }

        [HttpPost("{orgId}/members")]
        public async Task<IActionResult> AddMember(string orgId, OrganizationMemberCreateRequestDto dto)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }

            var member = await _organizationRepository.AddMemberAsync(orgId, dto.UserId, dto.Role, currentUserId);
            if (member == null)
            {
                return NotFound(ApiErrors.General("Member could not be added."));
            }

            await _organizationRepository.SaveChangesAsync();
            return Ok(MapMember(member, currentUserId));
        }

        [HttpPatch("{orgId}/members/{userId}")]
        public async Task<IActionResult> UpdateMemberRole(string orgId, string userId, OrganizationMemberUpdateRequestDto dto)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }

            var member = await _organizationRepository.UpdateMemberRoleAsync(orgId, userId, dto.Role, currentUserId);
            if (member == null)
            {
                return NotFound(ApiErrors.General("Member not found."));
            }

            await _organizationRepository.SaveChangesAsync();
            return Ok(MapMember(member, currentUserId));
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }

            var organizations = await _organizationRepository.GetMineAsync(currentUserId);

            var activeOrganization = organizations
                .OrderByDescending(organization => organization.CreatedById == currentUserId)
                .ThenBy(organization => organization.CreatedAt)
                .FirstOrDefault();

            if (activeOrganization == null)
            {
                return NotFound(ApiErrors.General("User has no organizations."));
            }

            return Ok(MapOrganization(activeOrganization, currentUserId));
        }

        [HttpGet("list")]
public async Task<IActionResult> GetList()
{
    var currentUserId = _userManager.GetUserId(User);

    if (string.IsNullOrWhiteSpace(currentUserId))
    {
        return Unauthorized(ApiErrors.General("Authentication is required."));
    }

    var organizations = await _organizationRepository.GetMineListAsync(currentUserId);

    return Ok(organizations);
}

        [HttpDelete("{orgId}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(string orgId, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }

            var member = await _organizationRepository.RemoveMemberAsync(orgId, userId, currentUserId);
            if (member == null)
            {
                return NotFound(ApiErrors.General("Member not found."));
            }

            await _organizationRepository.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{orgId}/members/me")]
        public async Task<IActionResult> LeaveOrganization(string orgId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiErrors.General("Authentication is required."));
            }

            var member = await _organizationRepository.LeaveOrganizationAsync(orgId, currentUserId);
            if (member == null)
            {
                return NotFound(ApiErrors.General("Member not found."));
            }

            await _organizationRepository.SaveChangesAsync();
            return NoContent();
        }

        private async Task<OrganizationResponseDto?> GetOrganizationResponseAsync(string orgId, string currentUserId)
        {
            var organization = await _organizationRepository.GetByIdForMemberAsync(orgId, currentUserId);
            return organization == null ? null : MapOrganization(organization, currentUserId);
        }

        private static OrganizationResponseDto MapOrganization(Organization organization, string currentUserId)
        {
            return new OrganizationResponseDto
            {
                Id = organization.Id,
                Name = organization.Name,
                Description = organization.Description,
                CreatedById = organization.CreatedById,
                CreatedAt = organization.CreatedAt,
                MemberCount = organization.Members.Count,
                IsOwner = organization.CreatedById == currentUserId
            };
        }

        private static OrganizationMemberResponseDto MapMember(Member member, string currentUserId)
        {
            return new OrganizationMemberResponseDto
            {
                Id = member.Id,
                UserId = member.UserId,
                Email = member.User?.Email,
                Role = member.Role,
                CreatedAt = member.CreatedAt,
                IsMe = member.UserId == currentUserId,
                IsOwner = string.Equals(member.Role, "owner", StringComparison.OrdinalIgnoreCase)
            };
        }
    }
}