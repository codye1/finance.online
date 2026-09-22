using finance.online.mvc.Handlers;
using finance.online.mvc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("FinanceOnlineApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7242");
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
        var currentUserService = context.RequestServices.GetRequiredService<ICurrentUserService>();
        var currentUser = currentUserService.GetUser();

        if (currentUser is null)
        {
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