using Blank.Data;
using Blank.Models.Tables;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Добавляем DbContext для MySQL
builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("LocalConnection"))
);

// Добавляем сервисы для работы с сессиями
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Включаем подробные ошибки для разработки
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Добавляем UseSession перед UseAuthorization
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Registration}/{id?}")
    .WithStaticAssets();

app.Run();