using Blank.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("RemoteConnection") ?? "")
);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/UserWorkspace/Error500");
    app.UseHsts();
}

app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;

    if (response.StatusCode == 404)
    {
        response.Redirect("/UserWorkspace/Error404");
    }
    else if (response.StatusCode == 500)
    {
        response.Redirect("/UserWorkspace/Error500");
    }
    else if (response.StatusCode == 403)
    {
        response.Redirect("/UserWorkspace/Error403");
    }
    else if (response.StatusCode == 401)
    {
        response.Redirect("/UserWorkspace/Error401");
    }
    else if (response.StatusCode == 400)
    {
        response.Redirect("/UserWorkspace/Error400");
    }
});

app.UseHttpsRedirection();
app.UseRouting();

// Middleware для установки русской культуры
app.Use(async (context, next) =>
{
    try
    {
        var culture = new CultureInfo("ru-RU");
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
    catch
    {
        // Если русская культура недоступна, используем инвариантную
    }
    await next();
});

app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Registration}/{id?}")
    .WithStaticAssets();

app.Run();