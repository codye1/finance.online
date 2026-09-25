using HandlebarsDotNet.Helpers.Helpers;
using Humanizer;
using tests.Mocks.Infrastructure;
using tests.Mocks.Models;
using WireMock;

namespace tests.Mocks.Controllers;

/// <summary>Handles /organizations/{id}/categories (list + create) and /categories/{id} (delete).</summary>
public sealed class CategoriesController
{
    private readonly FakeApiState _state;

    public CategoriesController(FakeApiState state) => _state = state;

    public ResponseMessage? TryHandle(string method, string path, IRequestMessage req, string me)
    {
        if (ApiHelpers.Matches(method, path, "GET", @"^/organizations/([^/]+)/categories$", out var mGet))
            return ApiHelpers.WithOrg(_state, mGet.Groups[1].Value, me, o =>
                ApiHelpers.Json(200, o.Categories.Select(Dto.CategoryDto).ToList()));

        if (ApiHelpers.Matches(method, path, "POST", @"^/organizations/([^/]+)/categories$", out var mPost))
            return ApiHelpers.WithOrg(_state, mPost.Groups[1].Value, me, o => CreateCategory(req, o));

        if (ApiHelpers.Matches(method, path, "DELETE", @"^/categories/([^/]+)$", out var mDel))
        {
            var id = mDel.Groups[1].Value;
            var org = _state.OrgsFor(me).FirstOrDefault(o => o.Categories.Any(c => c.Id == id));
            if (org is null) return ApiHelpers.Json(404, ApiHelpers.Error("_general", "Category not found."));
            org.Categories.RemoveAll(c => c.Id == id);
            return ApiHelpers.Json(204);
        }

        return null;
    }

    private ResponseMessage CreateCategory(IRequestMessage req, FakeOrg org)
    {
        var body = ApiHelpers.Body(req);
        var name = ApiHelpers.Str(body, "name")?.Trim();
        var color = ApiHelpers.Str(body, "color") ?? "#2563EB";

        if (string.IsNullOrEmpty(name))
            return ApiHelpers.Json(400, ApiHelpers.Error("name", "Name is required."));
        if (name.Length > 100)
            return ApiHelpers.Json(400, ApiHelpers.Error("name", "Name must be at most 100 characters."));
        if (org.Categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return ApiHelpers.Json(400, ApiHelpers.Error("name", "Category with this name already exists."));

        var category = new FakeCategory { Name = name, Color = color };
        org.Categories.Add(category);
        return ApiHelpers.Json(201, Dto.CategoryDto(category));
    }
}