using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;
using WireMock.Types;
using WireMock.Util;

namespace tests.Mocks;

/// <summary>
/// Fake of the Finance API that the MVC app talks to (server-side, via IHttpClientFactory).
/// Listens on https://localhost:7242 - the same address AuthController hardcodes - so the
/// MVC app needs no changes: just stop the real API and run the tests.
///
/// One catch-all WireMock mapping + a C# router, so state is kept in memory
/// (POST /categories then GET /categories returns the new item).
/// </summary>
public sealed class FakeFinanceApi : IDisposable
{
    public const string Url = "https://localhost:7242";
    private const int PageSize = 20;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
    private static readonly Regex EmailRx = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private readonly WireMockServer _server;
    private readonly object _lock = new();

    public FakeApiState State { get; } = new();

    public FakeFinanceApi()
    {
        _server = WireMockServer.Start(new WireMockServerSettings
        {
            Urls = new[] { Url },
            UseSSL = true,
            // Uses the ASP.NET Core dev certificate (dotnet dev-certs https --trust).
            // If the store lookup fails, export it to a PFX and use
            // X509CertificateFilePath / X509CertificatePassword instead.
            CertificateSettings = new WireMockCertificateSettings
            {
                X509StoreName = "My",
                X509StoreLocation = "CurrentUser",
                X509StoreThumbprintOrSubjectName = "localhost"
            }
        });

        _server
            .Given(Request.Create().WithPath("/*").UsingAnyMethod())
            .RespondWith(Response.Create().WithCallback(req => Handle(req)));
    }

    /// <summary>Seed ids are deterministic, so a Reset never invalidates ids already rendered in an open page.</summary>
    public void Reset()
    {
        lock (_lock) State.Reset();
    }

    /// <summary>Force a status code for matching requests until the next Reset (e.g. to test error UI).</summary>
    public void ForceStatus(string method, string pathRegex, int status)
    {
        lock (_lock) State.Overrides.Add((method, new Regex(pathRegex, RegexOptions.IgnoreCase), status));
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }

    // =====================================================================
    // Routing
    // =====================================================================

    private ResponseMessage Handle(IRequestMessage req)
    {
        lock (_lock)
        {
            try
            {
                return Route(req) ?? Json(404, Error("_general", $"No fake route for {req.Method} {req.Path}"));
            }
            catch (Exception ex)
            {
                return Json(500, Error("_general", ex.Message));
            }
        }
    }

