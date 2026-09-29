using HandlebarsDotNet.Helpers.Helpers;
using Humanizer;
using System.Text.RegularExpressions;
using tests.Mocks.Infrastructure;
using tests.Mocks.Models;
using WireMock;

namespace tests.Mocks.Controllers;

/// <summary>Handles /organizations/{id}/members (list + invite).</summary>
public sealed class MembersController
{
    private static readonly Regex EmailRx = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private readonly FakeApiState _state;

    public MembersController(FakeApiState state) => _state = state;

    public ResponseMessage? TryHandle(string method, string path, IRequestMessage req, string me)
    {
        if (ApiHelpers.Matches(method, path, "GET", @"^/organizations/([^/]+)/members$", out var mGet))
            return ApiHelpers.WithOrg(_state, mGet.Groups[1].Value, me, o =>
                ApiHelpers.Json(200, o.Members.Select(x => Dto.MemberDto(x, me)).ToList()));

        if (ApiHelpers.Matches(method, path, "POST", @"^/organizations/([^/]+)/members$", out var mPost))
            return ApiHelpers.WithOrg(_state, mPost.Groups[1].Value, me, o => InviteMember(req, o, me));

        if (ApiHelpers.Matches(method, path, "DELETE", @"^/organizations/([^/]+)/members/([^/]+)$", out var mDel))
            return ApiHelpers.WithOrg(_state, mDel.Groups[1].Value, me, o =>
                RemoveMember(o, Uri.UnescapeDataString(mDel.Groups[2].Value), me));
        return null;
    }

    private ResponseMessage InviteMember(IRequestMessage req, FakeOrg org, string me)
    {
        if (!Dto.IsOwner(org, me))
            return ApiHelpers.Json(403, ApiHelpers.Error("_general", "Only the owner can invite members."));

        var body = ApiHelpers.Body(req);
        var email = ApiHelpers.Str(body, "email")?.Trim().ToLowerInvariant();
        var role = (ApiHelpers.Str(body, "role") ?? "accountant").ToLowerInvariant();

        if (string.IsNullOrEmpty(email) || !EmailRx.IsMatch(email))
            return ApiHelpers.Json(400, ApiHelpers.Error("email", "Invalid email."));
        if (!_state.Users.ContainsKey(email))
            return ApiHelpers.Json(400, ApiHelpers.Error("email", "User with this email is not registered."));
        if (org.Members.Any(x => x.Email == email))
            return ApiHelpers.Json(400, ApiHelpers.Error("email", "User is already a member of this organization."));

        var member = new FakeMember { Email = email, Role = role };
        org.Members.Add(member);
        return ApiHelpers.Json(200, Dto.MemberDto(member, me));
    }

    private ResponseMessage RemoveMember(FakeOrg org, string userId, string me)
    {
        if (!Dto.IsOwner(org, me))
            return ApiHelpers.Json(403, ApiHelpers.Error("_general", "Only the owner can remove members."));

        // У фейку userId учасника = його email
        var target = org.Members.FirstOrDefault(x => x.Email.Equals(userId, StringComparison.OrdinalIgnoreCase));
        if (target is null)
            return ApiHelpers.Json(404, ApiHelpers.Error("_general", "Member not found."));

        if (target.Email == me)
            return ApiHelpers.Json(400, ApiHelpers.Error("_general", "Owner cannot remove themselves."));

        org.Members.Remove(target);
        return ApiHelpers.Json(204);
    }
}