using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WireMock;
using WireMock.Types;
using WireMock.Util;
using tests.Mocks.Models;

namespace tests.Mocks.Infrastructure;

/// <summary>
/// Low-level building blocks shared by every fake controller: route matching,
/// request body/query parsing, JSON response building, and the fake JWT.
/// </summary>
public static class ApiHelpers
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Matches a request against a verb + path pattern. Mirrors the old inline
    /// local function so every controller routes the same way.
    /// </summary>
    public static bool Matches(string method, string path, string verb, string pattern, out Match match)
    {
        match = Match.Empty;
        if (!verb.Equals(method, StringComparison.OrdinalIgnoreCase)) return false;
        match = Regex.Match(path, pattern, RegexOptions.IgnoreCase);
        return match.Success;
    }

    public static ResponseMessage WithOrg(FakeApiState state, string orgId, string me, Func<FakeOrg, ResponseMessage> action)
    {
        var org = state.OrgsFor(me).FirstOrDefault(o => o.Id == orgId);
        return org is null ? Json(404, Error("_general", "Organization not found.")) : action(org);
    }

    // ---- JWT (fake, unsigned) ----

    public static string MakeJwt(string email)
    {
        static string B64(object o) => Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(o))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var exp = DateTimeOffset.UtcNow.AddHours(8).ToUnixTimeSeconds();
        return $"{B64(new { alg = "HS256", typ = "JWT" })}.{B64(new { sub = email, email, exp })}.fake-signature";
    }

    public static string? EmailFromJwt(string token)
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

    // ---- Request parsing ----

    public static JsonElement Body(IRequestMessage req)
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

    public static string? Str(JsonElement obj, string name)
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

    public static bool TryDecimal(JsonElement obj, string name, out decimal value)
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

    public static string? Query(IRequestMessage req, string key) =>
        req.Query != null && req.Query.TryGetValue(key, out var v) ? v.FirstOrDefault() : null;

    // ---- Response building ----

    public static object Error(string field, string message) =>
        new { errors = new Dictionary<string, string[]> { [field] = new[] { message } } };

    public static ResponseMessage Json(int status, object? body = null)
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