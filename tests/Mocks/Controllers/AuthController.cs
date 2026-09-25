using HandlebarsDotNet.Helpers.Helpers;
using System.Text.RegularExpressions;
using tests.Mocks.Infrastructure;
using WireMock;

namespace tests.Mocks.Controllers;

/// <summary>Handles /auth/login and /auth/register — the only routes that don't require a token.</summary>
public sealed class AuthController
{
    private static readonly Regex EmailRx = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private readonly FakeApiState _state;

    public AuthController(FakeApiState state) => _state = state;

    public ResponseMessage? TryHandle(string method, string path, IRequestMessage req)
    {
        if (ApiHelpers.Matches(method, path, "POST", @"^/auth/login$", out _)) return Login(req);
        if (ApiHelpers.Matches(method, path, "POST", @"^/auth/register$", out _)) return Register(req);
        return null;
    }

    private ResponseMessage Login(IRequestMessage req)
    {
        var body = ApiHelpers.Body(req);
        var email = ApiHelpers.Str(body, "email")?.Trim().ToLowerInvariant();
        var password = ApiHelpers.Str(body, "password");

        if (email is null || !_state.Users.TryGetValue(email, out var expected) || expected != password)
            return ApiHelpers.Json(401, ApiHelpers.Error("_general", "Invalid email or password."));

        _state.LastLoginEmail = email;
        return ApiHelpers.Json(200, new
        {
            accessToken = ApiHelpers.MakeJwt(email),
            refreshToken = Guid.NewGuid().ToString("N"),
            tokenType = "Bearer",
            expiresIn = 3600
        });
    }

    private ResponseMessage Register(IRequestMessage req)
    {
        var body = ApiHelpers.Body(req);
        var email = ApiHelpers.Str(body, "email")?.Trim().ToLowerInvariant();
        var password = ApiHelpers.Str(body, "password");

        if (string.IsNullOrEmpty(email) || !EmailRx.IsMatch(email))
            return ApiHelpers.Json(400, ApiHelpers.Error("email", "Invalid email."));
        if (string.IsNullOrEmpty(password) || password.Length < 6)
            return ApiHelpers.Json(400, ApiHelpers.Error("password", "Password must be at least 6 characters."));
        if (_state.Users.ContainsKey(email))
            return ApiHelpers.Json(400, ApiHelpers.Error("email", "User with this email already exists."));

        _state.Users[email] = password;
        return ApiHelpers.Json(200, new { });
    }
}