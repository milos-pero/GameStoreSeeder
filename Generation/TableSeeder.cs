using DBDataGenerator.Config;
using DBDataGenerator.Db;
using DBDataGenerator.Schema;
using Microsoft.EntityFrameworkCore;

namespace DBDataGenerator.Generation;

public static class TableSeeder
{
    private const int BatchSize = 500;

    public static async Task<List<int>> SeedAsync(
        ConnectionConfig config,
        TableInfo table,
        int rowCount,
        Dictionary<string, List<int>> idPool)
    {
        var start = DateTime.Now;
        Console.WriteLine($"[{start:HH:mm:ss.fff}] ▶  {table.Name} starting...");

        using var db = new DynamicDbContext(config);
        var generator = new DataGenerator(idPool);

        // skip solo auto-increment PKs; keep columns that are PK+FK (composite keys)
        var insertCols = table.Columns
            .Where(c => !c.IsPrimaryKey || c.IsForeignKey)
            .ToList();

        string quotedTable = QuoteIdentifier(table.Name, config.DbType);
        string colList = string.Join(", ",
            insertCols.Select(c => QuoteIdentifier(c.Name, config.DbType)));

        string verb = config.DbType == DatabaseType.MySQL
            ? "INSERT IGNORE INTO"
            : "INSERT INTO";

        int inserted = 0;

        for (int batchStart = 0; batchStart < rowCount; batchStart += BatchSize)
        {
            int batchEnd = Math.Min(batchStart + BatchSize, rowCount);

            var rowPlaceholders = new List<string>();
            var paramValues = new List<object>();
            int paramIndex = 0;

            for (int i = batchStart; i < batchEnd; i++)
            {
                var placeholders = new List<string>();
                foreach (var col in insertCols)
                {
                    var value = generator.GenerateValue(col, i);
                    paramValues.Add(value!);
                    placeholders.Add($"{{{paramIndex++}}}");
                }
                rowPlaceholders.Add($"({string.Join(", ", placeholders)})");
            }

            var sql = $"{verb} {quotedTable} ({colList}) VALUES {string.Join(", ", rowPlaceholders)}";

            if (config.DbType == DatabaseType.PostgreSQL)
                sql += " ON CONFLICT DO NOTHING";

            try
            {
                // everything goes through EF Core
                inserted += await db.Database.ExecuteSqlRawAsync(sql, paramValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [WARN] batch {batchStart}-{batchEnd} failed for {table.Name}: {ex.Message}");
            }
        }

        var end = DateTime.Now;
        Console.WriteLine($"[{end:HH:mm:ss.fff}] ✔  {table.Name} done — {inserted} rows in {(end - start).TotalSeconds:F2}s");

        // read back IDs through EF Core for FK pool
        var pkCol = table.Columns.FirstOrDefault(c => c.IsPrimaryKey && !c.IsForeignKey);
        if (pkCol != null)
        {
            var ids = await db.Database
                .SqlQueryRaw<int>(
                    $"SELECT {QuoteIdentifier(pkCol.Name, config.DbType)} " +
                    $"FROM {QuoteIdentifier(table.Name, config.DbType)}")
                .ToListAsync();
            return ids;
        }

        return new List<int>();
    }

    private static string QuoteIdentifier(string name, DatabaseType dbType) =>
        dbType == DatabaseType.PostgreSQL ? $"\"{name}\"" : $"`{name}`";
}