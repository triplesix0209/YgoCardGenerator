#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using YgoCardGenerator.Persistences.Entities;

namespace YgoCardGenerator.Persistences
{
    public class DataContext : DbContext
    {
        public DbSet<Data> Data { get; set; }

        public DbSet<Text> Text { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            base.OnConfiguring(builder);

            builder.UseSqlite(new SqliteConnectionStringBuilder
            {
                Mode = SqliteOpenMode.ReadWriteCreate,
                DataSource = "cards.cdb",
            }.ToString());
        }
    }
}
