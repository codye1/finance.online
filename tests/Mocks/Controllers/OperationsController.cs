using HandlebarsDotNet.Helpers.Helpers;
using Humanizer;
using tests.Mocks.Infrastructure;
using tests.Mocks.Models;
using WireMock;

namespace tests.Mocks.Controllers;

/// <summary>Handles /organizations/{id}/operations[/summary] and /operations/{id} (delete/patch).</summary>
public sealed class OperationsController
{
    private const int PageSize = 20;

    private readonly FakeApiState _state;

    public OperationsController(FakeApiState state) => _state = state;

    public ResponseMessage? TryHandle(string method, string path, IRequestMessage req, string me)
    {
        if (ApiHelpers.Matches(method, path, "GET", @"^/organizations/([^/]+)/operations/summary$", out var mSummary))
            return ApiHelpers.WithOrg(_state, mSummary.Groups[1].Value, me, o => Summary(req, o));

        if (ApiHelpers.Matches(method, path, "GET", @"^/organizations/([^/]+)/operations$", out var mList))
            return ApiHelpers.WithOrg(_state, mList.Groups[1].Value, me, o => ListOperations(req, o));

        if (ApiHelpers.Matches(method, path, "POST", @"^/organizations/([^/]+)/operations$", out var mCreate))
            return ApiHelpers.WithOrg(_state, mCreate.Groups[1].Value, me, o => CreateOperation(req, o));

        if (ApiHelpers.Matches(method, path, "DELETE", @"^/operations/([^/]+)$", out var mDel))
        {
            var id = mDel.Groups[1].Value;
            var org = _state.OrgsFor(me).FirstOrDefault(o => o.Operations.Any(x => x.Id == id));
            if (org is null) return ApiHelpers.Json(404, ApiHelpers.Error("_general", "Operation not found."));
            org.Operations.RemoveAll(x => x.Id == id);
            return ApiHelpers.Json(204);
        }

        if (ApiHelpers.Matches(method, path, "PATCH", @"^/operations/([^/]+)$", out var mPatch))
        {
            var id = mPatch.Groups[1].Value;
            var op = _state.OrgsFor(me).SelectMany(o => o.Operations).FirstOrDefault(x => x.Id == id);
            if (op is null) return ApiHelpers.Json(404, ApiHelpers.Error("_general", "Operation not found."));
            var body = ApiHelpers.Body(req);
            if (ApiHelpers.TryDecimal(body, "amount", out var amount) && amount > 0) op.Amount = amount;
            if (ApiHelpers.Str(body, "description") is { } d) op.Description = d;
            if (ApiHelpers.Str(body, "type") is { } t) op.Type = t.ToLowerInvariant();
            return ApiHelpers.Json(204);
        }

        return null;
    }

    private ResponseMessage ListOperations(IRequestMessage req, FakeOrg org)
    {
        var page = int.TryParse(ApiHelpers.Query(req, "page"), out var p) && p > 0 ? p : 1;
        var ops = Dto.FilterByPeriod(org.Operations, ApiHelpers.Query(req, "period"))
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(o => Dto.OperationDto(o, org))
            .ToList();
        return ApiHelpers.Json(200, ops);
    }

    private ResponseMessage Summary(IRequestMessage req, FakeOrg org)
    {
        var period = ApiHelpers.Query(req, "period") ?? "all";
        var ops = Dto.FilterByPeriod(org.Operations, period).ToList();
        var income = ops.Where(o => o.Type == "income").Sum(o => o.Amount);
        var expense = ops.Where(o => o.Type == "expense").Sum(o => o.Amount);
        var net = income - expense;
        var margin = income == 0 ? 0m : Math.Round(net / income * 100m, 2);

        return ApiHelpers.Json(200, new { period, income, expense, netProfit = net, margin });
    }

    private ResponseMessage CreateOperation(IRequestMessage req, FakeOrg org)
    {
        // OperationCreateDto is not visible from the controllers, so field names are read leniently.
        var body = ApiHelpers.Body(req);

        if (!ApiHelpers.TryDecimal(body, "amount", out var amount) || amount <= 0)
            return ApiHelpers.Json(400, ApiHelpers.Error("amount", "Amount must be greater than zero."));

        var categoryId = ApiHelpers.Str(body, "categoryId") ?? ApiHelpers.Str(body, "category_id") ?? ApiHelpers.Str(body, "category");
        var category = org.Categories.FirstOrDefault(c => c.Id == categoryId);
        if (category is null)
            return ApiHelpers.Json(400, ApiHelpers.Error("categoryId", "Category is required."));

        var type = (ApiHelpers.Str(body, "type") ?? "expense").ToLowerInvariant();
        var createdAt = DateTime.UtcNow;
        foreach (var key in new[] { "date", "createdAt", "occurredAt" })
        {
            if (DateTime.TryParse(ApiHelpers.Str(body, key), out var parsed))
            {
                createdAt = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
                break;
            }
        }

        var op = new FakeOperation
        {
            CategoryId = category.Id,
            CategoryName = category.Name,
            Amount = amount,
            Type = type,
            Description = ApiHelpers.Str(body, "description") ?? ApiHelpers.Str(body, "note") ?? "",
            CreatedAt = createdAt
        };
        org.Operations.Add(op);
        return ApiHelpers.Json(201, Dto.OperationDto(op, org));
    }
}