    private ResponseMessage? Route(IRequestMessage req)
    {
        var method = req.Method;
        var path = req.Path.Length > 1 ? req.Path.TrimEnd('/') : req.Path;

        foreach (var o in State.Overrides)
        {
            if (o.Method.Equals(method, StringComparison.OrdinalIgnoreCase) && o.Path.IsMatch(path))
                return Json(o.Status, Error("_general", "Status forced by test."));
        }

        Match m = Match.Empty;
        bool R(string verb, string pattern)
        {
            if (!verb.Equals(method, StringComparison.OrdinalIgnoreCase)) return false;
            m = Regex.Match(path, pattern, RegexOptions.IgnoreCase);
            return m.Success;
        }

        // ---- Auth (no token required) ----
        if (R("POST", @"^/auth/login$")) return Login(req);
        if (R("POST", @"^/auth/register$")) return Register(req);

        var me = CurrentEmail(req);
        if (me is null) return Json(401, Error("_general", "Unauthorized."));

        // ---- Organizations ----
        if (R("GET", @"^/organizations/active$")) return ActiveOrganization(me);
        if (R("GET", @"^/organizations/list$"))
            return Json(200, State.OrgsFor(me).Select(o => OrgDto(o, me)).ToList());
        if (R("POST", @"^/organizations$")) return CreateOrganization(req, me);

        if (R("GET", @"^/organizations/([^/]+)$"))
            return WithOrg(m.Groups[1].Value, me, o => Json(200, OrgDto(o, me)));

        if (R("DELETE", @"^/organizations/([^/]+)$"))
            return WithOrg(m.Groups[1].Value, me, o =>
            {
                if (!IsOwner(o, me)) return Json(403, Error("_general", "Only the owner can delete the organization."));
                State.Orgs.Remove(o);
                return Json(204);
            });

        // ---- Members ----
        if (R("GET", @"^/organizations/([^/]+)/members$"))
            return WithOrg(m.Groups[1].Value, me, o =>
                Json(200, o.Members.Select(x => MemberDto(x, me)).ToList()));

        if (R("POST", @"^/organizations/([^/]+)/members$"))
            return WithOrg(m.Groups[1].Value, me, o => InviteMember(req, o, me));

        // ---- Categories ----
        if (R("GET", @"^/organizations/([^/]+)/categories$"))
            return WithOrg(m.Groups[1].Value, me, o =>
                Json(200, o.Categories.Select(CategoryDto).ToList()));

        if (R("POST", @"^/organizations/([^/]+)/categories$"))
            return WithOrg(m.Groups[1].Value, me, o => CreateCategory(req, o));

        if (R("DELETE", @"^/categories/([^/]+)$"))
        {
            var id = m.Groups[1].Value;
            var org = State.OrgsFor(me).FirstOrDefault(o => o.Categories.Any(c => c.Id == id));
            if (org is null) return Json(404, Error("_general", "Category not found."));
            org.Categories.RemoveAll(c => c.Id == id);
            return Json(204);
        }

        // ---- Operations ----
        if (R("GET", @"^/organizations/([^/]+)/operations/summary$"))
            return WithOrg(m.Groups[1].Value, me, o => Summary(req, o));

        if (R("GET", @"^/organizations/([^/]+)/operations$"))
            return WithOrg(m.Groups[1].Value, me, o => ListOperations(req, o));

        if (R("POST", @"^/organizations/([^/]+)/operations$"))
            return WithOrg(m.Groups[1].Value, me, o => CreateOperation(req, o));

        if (R("DELETE", @"^/operations/([^/]+)$"))
        {
            var id = m.Groups[1].Value;
            var org = State.OrgsFor(me).FirstOrDefault(o => o.Operations.Any(x => x.Id == id));
            if (org is null) return Json(404, Error("_general", "Operation not found."));
            org.Operations.RemoveAll(x => x.Id == id);
            return Json(204);
        }

        if (R("PATCH", @"^/operations/([^/]+)$"))
        {
            var id = m.Groups[1].Value;
            var op = State.OrgsFor(me).SelectMany(o => o.Operations).FirstOrDefault(x => x.Id == id);
            if (op is null) return Json(404, Error("_general", "Operation not found."));
            var body = Body(req);
            if (TryDecimal(body, "amount", out var amount) && amount > 0) op.Amount = amount;
            if (Str(body, "description") is { } d) op.Description = d;
            if (Str(body, "type") is { } t) op.Type = t.ToLowerInvariant();
            return Json(204);
        }

        return null;
    }

    private ResponseMessage WithOrg(string orgId, string me, Func<FakeOrg, ResponseMessage> action)
    {
        var org = State.OrgsFor(me).FirstOrDefault(o => o.Id == orgId);
        return org is null ? Json(404, Error("_general", "Organization not found.")) : action(org);
    }

    // =====================================================================
    // Handlers
    // =====================================================================

    private ResponseMessage Login(IRequestMessage req)
    {
        var body = Body(req);
        var email = Str(body, "email")?.Trim().ToLowerInvariant();
        var password = Str(body, "password");

        if (email is null || !State.Users.TryGetValue(email, out var expected) || expected != password)
            return Json(401, Error("_general", "Invalid email or password."));

        State.LastLoginEmail = email;
        return Json(200, new
        {
            accessToken = MakeJwt(email),
            refreshToken = Guid.NewGuid().ToString("N"),
            tokenType = "Bearer",
            expiresIn = 3600
        });
    }

    private ResponseMessage Register(IRequestMessage req)
    {
        var body = Body(req);
        var email = Str(body, "email")?.Trim().ToLowerInvariant();
        var password = Str(body, "password");

        if (string.IsNullOrEmpty(email) || !EmailRx.IsMatch(email))
            return Json(400, Error("email", "Invalid email."));
        if (string.IsNullOrEmpty(password) || password.Length < 6)
            return Json(400, Error("password", "Password must be at least 6 characters."));
        if (State.Users.ContainsKey(email))
            return Json(400, Error("email", "User with this email already exists."));

        State.Users[email] = password;
        return Json(200, new { });
    }

    private ResponseMessage ActiveOrganization(string me)
    {
        var orgs = State.OrgsFor(me).ToList();
        var active = State.ActiveOrg.TryGetValue(me, out var id) ? orgs.FirstOrDefault(o => o.Id == id) : null;
        active ??= orgs.FirstOrDefault();
        return active is null
            ? Json(404, Error("_general", "No organizations."))
            : Json(200, OrgDto(active, me));
    }

    private ResponseMessage CreateOrganization(IRequestMessage req, string me)
    {
        var body = Body(req);
        var name = Str(body, "name")?.Trim();
        if (string.IsNullOrEmpty(name))
            return Json(400, Error("name", "Name is required."));

        var org = new FakeOrg { Name = name, Description = Str(body, "description") ?? "" };
        org.Members.Add(new FakeMember { Email = me, Role = "owner" });
        State.Orgs.Add(org);
        State.ActiveOrg[me] = org.Id;
        return Json(201, OrgDto(org, me));
    }

    private ResponseMessage InviteMember(IRequestMessage req, FakeOrg org, string me)
    {
        if (!IsOwner(org, me))
            return Json(403, Error("_general", "Only the owner can invite members."));

        var body = Body(req);
        var email = Str(body, "email")?.Trim().ToLowerInvariant();
        var role = (Str(body, "role") ?? "accountant").ToLowerInvariant();

        if (string.IsNullOrEmpty(email) || !EmailRx.IsMatch(email))
            return Json(400, Error("email", "Invalid email."));
        if (!State.Users.ContainsKey(email))
            return Json(400, Error("email", "User with this email is not registered."));
        if (org.Members.Any(x => x.Email == email))
            return Json(400, Error("email", "User is already a member of this organization."));

        var member = new FakeMember { Email = email, Role = role };
        org.Members.Add(member);
        return Json(200, MemberDto(member, me));
    }

