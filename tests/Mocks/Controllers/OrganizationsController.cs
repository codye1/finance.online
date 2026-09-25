using HandlebarsDotNet.Helpers.Helpers;
using Humanizer;
using System.Reflection;
using tests.Mocks.Models;
using tests.Mocks.Infrastructure;
using WireMock;

namespace tests.Mocks.Controllers;

/// <summary>Handles /organizations/active, /organizations/list, and /organizations[/{id}].</summary>
public sealed class OrganizationsController
{
    private readonly FakeApiState _state;

    public OrganizationsController(FakeApiState state) => _state = state;

    public ResponseMessage? TryHandle(string method, string path, IRequestMessage req, string me)
    {
        if (ApiHelpers.Matches(method, path, "GET", @"^/organizations/active$", out _))
            return ActiveOrganization(me);

        if (ApiHelpers.Matches(method, path, "GET", @"^/organizations/list$", out _))
            return ApiHelpers.Json(200, _state.OrgsFor(me).Select(o => Dto.OrgDto(o, me)).ToList());

        if (ApiHelpers.Matches(method, path, "POST", @"^/organizations$", out _))
            return CreateOrganization(req, me);

        if (ApiHelpers.Matches(method, path, "GET", @"^/organizations/([^/]+)$", out var mGet))
            return ApiHelpers.WithOrg(_state, mGet.Groups[1].Value, me, o => ApiHelpers.Json(200, Dto.OrgDto(o, me)));

        if (ApiHelpers.Matches(method, path, "DELETE", @"^/organizations/([^/]+)$", out var mDel))
            return ApiHelpers.WithOrg(_state, mDel.Groups[1].Value, me, o =>
            {
                if (!Dto.IsOwner(o, me)) return ApiHelpers.Json(403, ApiHelpers.Error("_general", "Only the owner can delete the organization."));
                _state.Orgs.Remove(o);
                return ApiHelpers.Json(204);
            });

        return null;
    }

    private ResponseMessage ActiveOrganization(string me)
    {
        var orgs = _state.OrgsFor(me).ToList();
        var active = _state.ActiveOrg.TryGetValue(me, out var id) ? orgs.FirstOrDefault(o => o.Id == id) : null;
        active ??= orgs.FirstOrDefault();
        return active is null
            ? ApiHelpers.Json(404, ApiHelpers.Error("_general", "No organizations."))
            : ApiHelpers.Json(200, Dto.OrgDto(active, me));
    }

    private ResponseMessage CreateOrganization(IRequestMessage req, string me)
    {
        var body = ApiHelpers.Body(req);
        var name = ApiHelpers.Str(body, "name")?.Trim();
        if (string.IsNullOrEmpty(name))
            return ApiHelpers.Json(400, ApiHelpers.Error("name", "Name is required."));

        var org = new Models.FakeOrg { Name = name, Description = ApiHelpers.Str(body, "description") ?? "" };
        org.Members.Add(new Models.FakeMember { Email = me, Role = "owner" });
        _state.Orgs.Add(org);
        _state.ActiveOrg[me] = org.Id;
        return ApiHelpers.Json(201, Dto.OrgDto(org, me));
    }
}