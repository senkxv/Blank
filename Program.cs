using Blank.Data;
using Microsoft.AspNetCore.DataProtection;
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
    options.Cookie.MaxAge = TimeSpan.FromDays(2);
});

var keysPath = Path.Combine(builder.Environment.WebRootPath, "keys");
Directory.CreateDirectory(keysPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("BlankApp");

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

app.UseStatusCodePagesWithReExecute("/UserWorkspace/Error{0}");

app.UseHttpsRedirection();
app.UseRouting();

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