    private ResponseMessage CreateCategory(IRequestMessage req, FakeOrg org)
    {
        var body = Body(req);
        var name = Str(body, "name")?.Trim();
        var color = Str(body, "color") ?? "#2563EB";

        if (string.IsNullOrEmpty(name))
            return Json(400, Error("name", "Name is required."));
        if (name.Length > 100)
            return Json(400, Error("name", "Name must be at most 100 characters."));
        if (org.Categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return Json(400, Error("name", "Category with this name already exists."));

        var category = new FakeCategory { Name = name, Color = color };
        org.Categories.Add(category);
        return Json(201, CategoryDto(category));
    }

    private ResponseMessage ListOperations(IRequestMessage req, FakeOrg org)
    {
        var page = int.TryParse(Query(req, "page"), out var p) && p > 0 ? p : 1;
        var ops = FilterByPeriod(org.Operations, Query(req, "period"))
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(o => OperationDto(o, org))
            .ToList();
        return Json(200, ops);
    }

    private ResponseMessage Summary(IRequestMessage req, FakeOrg org)
    {
        var period = Query(req, "period") ?? "all";
        var ops = FilterByPeriod(org.Operations, period).ToList();
        var income = ops.Where(o => o.Type == "income").Sum(o => o.Amount);
        var expense = ops.Where(o => o.Type == "expense").Sum(o => o.Amount);
        var net = income - expense;
        var margin = income == 0 ? 0m : Math.Round(net / income * 100m, 2);

        return Json(200, new { period, income, expense, netProfit = net, margin });
    }

    private ResponseMessage CreateOperation(IRequestMessage req, FakeOrg org)
    {
        // OperationCreateDto is not visible from the controllers, so field names are read leniently.
        var body = Body(req);

        if (!TryDecimal(body, "amount", out var amount) || amount <= 0)
            return Json(400, Error("amount", "Amount must be greater than zero."));

        var categoryId = Str(body, "categoryId") ?? Str(body, "category_id") ?? Str(body, "category");
        var category = org.Categories.FirstOrDefault(c => c.Id == categoryId);
        if (category is null)
            return Json(400, Error("categoryId", "Category is required."));

        var type = (Str(body, "type") ?? "expense").ToLowerInvariant();
        var createdAt = DateTime.UtcNow;
        foreach (var key in new[] { "date", "createdAt", "occurredAt" })
        {
            if (DateTime.TryParse(Str(body, key), out var parsed))
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
            Description = Str(body, "description") ?? Str(body, "note") ?? "",
            CreatedAt = createdAt
        };
        org.Operations.Add(op);
        return Json(201, OperationDto(op, org));
    }

    // =====================================================================
    // DTO shapes (inferred from views / Map* methods in the MVC controllers)
    // =====================================================================

    private static object OrgDto(FakeOrg o, string me) =>
        new { id = o.Id, name = o.Name, description = o.Description, isOwner = IsOwner(o, me) };

    private static object MemberDto(FakeMember m, string me) => new
    {
        email = m.Email,
        role = m.Role,
        isOwner = m.Role == "owner",
        isMe = m.Email == me,
        createdAt = m.CreatedAt
    };

    private static object CategoryDto(FakeCategory c) => new { id = c.Id, name = c.Name, color = c.Color };

    private static object OperationDto(FakeOperation o, FakeOrg org) => new
    {
        id = o.Id,
        organizationId = org.Id,
        categoryId = o.CategoryId,
        categoryName = o.CategoryName,
        amount = o.Amount,
        type = o.Type,
        description = o.Description,
        createdAt = o.CreatedAt
    };

    private static bool IsOwner(FakeOrg org, string me) =>
        org.Members.Any(m => m.Email == me && m.Role == "owner");

    private static IEnumerable<FakeOperation> FilterByPeriod(IEnumerable<FakeOperation> ops, string? period)
    {
        var from = period?.ToLowerInvariant() switch
        {
            "week" => DateTime.UtcNow.AddDays(-7),
            "month" => DateTime.UtcNow.AddDays(-30),
            "year" => DateTime.UtcNow.AddDays(-365),
            _ => DateTime.MinValue
        };
        return ops.Where(o => o.CreatedAt >= from);
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private string? CurrentEmail(IRequestMessage req)
    {
        if (req.Headers != null
            && req.Headers.TryGetValue("Authorization", out var values)
            && values.FirstOrDefault() is { } header
            && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var email = EmailFromJwt(header["Bearer ".Length..].Trim());
            if (email is not null && State.Users.ContainsKey(email)) return email;
        }

        // Fallback: how the MVC app attaches the token to "FinanceOnlineApi" is not visible
        // from the controllers, so assume the most recently logged-in user.
        return State.LastLoginEmail;
    }

    private static string MakeJwt(string email)
    {
        static string B64(object o) => Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(o))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var exp = DateTimeOffset.UtcNow.AddHours(8).ToUnixTimeSeconds();
        return $"{B64(new { alg = "HS256", typ = "JWT" })}.{B64(new { sub = email, email, exp })}.fake-signature";
    }

    private static string? EmailFromJwt(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2) return null;
        try
        {
            var p = parts[1].Replace('-', '+').Replace('_', '/');
            p = p.PadRight(p.Length + (4 - p.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Convert.FromBase64String(p));
            return doc.RootElement.TryGetProperty("email", out var e) ? e.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    private static JsonElement Body(IRequestMessage req)
    {
        var text = string.IsNullOrWhiteSpace(req.Body) ? "{}" : req.Body;
        try
        {
            using var doc = JsonDocument.Parse(text);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            using var doc = JsonDocument.Parse("{}");
            return doc.RootElement.Clone();
        }
    }

    private static JsonElement? Prop(JsonElement obj, string name)
    {
        if (obj.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in obj.EnumerateObject())
            if (p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return p.Value;
        return null;
    }

    private static string? Str(JsonElement obj, string name)
    {
        var p = Prop(obj, name);
        if (p is null) return null;
        return p.Value.ValueKind switch
        {
            JsonValueKind.String => p.Value.GetString(),
            JsonValueKind.Number => p.Value.GetRawText(),
            _ => null
        };
    }

    private static bool TryDecimal(JsonElement obj, string name, out decimal value)
    {
        value = 0;
        var p = Prop(obj, name);
        if (p is null) return false;
        return p.Value.ValueKind switch
        {
            JsonValueKind.Number => p.Value.TryGetDecimal(out value),
            JsonValueKind.String => decimal.TryParse(p.Value.GetString(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out value),
            _ => false
        };
    }

    private static string? Query(IRequestMessage req, string key) =>
        req.Query != null && req.Query.TryGetValue(key, out var v) ? v.FirstOrDefault() : null;

    private static object Error(string field, string message) =>
        new { errors = new Dictionary<string, string[]> { [field] = new[] { message } } };

    private static ResponseMessage Json(int status, object? body = null)
    {
        var msg = new ResponseMessage { StatusCode = status };

        if (body is not null)
        {
            // Content-Type only when there is a body: 204 No Content must not carry one.
            msg.Headers = new Dictionary<string, WireMockList<string>>
            {
                ["Content-Type"] = new WireMockList<string>("application/json; charset=utf-8")
            };
            msg.BodyData = new BodyData
            {
                DetectedBodyType = BodyType.String,
                BodyAsString = JsonSerializer.Serialize(body, JsonOpts),
                Encoding = Encoding.UTF8
            };
        }

        return msg;
    }
}

// =========================================================================
// In-memory state
// =========================================================================

public sealed class FakeApiState
{
    public const string SeedEmail = "newuser@example.com";
    public const string SeedPassword = "Password123!";
    public const string SeedInvitableEmail = "member@example.com";

    public Dictionary<string, string> Users { get; private set; } = new();
    public List<FakeOrg> Orgs { get; private set; } = new();
    public Dictionary<string, string> ActiveOrg { get; private set; } = new();
    public List<(string Method, Regex Path, int Status)> Overrides { get; private set; } = new();
    public string? LastLoginEmail { get; set; }

    public FakeApiState() => Reset();

    public IEnumerable<FakeOrg> OrgsFor(string email) =>
        Orgs.Where(o => o.Members.Any(m => m.Email == email));

    public void Reset()
    {
        Users = new Dictionary<string, string>
        {
            [SeedEmail] = SeedPassword,
            [SeedInvitableEmail] = SeedPassword   // registered, but not a member of any org
        };
        Orgs = new List<FakeOrg>();
        ActiveOrg = new Dictionary<string, string>();
        Overrides = new List<(string, Regex, int)>();
        LastLoginEmail = null;

        var org = new FakeOrg { Id = "org-seed-1", Name = "Test Organization", Description = "Seeded for UI tests" };
        org.Members.Add(new FakeMember { Email = SeedEmail, Role = "owner" });

        var salary = new FakeCategory { Id = "cat-salary", Name = "Salary", Color = "#10B981" };
        var food = new FakeCategory { Id = "cat-food", Name = "Food", Color = "#EF4444" };
        var rent = new FakeCategory { Id = "cat-rent", Name = "Rent", Color = "#2563EB" };
        org.Categories.AddRange(new[] { salary, food, rent });

        org.Operations.Add(new FakeOperation { Id = "op-salary", CategoryId = salary.Id, CategoryName = salary.Name, Amount = 5000m, Type = "income", Description = "Salary", CreatedAt = DateTime.UtcNow.AddDays(-2) });
        org.Operations.Add(new FakeOperation { Id = "op-groceries", CategoryId = food.Id, CategoryName = food.Name, Amount = 250m, Type = "expense", Description = "Groceries", CreatedAt = DateTime.UtcNow.AddDays(-1) });
        org.Operations.Add(new FakeOperation { Id = "op-rent", CategoryId = rent.Id, CategoryName = rent.Name, Amount = 1200m, Type = "expense", Description = "Rent", CreatedAt = DateTime.UtcNow.AddDays(-3) });

        Orgs.Add(org);
        ActiveOrg[SeedEmail] = org.Id;
    }
}

public sealed class FakeOrg
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<FakeMember> Members { get; } = new();
    public List<FakeCategory> Categories { get; } = new();
    public List<FakeOperation> Operations { get; } = new();
}

public sealed class FakeMember
{
    public string Email { get; set; } = "";
    public string Role { get; set; } = "accountant";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class FakeCategory
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#2563EB";
}

public sealed class FakeOperation
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string CategoryId { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public decimal Amount { get; set; }
    public string Type { get; set; } = "expense";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}