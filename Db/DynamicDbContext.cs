using DBDataGenerator.Config;
using Microsoft.EntityFrameworkCore;

namespace DBDataGenerator.Db
{
    public class DynamicDbContext : DbContext
    {
        private readonly ConnectionConfig _config;
        public DynamicDbContext(ConnectionConfig config)
        {
            _config = config;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var cs = _config.BuildConnectionString();
            switch (_config.DbType)
            {
                case DatabaseType.MySQL:
                    optionsBuilder.UseMySql(cs, ServerVersion.AutoDetect(cs));
                    break;
                case DatabaseType.PostgreSQL:
                    optionsBuilder.UseNpgsql(cs);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported database type: {_config.DbType}");
            }
        }
    }
}