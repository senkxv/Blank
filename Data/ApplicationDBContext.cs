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

            // Представление (без ключа)
            modelBuilder.Entity<MainPage>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("Главная");
            });

            // ==================== ОСНОВНЫЕ ТАБЛИЦЫ ====================

            modelBuilder.Entity<Documents>(entity =>
            {
                entity.HasKey(e => e.ид_документа);
                entity.Property(e => e.ид_документа).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Positions>(entity =>
            {
                entity.HasKey(e => e.ид_позиции);
                entity.Property(e => e.ид_позиции).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Organization>(entity =>
            {
                entity.HasKey(e => e.ид_организации);
                entity.Property(e => e.ид_организации).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Drivers>(entity =>
            {
                entity.HasKey(e => e.ид_водителя);
                entity.Property(e => e.ид_водителя).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Goods>(entity =>
            {
                entity.HasKey(e => e.ид_товара);
                entity.Property(e => e.ид_товара).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Document_Type>(entity =>
            {
                entity.HasKey(e => e.ид_типа);
                entity.Property(e => e.ид_типа).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Loading_Point>(entity =>
            {
                entity.HasKey(e => e.ид_пункта_погрузки);
                entity.Property(e => e.ид_пункта_погрузки).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Unloading_Point>(entity =>
            {
                entity.HasKey(e => e.ид_пункта_разгрузки);
                entity.Property(e => e.ид_пункта_разгрузки).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Transport>(entity =>
            {
                entity.HasKey(e => e.ид_транспорта);
                entity.Property(e => e.ид_транспорта).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Transport_Mark>(entity =>
            {
                entity.HasKey(e => e.ид_марки);
                entity.Property(e => e.ид_марки).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Transport_Type>(entity =>
            {
                entity.HasKey(e => e.ид_типа_транспорта);
                entity.Property(e => e.ид_типа_транспорта).ValueGeneratedOnAdd();
            });

            // ==================== ОСТАЛЬНЫЕ СУЩНОСТИ ====================

            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasKey(e => e.ид_пользователя);           // предположительное название ключа
                entity.Property(e => e.ид_пользователя).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Posts>(entity =>
            {
                entity.HasKey(e => e.ид_должности);
                entity.Property(e => e.ид_должности).ValueGeneratedOnAdd();
            });

            // Если у вас есть другие сущности — добавьте их сюда аналогично
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