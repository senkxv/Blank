using Microsoft.EntityFrameworkCore;
using Blank.Models;

namespace Blank.Data
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
            : base(options)
        {
        }

        // DbSet для всех таблиц
        public DbSet<Organization> Организации { get; set; }
        public DbSet<Drivers> Водители { get; set; }
        public DbSet<Transport> Транспорт { get; set; }
        public DbSet<Transport_Type> ТипыТранспорта { get; set; }
        public DbSet<Transport_Mark> МаркиТранспорта { get; set; }
        public DbSet<Goods> Товары { get; set; }
        public DbSet<Document_Type> ТипыДокументов { get; set; }
        public DbSet<Documents> Документы { get; set; }
        public DbSet<Users> Пользователи { get; set; }
        public DbSet<Posts> Должности { get; set; }
        public DbSet<Positions> Позиции { get; set; }
    }
}