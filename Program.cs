using DBDataGenerator.Config;
using DBDataGenerator.Db;
using DBDataGenerator.Generation;
using DBDataGenerator.Graph;
using DBDataGenerator.Schema;

const int RowCount = 10_000;

// connection
var config = ConnectionConfig.MakeConnectionConfig();

Console.WriteLine("\nConnecting to database...");
using var db = new DynamicDbContext(config);
try
{
    await db.Database.CanConnectAsync();
    Console.WriteLine($"Connected to [{config.DbType}] {config.Host}:{config.Port}/{config.Database}\n");
}
catch (Exception ex)
{
    Console.WriteLine($"Connection failed: {ex.Message}");
    return;
}

// schema discovery
Console.WriteLine("Discovering schema...");
var tables = await SchemaDiscovery.DiscoverAsync(db, config.Database, config.DbType);
Console.WriteLine($"Found {tables.Count} tables:\n");

foreach (var table in tables)
{
    Console.WriteLine($"  {table}");
    foreach (var col in table.Columns)
        Console.WriteLine($"      {col}");
    Console.WriteLine();
}

// dependency graph / waves
var waves = DependencyGraph.BuildWaves(tables);

Console.WriteLine($"Execution plan — {waves.Count} waves:\n");
for (int i = 0; i < waves.Count; i++)
{
    Console.WriteLine($"  Wave {i + 1}: {string.Join(", ", waves[i].Select(t => t.Name))}");
}

Console.WriteLine();
Console.Write($"Ready to insert {RowCount:N0} rows per table. Press ENTER to start...");
Console.ReadLine();

// seeding

// idPool holds the inserted IDs for each table so FK columns in later
// waves can reference real existing rows
var idPool = new Dictionary<string, List<int>>();
var totalStart = DateTime.Now;

Console.WriteLine($"\n[{totalStart:HH:mm:ss.fff}] Seeding started\n");

for (int i = 0; i < waves.Count; i++)
{
    var wave = waves[i];
    Console.WriteLine($"── Wave {i + 1}: {string.Join(", ", wave.Select(t => t.Name))} ──");

    var waveStart = DateTime.Now;

    // tables in the same wave run in parallel
    var tasks = wave.Select(async table =>
    {
        var ids = await TableSeeder.SeedAsync(config, table, RowCount, idPool);

        // register returned IDs in the pool so next waves can use them
        lock (idPool)
        {
            idPool[table.Name] = ids;
        }
    });

    await Task.WhenAll(tasks);

    var waveEnd = DateTime.Now;
    Console.WriteLine($"── Wave {i + 1} finished in {(waveEnd - waveStart).TotalSeconds:F2}s\n");
}

// summary
var totalEnd = DateTime.Now;
var totalTime = (totalEnd - totalStart).TotalSeconds;

Console.WriteLine("═══════════════════════════════════════");
Console.WriteLine($"  Database  : [{config.DbType}] {config.Host}:{config.Port}/{config.Database}");
Console.WriteLine($"  Rows/table: {RowCount:N0}");
Console.WriteLine($"  Tables    : {tables.Count}");
Console.WriteLine($"  Waves     : {waves.Count}");
Console.WriteLine($"  Started   : {totalStart:HH:mm:ss.fff}");
Console.WriteLine($"  Finished  : {totalEnd:HH:mm:ss.fff}");
Console.WriteLine($"  Total time: {totalTime:F2}s");
Console.WriteLine("═══════════════════════════════════════");