using Blank.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Включаем подробные ошибки для отладки
builder.Environment.EnvironmentName = "Development";

builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("RemoteConnection") ?? "")
);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Для HTTP
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Глобальный отлов ошибок
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync($@"
            <html>
            <body style='font-family:monospace;padding:20px;background:#1a1a1a;color:#fff;'>
                <h1 style='color:#ff4444;'>Ошибка сервера</h1>
                <p><b>Сообщение:</b> {ex.Message}</p>
                <p><b>Внутренняя:</b> {ex.InnerException?.Message}</p>
                <p><b>Тип:</b> {ex.GetType().FullName}</p>
                <pre style='background:#333;padding:10px;overflow-x:auto;'>{ex.StackTrace}</pre>
            </body>
            </html>
        ");
    }
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Authorization}/{id?}");

app.Run();