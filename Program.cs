using Blank.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("LocalConnection") ?? "")
);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var cultureInfo = new System.Globalization.CultureInfo("ru-RU");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

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

app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Registration}/{id?}")
    .WithStaticAssets();

app.Run();