using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace tests.Mocks;

public sealed class MvcAppFactory : WebApplicationFactory<Program>
{
    private readonly string _financeApiBaseUrl;
    private IHost? _kestrelHost;

    public string ServerAddress { get; private set; } = "";

    public MvcAppFactory(string financeApiBaseUrl)
    {
        _financeApiBaseUrl = financeApiBaseUrl;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FinanceApi:BaseUrl"] = _financeApiBaseUrl
            });
        });

        // 1) Звичайний хост на TestServer — саме його чекає внутрішня
        //    механіка WebApplicationFactory (Server/Services/EnsureServer).
        //    Ми його ніколи не використовуємо для реальних HTTP-запитів,
        //    він потрібен лише щоб задовольнити базовий клас.
        var testHost = builder.Build();

        // 2) Окремий, справжній Kestrel-хост на випадковому порту — саме
        //    на нього ходитиме браузер Playwright.
        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseKestrel();
            webHostBuilder.UseUrls("https://127.0.0.1:0");
        });

        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        var addresses = _kestrelHost.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>();

        ServerAddress = addresses!.Addresses.First().Replace("127.0.0.1", "localhost");

        testHost.Start();
        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _kestrelHost?.Dispose();
        }
    }
}