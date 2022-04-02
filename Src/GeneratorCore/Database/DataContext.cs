using GeneratorCore.Database.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GeneratorCore.Database
{
    public class DataContext : DbContext
    {
        public DbSet<DataEntity> Data { get; set; }

        public DbSet<TextEntity> Text { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            base.OnConfiguring(builder);

            var connectionString = new SqliteConnectionStringBuilder
            {
                Mode = SqliteOpenMode.ReadWriteCreate,
                DataSource = "cards.cdb",
            }.ToString();
            builder.UseSqlite(connectionString);
        }
    }
}
