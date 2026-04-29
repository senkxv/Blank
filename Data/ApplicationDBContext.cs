using Microsoft.EntityFrameworkCore;
using Blank.Models.Tables;
using Blank.Models.Views;

namespace Blank.Data
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MainPage>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("Главная");
            });
        }

        public DbSet<MainPage> Главная { get; set; }
        public DbSet<Organization> Организации { get; set; }
        public DbSet<Drivers> Водители { get; set; }
        public DbSet<Transport> Транспорт { get; set; }
        public DbSet<Goods> Товары { get; set; }
        public DbSet<Document_Type> Типы_Документов { get; set; }
        public DbSet<Documents> Документы { get; set; }
        public DbSet<Users> Пользователи { get; set; }
        public DbSet<Posts> Должности { get; set; }
        public DbSet<Positions> Позиции { get; set; }
        public DbSet<Unloading_Point> Пункт_Разгрузки { get; set; }
        public DbSet<Loading_Point> Пункт_Погрузки { get; set; }
        public DbSet<Transport_Type> Тип_Транспорта { get; set; }
        public DbSet<Transport_Mark> Марка_Транспорта { get; set; }
    }
}