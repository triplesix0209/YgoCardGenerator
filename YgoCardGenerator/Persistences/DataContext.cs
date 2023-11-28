#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using YgoCardGenerator.Persistences.Entities;

namespace YgoCardGenerator.Persistences
{
    public class DataContext : DbContext
    {
        private string _dataSource;

        public DbSet<Data> Data { get; set; }

        public DbSet<Text> Text { get; set; }

        public DbSet<Setcode> Setcode { get; set; }

        public DataContext(string dataSource)
        {
            _dataSource = dataSource;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            base.OnConfiguring(builder);

            builder.UseSqlite(new SqliteConnectionStringBuilder
            {
                Mode = SqliteOpenMode.ReadWriteCreate,
                DataSource = _dataSource,
            }.ToString());
        }
    }
}
