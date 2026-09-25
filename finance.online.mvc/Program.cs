using finance.online.mvc.Handlers;
using finance.online.mvc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<AuthHeaderHandler>();

builder.Services.AddControllersWithViews();

builder.Services
    .AddHttpClient("FinanceOnlineApi", (sp, client) =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["FinanceApi:BaseUrl"] ?? "https://localhost:7242";
        client.BaseAddress = new Uri(baseUrl);
    })
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddScoped<IComponentAssetManager, ComponentAssetManager>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.Use(async (context, next) =>
{
    var path = context.Request.Path;

    var isAuthPage = path.StartsWithSegments("/auth");

    if (!isAuthPage)
    {
        var currentUserService =
            context.RequestServices.GetRequiredService<ICurrentUserService>();

        var currentUser = currentUserService.GetUser();

        if (currentUser is null)
        {
            var isAjaxRequest =
                string.Equals(
                    context.Request.Headers["X-Requested-With"],
                    "XMLHttpRequest",
                    StringComparison.OrdinalIgnoreCase);

            if (isAjaxRequest)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            context.Response.Redirect("/auth");
            return;
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }