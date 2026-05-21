using GameStoreSeeder.Config;
using Microsoft.EntityFrameworkCore;

namespace GameStoreSeeder.Db
{
    private readonly ConnectionConfig config;
    public class DynamicDbContext
    {
        public DynamicDbContext(ConnectionConfig config)
        {
            config = config;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var cs = Config.BuildConnectionString();
            switch (Config.DbType)
            {
                case DatabaseType.MySQL:
                    optionsBuilder.UseMySql(cs, ServerVersion.AutoDetect(cs));
                    break;
                case DatabaseType.PostgreSQL:
                    optionsBuilder.UseNpgsql(cs);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported database type: {Config.DbType}");
            }
        }
    }
}