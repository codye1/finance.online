using HandlebarsDotNet.Helpers.Helpers;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using tests.Mocks.Controllers;
using tests.Mocks.Infrastructure;
using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace tests.Mocks;

/// <summary>
/// Fake of the Finance API that the MVC app talks to (server-side, via IHttpClientFactory).
/// Each instance listens on its own dynamically-allocated port, so multiple instances can
/// run side-by-side for parallel test workers without colliding.
///
/// One catch-all WireMock mapping; routing itself is delegated to one controller per
/// resource (Auth, Organizations, Members, Categories, Operations), each owning its own
/// path matching. State is kept in memory (POST /categories then GET /categories returns
/// the new item).
/// </summary>
public sealed class FakeFinanceApi : IDisposable
{
    /// <summary>Base URL this instance is actually listening on (e.g. https://localhost:53214).</summary>
    public string Url { get; }

    private readonly WireMockServer _server;
    private readonly object _lock = new();

    public FakeApiState State { get; } = new();

    private readonly AuthController _auth;
    private readonly OrganizationsController _organizations;
    private readonly MembersController _members;
    private readonly CategoriesController _categories;
    private readonly OperationsController _operations;

    /// <param name="port">Pass 0 (default) to bind to a free OS-assigned port — required for
    /// running multiple instances in parallel. Pass an explicit port only for single-instance,
    /// sequential runs that rely on a fixed address.</param>
    public FakeFinanceApi(int port = 0)
    {
        _auth = new AuthController(State);
        _organizations = new OrganizationsController(State);
        _members = new MembersController(State);
        _categories = new CategoriesController(State);
        _operations = new OperationsController(State);

        var actualPort = port == 0 ? GetFreePort() : port;
        Url = $"https://localhost:{actualPort}";

        _server = WireMockServer.Start(new WireMockServerSettings
        {
            Urls = new[] { Url },
            UseSSL = true,
            // Uses the ASP.NET Core dev certificate (dotnet dev-certs https --trust).
            // The cert is bound to the "localhost" subject name, not to a specific port,
            // so it stays valid regardless of which port we bind to.
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

    /// <summary>
    /// Picks a free TCP port by binding a throwaway listener to port 0 and reading back
    /// what the OS assigned. There's a small theoretical race (another process could grab
    /// the port between Stop() and WireMock's own bind), but it's negligible in practice
    /// and standard practice for test infrastructure.
    /// </summary>
    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
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
    // Routing — dispatch only; each controller owns its own path matching.
    // =====================================================================

    private ResponseMessage Handle(IRequestMessage req)
    {
        lock (_lock)
        {
            try
            {
                return Route(req) ?? ApiHelpers.Json(404, ApiHelpers.Error("_general", $"No fake route for {req.Method} {req.Path}"));
            }
            catch (Exception ex)
            {
                return ApiHelpers.Json(500, ApiHelpers.Error("_general", ex.Message));
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
                return ApiHelpers.Json(o.Status, ApiHelpers.Error("_general", "Status forced by test."));
        }

        // ---- Auth (no token required) ----
        var authResponse = _auth.TryHandle(method, path, req);
        if (authResponse is not null) return authResponse;

        var me = CurrentEmail(req);
        if (me is null) return ApiHelpers.Json(401, ApiHelpers.Error("_general", "Unauthorized."));

        return _organizations.TryHandle(method, path, req, me)
            ?? _members.TryHandle(method, path, req, me)
            ?? _categories.TryHandle(method, path, req, me)
            ?? _operations.TryHandle(method, path, req, me);
    }

    private string? CurrentEmail(IRequestMessage req)
    {
        if (req.Headers != null
            && req.Headers.TryGetValue("Authorization", out var values)
            && values.FirstOrDefault() is { } header
            && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var email = ApiHelpers.EmailFromJwt(header["Bearer ".Length..].Trim());
            if (email is not null && State.Users.ContainsKey(email)) return email;
        }

        // Fallback: how the MVC app attaches the token to "FinanceOnlineApi" is not visible
        // from the controllers, so assume the most recently logged-in user.
        return State.LastLoginEmail;
    }
}